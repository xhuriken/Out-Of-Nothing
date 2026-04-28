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
- **Résultat :** Compilation Unity validée à 100% (0 Erreurs). Prêt pour les tests In-Game.
