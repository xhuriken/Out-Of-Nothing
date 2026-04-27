# DEVELOPMENT LOG - Out Of Nothing

## RÈGLES DE RIGUEUR (META-RULES)
1. **SSOT (Single Source of Truth)** : Toute modification doit respecter la source unique de vérité.
2. **Double-Validation** : Toujours vérifier la visibilité des membres (public/private) pour éviter les erreurs de compilation.
3. **Traçabilité Totale** : Mise à jour systématique de ce log AVANT de rendre la main.
4. **Zéro Oubli** : Comparer l'intention initiale du prompt avec l'implémentation finale.

---

## [Phase 3.D] - Finalisation Précision et Synchronisation
**Date** : 2026-04-27
**Auteur** : Antigravity (AI)

### 1. Fix Compilation : TickRate
- **Problème** : `PowerTickManager` n'exposait pas son intervalle de tick, empêchant `EnergyManager` de calculer la durée du cycle.
- **Solution** : Ajout de la propriété `public float TickRate`.
- **Fichier** : `PowerTickManager.cs`

### 2. Synchronisation Structurelle (Tick Dependency)
- **Pourquoi ?** : Le système d'énergie fonctionnait de manière asynchrone (FixedUpdate), ce qui créait des décalages avec la logique des machines.
- **Comment** : Suppression du `FixedUpdate` de `EnergyManager`. Celui-ci s'abonne désormais à `OnPowerTick`.
- **Résultat** : L'énergie ne circule QUE si le `PowerTickManager` bat le rappel. Cohérence totale du gameplay.
- **Fichier** : `EnergyManager.cs`

### 3. Quantification de l'Énergie (Professional Precision)
- **Pourquoi ?** : Les erreurs de flottants (`0.999999`) bloquaient les machines de production.
- **Comment** : Implémentation de `EnergyNetwork.Quantize(float value)` arrondissant à 4 décimales.
- **Résultat** : "Clean Energy Packets". Les valeurs sont nettes (ex: 50.0000) et les comparaisons (`>=`) sont désormais instantanées et exactes.
- **Fichiers** : `EnergyNetwork.cs`, `MachineEntity.cs`, `RedMaterialisatorMachine.cs`, `GeneratorMachine.cs`, `YellowBallBehavior.cs`.

### 4. Robustesse des Abonnements
- **Problème** : Les machines perdaient leur lien avec le Tick lors des cycles Enable/Disable ou en cas d'ordre de Start variable.
- **Solution** : Déplacement de l'abonnement dans `OnEnable` / `OnDisable` avec désabonnement préventif (`-=`) pour éviter les doubles enregistrements.
- **Fichier** : `MachineEntity.cs`

---

## [Phase 4] - Tick Manager & Synchronisation (À VENIR)
- **Objectif** : Groupement par Network et décalage de cycles pour l'optimisation.
