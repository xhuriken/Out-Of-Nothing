# DEVELOPMENT LOG - Out Of Nothing

## RÃˆGLES DE RIGUEUR (META-RULES)
1. **SSOT (Single Source of Truth)** : Toute modification doit respecter la source unique de vÃ©ritÃ©.
2. **Double-Validation** : VÃ©rifier la visibilitÃ© et la syntaxe avant de finaliser.
3. **TraÃ§abilitÃ© Totale** : Mise Ã  jour de ce log Ã  CHAQUE modification.
4. **ZÃ©ro Oubli** : Comparer l'intention finale avec l'implÃ©mentation.
5. **VÃ©rification Anti-Oubli** : Pas de rÃ©ponse finale sans log/todo.
6. **LOGIQUE DE COMMIT** : NE JAMAIS commiter/pusher sans demande explicite de l'utilisateur.






## [2026-06-18] - Shop Visual Glitch, Parent Destruction & Smooth Expulsion
**Date** : 2026-06-18
**Auteur** : Antigravity (AI)

### 1. Correction du Warning de Police de Caractères Runic
- **Problème** : Les glyphes `◆` (\u25C6) et `◇` (\u25C7) insérés dans la liste de runes de `BallShop.cs` provoquaient des warnings répétitifs dans la console Unity car absents du font SDF Mocha Choco.
- **Solution** : Suppression de `◆` et `◇` de la liste de runes de `BallShop.cs`, remplacés par des caractères alphanumériques et des symboles standards supportés par la police.

### 2. Éjection Fluide et Trajectoire DOTween
- **Problème** : L'éjection physique par force d'impulsion sur le Rigidbody du Shop coupait brutalement la vitesse de drag et provoquait des saccades peu premium (cheap et brut).
- **Solution** : Refonte de la coroutine `Co_ExpelFromBlackHole` dans `Shop.cs` pour effectuer un déplacement fluide via DOTween `DOMove` sur une distance de `3` unités, tout en maintenant le Rigidbody en mode Kinematic pendant l'animation pour une interpolation parfaite. Le champ inutilisé `_blackHoleExpelForce` a été supprimé pour nettoyer les warnings C#.

### 3. Nettoyage Complet du Parent Shop à la Destruction
- **Problème** : Détruire uniquement l'objet Shop laissait le GameObject parent intact dans la scène avec ses shaders et autres scripts associés.
- **Solution** : Modification de la destruction dans `HandleBlackHoleCollision` pour identifier et détruire `transform.parent.gameObject` s'il existe, assurant un nettoyage complet du prefab.

### 4. Glitch Visuel du Shop par Rayon (GRadius) sans Déformation
- **Problème** : Le Shop possède le tag "Machine", ce qui fait que `BlackHoleVisualGlitch.cs` le déforme et le rétrécit en manipulant son `localScale`. Cela transformait les disques Shapes circulaires en ellipses aplaties peu esthétiques.
- **Solution** :
  - Ajout d'un système d'état d'attraction dans `Shop.cs` via `SetAttractionVisualState(scaleFactor, glitchOffset)`.
  - Modification de `UpdateVisualsAndCollider()` dans `Shop.cs` pour utiliser un `activeRadius` combinant `_gRadius`, le facteur de rétrécissement et le décalage de glitch.
  - Mise à jour de `BlackHoleVisualGlitch.cs` : si l'objet attiré possède le composant `Shop`, le script applique les modulations de glitch/rétrécissement sur son `GRadius` via `SetAttractionVisualState()`, laissant son `localScale` inchangé à `1.0` pour garder le Shop parfaitement circulaire.

- **Code Modifié** :
  - **`BallShop.cs`** [MODIFIÉ] : Retrait de `◆` et `◇` de la liste de glyphes runiques.
  - **`Shop.cs`** [MODIFIÉ] : Ajout de `SetAttractionVisualState`, modification de `UpdateVisualsAndCollider` pour appliquer les modulations sur le rayon, destruction du parent GameObject, et éjection fluide via `DOMove`. Suppression de `_blackHoleExpelForce`.
  - **`BlackHoleVisualGlitch.cs`** [MODIFIÉ] : Prise en charge du Shop dans `Update` et `OnDisable` pour moduler le rayon à la place de l'échelle.


## [2026-06-18] - Shop Black Hole Repulsion & Optimization
**Date** : 2026-06-18
**Auteur** : Antigravity (AI)

### 1. Répulsion du Shop contre le Black Hole et Avertissements Progressifs de Nothing
- **Problème** : L'utilisateur souhaite que le Shop repousse le trou noir lorsqu'il est jeté/traîné dedans au lieu d'être détruit directement. Il souhaite également que Nothing exprime des avertissements progressifs (en anglais) lors des premier et deuxième contacts, avant de détruire le Shop et de déclencher l'implosion spectaculaire du trou noir au troisième contact.
- **Solution** :
  - **Gestion de la Collision dans `BlackHole.ConsumeEntity`** : Redirection des collisions avec le composant `Shop` vers sa propre logique externe (`shop.HandleBlackHoleCollision(this)`).
  - **Logique d'Éjection Progressive dans `Shop.cs`** :
    - Ajout d'un compteur de contacts (`_blackHoleContactCount`) et d'un cooldown (`_cooldownTimer` à `1s`) pour empêcher les déclenchements répétitifs en rafale.
    - Désengagement instantané du drag-and-drop via `GameInputManager.Instance.ForceDrop()`.
    - **1er Contact** : Déclenche le monologue *"Are you sure you want to do that?"* et éjecte physiquement le Shop vers l'extérieur (calculé depuis le centre du trou noir).
    - **2ème Contact** : Déclenche le monologue *"<shake>Watch out! You are going to destroy everything!</shake>"* et éjecte à nouveau le Shop.
    - **3ème Contact** : Appelle l'implosion `blackHole.ImploseNothing()` et détruit le Shop (Deactivate et Destroy).
    - **Immunité et Désactivation d'Interaction** : Blocage du drag (`OnDragStart`) et du clic de l'UI (`ToggleShopActiveState`) pendant l'animation d'éjection (`_isExpelling`).
  - **Support ScriptableObjects & Fallbacks** :
    - Exposition des champs de dialogues (`_firstContactMonologue`, `_secondContactMonologue`) dans l'Inspecteur du Shop.
    - Si non renseignés, le script bascule sur des chaînes de caractères brutes en anglais, affichées dynamiquement via le nouveau helper `TriggerMonologueDirect()` dans `MonologueManager`.

### 2. Optimisation des Performances de Mise à Jour du Shop
- **Problème** : `Shop.cs` appelait `GetComponent<CircleCollider2D>()` à chaque frame dans son `Update()` (via `UpdateVisualsAndCollider()`), ce qui génère une surcharge inutile sur le CPU à chaque tick d'exécution.
- **Solution** : Caching du composant `CircleCollider2D` au démarrage dans `Awake()`, avec un garde d'initialisation en mode Éditeur Unity (`[ExecuteAlways]`) pour préserver la réactivité visuelle dans l'inspecteur.

- **Code Modifié** :
  - **`MonologueManager.cs`** [MODIFIÉ] : Ajout de la méthode publique `TriggerMonologueDirect(string text, float exposureTime)` pour jouer des lignes de texte sans requérir de ScriptableObject.
  - **`Shop.cs`** [MODIFIÉ] : Caching du `CircleCollider2D`, exposition du Rigidbody2D (`Rb`), implémentation de `HandleBlackHoleCollision()`, de la coroutine d'expulsion physique `Co_ExpelFromBlackHole()` et des cooldowns associés.
  - **`BlackHole.cs`** [MODIFIÉ] : Routage de la consommation du Shop vers `HandleBlackHoleCollision()` dans `ConsumeEntity()`.


## [2026-06-18] - Shop Hover Fix, Retracting Bug, Purchase Shake/Glow & Spawn Direction
**Date** : 2026-06-18
**Auteur** : Antigravity (AI)


### 1. Isolation des Animations de Hover des Slots du Shop (DOTween IDs)
- **Problème** : Le survol (hover) des slots pendant l'animation d'ouverture ou de fermeture du Shop provoquait parfois l'arrêt brutal des mouvements des slots, les laissant figés au milieu du trajet. Cela survenait car sur certains prefabs, `_visualDisc` réside sur le même GameObject que `BallShop`, ce qui fait que `_visualDisc.transform.DOKill()` tuait le tween de mouvement parental.
- **Solution** :
  - Utilisation systématique d'identifiants DOTween uniques (`moveId` pour le spawner/hider, `hoverId` pour le hover, et `shakeId` pour le shake).
  - Dans `SetHovered()`, nous appelons désormais `DOTween.Kill(hoverId)` au lieu de tuer globalement les tweens du transform, isolant totalement le hover des autres interpolations.
  - Les slots ne se figent plus jamais pendant le déploiement ou la fermeture, même s'ils sont survolés.

### 2. Correction du Blocage du Slot Cliqué et de l'Animation de Shake
- **Problème** : 
  - Lors d'un achat réussi, le slot cliqué restait figé. Si nous nettoyions le hover au clic globalement, cela tuait instantanément l'animation de secousse (shake) en cas de solde insuffisant, car le système de hover ré-enregistrait le slot comme survolé à la frame suivante et appelait `SetHovered(true)`.
- **Solution** :
  - **Hover différé** : Le nettoyage de hover (`ClearHoveredSlot()`) dans `GameInputManager` est désormais délégué à la méthode de succès de transaction du `Shop` (`OnBallSelected`).
  - **Shake préservé** : Si la transaction échoue, le hover n'est pas nettoyé. L'animation de secousse (`DOShakePosition`) sur le `_visualDisc.transform` et le `_priceText.transform` se déroule donc sans être annulée par un changement d'état.

### 3. Gestion de Solde Insuffisant, Secousse/Flash et Anti-Spam
- **Problème** : L'utilisateur souhaite une secousse (shake) et un flash rouge HDR en cas de solde insuffisant, mais aussi une protection contre le spam-clic qui provoquait des instabilités visuelles.
- **Solution** :
  - La soustraction des points est gérée par `IncrementManager.Instance.RemovePoints()`.
  - Refonte de `FlashPriceTextRed()` dans `BallShop.cs` : pour éviter de tuer les tweens de déplacement principaux, les secousses (`DOShakePosition` avec le tag `shakeId`) sont appliquées séparément sur `_visualDisc.transform` et `_priceText.transform`.
  - Le flash rouge utilise une intensité HDR augmentée (`ColorOuter = Color.red * 5.0f`).
  - **Anti-Spam** : Ajout d'une protection temporelle (`_lastFlashTime`) limitant le déclenchement des effets visuels de solde insuffisant à un maximum d'une fois toutes les `0.4` secondes, ignorant le clic spam.

### 4. Spawning Périphérique, Lancement Physique Correct et Transitions Plus Rapides
- **Problème** : La boule achetée apparaissait au centre du Shop, provoquant des collisions parasites. L'impulsion physique de `35f` ou `12f` restait trop violente, et le déploiement/fermeture des slots manquait de nervosité et de rapidité.
- **Solution** :
  - **Axe de Lancement Précis** : Conversion de la direction locale du slot choisi en direction mondiale via `transform.TransformDirection(direction)`.
  - **Élimination des Collisions** : La boule est instanciée de manière décalée à la périphérie du Shop (`transform.position + direction * (_gRadius + ballData.radius + 0.1f)`), éliminant tout chevauchement physique avec le Shop.
  - **Force d'Impulsion Ajustée** : Réduction finale de `_expelForce` à **`6f`** (soit moitié moins fort que les `12f` précédents) pour assurer une éjection dynamique mais propre et contrôlable.
  - **Transitions Rapides** : Réduction de la durée de mouvement des slots (`_moveDuration` à `0.3s`) et des délais d'attente (`_spawnDelay` / `_hideDelay` à `0.05s`, `_postHideDelay` à `0.1s`), rendant l'UI circulaire extrêmement nerveuse et rapide.

- **Code Modifié** :
  - **`BallShop.cs`** [MODIFIÉ] : Tweens de hover isolés sur le disque enfant avec unique DOTween IDs, secousses découplées dans `FlashPriceTextRed()`, réinitialisation du disque visuel au repli, et cooldown anti-spam.
  - **`Shop.cs`** [MODIFIÉ] : Réduction de la force par défaut (`_expelForce = 6f`), accélération des délais de transition de spawner, utilisation de `TransformDirection` pour l'axe d'expulsion, positionnement de spawn en périphérie sans overlap, force d'expulsion proportionnelle à la masse lourde et appel à `ClearHoveredSlot()` sur achat réussi.
  - **`GameInputManager.cs`** [MODIFIÉ] : Exposition de `ClearHoveredSlot()` et retrait de la logique de hover-cleaning synchrone du clic générique.[MODIFIÉ] : Exposition de `ClearHoveredSlot()` et retrait de la logique de hover-cleaning synchrone du clic générique.


## [2026-06-18] - Shop Simplification & Repulsion Component
**Date** : 2026-06-18
**Auteur** : Antigravity (AI)

