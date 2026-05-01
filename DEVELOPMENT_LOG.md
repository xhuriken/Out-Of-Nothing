# DEVELOPMENT LOG - Out Of Nothing

## RÈGLES DE RIGUEUR (META-RULES)
1. **SSOT (Single Source of Truth)** : Toute modification doit respecter la source unique de vérité.
2. **Double-Validation** : Vérifier la visibilité et la syntaxe avant de finaliser.
3. **Traçabilité Totale** : Mise à jour de ce log à CHAQUE modification.
4. **Zéro Oubli** : Comparer l'intention finale avec l'implémentation.
5. **Vérification Anti-Oubli** : Pas de réponse finale sans log/todo.
6. **LOGIQUE DE COMMIT** : NE JAMAIS commiter/pusher sans demande explicite de l'utilisateur.

---

## [Phase 4.B] - Hybridation Flux/Tick (Fluidité)
**Date** : 2026-04-27
**Auteur** : Antigravity (AI)

### 1. Retour à la Fluidité Visuelle
- **Problème** : L'asservissement total au Tick Manager rendait les transferts d'énergie discrets et saccadés (bonds de 1.0).
- **Solution** : Déplacement de `network.ProcessTick` vers le `FixedUpdate` de l'`EnergyManager`.
- **Résultat** : L'énergie circule à nouveau de manière fluide (basée sur `Time.fixedDeltaTime`).

### 2. Maintien de la Cadence Logique
- **Solution** : Les classes dérivées de `MachineEntity` (ex: `RedMaterialisatorMachine`) restent cadencées par le `PowerTickManager`.
- **Résultat** : Un remplissage "liquide" mais une exécution "mécanique" (TICK).

---

## [Phase 4.A] - Correction Critique : Race Condition Singleton
**Date** : 2026-04-27

### 1. Fix : Ordre d'Exécution (PowerTickManager)
- **Solution** : Passage du `PowerTickManager` en `DefaultExecutionOrder(-200)`.

### 2. Robustesse : Double-Abonnement (EnergyManager)
- **Solution** : Ajout d'une sécurité `SubscribeToTick()` (Note : retirée en 4.B car le flux est redevenu continu).

---

## [Phase 4] - Tick Manager & Synchronisation (EN COURS)
- [x] Hybridation Flux/Tick pour la fluidité.
- [ ] Groupement par Type & Network.

---

## [Phase 5] - Refonte Théorique du Flux d'Énergie
**Date** : 2026-04-28
**Auteur** : Antigravity (AI)

### 1. Spécification (Validée avec Modifications)
- Création du document `DOC_ENERGY_REFACTOR_SPEC.md`.
- **Modifications (Demande Utilisateur) :** Suppression des variables `Efficiency` et `NetworkPriority`. Le système de Load Balancing doit être un pur "pro-rata" : si la demande dépasse l'offre, tout le monde reçoit moins proportionnellement à sa demande sans traitement de faveur.

### 2. Implémentation : Architecture & Global Solver
- **Interfaces :** Nettoyage de `IEnergyNode` (ajout `MaxStorage`, `CurrentEnergy`, `EnergyAllocationRate`), `IEnergyProducer` (`ProductionPerTick`, `OutputTransferSpeed`) et `IEnergyConsumer` (`InputTransferSpeed`, `ConsumptionPerAction`).
- **Refactoring Machines :** Suppression des variables locales isolées (`_maxCapacity`, etc.) au profit de propriétés normalisées héritées ou interfacées. `YellowBallBehavior` est désormais à la fois Consumer et Producer pour interagir avec le réseau.
- **Global Solver (`EnergyNetwork`) :** Réécriture complète.
  - `CalculateAllocation(tickRate)` : Lancé par l'`EnergyManager` **uniquement lors du PowerTick**. Fait le bilan Offre/Demande et calcule les Ratios de transfert.
  - `ProcessFluidTransfer(deltaTime)` : Lancé **à chaque FixedUpdate**. Applique les ratios de manière purement fluide (`CurrentEnergy += Allocation * dt`).
- **Correction (Bug Générateur) :** Le `GeneratorMachine` ne se remplissait plus car la méthode `ProduceEnergy` était devenue obsolète avec le nouveau réseau. Ajout de la production fluide directement dans son `FixedUpdate()`.
- **Correction (Bug de Synchronisation "1 tick sur 2") :** L'`EnergyManager` calculait les besoins du réseau **avant** ou **en même temps** que les machines vidaient leur propre jauge. Une machine qui spammait `CurrentEnergy = 0` le faisait *après* le passage du réseau, demandant donc 0 au tick suivant (d'où l'alternance et la désynchronisation).
  - Création de l'événement `OnPostPowerTick` dans `PowerTickManager`.
  - Le réseau attend que *toutes* les machines aient agi avant de calculer la distribution du tick suivant, garantissant un flux pur et constant.
