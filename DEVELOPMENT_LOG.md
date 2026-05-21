# DEVELOPMENT LOG - Out Of Nothing

## RÃˆGLES DE RIGUEUR (META-RULES)
1. **SSOT (Single Source of Truth)** : Toute modification doit respecter la source unique de vÃ©ritÃ©.
2. **Double-Validation** : VÃ©rifier la visibilitÃ© et la syntaxe avant de finaliser.
3. **TraÃ§abilitÃ© Totale** : Mise Ã  jour de ce log Ã  CHAQUE modification.
4. **ZÃ©ro Oubli** : Comparer l'intention finale avec l'implÃ©mentation.
5. **VÃ©rification Anti-Oubli** : Pas de rÃ©ponse finale sans log/todo.
6. **LOGIQUE DE COMMIT** : NE JAMAIS commiter/pusher sans demande explicite de l'utilisateur.

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

 # #   2 0 2 6 - 0 5 - 2 1   C r a f t i n g   I n t e r a c t i o n   &   P o l i s h   F i x e s 
 -   * * D r a g   i n   C r a f t   M o d e * * :   A l l o w e d   d r a g g i n g   o f   b a l l s   t h a t   a r e   a c t i v e l y   _ i s P r o c e s s i n g   I F   t h e y   b e l o n g   t o   t h e   c u r r e n t   C r a f t i n g M a n a g e r   s e l e c t i o n . 
 -   * * A d d i t i o n a l   P r e v i e w   R e n d e r i n g * * :   I n t r o d u c e d   _ a d d i t i o n a l P r e v i e w O b j e c t   w h i c h   c a n   n o w   i n s t a n t i a t e   a   p r e f a b   p r o v i d e d   i n   t h e   i n s p e c t o r   a n d   r e p l i c a t e   t h e   s c a l i n g   l i f e c y c l e   o f   t h e   m a i n   c r a f t i n g   s h a d o w   p r e v i e w . 
 -   * * D u p l i c a t i o n   R a n d o m   S t a t e * * :   I s o l a t e d   R a n d o m . I n i t S t a t e   d u r i n g   E n e r g y M a n a g e r . O n D r a w G i z m o s   t o   p r o t e c t   t h e   g l o b a l   r a n d o m   s t r e a m ,   a l l o w i n g   c o r r e c t   o r g a n i c   m i t o s i s   d i r e c t i o n   s e p a r a t i o n . 
 -   * * N a N   I n p u t   H a n d l e r s * * :   P a t c h e d   t h e   I n p u t   S y s t e m   M o u s e . c u r r e n t . p o s i t i o n . R e a d V a l u e ( )   y i e l d i n g   N a N   s t a t e s   i n   U n i t y   E d i t o r ,   e f f e c t i v e l y   b l o c k i n g   a l l   m a t r i x   c a l c u l a t i o n   c r a s h e s   o r i g i n a t i n g   f r o m   S c r e e n T o W o r l d P o i n t . 
 -   * * D O T w e e n   P r o t e c t i o n s * * :   E x t r a c t e d   . S e t L o o p s ( - 1 )   o u t   f r o m   S e q u e n c e   . J o i n   e x t e n s i o n s   t o   c o r r e c t   a n   i n f i n i t e   l o o p   c r a s h   i n   G a m e C u r s o r ,   a n d   s u p p l e m e n t e d   C r a f t i n g M a n a g e r   s e l e c t i o n   r o u t i n e s   w i t h   a   D O K i l l ( )   t o   p r e v e n t   a d d i t i v e   D O P u n c h S c a l e   o v e r l a p   w h e n   s p a m   c l i c k i n g .  
 

---

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