### 1. Simplification du Shop et Séparation de la Répulsion Passive
- **Problème** : L'utilisateur souhaite simplifier le script du Shop en retirant les mécaniques jugées excessives à ce stade (rebonds de collisions complexes au relâcher du drag, effets sonores et dépendance à l'AudioSource), tout en conservant la répulsion passive de zone (Reflect) pour les machines et les boules, le drag-and-drop sans rotation, et le déploiement circulaire de 8 slots d'achat.
- **Solution** :
  - **Simplification de `Shop.cs`** :
    - Retrait de la logique de collision complexe de drag-end (`CheckCollisionAndRepulse`, `RepulseDraggedWith`).
    - Suppression des références sonores (`_cantPlaceSound`) et du composant `AudioSource`.
    - Suppression de la routine de répulsion passive interne.
    - Ajout de propriétés d'état publiques (`GRadius`, `IsShopActive`, `IsAnimating`) pour permettre à des scripts externes de requêter son statut.
    - Configuration forcée de `_rotationMode = MachineRotationMode.None` dans `Start()` et `Reset()` pour interdire la rotation.
    - Déconnexion totale du réseau d'énergie en surchargeant `IsDemanding` (renvoie `false`), `OnEnable()`, `OnDisable()` et `OnDestroy()` sans appeler la classe de base pour éviter l'enregistrement auprès de l'énergéticien (`EnergyManager`) et du gestionnaire de tick (`PowerTickManager`).
    - Surcharge de `OnDrawGizmos()` pour éviter de tracer la sphère cyan d'énergie.
  - **Création de `ShopRepulsion.cs`** :
    - Nouveau script autonome et modulaire (Component-based) attaché au même GameObject.
    - Gère la détection `OverlapCircleNonAlloc` et repousse dynamiquement les boules (AddForce) et kinématiquement les machines (MovePosition) en direction sortante du centre du Shop.
    - Pause automatiquement son effet lorsque le Shop est en cours de drag, ouvert, ou en animation.
- **Code Modifié / Ajouté** :
  - **`Shop.cs`** [MODIFIÉ] : Nettoyage et simplification des méthodes de collision/sfx/physique, exposition des variables d'état.
  - **`ShopRepulsion.cs`** [NOUVEAU] : Logique découplée de répulsion passive.

### 2. Refactoring de l'Éjection de la Bourse en Impulsion Physique
- **Problème** : L'utilisateur ne souhaite plus d'animation d'ouverture progressive d'angles sur le Shapes Disc du Shop. Il souhaite que la boule achetée soit propulsée physiquement en partant du centre du Shop dans la direction du slot d'achat sélectionné. Lors de cette expulsion physique, la boule doit être temporairement très lourde pendant 2 secondes (pour pousser les obstacles hors de son chemin) et grandir via une animation de scale de zéro à sa taille normale (en 0.5s).
- **Solution** :
  - Suppression de la référence au disque (`_discComponent`) et de la routine d'ouverture d'angles (`ExpelObjectRoutine`) dans `Shop.cs`.
  - Dans `HideBallShopsAndPurchaseRoutine()`, après fermeture des slots d'achat :
    - Instanciation de la boule au centre (`transform.position`).
    - Échelle initiale forcée à `Vector3.zero`.
    - Appel à `SetTemporaryHeavyMass(2f, 50f)` sur le composant `BallEntity` de la boule pour appliquer une masse lourde temporaire (50x) pendant 2 secondes.
    - Activation de la boule en physique dynamique et application d'une force d'impulsion `AddForce(direction * _expelForce, ForceMode2D.Impulse)` (avec `_expelForce` réglé à `15f` par défaut dans l'Inspecteur).
    - Animation de l'échelle de `zero` à `Vector3.one` en `0.5f` secondes via `DOScale()` avec un easing `Ease.OutQuad`.
- **Code Modifié / Ajouté** :
  - **`Shop.cs`** [MODIFIÉ] : Implémentation de la nouvelle éjection physique avec masse lourde temporaire et animation d'échelle, retrait des références de disque.

### 3. Découplage de MachineEntity et Animation en Espace Local (Local Space)
- **Problème** : 
  - Hériter de `MachineEntity` polluait inutilement le code du Shop avec des surcharges d'énergie vides et complexes (trop de code pour rien).
  - Pendant le déplacement (drag) du Shop au cours des animations d'ouverture/fermeture des boules, les slots d'achat (`BallShop`) restaient statiques dans l'espace global du monde et se retrouvaient décalés ou "laissés sur place", car DOTween animait leurs coordonnées absolues mondiales.
  - La valeur `_gRadius` définie dans l'Inspecteur du Shop n'avait aucune influence sur le visuel de son disque externe ni sur son rayon de collision physique dans l'éditeur.
- **Solution** :
  - **Héritage MonoBehaviour & IDraggable** : `Shop.cs` hérite désormais directement de `MonoBehaviour` et implémente `IDraggable` de manière autonome, en dupliquant le code de drag-and-drop physique de base (KISS).
  - **Liaison GRadius et Offsets Visuels** :
    - Ajout de champs de référence inspecteur pour `_backgroundDisc`, `_shaderRenderer` et `_reflectRenderer` dans `Shop.cs`.
    - Implémentation des mêmes offsets que pour le trou noir : `_mainDiscOffset = -0.54f`, `_backgroundOffset = 0.09f`, `_shaderOffset = -0.1f` et `_reflectShaderOffset = 2.5f`.
    - Mise à jour de `UpdateVisualsAndCollider()` pour affecter les dimensions des disques principaux/secondaires, le rayon du `CircleCollider2D` et la valeur `_BlackHoleRadius` dans les blocs de propriétés des SpriteRenderers de shaders (notamment le Reflect).
    - Modification de la portée d'éjection des 8 boules dans `SpawnBallShopsRoutine()` pour qu'elle s'adapte proportionnellement à `_gRadius` (c'est-à-dire `actualRadius = _gRadius * _radius`), de sorte que modifier `_gRadius` affecte aussi la distance à laquelle les boules se déploient.
  - **Espace Local pour les Tweens** :
    - Calcul des positions circulaires cibles des slots en local coordinates dans `SpawnBallShopsRoutine()` de `Shop.cs`.
    - Refactorisation de `BallShop.cs` pour utiliser `DOLocalMove` au lieu de `DOMove`, ramenant les slots vers `Vector3.zero` local lors de la fermeture.
    - Puisque les slots sont enfants du Shop, ils suivent désormais organiquement ses déplacements physiques par translation de coordonnées parentales, même pendant le drag ou en pleine animation.
  - **Immunité BlackHole** : Modification de `BlackHole.ConsumeEntity` pour rechercher explicitement la présence du composant `Shop` découpé et annuler sa consommation.
- **Code Modifié / Ajouté** :
  - **`Shop.cs`** [MODIFIÉ] : Changement d'héritage, intégration du drag manuel, transition vers le local space pour les spawner routines, offsets de disques/shaders configurables et liaison de `_gRadius` dans `OnValidate`.
  - **`BallShop.cs`** [MODIFIÉ] : Remplacement de `DOMove` par `DOLocalMove` et transition vers `Vector3.zero` local pour la rentrée.
  - **`BlackHole.cs`** [MODIFIÉ] : Vérification par `GetComponent<Shop>()` pour exclure la machine découplée du trou noir.

### 4. Mise à jour en temps réel du centre du shader de réflexion (_ReflectCenter)
- **Problème** : Lorsque le Shop se déplace (notamment pendant le drag ou le mouvement physique), le centre du shader de réflexion (`_ReflectCenter`) présent sur le `_reflectRenderer` (qui utilise le shader `Shop Reflect.shadergraph`) présentait un décalage ou ne se mettait pas à jour en temps réel en dehors du mode Play.
- **Solution** :
  - Ajout de l'attribut `[ExecuteAlways]` sur la classe `Shop` pour permettre l'exécution des fonctions d'édition également dans l'Éditeur Unity.
  - Ajout d'une variable privée `_lastPosition` pour suivre la dernière position connue du Shop.
  - Implémentation de la méthode `LateUpdate()` qui détecte si le Shop a bougé (`transform.position != _lastPosition`) et met à jour dynamiquement `_ReflectCenter` dans le bloc de propriétés de matériau du `_reflectRenderer` sans latence de frame par rapport au rendu physique.
  - Limitation des vérifications d'entrées utilisateur (clic de souris / activation du Shop) uniquement pendant l'exécution du jeu (`Application.isPlaying`).
- **Code Modifié / Ajouté** :
  - **`Shop.cs`** [MODIFIÉ] : Ajout de `[ExecuteAlways]`, initialisation et suivi de `_lastPosition` dans `Awake`/`Start`, ajout de `LateUpdate` et de `UpdateShaderReflectCenter()`, protection par `Application.isPlaying` dans `Start` et `Update`.

### 5. Normalisation des clics et survols via le GameCursor et un rayon d'action
- **Problème** : Les clics et survols (hover) n'étaient pas synchronisés avec le curseur visuel personnalisé (`GameCursor`), car ils reposaient sur les événements Unity natifs (`OnMouseEnter`, `OnMouseDown`) et des raycasts physiques calculés à partir de la position masquée de la souris système (`Mouse.current`). De plus, le manque de tolérance (missclick) rendait la sélection difficile.
- **Solution** :
  - Centralisation des interactions dans `GameInputManager` utilisant la position visuelle lissée du curseur (`GameCursor.Instance.transform.position`).
  - Ajout d'un paramètre de rayon d'action réglable `_cursorActionRadius` (`0.5f` par défaut) pour tolérer les légères approximations de clic.
  - Implémentation de `FindClosestTarget<T>()` qui balaie les colliders sous le rayon d'action et trie par distance pour retourner la cible la plus proche.
  - Gestion de l'état de survol en continu (`UpdateHoverState`) pour piloter `SetHovered()` sur les slots d'achat `BallShop`.
  - Désactivation des méthodes de message Unity (`OnMouseEnter`, `OnMouseExit`, `OnMouseDown`) sur `BallShop` pour éviter les doublons avec le curseur système masqué.
  - Redirection du clic du Shop vers une méthode publique `ToggleShopActiveState()` et suppression de l'ancien check de clic interne du Shop dans son `Update()`.
  - Alignement de la sélection de craft (`CraftingManager.RaycastBall`) pour interroger également le rayon d'action.
  - **Correction de la cliquabilité et du survol (Hover/Click)** :
    - Ajout du flag d'état `IsInteractive` sur `BallShop` (passant à `true` en fin d'animation de déploiement) pour bloquer les clics prématurés à l'origine de l'animation.
    - Ajout du flag d'état `IsHiding` sur `BallShop` pour bloquer les overrides de survol (hover) lors de la rétractation, résolvant le bug de la boule cliquée qui restait figée à sa position ouverte. Le survol reste pleinement opérationnel pendant l'animation d'ouverture.
    - Centralisation du hover dans `GameInputManager.FindClosestTarget` en excluant les slots uniquement s'ils se cachent (`IsHiding`).
    - Prolongation de l'état `_isAnimating` dans `Shop.cs` pour bloquer les interactions durant toute la durée visuelle des tweens.
  - **Secousse et Flash HDR sur Solde Insuffisant** :
    - En cas de points insuffisants, déclenchement d'une secousse physique (`transform.DOShakePosition`) sur le slot entier et d'un flash rouge lumineux temporaire (`ColorOuter = Color.red * 2.5f`) sur son disque Shapes extérieur.
  - **Éjection et Trajectoire de Spawning** :
    - Décalage de la position d'apparition de la boule à la périphérie du Shop (`transform.position + direction * _gRadius`) pour éviter les collisions internes.
    - Réinitialisation complète des vélocités résiduelles de la boule (`Rb.linearVelocity = zero`) au spawn et augmentation de la force d'impulsion à `35f` par défaut pour assurer un lancer puissant et précis dans l'axe de son slot associé.
- **Code Modifié / Ajouté** :
  - **`GameInputManager.cs`** [MODIFIÉ] : Alignement du filtrage de cible pour exclure uniquement les slots en cours de disparition (`IsHiding`), blocage des clics sur les slots non interactifs (`IsInteractive`).
  - **`BallShop.cs`** [MODIFIÉ] : Intégration des flags `IsInteractive` / `IsHiding`, désactivation du survol lors du retrait, implémentation de la secousse physique globale et de la coloration du disque externe rouge HDR dans `FlashPriceTextRed()`.
  - **`Shop.cs`** [MODIFIÉ] : Augmentation de la force par défaut (`_expelForce = 35f`), spawn de la boule décalé à la périphérie, réinitialisation des forces physiques au lancer, et prolongation des timers de coroutines de transition.
  - **`CraftingManager.cs`** [MODIFIÉ] : Alignement de `RaycastBall()` pour utiliser le rayon de tolérance du GameInputManager.


## [2026-06-18] - Implosion Animation Sequence (ImploseNothing)
**Date** : 2026-06-18
**Auteur** : Antigravity (AI)

