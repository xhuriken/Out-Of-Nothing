# TO-DO : Visuals & Topologie des Yellow Balls

## 1. Visuels des Yellow Balls
- [x] Retirer la modification du `transform.localScale` dans `YellowBallBehavior.cs`.
- [x] Ajouter une gestion de couleur (Color de début "Jaune" et de fin "Gris neutre") avec une transition fluide basée sur `CurrentEnergy / MaxStorage`.
- [x] Désactiver la destruction automatique (`Destroy`) lorsque la balle atteint 0 énergie (elle devient juste grise et inactive).

## 2. Refonte du Solver (Pathfinding & Priorités)
- [x] Modifier le BFS dans `EnergyManager.RebuildNetworks()` pour démarrer à partir des `IEnergyProducer` (Générateurs) et propager un entier `DistanceToSource` à chaque nœud.
- [x] Dans `EnergyNetwork`, stocker une liste séparée `List<YellowBallBehavior> _cables` triée par `DistanceToSource` croissante.
- [x] Réécrire la logique de `CalculateAllocation` et `ProcessFluidTransfer` :
    - Étape A (Remplissage) : Injecter la production des Générateurs EN PRIORITÉ dans la liste `_cables` (du plus proche au plus lointain).
    - Étape B (Distribution) : S'il reste de l'énergie des Générateurs, l'allouer aux Consumers.
    - Étape C (Soutirage) : Si les Consumers manquent d'énergie, ils ponctionnent le déficit dans la liste `_cables` à l'envers (du plus lointain au plus proche).
- [ ] Mettre à jour `DOC_ENERGY_ARCHITECTURE.md` et `DEVELOPMENT_LOG.md`.
