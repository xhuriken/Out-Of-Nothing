# TODO : Implémentation du Drag & Drop et Rebuild Réseau

## Phase 1 : Système de Flag Topologique (Dirty Flag)
- [ ] Ajouter une méthode `MarkTopologyDirty()` dans `EnergyManager`.
- [ ] Dans `EnergyManager`, au début de `HandlePowerTick` (ou `OnPostPowerTick`), si la topologie est marquée "Dirty", déclencher un `DiscoverNetworks()` complet avant de calculer les allocations, puis remettre le flag à faux.

## Phase 2 : Drag des Machines (MachineEntity)
- [ ] Dans `MachineEntity`, à l'événement de début de drag (OnDragStart) :
    - Retirer explicitement la machine du `CurrentNetwork`.
    - Mettre l'`EnergyAllocationRate` à 0.
    - Appeler `EnergyManager.Instance.MarkTopologyDirty()`.
- [ ] Dans `MachineEntity`, à l'événement de fin de drag (OnDragEnd) :
    - Appeler `EnergyManager.Instance.MarkTopologyDirty()`.

## Phase 3 : Drag Dynamique des YellowBalls
- [ ] Dans `YellowBallBehavior`, créer un suivi des connexions : `HashSet<Collider2D> _currentNeighbors`.
- [ ] Ajouter une logique (ex: dans `FixedUpdate`) qui ne s'active *que* si la balle est `IsBeingDragged` :
    - Effectuer un `Physics2D.OverlapCircleAll`.
    - Comparer le résultat avec `_currentNeighbors`.
    - Si une différence est détectée (nouvelle connexion ou perte d'une connexion), mettre à jour `_currentNeighbors` et appeler `EnergyManager.Instance.MarkTopologyDirty()`.

## Phase 4 : Nettoyage et Optimisations
- [ ] S'assurer que les appels à `DiscoverNetworks()` sont retirés des événements de collisions immédiats (`OnCollisionEnter`, etc.) pour centraliser tout le recalcul topologique derrière le "Dirty Flag" et protéger les performances.
