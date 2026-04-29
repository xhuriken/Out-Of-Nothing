# Architecture du Système d'Énergie (Phase 5)

Ce document décrit la structure, la logique et le cycle de vie du système d'énergie du jeu, en respectant les principes KISS (Keep It Simple, Stupid) et SSOT (Single Source Of Truth).

---

## 1. Vue d'Ensemble et Chef d'Orchestre (Tick System)

Le système d'énergie repose sur un modèle hybride : 
- **Discret (Tick-based) :** Les décisions (combien transférer, qui donne à qui) sont prises à intervalles réguliers (ex: toutes les secondes).
- **Continu (FixedUpdate) :** L'application visuelle et le transfert des fluides d'énergie se font progressivement pour un "Game Feel" agréable.

### `PowerTickManager` (Singleton)
**Rôle :** C'est le métronome du jeu. Il dicte quand les actions réseau doivent se produire.
- **Variables :** `_tickRate` (Durée d'un cycle en secondes).
- **Événements :**
  - `OnPowerTick` : Les machines exécutent leurs actions locales (ex: un RedMaterialisator consomme l'énergie et crache sa boule).
  - `OnPostPowerTick` : L'`EnergyManager` prend une photo des jauges après consommation, reconstruit les réseaux si nécessaire, et calcule la nouvelle distribution d'énergie pour le tick à venir.

---

## 2. Le Gestionnaire et le Solver

### `EnergyManager` (Singleton)
**Rôle :** Gestionnaire de Topologie. Il surveille l'apparition/disparition des nœuds et dessine les arcs électriques.
- **Variables :** `_isDirty` (Flag d'optimisation. Si vrai, un Rebuild aura lieu au prochain `OnPostPowerTick`).
- **Méthodes :**
  - `RegisterNode` / `UnregisterNode` : Ajoute/retire une entité de la liste globale (`_allNodes`).
  - `MarkTopologyDirty()` : Demande un recalcul des réseaux. Très léger, il évite de recalculer la topologie plusieurs fois par frame.
  - `RebuildNetworks()` : Algorithme de *FloodFill* (BFS) qui groupe les nœuds proches physiquement en entités logiques `EnergyNetwork`.

### `EnergyNetwork` (L'Intelligence)
**Rôle :** C'est un "sous-réseau" isolé. Il calcule mathématiquement la répartition de l'énergie entre ses membres.
- **Variables :** `_producers`, `_consumers`, `_nodes` (Membres exclusifs de ce réseau).
- **Méthodes :**
  - `CalculateAllocation(tickRate)` : Lancée par l'`EnergyManager` au tick. Calcule l'offre totale et la demande totale. Définit le `EnergyAllocationRate` de chaque nœud au "Pro-rata" (si la demande excède l'offre, l'énergie est partagée équitablement).
  - `ProcessFluidTransfer(deltaTime)` : Lancée par l'`EnergyManager` dans le `FixedUpdate`. Applique le `EnergyAllocationRate` (qui peut être positif ou négatif) à la variable `CurrentEnergy` de chaque nœud.

---

## 3. Les Contrats (Interfaces)

C'est l'ADN du système. Aucune classe ne communique directement avec une autre classe concrète, tout passe par ces contrats.

### `IEnergyNode`
**Rôle :** Identifiant topologique. N'importe quel objet physique capable de contenir ou de faire transiter de l'énergie.
- `Position`, `ConnectionRadius` : Pour le calcul spatial.
- `MaxStorage` : Le plafond d'énergie de l'entité.
- `CurrentEnergy` : La réserve actuelle (SSOT).
- `EnergyAllocationRate` : La vitesse à laquelle l'entité va se remplir ou se vider lors du prochain tick (assigné par le solver).

### `IEnergyProducer`
**Rôle :** Entité capable d'injecter de l'énergie dans le réseau.
- `ProductionPerTick` : La quantité d'énergie générée *ex-nihilo* (ou contenue) par tick.
- `OutputTransferSpeed` : Vitesse maximale à laquelle cette entité accepte de se vider.

### `IEnergyConsumer`
**Rôle :** Entité ayant besoin d'énergie pour fonctionner.
- `ConsumptionPerAction` : Le "prix" d'une action.
- `InputTransferSpeed` : Vitesse maximale d'aspiration de l'énergie.

---

## 4. Les Entités Concrètes

### `MachineEntity` (Classe Abstraite)
**Rôle :** Parent de toutes les machines. Implémente `IEnergyNode`.
- **Drag & Drop "Hard Disconnect" :**
  - Lors d'un Drag (`OnDragStart`), la machine s'auto-exclut de son `CurrentNetwork`, met son `AllocationRate` à 0, et lève le `MarkTopologyDirty()`. Ceci empêche instantanément toute anomalie visuelle (production/consommation fantôme en plein vol).

### `GeneratorMachine` (Hérite de MachineEntity, implémente IEnergyProducer)
- **Fonctionnement :** Dans son propre `FixedUpdate`, il remplit fluidement son `CurrentEnergy` jusqu'à son `MaxStorage`. Au moment du Tick, le réseau aspire ce `CurrentEnergy` pour l'envoyer aux consommateurs.

### `RedMaterialisatorMachine` (Hérite de MachineEntity, implémente IEnergyConsumer)
- **Fonctionnement (Just-In-Time) :** Intègre un `_startFillTick`. Plutôt que de pomper aveuglément l'énergie dès qu'il est vide, le Materialisator calcule à quel moment exact il doit ouvrir ses vannes pour être plein "pile à l'heure" de la cadence globale (Step Sequencer). 
  - Tant que le tick n'est pas atteint (ou s'il est déconnecté de tout générateur), il reste en état de veille (Gris) et ne demande **aucune** énergie au solver (`InputTransferSpeed = 0`).
  - Une fois l'heure atteinte, il ouvre les vannes (Jaune).
  - Au déclenchement du `OnPowerTick` (si modulo cadence = offset), s'il est plein, il vide sa jauge, crache une boule, et recalcule son prochain cycle de veille. S'il n'est pas plein (pénurie), il rate le coche mais cible automatiquement le cycle suivant.

### `YellowBallBehavior` (Implémente IEnergyNode, Producer, Consumer)
**Rôle :** C'est une Batterie (ou un Câble).
- **Fonctionnement (Dynamic Drag) :** Contrairement aux machines, une Yellow Ball n'est pas déconnectée quand on la saisit. Pendant son Drag, elle observe ses voisins (`Physics2D.OverlapCircleNonAlloc`). Si ceux-ci changent, elle appelle `MarkTopologyDirty()`. Le joueur peut donc "balader" l'énergie d'un réseau à l'autre en temps réel. Si la balle atteint 0 énergie, elle s'auto-détruit.

---

## 5. Exemples / Use Cases (Cycle de vie)

### Use Case A : Un Générateur alimente un Red Materialisator (Ratio 1:1)
1. **Initialisation :** Le Générateur produit 1 énergie/tick. Le Materialisator a besoin de 1 énergie pour faire une action.
2. **OnPostPowerTick (T=0) :** Le solver voit que le Générateur a 1, et que le Materialisator a besoin de 1. Il set `EnergyAllocationRate` à +1 pour le Red et -1 pour le Gen.
3. **Pendant le Tick (FixedUpdate) :** Visuellement, la jauge du générateur se vide fluidement vers le Red Materialisator pendant 1 seconde.
4. **OnPowerTick (T=1) :** Le Red Materialisator constate qu'il est plein. Il invoque `SpawnBall()` et retombe à 0 d'énergie.

### Use Case B : Deux Red Materialisators sur un seul Générateur (Pénurie)
1. L'offre totale est de 1. La demande totale est de 2 (1 pour chaque Red).
2. Le `EnergyNetwork` applique le prorata : `Offre / Demande = 1 / 2 = 0.5`.
3. Chaque Red Materialisator reçoit un `AllocationRate` de 0.5.
4. **Résultat :** Les Red Materialisators se rempliront deux fois moins vite, et cracheront donc une boule tous les 2 Ticks, de manière parfaitement synchronisée.