- **Ajout (State Management & Drag) :** Gestion de l'isolation topologique lors du déplacement des entités.
  - *MachineEntity (Hard Disconnect)* : Saisir une machine la retire instantanément de son réseau local (`CurrentNetwork.RemoveNode()`) et bloque son débit, stoppant la production/consommation de ressources "fantômes".
  - *YellowBallBehavior (Dynamic Drag)* : Saisir une balle ne la déconnecte pas. Son mouvement est traqué (`ExecuteFixedUpdate`). Si ses connexions physiques (Collider Overlap) changent pendant qu'elle est tenue, elle signale une rupture.
  - *Dirty Flag Topology* : Optimisation massive. Plutôt que de rebuild le réseau à chaque frame pendant un mouvement, on passe le flag `EnergyManager.IsTopologyDirty` à `true`. Le FloodFill complet n'est exécuté qu'une seule fois par cycle, au `OnPostPowerTick`.
- **Équilibrage (Economy Rebalancing) :** 
  - Passage à 6 Ticks par seconde (`TickRate = 0.1666f`) pour une meilleure fluidité.
  - Ajustement du `RedMaterialisator` : `InputTransferSpeed` = 0.05. Il faut désormais ~3.33 secondes pour générer 1 boule (1 / (0.05 * 6)).
  - Ajustement du `Generator` : `ProductionPerTick` = 0.12. Un générateur peut soutenir 2 Materialisators (0.10/tick) de manière fluide, mais s'essoufflera face à 3 Materialisators (0.15/tick), créant la pénurie demandée.
- **Corrections & Synchronisation Avancée :**
  - *Bug "Ghost Energy"* : Les machines saisies continuaient à recevoir de l'énergie. Le solver `EnergyManager.CanConnect` rejette désormais formellement toute connexion si `IsBeingDragged` est vrai, garantissant une isolation physique absolue.
  - *Sequencer Sync (Cadence)* : Ajout d'un système de *Step Sequencer* global dans `PowerTickManager` (`CurrentTickCount`). Les `RedMaterialisators` ne tirent plus bêtement quand ils sont pleins. Ils patientent sagement jusqu'à ce que `(CurrentTickCount % Cadence == Offset)`.
- **Refonte des Yellow Balls (Câbles/Batteries) :**
  - *Visuels* : Suppression du scale dynamique. Ajout d'une transition de couleur Jaune -> Gris basée sur l'énergie. Exposition de `_currentEnergy` dans l'Inspector.
  - *Topologie (Near/Far)* : Implémentation d'un double BFS dans le `EnergyManager`. Chaque nœud connaît sa distance au générateur le plus proche (`DistanceToSource`).
  - *Logique de Flux Prioritaire* : Réécriture complète de `CalculateAllocation`. L'énergie suit désormais un ordre strict : 
    1. Remplissage des câbles en priorité (du plus proche au plus loin du générateur).
    2. Distribution aux machines (Consumers).
    3. Si manque d'énergie, les machines puisent dans les câbles (du plus loin au plus proche).
- **Résultat :** Compilation Unity validée à 100% (0 Erreurs). Prêt pour les tests In-Game.

---

## [Phase 6] - Refonte des Collisions (Collider-Based) & Arcs Électriques
**Date** : 2026-05-01
**Auteur** : Antigravity (AI)

### 1. Détection par Colliders (Précision Arbitraire)
- **Problème** : La détection centre-à-centre (cercles) était imprécise pour les machines non circulaires.
- **Solution** : Création de `EnergyCollisionUtility`. 
  - Utilise `Collider2D.ClosestPoint` et `Physics2D.OverlapCircle` pour vérifier si le rayon de connexion d'un nœud touche réellement la "carrosserie" (collider) de l'autre.
  - S'adapte à n'importe quelle forme (Box, Polygon, etc.).
- **Contrat** : Ajout de `IEnergyNode.PhysicsCollider` pour garantir l'accès au composant physique.

### 2. Visualisation Premium des Arcs
- **Ancrage Dynamique** : Les arcs ne partent plus du centre des machines mais du point le plus proche sur le bord de leur collider (`EnergyCollisionUtility.GetAnchorPoint`).
- **États Visuels (Feedback)** :
  - **Gris (Preview/Drag)** : L'arc s'affiche en temps réel dès qu'on approche une machine d'un réseau potentiel (géré dans `EnergyManager.Update`).
  - **Gris (Waiting/Sync)** : L'arc est connecté mais la machine attend son tick de synchronisation (allocation = 0).
  - **Bleu (Active)** : L'arc s'allume en bleu cyan quand l'énergie circule réellement.
- **Optimisation** : Pool d'arcs dynamique avec indexation séparée pour les arcs de réseau (persistants) et les arcs de preview (éphémères).

### 3. Nettoyage Technique
- Migration de `CanConnect` vers `CanConnectInternal` (ajout de l'option `ignoreDrag` pour les previews).
- Correction de `Rigidbody2D.GetAttachedComponent` en `GetComponent<Collider2D>()`.
- Enregistrement de `EnergyCollisionUtility.cs` dans `Assembly-CSharp.csproj` pour la compilation.

- **Résultat** : Réseaux robustes aux formes complexes, feedback visuel instantané et premium. 0 erreurs de compilation.
