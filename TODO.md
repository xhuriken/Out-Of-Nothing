# TODO List - Out Of Nothing

## Phase 1 : Initialisation & Rigueur ✅
- [x] Créer TODO.md interne
- [x] Configurer DEVELOPMENT_LOG.md

## Phase 2 : Gestion Spatiale & Rayons (SSOT) ✅
- [x] Ajouter `PhysicalRadius` à `IEnergyNode`
- [x] Implémenter Edge-to-Edge dans `EnergyManager`
- [x] Corriger les arcs électriques (`Shortest Path` sur circonférence)
- [x] **RE-CORRECTION** : Retour à la détection `Radius-vs-Collider` (Demande utilisateur)

## Phase 3 : Correction de Bruit & Synchronisation ✅
- [x] Fix : Vidage des Red Machines (Spawn restauré)
- [x] Implémenter la **Quantification** (4 décimales) pour supprimer les résidus flottants
- [x] Asservir l'**EnergyManager** au **PowerTickManager** (Dépendance stricte)
- [x] **FIX CRITIQUE** : Résolution de la Race Condition Singleton (Execution Order -200)
- [x] Robustesse : Inscription des machines dans `OnEnable`
- [x] Visibilité : Exposer l'énergie et l'état `IsRunning` dans l'inspecteur
- [x] Commit & Push sur la branche `something`

## Phase 4 : Tick Manager & Synchronisation (EN COURS) 🔄
- [ ] Groupement par Type & Network (Répartition sur ticks différents)
- [ ] Synchronisation Automatique : Attente du cycle complet pour les nouvelles machines
- [ ] États Latents : Feedback visuel (atténuation) et arrêt de consommation
- [ ] Manual Sync Offset : Permettre un décalage manuel optionnel

## Prochaines Étapes :
1. Définir la structure de groupement dans le `PowerTickManager`.
2. Implémenter la logique de Latency State pour les nouvelles machines.