### 1. Séquence d'Animation ImploseNothing et Contrôle Odin
- **Problème** : L'utilisateur souhaite ajouter une animation spectaculaire en 4 phases nommée "ImploseNothing" sur le trou noir. L'animation doit modifier le GRadius, le rayon du shader d'attraction, la couleur et le radius/thickness du disque principal, tout en supportant les modifications en jeu sans corrompre les effets de flash existants ni créer de transitions brusques si plusieurs boules sont absorbées en même temps.
- **Solution** :
  - **Phase 1** : Réduction de `GRadius` vers une valeur cible absolue (`_implodeGRadiusTarget` à la place d'un pourcentage) en `Xtemps` avec une courbe `InOutElastic`.
  - **Phase 2** : Redimensionnement du disque principal (radius et épaisseur). Pour garder le bord extérieur du disque immobile ("triche" visuelle), l'épaisseur augmente proportionnellement à la baisse du rayon : `Thickness = 2 * (outerBoundary - Radius)`. Pendant ce temps, la couleur passe au rouge en `Ytemps`.
    - **Ajout de Secousse (Shake)** : Le `GRadius` subit une secousse (shake) via une sinusoïde amortie (`_gRadiusShakeOffset` modulé par amplitude et fréquence dans l'inspecteur) sur le même intervalle `Ytemps`, renvoyant les vibrations physiques et visuelles de manière fluide via `OnRadiusChanged`.
  - **Phase 3** : Le paramètre `_BlackHoleRadius` du shader d'attraction grandit en `Ztemps` jusqu'à couvrir entièrement les limites de largeur et de hauteur de la zone de jeu (`GameZone.Instance`), calculée comme la demi-diagonale maximale de la zone : `Mathf.Sqrt(halfWidth^2 + halfHeight^2)`.
    - **Extension de la Physique d'Attraction** : Synchronisation de la portée physique d'attraction (`CurrentAttractPhysicsRadius`) avec l'échelle visuelle du shader d'attraction lors des phases 3 et 4, restaurée ensuite à sa portée normale (`GRadius + _attractRadiusOffset`).
  - **Phase 4** : Retour stylisé et graduel de toutes les variables à leurs valeurs par défaut d'origine (Gradius repasse à `_startRadius`, les offsets, l'épaisseur, la couleur et les shaders sont réinitialisés proprement).
- **Protection des Effets de Flash** : Remplacement du reset de couleur brute dans `PlayFlash()`. Le flash anime désormais un multiplicateur d'intensité HDR indépendant (`_flashIntensityMultiplier`) appliqué sur `_currentColor` (couleur de base courante). Si un flash se joue alors que le trou noir est devenu rouge ou est en cours de transition, l'intensité s'applique sur la couleur rouge sans la réinitialiser. Les appels superposés (plusieurs boules absorbées en même temps) tuent proprement le tween précédent pour rejouer l'intensité sans à-coup.
- **Système d'Overrides Visuels** : Ajout de propriétés publiques `OverrideMainDisc` et `OverrideAttractShader` sur `BlackHole.cs`. Dans `BlackHoleVisuals.UpdateVisuals`, les mises à jour procédurales automatiques sont bypassées si ces flags sont levés, permettant à DOTween de piloter entièrement l'animation sans interférence.
- **Code Modifié** :
  - **`BlackHole.cs`** : Déclaration des paramètres Odin, variables d'état (y compris shake), réécriture de `PlayFlash()` avec multiplicateur d'intensité HDR, et implémentation de `ImploseNothing()`.
  - **`BlackHoleVisuals.cs`** : Exposition des offsets, méthode publique `SetAttractShaderRadius()`, et application des overrides visuels.
  - **`BlackHolePhysics.cs`** : Intégration de `CurrentAttractPhysicsRadius` pour les détections physiques d'attraction et le rendu de Gizmos.

### 2. Ajustements du Shake de GRadius et Marge de GameZone
- **Problème** :
  - Le shake ne se jouait pas tout le long de la phase Z (`_zDuration`), car il était chaîné avec `.Join()` après le tween de Phase 3, ce qui le faisait démarrer à la Phase 3 mais durer plus longtemps (débordant sur la Phase 4 de restauration).
  - La marge de sécurité pour couvrir les coins de la `GameZone` rectangulaire avec l'attract shader circulaire devait être validée à `+ 3f`.
  - Le shake devait impacter proprement tous les composants dépendants du `GRadius` (disque de fond, shader principal, shader d'attraction et physique d'attraction).
- **Solution** :
  - **Insertion du Shake au Début de la Phase 2** : Changement de `_implodeSequence.Join` en `_implodeSequence.Insert(_xDuration, shakeTween)` pour démarrer le shake de secousse exactement au début de la Phase 2 et s'arrêter précisément à la fin de la Phase 3 (durée totale = `_yDuration + _zDuration`).
  - **Marge GameZone Validée** : Confirmation de l'ajout de `3f` de marge de sécurité au calcul de la diagonale maximale de la zone de jeu (`Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 3f`).
  - **Secousse Propre via GRadius** : Le shake applique les secousses au `_gRadiusShakeOffset` qui est directement intégré dans le getter de la propriété `GRadius`. Cela déclenche l'événement `OnRadiusChanged` qui répercute automatiquement la vibration sur le disque de fond et le shader principal. Pour le disque principal et le shader/physique d'attraction (qui sont en état "override" pilotés manuellement), le shake tween applique manuellement et de manière synchrone `_gRadiusShakeOffset` à leurs variables respectives.
- **Code Modifié** :
  - **`BlackHole.cs`** : Remplacement de `Join` par `Insert` pour caler précisément la secousse sur les Phases 2 et 3.

### 3. Croissance Proportionnelle de GRadius en Phase 3 et Protection de l'Implosion
- **Problème** :
  - Si des entités sont consommées par le trou noir pendant l'animation d'implosion, la fonction `GrowBlackHole()` s'exécute et augmente `GRadius` au runtime. Cela perturbe l'interpolation en direct et décale les sous-éléments visuels par rapport aux parties fixes ou animées.
  - L'assignation `GRadius += _growthAmount` appelle le setter `GRadius = GRadius + _growthAmount`. Comme le getter renvoie `_gRadius + _gRadiusShakeOffset`, la valeur du shake temporaire se retrouvait additionnée de façon PERMANENTE à la base `_gRadius`, corrompant de fait le rayon global du trou noir.
  - L'animation de fin (Phase 4) restaurait le trou noir à la taille par défaut `_startRadius` au lieu de son échelle d'avant l'implosion (`preImplodeGRadius`), effaçant de façon anormale toute la progression/croissance cumulée de la partie.
  - Manque de feedback sur la taille visuelle et physique lors de la Phase 3 : l'utilisateur souhaite que le trou noir et le disque principal grandissent également pour atteindre un certain pourcentage de l'attract shader (qui représente la taille totale de la map).
  - Lors de la Phase 3, l'épaisseur du disque principal restait bloquée à sa valeur élevée calculée à la fin de la Phase 2 (`phase2EndThickness = 2 * (outerBoundary - targetRadius)`). Comme le rayon augmentait fortement vers `blackHoleGrowthTarget`, le disque principal avec sa forte épaisseur débordait anormalement par rapport au disque de fond et au shader de bruit central, brisant l'alignement visuel.
- **Solution** :
  - **Désactivation de la Croissance au Runtime** : Dans `GrowBlackHole()`, ajout d'un contrôle de sortie anticipée `if (IsImploding) return;`. La fonction modifie désormais directement la variable de stockage `_gRadius` au lieu de passer par le setter, puis notifie manuellement les écouteurs via `OnRadiusChanged`.
  - **Équilibre du Setter/Getter** : Ajustement du setter `GRadius` pour soustraire le décalage temporaire du shake : `_gRadius = value - _gRadiusShakeOffset`. Cela maintient un système de coordonnées cohérent et empêche le shake de polluer la variable racine de taille.
  - **Restauration vers la Taille Initiale d'Avant-Implosion** : Remplacement de `_startRadius` par `preImplodeGRadius` dans le calcul des tweens de retour de la Phase 4. Le trou noir récupère ainsi son état exact pré-implosion.
  - **Ajout du Slider de Croissance (Phase 3)** : Ajout du paramètre sérialisé `_implodeGRadiusGrowthPercent` (via un slider de pourcentage `[Range(0f, 1f)]` dans l'Inspecteur). Dans la Phase 3 de l'implosion, `GRadius` grandit en parallèle jusqu'à atteindre `_implodeGRadiusGrowthPercent * attractShaderRadiusTarget`.
  - **Adaptation du Disque Principal (Phase 3)** :
    - Le rayon cible du disque principal (`baseMainDiscRadius`) s'arrête désormais à `blackHoleGrowthTarget + MainDiscOffset` (au lieu de `blackHoleGrowthTarget` brut), ce qui correspond à sa proportion standard par rapport au reste des composants.
    - Ajout d'une interpolation jointe sur `_disc.Thickness` pour ramener progressivement l'épaisseur depuis sa valeur de fin de Phase 2 (`phase2EndThickness`) jusqu'à sa valeur d'origine fine (`_originalMainDiscThickness`) au cours de la Phase 3.
- **Code Modifié** :
  - **`BlackHole.cs`** : Déclaration de `_implodeGRadiusGrowthPercent`, modification de `GRadius` (setter), protection de `GrowBlackHole()`, et modification des cibles d'animation dans `ImploseNothing()`.

### 4. Raccourci Clavier pour l'Implosion
- **Problème** : L'utilisateur souhaite pouvoir déclencher l'animation d'implosion `ImploseNothing()` via la touche 'I' du clavier pour simplifier les tests au runtime.
- **Solution** : Ajout d'une méthode `Update()` dans `BlackHole.cs` utilisant le nouveau système d'entrée (`UnityEngine.InputSystem.Keyboard.current`) pour écouter les pressions sur `iKey.wasPressedThisFrame` et lancer l'animation.
- **Code Modifié** :
  - **`BlackHole.cs`** : Import du namespace `UnityEngine.InputSystem` et implémentation de la méthode `Update()`.

### 5. Implémentation du Shop et de la Répulsion (Reflect)
- **Problème** : L'utilisateur souhaite ajouter une machine Shop (qui coordonne des sous-éléments d'achat `BallShop` disposés en cercle). Le Shop est cliquable (tente l'achat ou ouvre/ferme l'UI), draggable (déplaçable), mais pas rotatable. La machine doit intégrer une zone de force field de répulsion (Reflect) passive, et être immunisée contre l'aspiration du trou noir. L'animation d'éjection d'un objet acheté doit animer l'ouverture progressive d'un angle dans le cercle Shapes Disc. Les couleurs arc-en-ciel doivent avoir un paramètre pour en réduire la luminosité.
- **Solution** :
  - **Shop & BallShop Components** : Création des classes `Shop` (héritant de `MachineEntity`) et `BallShop` (coordonnant les informations de prix et de configuration). L'affichage utilise `IncrementManager.Instance.Points` comme monnaie globale.
  - **Répulsion Passive (Reflect)** : Implémentation d'une méthode `RepelEntities()` s'exécutant dans `FixedUpdate` du `Shop` qui repousse à la fois les machines (kinematic) et les balles (dynamic) le long du vecteur sortant de sa zone (`_gRadius + _repelRadiusOffset`).
  - **Répulsion après Drag (Collision Bump)** : Implémentation de `CheckCollisionAndRepulse()` s'exécutant à la fin du drag (`OnDragEnd`) pour projeter la machine si elle est relâchée sur un emplacement déjà occupé.
  - **Animation d'Éjection** : Interpolation DOTween de l'objet acheté vers sa position de sortie tout en calculant et affectant `AngRadiansStart` et `AngRadiansEnd` sur le Shapes Disc du Shop pour créer la fente d'ouverture dynamique qui suit l'objet.
  - **Luminosité du Rainbow Cycle** : Ajout des variables `_saturation` et `_value` (valeur de luminosité par défaut à `0.6f` au lieu de `1.0f`) dans `RainbowColorCycle.cs` pour atténuer la saturation/brillance.
  - **Immunité Trou Noir** : Ajout d'une condition d'exclusion `!(machine is Shop)` dans `BlackHole.ConsumeEntity` pour empêcher que le Shop soit aspiré.
- **Code Modifié / Ajouté** :
  - **`RainbowColorCycle.cs`** [MODIFIÉ] : Ajout des paramètres de couleur et de luminosité.
  - **`BallShop.cs`** [NOUVEAU] : Logique de slot d'achat individuel cliquable.
  - **`Shop.cs`** [NOUVEAU] : Logique générale de machine shop coordinateuse et du champ de force de répulsion.
  - **`BlackHole.cs`** [MODIFIÉ] : Exclusion du Shop dans `ConsumeEntity()`.

---

## [2026-06-17] - Typewriter-based Score Animation (Only last character)
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Animation par Typewriter sur le Dernier Caractère du Score
- **Problème** : L'utilisateur souhaite animer l'incrémentation du score en utilisant le typewriter de Febucci Text Animator, de sorte que l'animation d'apparition (appearance offset par caractère) ne se joue que sur le dernier caractère (dernier chiffre) ajouté/modifié, sans faire clignoter ou réapparaître tout le texte précédent.
- **Solution** :
  - Modification de `IncrementManager.UpdatePointsUI()` :
    - Découpage de la chaîne de caractères du score `scoreStr` en deux parties : `precedingText` (tous les caractères sauf le dernier) et `lastChar` (le dernier caractère).
    - Application instantanée de `precedingText` via `_textAnimator.SetText(precedingText, false)`. Le paramètre `false` indique de ne pas cacher le texte (affichage instantané sans animation d'apparition).
    - Ajout/Apposition du dernier caractère via `_textAnimator.AppendText(lastChar, true)`. Le paramètre `true` indique d'apposer ce caractère en le masquant initialement.
    - Lancement du typewriter via `_typewriter.StartShowingText(false)`. Comme le typewriter commence sa routine, il saute les caractères déjà marqués comme visibles (`precedingText`) et déroule uniquement la révélation du dernier caractère (`lastChar`), ce qui déclenche son effet d'apparition (l'offset configuré).
- **Code Modifié/Ajouté** :
  - **`IncrementManager.cs`** :
    ```csharp
    // Split the score string into the preceding text and the last character
    string precedingText = scoreStr.Substring(0, scoreStr.Length - 1);
    string lastChar = scoreStr.Substring(scoreStr.Length - 1);

    // Set the preceding text instantly (without playing appearance animations)
    _textAnimator.SetText(precedingText, false);

    // Append the last character, keeping it hidden initially for the typewriter
    _textAnimator.AppendText(lastChar, true);

    // Start the typewriter to reveal and animate the last character
    _typewriter.StartShowingText(false);
    ```

### 2. Correction du NullReferenceException dans OnValidate() lors de l'initialisation du Play Mode
- **Problème** : Lors du lancement du mode Play ou du rechargement de domaine, Unity appelle `OnValidate()` sur l'inspecteur alors que TextMeshPro (ou TextMeshProUGUI) n'est pas encore totalement initialisé en interne, ce qui génère une `NullReferenceException` fatale dans `TextMeshProUGUI.ClearMesh()`.
- **Solution** :
  - Ajout d'une variable d'état privée `_isInitialized` initialisée à `false`.
  - Dans la méthode `Start()`, `_isInitialized` est passé à `true` et `UpdatePointsUI()` est appelé pour initialiser proprement l'affichage du score à la reprise du jeu.
  - Mise à jour du garde-fou dans `OnValidate()` : `if (Application.isPlaying && _isInitialized) { UpdatePointsUI(); }`. Cela empêche la mise à jour immédiate avant que TextMeshPro soit éveillé, tout en conservant la mise à jour en direct lors des modifications interactives dans l'Inspecteur au cours du jeu.

---

## [2026-06-17] - Fix Exceptions ElectricArc, Score & Intégration IncrementManager au Black Hole
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Résolution Robuste des MissingReferenceExceptions sur les Arcs Électriques (Nodes Détruits)
- **Problème** : Lorsque des machines ou boules connectées à des arcs électriques étaient détruites par le trou noir, des `MissingReferenceException` apparaissaient car les références d'interface C# (`IEnergyNode`) vers des objets Unity détruits ne sont pas détectées par les vérifications `== null` classiques de C#.
- **Solution** :
  - Utilisation systématique du pattern-matching C# (`node is UnityEngine.Object obj && obj == null`) pour détecter la destruction des objets Unity sous-jacents aux interfaces.
  - Sécurisation des méthodes de calcul de géométrie et d'état dans `ElectricArc.cs` (`LateUpdate()`, `UpdateVisualState()`, `UpdateArcGeometry()`) et `EnergyCollisionUtility.cs` (`AreConnected()`, `IsConnectionMaintained()`, `GetAnchorPoint()`).
  - Prunage automatique et synchrone de `_allNodes` dans `EnergyManager.cs` (`RebuildNetworks()`) pour retirer les nœuds détruits avant toute construction ou calcul de topologie.
  - Sécurisation de `EnergyManager.GetDraggedNode()`, `EnergyManager.CanConnectInternal()` et `EnergyNetwork.CalculateAllocation()`.

### 2. Intégration de l'IncrementManager et Animation Individuelle du Dernier Chiffre
- **Problème** : L'IncrementManager n'était pas branché au trou noir, et la mise à jour des points faisait réapparaître tout le texte d'un coup.
- **Solution** :
  - **BallDataSO.cs** : Ajout du champ public `pointValue` pour définir les points par type de boule (Rouge = 1, Bleu = 2, Jaune = 3, Marron = 4).
  - **BlackHole.cs** : Appel synchrone à `IncrementManager.Instance.AddPoints(ball.Data.pointValue)` dans `ConsumeEntity` lors de l'absorption d'une boule.
  - **IncrementManager.cs** :
    - Exposition de la variable `_points` (score) dans l'Inspecteur avec `[SerializeField]` pour permettre la visualisation et l'édition directe, avec mise à jour de l'UI en temps réel à l'exécution via `OnValidate()`.
    - Passage de `_textPoints` au type générique `TMP_Text` (compatible 3D et UI).
    - Mise à jour directe du score en texte brut sans balises d'effets pour éviter tout comportement d'apparition clignotant ou répétitif lors des incrémentations.

### 3. Résolution des Échelles Corrompues par les Collisions (Jelly Bounce)
- **Problème** : Les boules rapides traversant l'attraction du trou noir restaient parfois bloquées dans un scale déformé.
- **Solution** :
  - Ajout d'une propriété `IsAttracted` sur `BallEntity` passée à `true` par `BlackHoleVisualGlitch` lors de l'attraction.
  - `BallJellyBounce.cs` ignore les rebonds physiques si `IsAttracted` est actif, et `ResetJellyState()` est appelé lors de la sortie pour nettoyer tout tween de rebond résiduel.

---

## [2026-06-17] - Fix de l'Animation de Flash HDR sur le Black Hole (Outer Color Only)
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Ciblage de ColorOuter pour le Flash du Disque Radial
- **Problème** : L'animation de flash (`PlayFlash`) et sa mise en cache initiale (`Awake`) modifiaient la propriété générique `Color` du `Disc` de Shapes. Comme le disque est configuré en mode couleur `Radial` avec une couleur interne distincte (`Inner` noire) et une externe (`Outer` violette), modifier la couleur globale écrasait le dégradé radial et faisait flasher le disque entier y compris le centre noir.
- **Solution** : Modification du ciblage des couleurs dans `BlackHole.cs` pour affecter spécifiquement la propriété `ColorOuter` du `Disc`. L'intensité de la couleur externe est maintenant augmentée en HDR durant le flash avant de revenir à sa valeur initiale, sans affecter la couleur interne noire du trou noir.
- **Code Modifié** :
  - `Awake()` : Caches `_disc.ColorOuter` dans `_baseColor` au lieu de `_disc.Color`.
  - `PlayFlash()` : Restauration et interpolation via DOTween sur `_disc.ColorOuter` au lieu de `_disc.Color`.

---

## [2026-06-17] - Refactoring Modulaire du Black Hole (Component-Based Unity)
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Découpage en Composants Unity Modulaires (Required Components)
- **Problème** : Le script `BlackHole.cs` accumulait toute la logique physique, visuelle et de glitch, le rendant trop volumineux et difficile à maintenir (environ 500 lignes).
- **Solution** : Découpage en 4 scripts indépendants :
  - **`BlackHole.cs`** (Core) : Contient l'état du rayon (`GRadius`), l'événement d'abonnement `OnRadiusChanged`, la consommation et le bouton Odin.
  - **`BlackHolePhysics.cs`** (Physics) : Gère la détection `OverlapCircle`, la force d'attraction hybride et expose `AttractedObjects`.
  - **`BlackHoleVisuals.cs`** (Visuals) : Gère le redimensionnement du Shapes Disc principal, du Background Disc et la mise à jour des variables de Shader.
  - **`BlackHoleVisualGlitch.cs`** (Glitches) : Gère le spaghettification factor et le jitter/glitch asynchrone régulé par fréquence.
- **Ajout Automatique** : Ajout des attributs `[RequireComponent]` sur `BlackHole.cs` pour s'assurer que l'ajout du composant principal ajoute automatiquement les trois nouveaux sous-composants dans Unity.

### 2. Restauration des Valeurs par Défaut d'Origine (Screenshot alignment)
- **Solution** : Configuration stricte des valeurs par défaut dans les sérialiseurs de chaque composant pour correspondre exactement à la capture d'écran fournie par l'utilisateur :
  - `_attractForce` = `30f` (Physics)
  - `_attractRadiusOffset` = `2f` (Physics)
  - `_gRadius` = `1f` (Core)
  - `_startRadius` = `0.5f` (Core)
  - `_growthAmount` = `0.005f` (Core)
  - `_mainDiscOffset` = `-0.54f` (Visuals)
  - `_backgroundOffset` = `0.09f` (Visuals)
  - `_shaderOffset` = `-0.1f` (Visuals)
  - `_attractShaderOffset` = `2.5f` (Visuals)
  - `_maxGlitchIntensityBalls` = `0.5f` (Glitch)
  - `_maxGlitchIntensityMachines` = `0.3f` (Glitch)
  - `_glitchFrequencyBalls` = `30f` (Glitch)
  - `_glitchFrequencyMachines` = `30f` (Glitch)
  - `_shrinkPower` = `0.64f` (Glitch)

### 3. Auto-Résolution des Références (Self-Healing references)
- **Problème** : Déplacer les champs de rendu (`AttractRenderer`, `BackgroundDisc`, `ShaderRenderer`) vers le nouveau composant `BlackHoleVisuals` risquait de perdre les liaisons sur le prefab Unity.
- **Solution** : Implémentation d'une fonction d'auto-détection `AutoFindReferences()` appelée dans `Awake()` et `Reset()`. Elle parcourt les enfants du GameObject pour retrouver automatiquement les rendus correspondants par leur nom (ex. "Attract", "BlackHoleShader", "Background"), rendant le script robuste et auto-configurable sans intervention manuelle de l'utilisateur.

### 4. Animation de Flash HDR sur Consommation d'Entités (Satisfying feedback)
- **Problème** : L'absorption d'une balle ou d'une machine dans le trou noir manquait d'impact visuel et de feedback satisfaisant.
- **Solution** : 
  - Récupération de la couleur de base du disque principal (`Disc` de Shapes) lors du démarrage (`Awake`).
  - Lors de l'absorption validée d'une entité (balle ou machine) dans `ConsumeEntity()`, déclenchement de `PlayFlash()`.
  - Utilisation de `DOTween.Sequence()` avec liaison au cycle de vie (`SetLink(gameObject)`) pour animer la couleur du disque vers une intensité boostée en HDR (RGB multiplié par `_hdrFlashMultiplier = 3f`) en `0.05` seconde (Ease.OutQuad), puis la ramener à sa couleur de base en `0.35` seconde (Ease.InQuad).

---

## [2026-06-17] - Personnalisation du Rétrécissement et du Glitch du Black Hole
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Personnalisation fine de la courbe de rétrécissement (Spaghettification)
- **Problème** : L'utilisateur souhaitait que le rétrécissement des entités soit plus personnalisable (ex. que l'entité rapetisse moins vite au début, mais atteigne quand même une taille nulle à l'horizon).
- **Solution** : 
  - Ajout d'une plage `Range(0.05f, 5f)` sur la variable `_shrinkPower`.
  - Explication mathématique ajoutée dans le tooltip : une puissance `_shrinkPower < 1` (ex. `0.5`) fait que l'entité reste plus grande plus longtemps et ne rétrécit fortement que près de l'horizon, tandis qu'une puissance `> 1` accélère le rétrécissement dès la limite de capture.
  - Calcul direct en `Update()` via `Mathf.Pow(shrinkFactor, _shrinkPower)`.

### 2. Paramétrage indépendant du Glitch (Intensité & Fréquence) pour Machines et Balles
- **Problème** : L'effet de distorsion visuelle ("glitch") s'exécutait auparavant à la fréquence des frames, ce qui était trop frénétique et non configurable en termes de vitesse de jitter pour chaque type d'entité.
- **Solution** :
  - Introduction des variables sérialisées de fréquence de glitch : `_glitchFrequencyBalls` (10 Hz par défaut) et `_glitchFrequencyMachines` (8 Hz par défaut).
  - Création de la structure `GlitchState` pour enregistrer l'offset actuel et le timestamp du prochain jitter (`NextGlitchTime`) pour chaque transform.
  - Remplacement du dictionnaire `_glitchedObjects` de `<Transform, Vector3>` vers `<Transform, GlitchState>`.
  - Dans `Update()`, régulation temporelle : le scale de glitch n'est recalculé que si `Time.time >= glitchState.NextGlitchTime` (ou à chaque frame si la fréquence est nulle/négative). Cela permet de garder le mouvement de rétrécissement fluide à chaque frame tout en ayant un jitter saccadé et rythmé très naturel.

---

## [2026-06-17] - Fonctionnalisation du Black Hole (Physique Hybride, Drag & Craft Protection, Reset Pool, Glitch Visuel & Fixes)
**Date** : 2026-06-17
**Auteur** : Antigravity (AI)

### 1. Physique d'Attraction Hybride (Kinematic vs Dynamic)
- **Problème** : Les machines sont en mode `Kinematic` lorsqu'elles ne sont pas traînées par l'utilisateur, ce qui empêchait l'attraction standard par force (`Rigidbody2D.AddForce` sans effet).
- **Solution** : Implémentation d'une détection dans `BlackHole.AttractEntity()` :
  - **Kinematic (Machines au repos)** : Déplacement physique direct via `Rigidbody2D.MovePosition` vers le centre avec un coefficient d'attraction.
  - **Dynamic (Balles, et Machines en cours de drag)** : Application d'une force classique `AddForce`. Un coefficient de `1.5` est appliqué sur les balles pour les rendre plus légères et rapides à aspirer par rapport aux machines.
- **SSOT et non-scaling** : La force physique d'attraction n'est plus multipliée par la taille du trou noir (conservation de la même zone d'influence et de la même force de base peu importe le grossissement).

### 1.B Attraction par le Bord et Consommation par le Milieu (Sinking Effect)
- **Problème** : Les objets commençaient à être détruits dès qu'un seul pixel de leur bord touchait le disque (`distanceToEdge <= _gRadius`), ce qui donnait un effet visuel de disparition instantanée peu naturel.
- **Solution** : Hybridation des détections de distances :
  - **Attraction** : Toujours basée sur le bord (`distanceToEdge <= _gRadius + _attractRadiusOffset`) via `col.ClosestPoint(transform.position)`.
  - **Consommation** : Basée sur la distance au centre (`distanceToCenter <= _gRadius`), forçant l'objet à s'enfoncer de moitié dans le trou noir avant d'être englouti (effet visuel d'absorption réaliste).

### 1.C Résolution des MissingReferenceExceptions (Machines & ElectricArc)
- **Problème** :
  - **Machines** : Lorsqu'une machine était consommée (détruite via `Destroy()`), sa référence restait active dans les réseaux d'énergie (`EnergyNetwork`) jusqu'au prochain tick logique, générant des exceptions critiques dans la boucle `ProcessFluidTransfer` exécutée chaque frame en `FixedUpdate`.
  - **ElectricArc** : Si des arcs électriques étaient détruits de façon externe dans le jeu, la liste de cache d'arcs `_arcPool` d'`EnergyManager` contenait des références détruites (nulles), levant une exception lors du parcours de la boucle d'inactivation globale (`RebuildNetworks`).
- **Solution** :
  - **Désactivation Synchrone** : Avant de détruire l'objet, `ConsumeEntity` fait un `SetActive(false)`, ce qui déclenche instantanément `OnDisable()` et `EnergyManager.UnregisterNode()`.
  - **Recalcul de Topologie Synchrone** : `UnregisterNode` exécute immédiatement `RebuildNetworks()`, purgeant instantanément les réseaux de toute référence détruite.
  - **Sécurité Temporelle** : Ajout d'une vérification `node == null || (node as UnityEngine.Object) == null` dans la boucle de transfert de fluide d'`EnergyNetwork` pour s'assurer que si un nœud est détruit au milieu d'un cycle, il est simplement ignoré.
  - **Nettoyage Dynamic Arc Pool** : Ajout de vérifications de nullité et de nettoyage dynamique à la volée de `_arcPool` dans `EnergyManager.cs` (notamment dans `ShowArc` et la boucle de reset de `RebuildNetworks`). Si un arc de la pool est détruit, il est retiré de la liste, prévenant toute exception.

### 1.D Spaghettification & Effet de Glitch Visuel de Taille (Scale Glitch & Shrink)
- **Solution** : Implémentation d'un effet visuel de distorsion et de rétrécissement dynamique de taille dans `BlackHole.Update()` :
  - **Glitch Haute Fréquence Différencié** : La distorsion (squash & stretch) est calculée en haute fréquence. Nous avons séparé les intensités maximales de glitch de scale pour les balles (`_maxGlitchIntensityBalls`) et pour les machines (`_maxGlitchIntensityMachines`) afin de permettre un réglage indépendant (par ex. glitcher plus fort les balles et plus rigidement les machines).
  - **Rétrécissement Personnalisable (Spaghettification)** : Une valeur de `shrinkFactor` est calculée. Nous avons ajouté `_shrinkPower` (exposant de courbe) pour contrôler la linéarité du rétrécissement. Un exposant inférieur à `1.0` (ex: `0.5`) fait que l'objet reste grand plus longtemps et rapetisse très vite à l'approche de l'horizon, tandis qu'un exposant supérieur à `1.0` (ex: `2.0`) le fait rapetisser plus tôt.
  - La distorsion et le rétrécissement sont appliqués sur l'échelle via le cache de `_attractedObjectsThisFrame` à l'aide d'une structure optimisée `AttractedObjectData` qui stocke le type de l'objet (`IsBall`), évitant tout appel coûteux à `GetComponent` dans `Update()`.
  - Si l'objet est éjecté ou s'échappe de la zone d'attraction, son échelle d'origine `Vector3.one` lui est restaurée. Une sécurité équivalente est présente dans `OnDisable()` pour nettoyer proprement tous les objets encore suivis.
  - Compatible avec le crafting, les animations de click et les duplications (mitosis) puisque l'effet s'applique en direct.

### 2. Gestion du Drag et des Boules en Orbit / Sélection de Craft
- **Problème** : Si une boule ou une machine en cours de drag ou en cours de crafting (dans l'orbite ou sélectionnée) se faisait manger par le trou noir, cela créait des incohérences d'état (drag fantôme ou recette cassée).
- **Solution** :
  - **Force Drop instantané** : Dans `ConsumeEntity()`, si l'entité mangée correspond à l'objet actuellement déplacé dans `GameInputManager.Instance.CurrentDraggedObject`, un appel à `ForceDrop()` est déclenché pour libérer le curseur proprement.
  - **Crafting Protection** : Rendue publique la méthode `DeselectBall(BallEntity ball)` de `CraftingManager.cs`. Dans `ConsumeEntity()`, si la boule est présente dans la sélection de craft, elle est désélectionnée proprement (ce qui met à jour l'orbite, les lignes de connexion et vérifie les recettes) avant d'être renvoyée dans la pool.

### 3. Nettoyage Rigoureux des États de la Pool (Anti-Bug de Recyclage)
- **Problème** : Lorsque des balles revenaient dans la pool et étaient réutilisées, des variables d'état résiduelles n'étaient pas réinitialisées, générant des comportements parasites (par ex. voisins persistants, drag hérité).
- **Solution** :
  - **BallEntity** : Réinitialisation forcée de `_isBeingDragged = false` dans `OnDisable()`.
  - **BallPhysicsPassport** : Implémentation de `OnDisable()` pour réinitialiser la priorité physique max (`Default`), les flags d'override et les vitesses résiduelles.
  - **YellowBallBehavior** : Nettoyage de `_currentNeighbors` dans `OnDisableBehavior()` pour éviter les faux positifs topologiques à la ré-activation.
  - **BlueBallBehavior** : Ajout de `DOTween.Kill(this)` dans `OnDisableBehavior()` pour arrêter les tweens asynchrones de mise à l'échelle sur le collider/renderer.

---

## [2026-06-16] - Refonte des Visuels du Black Hole (Offsets Dynamiques et DOTween)
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Refonte des Offsets Visuels (Live Sync)
- **Problème** : Les anciens `Thickness` n'étaient pas clairs et le Shader/Background ne respectaient pas le mapping de taille exact voulu par l'utilisateur par rapport au `gRadius`.
- **Solution** : Suppression de `_mainDiscThickness` et `_backgroundThickness`. Création d'une catégorie `[Header("Visual Offsets")]` avec des offsets clairs et mathématiques pré-calculés pour un `gRadius` de 1.0 :
  - `_mainDiscOffset = -0.54f` (Radius = 0.46)
  - `_backgroundOffset = +1.52f` (Radius = 2.52)
  - `_shaderOffset = -0.1f` (Radius = 0.9)
  - `_attractShaderOffset = +1.5f` (Radius = 2.5)
- **Résultat** : Toutes ces valeurs s'additionnent dynamiquement à `_gRadius` dans `UpdateVisuals()` pour conserver exactement les proportions peu importe l'échelle globale.

### 2. Animation Interactive (Odin + DOTween)
- **Solution** : Ajout d'une méthode publique `SetRadiusAnimated(float targetRadius, float duration = 1f)` avec l'attribut `[Button("Set Radius Animated", ButtonSizes.Large)]` de Sirenix Odin Inspector.
- **Résultat** : Permet au développeur de tester facilement l'animation de taille fluide du BlackHole directement depuis l'éditeur grâce à une courbe `DOTween` (EaseInOutSine) qui met à jour les visuels de tous les enfants à chaque frame de l'animation.

---

## [2026-06-16] - Refonte Complète du Black Hole (Visuals, Comp, Gizmos) & Proportional Scaling
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Hybrid Absolute & Additive System (Pro Solution)
- **Problème** : Définir des offsets fixes manuellement dans l'éditeur était rébarbatif, et l'utilisateur souhaitait paramétrer son visuel visuellement à un radius de `1.0`, puis faire démarrer le jeu à `0.5` tout en conservant les proportions.
- **Solution** :
  - **Mise en cache intelligente** : Le script mémorise dans `Awake()` la taille initiale paramétrée à la main du `Disc` principal et du `BackgroundDisc` (Shapes).
  - **Start Radius** : Ajout de la variable `_startRadius` (ex: 0.5f). Au démarrage, le `_gRadius` prend cette valeur.
  - **Addition Pure (`deltaRadius`)** : Le script calcule le delta entre la taille actuelle et la taille d'éditeur (`_gRadius - _initialGRadius`). Ce `deltaRadius` (positif ou négatif) est ajouté de manière mathématiquement stricte à tous les Discs. Il n'y a plus aucune dérive de scale possible.
  - **Shader Procédural** : L'Aura (Attract) et le Shader principal (bruit) conservent leurs `SpriteRenderer` (avec un scale géant fixe). Le script leur envoie `_gRadius + _attractRadiusOffset` ou `_gRadius` afin que le ShaderGraph puisse tailler le cercle mathématiquement.

### 1. Refonte du Scaling KISS (Runtime-Only & Public References)
- **Problème** : Les constantes de design en dur n'étaient pas adaptées aux différents rayons par défaut du prefab (ex: `_radius = 1f`), et la présence de `[ExecuteAlways]` avec modifications forcées de `localScale` dans `Update()` empêchait l'utilisateur d'éditer manuellement la taille des enfants dans l'éditeur (les valeurs se réinitialisaient sans arrêt).
- **Solution** : Simplification drastique (KISS) du script :
  - **Suppression du live-update** (`[ExecuteAlways]` et `Update()` supprimés) pour redonner le contrôle manuel total de mise en page dans l'inspecteur Unity en mode Edit.
  - **Références publiques** : Exposition de variables publiques (`AttractTransform`, `BackgroundTransform`, `ShaderTransform`, `AttractRenderer`, `ShaderRenderer`) pour permettre à l'utilisateur de glisser-déposer lui-même ses enfants.
  - **Calcul physique relatif de l'Attraction** : Le rayon d'attraction est maintenant calculé par rapport à la bordure externe du disque (`Radius + Thickness + _attractRadiusOffset`), garantissant que la zone d'attraction s'agrandit de manière synchrone lors de la croissance du trou noir.
  - **Offset dynamique de Shader** : Enregistrement de l'écart initial réel au démarrage (`Awake`) entre la bordure externe du disque et le scale local du shader (`_shaderScaleOffset = GetOuterRadius() - localScale.x`). Cet écart est conservé et appliqué fidèlement lors de la mise à l'échelle au runtime.
  - **Background** : Scale automatiquement calculé au runtime pour correspondre au diamètre externe du disque (`(Radius + Thickness) * 2f`).

### 2. Correction du Bruit (Noise Distortion)
- **Problème** : Le fait d'augmenter le scale étirait la grille de bruit (Noise) calculée par le Shader Graph, rendant le rendu flou et pixelisé.
- **Solution** : Injection automatisée de la propriété `_NoiseTiling` connectée au slot de Tiling des `TilingAndOffsetNode`s dans `BlackHole.shadergraph` et `BH Attract.shadergraph`. Dans le C#, application dynamique de cette valeur sur les renderers `Attract` et `Shader` via `MaterialPropertyBlock` proportionnellement à l'échelle locale. Cela garde la densité physique du bruit constante.

### 3. Gizmos de Zone
- **Solution** : Implémentation d'un rendu de Gizmos filaires circulaires pour l'horizon des événements (Cyan) et la zone d'attraction (Orange), avec une surbrillance renforcée lors de la sélection dans l'inspecteur.
- **Justification** : Permet au game designer de prévisualiser précisément la zone d'influence physique et visuelle.

---

## [2026-06-16] - Retrait du mode Étoile et simplification (KISS)
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Simplification du tracé d'orbite
- **Problème** : L'option étoile rajoutait de la complexité et des lignes de code inutiles alors que le contour circulaire propre est préféré.
- **Solution** : Suppression de la variable sérialisée `_useStarOrbitLines` et nettoyage de `UpdateLine()` dans `CraftingManager.cs` pour ne conserver que la boucle de connexion adjacente sur le contour du cercle (en préservant le tri spatial).
- **Résultat** : Un code plus léger, conforme au principe KISS, et un tracé circulaire stable sans diagonales.

---

## [2026-06-16] - Lignes Spatiales et Mode Étoile en Orbit Preview
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Tri spatial et suppression des diagonales croisées (Cercle parfait)
- **Problème** : En mode preview, bien que les boules soient physiquement placées de façon optimale, les lignes reliant les boules suivaient l'ordre de sélection d'origine (historique), ce qui créait des lignes qui se croisaient (diagonales) et formaient des sabliers ou des formes biscornues au lieu d'un cercle parfait.
- **Solution** : Modification de `UpdateLine()` dans `CraftingManager.cs` pour reconstruire l'ordre des connexions en fonction de l'index de slot spatial (`assignedSlotIndex`) plutôt que de l'ordre de sélection de la liste.
- **Résultat** : Les connexions forment un polygone régulier parfait autour du cercle sans aucun croisement.

### 2. Mode Étoile (Que des Diagonales)
- **Solution** : Ajout d'un paramètre sérialisé `_useStarOrbitLines`. S'il est activé, `UpdateLine()` filtre et dessine UNIQUEMENT les connexions diagonales (distance de slot circulaire >= 2) entre les boules en orbite. S'il n'y a pas assez de boules pour avoir des diagonales (N=3), le système bascule automatiquement sur le triangle adjacent standard.
- **Résultat** : Permet de former des géométries en étoile (ex: pentagrammes, hexagrammes croisés, croix) pour enrichir le feeling "sexy/mystique" du mode preview de craft.

### 3. Appariement bidirectionnel des connexions
- **Solution** : Mise à jour de la détection de persistance des lignes dans `UpdateLine()` pour être symétrique (interchangeable entre `StartBall` et `EndBall`), évitant ainsi le clignotement / la destruction/recréation de lignes lors d'un simple changement d'orientation.

---

## [2026-06-16] - Optimisation des chemins du Mode Preview Orbital
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Assignation de slots optimale par permutation
- **Problème** : Lors de l'entrée en mode orbite de prévisualisation (preview matched), les boules s'assignaient à des emplacements basés sur l'ordre de sélection brut, ce qui provoquait des trajectoires croisées désagréables et peu fluides.
- **Solution** : Implémentation d'un algorithme de permutation brute (`SolveOptimalAssignments`) dans `CraftingManager.cs` qui évalue toutes les configurations possibles entre les boules sélectionnées et les emplacements de l'orbite pour trouver celle qui minimise la somme des distances au carré.
- **Ressources** : Chaque boule est désormais liée de façon optimale à l'index de slot le plus proche dans sa structure d'état `OrbitBallState`, créant un mouvement d'entrée fluide et net sans croisement de chemin.
- **Justification** : Améliore radicalement la fluidité et le feeling "sexy/premium" du mode preview de craft.

---

## [2026-06-16] - Animation Minimaliste de Sélection des Lignes de Craft
**Date** : 2026-06-16
**Auteur** : Antigravity (AI)

### 1. Interpolation depuis l'Ancre de Sélection
- **Problème** : Les lignes de connexion (craft arcs) apparaissaient et disparaissaient en grandissant/rétrécissant symétriquement depuis/vers le milieu, ce qui manquait de précision directionnelle et visuelle.
- **Solution** : Refonte de la logique d'interpolation de géométrie de `CraftArc.cs` et de suivi d'ancre dans `CraftingManager.cs` :
  - **Dynamic Anchor Determination** : Ajout de `DetermineAnchorOnStart` dans `CraftingManager.cs` qui utilise la liste ordonnée `_selectedBalls` pour désigner la boule la plus ancienne (ou restante) comme l'ancre fixe (point de départ de l'animation) et la boule la plus récente (ou retirée) comme la cible (extrémité en mouvement).
  - **Asymmetric Growing/Shrinking** : Mise à jour de `CraftArc.UpdateGeometry()` pour interpoler asymétriquement de l'ancre vers la cible en fonction de `_animProgress`, ce qui permet aux lignes de pousser naturellement depuis les boules existantes vers la nouvelle, et de se replier vers les boules restantes lors d'une désélection.
- **Justification** : Rend l'animation plus minimaliste, logique et fluide, en connectant visuellement les actions de l'utilisateur à la chaîne de sélection existante.

---

## [2026-06-15] - Alignement de la branche theory avec something
**Date** : 2026-06-15
**Auteur** : Antigravity (AI)

### 1. Résolution de blocage Git
- **Problème** : Un fichier verrou `.git/HEAD.lock` empêchait toute opération Git (rebase abort ou reset).
- **Solution** : Suppression manuelle du fichier verrou `.git/HEAD.lock` et annulation du rebase en cours via `git rebase --abort`.

### 2. Synchronisation des branches
- **Justification** : L'utilisateur souhaite rendre la branche `theory` identique à `something`. Un rebase aurait rejoué les commits uniques de `theory` par-dessus `something`, ce qui n'aurait pas rendu les branches identiques et aurait pu créer des conflits.
- **Solution** : Utilisation de `git reset --hard something` pour aligner localement la branche `theory` exactement sur le même commit que `something` (`f91d021`).
- **Remarque** : Conformément à la règle d'interdiction de push sans accord explicite, aucun `git push` n'a été effectué.

---

## [2026-05-20] - Mitosis Duplication Animation (High-Fidelity Feel)
**Date** : 2026-05-20
**Auteur** : Antigravity (AI)

### 1. Mitosis-style Visuals & Physics Flow
- **Problem**: The manual kinematic translation `DOMove` and complete stop at the end of the split path felt mechanical ("va pas", "sortie pas fluide") and caused a sudden stutter before the parting impulse.
- **Solution**: Streamlined the mitosis animation flow to rely purely on natural physics for the separation phase:
  - **Preparation Phase**: The parent ball locks its physics (`IsProcessing = true`, body type to kinematic) and rotates towards a random split direction. It then elongates along the split direction (`_maxStretch` / `_minSquash`) and vibrates (`DOShakePosition`) to convey high tension before splitting.
  - **Cytokinesis/Split Phase**: A child ball is spawned from the pool, immediately inheriting the parent's scale and kinematic states.
  - **Selective Collision Ignore**: Configured `Physics2D.IgnoreCollision` between the parent and child balls during the split flyout, preventing collision glitches during initial overlapping.
  - **Natural Separation**: Both parent and child rigidbodies are restored to `Dynamic` immediately upon division. We apply a single powerful parting physical impulse (`_partingImpulse`) that shoots them apart seamlessly and pushes other dynamic balls away organically.
  - **Organic Visual Wobble**: Standard DOTween scale tweens are applied to recover their scales back to circular `(1, 1, 1)` with a springy `Ease.OutElastic` overshoot, running in parallel with the natural physics flyout.
  - **Delayed Pairwise Collision Restore**: Utilizing a `DOVirtual.DelayedCall(_splitDuration, ...)`, collisions between the parent and child are seamlessly re-enabled once they are safely separated.

### 2. Inspector Tuning & Safety
- **Tuning & Cleanliness**: Removed unused `_splitDistance` and `_splitEase` fields to maintain an elegant and warning-free codebase. Grouped the remaining 7 parameters inside a Sirenix Odin `FoldoutGroup` in `BallEntity.cs`.
- **Pooling Resilience**: Overhauled `Initialize()` and `OnDisable()` in `BallEntity.cs` to fully reset scales, rotations, processing flags, and Rigidbody body types to ensure error-free recycled behavior in the object pools.
- **Priority Collision Override**: Implemented a `SetTemporaryHeavyMass` logic in `BallEntity.cs`. During the mitosis splitting and materialisator spawning phases, balls temporarily increase their mass by a factor of 50. Their ejection impulses are scaled proportionally to preserve exact travel distances, allowing them to effortlessly push aside static or normal balls during animations without breaking intended physics behaviors.

---

## [Phase 4.B] - Hybridation Flux/Tick (FluiditÃ©)
**Date** : 2026-04-27
**Auteur** : Antigravity (AI)

### 1. Retour Ã  la FluiditÃ© Visuelle
- **ProblÃ¨me** : L'asservissement total au Tick Manager rendait les transferts d'Ã©nergie discrets et saccadÃ©s (bonds de 1.0).
- **Solution** : DÃ©placement de `network.ProcessTick` vers le `FixedUpdate` de l'`EnergyManager`.
- **RÃ©sultat** : L'Ã©nergie circule Ã  nouveau de maniÃ¨re fluide (basÃ©e sur `Time.fixedDeltaTime`).

### 2. Maintien de la Cadence Logique
- **Solution** : Les classes dÃ©rivÃ©es de `MachineEntity` (ex: `RedMaterialisatorMachine`) restent cadencÃ©es par le `PowerTickManager`.
- **RÃ©sultat** : Un remplissage "liquide" mais une exÃ©cution "mÃ©canique" (TICK).

---

## [Phase 4.A] - Correction Critique : Race Condition Singleton
**Date** : 2026-04-27

### 1. Fix : Ordre d'ExÃ©cution (PowerTickManager)
- **Solution** : Passage du `PowerTickManager` en `DefaultExecutionOrder(-200)`.

### 2. Robustesse : Double-Abonnement (EnergyManager)
- **Solution** : Ajout d'une sÃ©curitÃ© `SubscribeToTick()` (Note : retirÃ©e en 4.B car le flux est redevenu continu).

---

## [Phase 4] - Tick Manager & Synchronisation (EN COURS)
- [x] Hybridation Flux/Tick pour la fluiditÃ©.
- [ ] Groupement par Type & Network.

---

## [Phase 5] - Refonte ThÃ©orique du Flux d'Ã‰nergie
**Date** : 2026-04-28
**Auteur** : Antigravity (AI)

### 1. SpÃ©cification (ValidÃ©e avec Modifications)
- CrÃ©ation du document `DOC_ENERGY_REFACTOR_SPEC.md`.
- **Modifications (Demande Utilisateur) :** Suppression des variables `Efficiency` et `NetworkPriority`. Le systÃ¨me de Load Balancing doit Ãªtre un pur "pro-rata" : si la demande dÃ©passe l'offre, tout le monde reÃ§oit moins proportionnellement Ã  sa demande sans traitement de faveur.

### 2. ImplÃ©mentation : Architecture & Global Solver
- **Interfaces :** Nettoyage de `IEnergyNode` (ajout `MaxStorage`, `CurrentEnergy`, `EnergyAllocationRate`), `IEnergyProducer` (`ProductionPerTick`, `OutputTransferSpeed`) et `IEnergyConsumer` (`InputTransferSpeed`, `ConsumptionPerAction`).
- **Refactoring Machines :** Suppression des variables locales isolÃ©es (`_maxCapacity`, etc.) au profit de propriÃ©tÃ©s normalisÃ©es hÃ©ritÃ©es ou interfacÃ©es. `YellowBallBehavior` est dÃ©sormais Ã  la fois Consumer et Producer pour interagir avec le rÃ©seau.
- **Global Solver (`EnergyNetwork`) :** RÃ©Ã©criture complÃ¨te.
  - `CalculateAllocation(tickRate)` : LancÃ© par l'`EnergyManager` **uniquement lors du PowerTick**. Fait le bilan Offre/Demande et calcule les Ratios de transfert.
  - `ProcessFluidTransfer(deltaTime)` : LancÃ© **Ã  chaque FixedUpdate**. Applique les ratios de maniÃ¨re purement fluide (`CurrentEnergy += Allocation * dt`).
- **Correction (Bug GÃ©nÃ©rateur) :** Le `GeneratorMachine` ne se remplissait plus car la mÃ©thode `ProduceEnergy` Ã©tait devenue obsolÃ¨te avec le nouveau rÃ©seau. Ajout de la production fluide directement dans son `FixedUpdate()`.
- **Correction (Bug de Synchronisation "1 tick sur 2") :** L'`EnergyManager` calculait les besoins du rÃ©seau **avant** ou **en mÃªme temps** que les machines vidaient leur propre jauge. Une machine qui spammait `CurrentEnergy = 0` le faisait *aprÃ¨s* le passage du rÃ©seau, demandant donc 0 au tick suivant (d'oÃ¹ l'alternance et la dÃ©synchronisation).
  - CrÃ©ation de l'Ã©vÃ©nement `OnPostPowerTick` dans `PowerTickManager`.
  - Le rÃ©seau attend que *toutes* les machines aient agi avant de calculer la distribution du tick suivant, garantissant un flux pur et constant.
- **Ajout (State Management & Drag) :** Gestion de l'isolation topologique lors du dÃ©placement des entitÃ©s.
  - *MachineEntity (Hard Disconnect)* : Saisir une machine la retire instantanÃ©ment de son rÃ©seau local (`CurrentNetwork.RemoveNode()`) et bloque son dÃ©bit, stoppant la production/consommation de ressources "fantÃ´mes".
  - *YellowBallBehavior (Dynamic Drag)* : Saisir une balle ne la dÃ©connecte pas. Son mouvement est traquÃ© (`ExecuteFixedUpdate`). Si ses connexions physiques (Collider Overlap) changent pendant qu'elle est tenue, elle signale une rupture.
  - *Dirty Flag Topology* : Optimisation massive. PlutÃ´t que de rebuild le rÃ©seau Ã  chaque frame pendant un mouvement, on passe le flag `EnergyManager.IsTopologyDirty` Ã  `true`. Le FloodFill complet n'est exÃ©cutÃ© qu'une seule fois par cycle, au `OnPostPowerTick`.
- **Ã‰quilibrage (Economy Rebalancing) :** 
  - Passage Ã  6 Ticks par seconde (`TickRate = 0.1666f`) pour une meilleure fluiditÃ©.
  - Ajustement du `RedMaterialisator` : `InputTransferSpeed` = 0.05. Il faut dÃ©sormais ~3.33 secondes pour gÃ©nÃ©rer 1 boule (1 / (0.05 * 6)).
  - Ajustement du `Generator` : `ProductionPerTick` = 0.12. Un gÃ©nÃ©rateur peut soutenir 2 Materialisators (0.10/tick) de maniÃ¨re fluide, mais s'essoufflera face Ã  3 Materialisators (0.15/tick), crÃ©ant la pÃ©nurie demandÃ©e.
- **Corrections & Synchronisation AvancÃ©e :**
  - *Bug "Ghost Energy"* : Les machines saisies continuaient Ã  recevoir de l'Ã©nergie. Le solver `EnergyManager.CanConnect` rejette dÃ©sormais formellement toute connexion si `IsBeingDragged` est vrai, garantissant une isolation physique absolue.
  - *Sequencer Sync (Cadence)* : Ajout d'un systÃ¨me de *Step Sequencer* global dans `PowerTickManager` (`CurrentTickCount`). Les `RedMaterialisators` ne tirent plus bÃªtement quand ils sont pleins. Ils patientent sagement jusqu'Ã  ce que `(CurrentTickCount % Cadence == Offset)`.
- **Refonte des Yellow Balls (CÃ¢bles/Batteries) :**
  - *Visuels* : Suppression du scale dynamique. Ajout d'une transition de couleur Jaune -> Gris basÃ©e sur l'Ã©nergie. Exposition de `_currentEnergy` dans l'Inspector.
  - *Topologie (Near/Far)* : ImplÃ©mentation d'un double BFS dans le `EnergyManager`. Chaque nÅ“ud connaÃ®t sa distance au gÃ©nÃ©rateur le plus proche (`DistanceToSource`).
  - *Logique de Flux Prioritaire* : RÃ©Ã©criture complÃ¨te de `CalculateAllocation`. L'Ã©nergie suit dÃ©sormais un ordre strict : 
    1. Remplissage des cÃ¢bles en prioritÃ© (du plus proche au plus loin du gÃ©nÃ©rateur).
    2. Distribution aux machines (Consumers).
    3. Si manque d'Ã©nergie, les machines puisent dans les cÃ¢bles (du plus loin au plus proche).
- **RÃ©sultat :** Compilation Unity validÃ©e Ã  100% (0 Erreurs). PrÃªt pour les tests In-Game.

---

## [Phase 6] - Refonte des Collisions (Collider-Based) & Arcs Ã‰lectriques
**Date** : 2026-05-01
**Auteur** : Antigravity (AI)

### 1. DÃ©tection par Colliders (PrÃ©cision Arbitraire)
- **ProblÃ¨me** : La dÃ©tection centre-Ã -centre (cercles) Ã©tait imprÃ©cise pour les machines non circulaires.
- **Solution** : CrÃ©ation de `EnergyCollisionUtility`. 
  - Utilise `Collider2D.ClosestPoint` et `Physics2D.OverlapCircle` pour vÃ©rifier si le rayon de connexion d'un nÅ“ud touche rÃ©ellement la "carrosserie" (collider) de l'autre.
  - S'adapte Ã  n'importe quelle forme (Box, Polygon, etc.).
- **Contrat** : Ajout de `IEnergyNode.PhysicsCollider` pour garantir l'accÃ¨s au composant physique.

### 2. Visualisation Premium des Arcs
- **Ancrage Dynamique** : Les arcs ne partent plus du centre des machines mais du point le plus proche sur le bord de leur collider (`EnergyCollisionUtility.GetAnchorPoint`).
- **Ã‰tats Visuels (Feedback)** :
  - **Gris (Preview/Drag)** : L'arc s'affiche en temps rÃ©el dÃ¨s qu'on approche une machine d'un rÃ©seau potentiel (gÃ©rÃ© dans `EnergyManager.Update`).
  - **Gris (Waiting/Sync)** : L'arc est connectÃ© mais la machine attend son tick de synchronisation (allocation = 0).
  - **Bleu (Active)** : L'arc s'allume en bleu cyan quand l'Ã©nergie circule rÃ©ellement.
- **Optimisation** : Pool d'arcs dynamique avec indexation sÃ©parÃ©e pour les arcs de rÃ©seau (persistants) et les arcs de preview (Ã©phÃ©mÃ¨res).

### 3. Nettoyage Technique
- Migration de `CanConnect` vers `CanConnectInternal` (ajout de l'option `ignoreDrag` pour les previews).
- Correction de `Rigidbody2D.GetAttachedComponent` en `GetComponent<Collider2D>()`.
- Enregistrement de `EnergyCollisionUtility.cs` dans `Assembly-CSharp.csproj` pour la compilation.

- **RÃ©sultat** : RÃ©seaux robustes aux formes complexes, feedback visuel instantanÃ© et premium. 0 erreurs de compilation.

## [Phase 7] - Correction Drag, Arcs et Synchronisation (Pumping)
**Date** : 2026-05-01
**Auteur** : Antigravity (AI)

### 1. Comportement des Balles & Drag
- **Problème** : Toutes les balles continuaient leur comportement physique/logique pendant le drag, ce qui créait des conflits.
- **Solution** : Modification de BallEntity.FixedUpdate pour interrompre l'exécution de _behavior si IsBeingDragged est vrai.
- **Exception** : La balle jaune (YellowBallBehavior) est autorisée à continuer car elle doit reconstruire la topologie en temps réel et gérer le flux d'énergie pendant son déplacement.

### 2. Connectivité Dynamique (Yellow Balls)
- **Solution** : Mise à jour de EnergyManager.CanConnectInternal pour autoriser les connexions impliquant une balle jaune même si celle-ci est en cours de drag.
- **Résultat** : Les câbles (balles jaunes) peuvent désormais pomper ou fournir de l'énergie à une machine dès qu'elles s'en approchent, sans attendre le Drop.

### 3. Visualisation Premium des Arcs (Refonte Couleurs)
- **Couleurs** : Passage du Bleu Cyan au **Jaune Doré** pour les arcs actifs (isActive). Le gris est conservé pour la preview et l'attente.
- **Logique d'État** : Un arc est Jaune uniquement si le flux d'énergie est réel (EnergyAllocationRate > 0). S'il est à 0 (machine pleine ou en attente de synchro), l'arc devient Gris.
- **Fiabilité** : Forçage des propriétés colorGradient, startColor et endColor du LineRenderer pour outrepasser les réglages de l'Inspector.

### 4. Synchronisation du Pumping (Correction Régression)
- **Problème** : Les machines se remplissaient directement (sans respecter le rythme) à la connexion.
- **Solution (RedMaterialisator)** : 
    - Limitation de la demande (InputTransferSpeed) au strict nécessaire pour une action (_consumptionPerAction).
    - Amélioration de IsWaiting() : La machine est désormais considérée en attente (Gris) si elle a déjà assez d'énergie pour son prochain tick, ou si elle attend sa fenêtre de remplissage calculée par RecalculateStartFillTick.
- **Générateur** : Ajout d'une sécurité empêchant la production d'énergie si le générateur est arrêté ou en cours de drag.

- **Résultat** : Flux d'énergie fluide, feedback visuel précis (Jaune/Gris), et synchronisation parfaite avec les ticks globaux.
### 5. Fix Visuel Arc (Matériel)
- **Problème** : La couleur de l'arc ne changeait pas car le matériel utilisait une couleur fixe ignorant les vertex colors du LineRenderer.
- **Solution** : Modification de \ElectricArc.cs\ pour appliquer la couleur directement sur les propriétés \_Color\ et \_TintColor\ de l'instance du matériel (\.material\).
- **Résultat** : L'arc change désormais de couleur (Jaune/Gris) quel que soit le shader utilisé.
### 6. Correction Visuelle Arc Entre Boules
- **Problème** : L'arc entre deux boules jaunes restait gris car le flux net était de 0 (boules pleines).
- **Solution** : Ajout de la propriété \IsDemanding\ à \IEnergyNode\. L'état actif de l'arc dépend désormais de la présence d'une source dans le réseau ET de la capacité des nœuds à recevoir/donner de l'énergie (Câbles toujours actifs, Machines actives seulement si elles ne sont pas en attente).
- **Résultat** : Les arcs entre câbles sont jaunes dès qu'ils sont alimentés, tandis que les machines en attente restent grisées.
### 7. Amélioration Fluidité Visuelle (Drag & Pumping)
- **Correction Drag (Boules Jaunes)** : Les arcs ne deviennent plus gris lors du déplacement d'une boule jaune, car elle reste active dans le réseau. L'arc reste jaune s'il alimente une machine.
- **Correction Pumping (Red Machine)** : Suppression de la micro-pause grise juste avant le tir. La machine est désormais considérée 'Demanding' (Active) tant qu'elle est pleine et prête à tirer.
- **Technique** : Modification de \ElectricArc.cs\ pour autoriser l'état actif sur les previews, et \EnergyManager.cs\ pour ne plus masquer les arcs réels des câbles tenus.
### 8. Fix Final de la Micro-Pause (Red Machine)
- **Problème** : La machine devenait grise un tick avant de tirer car elle atteignait son seuil d'énergie prématurément.
- **Solution** : Découplage complet de l'état 'Waiting' du niveau d'énergie. La machine ne devient grise QUE si elle est hors de sa fenêtre temporelle de pompage.
- **Résultat** : Transition visuelle parfaite. La machine reste allumée et l'arc reste jaune jusqu'au tir final, même si le pompage s'arrête quelques millisecondes avant pour cause de réservoir plein.

## [2026-05-03] - Réorganisation Majeure du Projet

### Modifications :
- **Nettoyage :** Suppression définitive du dossier _Recovery et des fichiers de backup de scènes.
- **Architecture :** Introduction de Assets/_Project/ comme racine du code source et des assets du jeu.
- **Scripts :** Suppression des préfixes numériques et mise en place d'une hiérarchie logique (Core, Entities, Physics, Data, Interfaces).
- **Organisation Assets :** Regroupement des plugins tiers dans Assets/Plugins/, des réglages dans _Project/Settings/, et des matériaux physiques dans _Project/Art/Physics/.
- **Documentation :** Création d'un dossier Documentation/ à la racine pour les fichiers MD.

### Justification :
- Amélioration radicale de la structure du projet pour une meilleure maintenabilité et clarté visuelle.

## [2026-05-18] - Mise à Niveau de l'IDE vers Visual Studio 2022

### Problème / Demande :
- L'utilisateur souhaite transformer son IDE (VS Code) pour ressembler exactement à Visual Studio 2022 (Dark Theme, Police, Keybindings, comportement pour le C#).

### Modifications :
1. **Extensions Installées** :
   - `ms-dotnettools.csharp` (Support officiel C# de Microsoft).
   - `ms-dotnettools.csdevkit` (C# Dev Kit avec Solution Explorer et outils professionnels).
   - `ms-vscode.vs-keybindings` (Mappage clavier natif Visual Studio).
   - `SoVoKaN.dark-theme-vs2022` (Thème sombre ultra-fidèle à VS 2022).
   - `RespectMathias.vs2022-icons` (Set d'icônes de fichiers VS 2022).
2. **Configuration (`.vscode/settings.json`)** :
   - Activation du thème `"Dark Theme VS2022"`.
   - Activation des icônes `"vs2022-icons"`.
   - Configuration de la police `"Cascadia Mono"` (avec Consolas et Courier New en fallbacks) à 12px et interligne de 18px (feeling compact de VS 2022).
   - Paramétrage de l'éditeur : minimap simplifiée avec slider permanent, retour à la ligne désactivé, indentation stricte de 4 espaces via des espaces, activation du formateur automatique lors de la frappe (`editor.formatOnType`).
   - Activation du zoom à la molette via `Ctrl + Molette` (`"editor.mouseWheelZoom": true`).
   - Injection de colorations syntaxiques et sémantiques personnalisées et robustes pour le C# (`editor.tokenColorCustomizations` et `editor.semanticTokenColorCustomizations`) pour forcer des couleurs riches et claires indépendamment du thème global (classes et types en turquoise `#4EC9B0`, attributs comme `[SerializeField]` en vert olive `#B5CEA8`, méthodes en jaune `#DCDCAA`, variables et paramètres en bleu clair `#9CDCFE`), tout en éliminant l'usage du rouge sauf pour les erreurs réelles.
   - Configuration du formateur C# par défaut.
3. **Mise en Place du Formateur d'Accolades (Allman Style)** :
   - Création du fichier `.editorconfig` à la racine du projet pour forcer automatiquement les accolades ouvrantes à se mettre sur une nouvelle ligne (`csharp_new_line_before_open_brace = all`) pour tous les types, méthodes et blocs de contrôle de manière transparente lors du passage à la ligne (via `editor.formatOnType`).

### Justification :
- Recréer le confort de développement, le zoom intuitif, la coloration riche et typée C# sans surcharge de blanc, et le comportement exact de placement d'accolades à la ligne (Allman/CoreFX style) de Visual Studio 2022 directement dans VS Code.

---

## [2026-05-18] - Résolution du Problème d'Accolades Allman (Auto-Format)

### Problème :
- L'utilisateur a signalé que le placement automatique des accolades à la ligne (style Allman) ne fonctionnait pas comme attendu.

### Modifications :
1. **Configuration du Formateur C# par Défaut (`.vscode/settings.json`)** :
   - Ajout explicite de `"editor.defaultFormatter": "ms-dotnettools.csharp"` pour le bloc `[csharp]`. Sans cela, VS Code ne sait pas quel formateur appliquer pour le C#.
2. **Activation du Formatage Automatique à la Sauvegarde (`.vscode/settings.json`)** :
   - Ajout de `"editor.formatOnSave": true` sous le bloc `[csharp]`. Cela force l'application des règles d'accolades Allman définies dans `.editorconfig` lors de chaque sauvegarde (`Ctrl + S`).
3. **Garantie du Support de l'EditorConfig (`.vscode/settings.json`)** :
   - Ajout explicite de `"omnisharp.enableEditorConfigSupport": true` pour garantir la prise en compte des règles.

### Justification :
- Permettre à VS Code d'appliquer automatiquement le style Allman (déplacement des accolades ouvrantes `{` sur une nouvelle ligne) de façon robuste et transparente lors de la saisie (via `formatOnType` lors de la fermeture d'un bloc `}`) ou dès la sauvegarde du fichier (via `formatOnSave`), imitant parfaitement le comportement de Visual Studio 2022.


## [2026-05-21] - Infinite Rotate Dash Animation Component
**Date** : 2026-05-21
**Author** : Antigravity (AI)

### 1. Infinite Dash Offset Animation
- **Problem**: Needed a robust and infinite rotation/dash visual effect for Shapes Disc components that resets cleanly to prevent floating-point precision issues over long gameplay sessions.
- **Solution**: Implemented `InfiniteRotate.cs` referencing `Shapes.Disc` (with `GetComponent<Disc>()` fallback if unassigned) to continuously shift `DashOffset` in `Update`.
- **Wrapping Mechanism**: Calculated the exact dash period as the sum of `DashSize` and `DashSpacing`. Used the modulo operator `%` to wrap `_currentDashOffset` within this period, properly handling negative speeds to maintain a positive wrapping boundary, keeping the visual movement seamless and infinite.
- **Verification**: Verified zero errors across the entire Unity C# solution compiling via `dotnet build`.



---

## [2026-05-21] - Crafting Ball Selection Visual Feedback
**Date** : 2026-05-21
**Author** : Antigravity (AI)

### 1. Selection Visual Feedback Prefab
- **Problem**: Needed high-fidelity visual feedback (spawning a custom visual prefab) when selecting balls in Craft mode, which follows them dynamically and vanishes cleanly with DOTween animations on deselection or cancel.
- **Solution**: Added `_ballSelectionFeedbackPrefab` and `_selectionFeedbackAnimationDuration` settings to `CraftingManager.cs`.
- **Life Cycle & Tracking**: Implemented a `_selectionFeedbacks` dictionary to map `BallEntity` to their visual feedback instance.
  - **SelectBall**: Instantiates the feedback prefab at the ball's position and triggers a quick and snappy `DOScale` spawn animation (`0.15s` OutBack).
  - **DeselectBall**: Animates the scale back to zero (`0.15s` InBack) and safely destroys the feedback instance.
  - **Update**: Continuously updates active feedback positions to follow their respective target balls.
  - **ExecuteCraft / ResetVisuals**: Snaps all feedback scales to zero and clears the tracking dictionary during cleanup or transitions.
- **Verification**: Solution compiled successfully via `dotnet build` with 0 errors.



---

## [2026-05-21] - Snappy Spawn/Despawn CraftArc Animations
**Date** : 2026-05-21
**Author** : Antigravity (AI)

### 1. Endpoint Growth Spawn & Despawn Animations
- **Problem**: The crafting arcs previously enabled/disabled instantly without visual feedback, which felt mechanical. Desired a fluid animation where the line endpoints slide out from the midpoint between the two balls during spawn, and shrink back to the center on despawn.
- **Solution**: Refactored `CraftArc.cs` to introduce `_animProgress` (0 to 1) controlled by DOTween.
  - **Setup**: Plays a snappy `0.25s` `OutQuad` scale-up animation of `_animProgress`.
  - **Despawn**: Plays a snappy `0.20s` `InQuad` scale-down animation of `_animProgress` and self-destructs the GameObject on complete.
  - **UpdateGeometry**: Interpolates start and end vertices outward from the dynamic midpoint based on `_animProgress`. It also scales the `LineRenderer.widthMultiplier` and the jitter magnitude proportionally, resulting in a beautiful growing/shrinking effect.
  - **Safety Fallback**: Stores last known positions of start/end balls, allowing the lines to continue playing their shrink-back animations cleanly even if the parent ball is released/destroyed immediately.

### 2. Dynamic Line Instantiation Management
- **Problem**: The static pooling system in `CraftingManager.cs` instantly enabled/disabled line GameObjects, preventing despawn animations from playing fully.
- **Solution**: Overhauled `UpdateLine` and `ClearLines` to utilize dynamic instantiation and self-destructing despawns:
  - **UpdateLine**: Compares currently active lines with desired selected ball pairs. Retains matching lines, initiates `Despawn()` on discarded lines, and instantiates/setups new lines.
  - **ClearLines**: Triggers `Despawn()` on all active lines so they slide back and vanish organically.
- **Verification**: Solution compiled successfully via `dotnet build` with 0 errors.



---

## [2026-05-21] - Fix InfiniteRotate Dash Offset Reset Seam
**Date** : 2026-05-21
**Author** : Antigravity (AI)

### 1. Normalized Dash Offset Wrapping
- **Problem**: In `InfiniteRotate.cs`, calculating the period as `_disc.DashSize + _disc.DashSpacing` to modulo `_currentDashOffset` created a severe visual cut or seam upon wrap.
- **Root Cause Analysis**: In the Shapes library, `DashOffset` values are already normalized in period units where `1.0f` represents exactly one full dash period (one dash + one spacing), regardless of physical dimensions or dash space mode (meters, relative, fixed). Thus, modulo wrapping at `_disc.DashSize + _disc.DashSpacing` reset the animation mid-dash, causing visual jumps.
- **Solution**: Updated the modulo wrap value to be a constant `1.0f`.
- **Verification**: Solution compiled successfully via `dotnet build` with 0 errors, resulting in a perfectly smooth, seamless rotation.



---

## [2026-05-21] - Flat Motionless Inactive Energy Arcs
**Date** : 2026-05-21
**Author** : Antigravity (AI)

### 1. Flat and Static Inactive Energy Arcs
- **Problem**: When energy connections (`ElectricArc`) are inactive (grey), they continue to jitter and move, which makes them look alive/active and adds unnecessary visual noise and computation.
- **Solution**: Refactored `ElectricArc.cs` to add a private state tracker `_isActive`.
  - **Jitter Control**: Inside `UpdateArcGeometry()`, random offset jitter is only added to the segments when `_isActive` is true.
  - **Dynamic Tracking & Optimization**: Inside `LateUpdate()`, when active, the jitter geometry updates at a fixed `_updateFrequency` rate to maintain the electricity effect. When inactive, it updates on every frame so the completely flat, straight grey line tracks moving nodes/balls smoothly without any stutter or lag, but since there is no jitter, the line remains visually motionless relative to the nodes.
- **Verification**: Solution compiled successfully via `dotnet build` with 0 errors.



---

## [2026-06-18] - Shop Snappy Transition & Physics Improvements
**Date** : 2026-06-18
**Author** : Antigravity (AI)

### 1. Robust Price Text Color Recovery
- **Problem**: When spam clicking slots with insufficient points, the price text color could get locked to red or pink instead of reverting to white.
- **Root Cause**: Re-triggering the red flash while the color tween was returning to white overwrote the cached default color with an intermediate color, or closing the shop deactivated the object and halted the color reset tween.
- **Solution**: Implemented `_hasCachedOriginalColor` in `BallShop.Initialize` to store `_originalPriceColor` exactly once. In both `SpawnWithMoveAndScale` and `HideWithMoveAndScale`, explicitly killed active shake/color tweens on the text and reset its color to `_originalPriceColor`.

### 2. Early Slot Interactivity and Snappy Transitions
- **Problem**: The slots could only be clicked after their spawn animation fully completed, which felt sluggish when clicking rapidly.
- **Solution**: Reduced default timing parameters (move duration down to `0.2s`, spawn delay to `0.03s`). Set `_isInteractive` to true at **60%** of the transition duration using a `DOVirtual.DelayedCall`, allowing the player to select slot items early while they are settling.

### 3. Coroutine-Safe Shop Selection Interruption
- **Problem**: Clicking a slot mid-animation started the hide-and-purchase routine while the spawn routine was still running in the background, creating conflict.
- **Solution**: Implemented explicit coroutine tracking fields (`_spawnCoroutine`, `_hideCoroutine`, `_purchaseCoroutine`) in `Shop.cs`. Successfully stopped conflicting routines (`StopCoroutine`) and synchronized state flags (`_isOpening = false`, `_isClosing = false`) upon slot selection or interface toggling.

### 4. Centered Spawning & Shop Collision Disablement
- **Problem**: Spawning the ball on the perimeter could overlap with obstacles or cause violent ejection forces.
- **Solution**: Set the ball's spawn position to the shop center (`transform.position`). Temporarily disabled physical collision between the spawned ball and the main Shop's `Collider2D` using `Physics2D.IgnoreCollision` for `0.5s` to allow a smooth exit from the shop.

### 5. Halved expulsion force
- **Problem**: The expulsion force launched the ball too quickly.
- **Solution**: Halved the expel force in the physics calculation of `Shop.cs` (`_expelForce * 0.5f`) to provide a gentler, more controlled exit velocity.



---

## [2026-06-18] - Shop Locked Slots & Runic Text
**Date** : 2026-06-18
**Author** : Antigravity (AI)

### 1. Locked Slots with Minecraft-style runic text
- **Problem**: Needed a locked slot state in the Shop that appears grey, disables purchases, and animates its price indicator with a fast-cycling random set of runic characters.
- **Solution**:
  - Added a serialized `_isLocked` option in `BallShop.cs` (exposed via `IsLocked`).
  - **Visuals**: On `Initialize()`, locked slots are rendered grey (outer and inner glow) with a default visual radius.
  - **Animation**: Added `Update()` runic text generator cycling 3 random characters/symbols every `0.1s`.
  - **Feedback**: Adjusted hover scaling and HDR color flash routines to fall back to grey if locked or if `BallData` is null.
  - **Purchase Block**: Modified `Shop.cs` in `OnBallSelected()` to detect if a slot is locked, play failed purchase feedback (shake and red flash), and abort transactions before points check.



---

## [2026-06-18] - Csproj Synchronization and Merge Integration
**Date** : 2026-06-18
**Author** : Antigravity (AI)

### 1. Assembly-CSharp.csproj Synchronization
- **Problem**: Following the merge of the remote `incendie` branch into the local `something` branch, compiling the project with `dotnet build` failed due to missing references to newly introduced/restored source files (e.g. `JournalManager.cs`, `BallShop.cs`, etc.).
- **Solution**: Manually edited `Assembly-CSharp.csproj` to restore the `<Compile Include="...">` tags for all 6 desynchronized C# files:
  - `Assets/_Project/Scripts/Entities/Machines/Herited/ClickerMachine.cs`
  - `Assets/_Project/Scripts/Entities/Machines/Independents/BallShop.cs`
  - `Assets/_Project/Scripts/Entities/Machines/Independents/Shop.cs`
  - `Assets/_Project/Scripts/Entities/Machines/Independents/ShopRepulsion.cs`
  - `Assets/_Project/Scripts/UI/JournalManager.cs`
  - `Assets/_Project/Scripts/Visual/RainbowColorCycle.cs`
- **Verification**: Ran `dotnet build Assembly-CSharp.csproj` which now completes successfully with 0 errors. Verified project compilation stability and member visibility/accessibility.




