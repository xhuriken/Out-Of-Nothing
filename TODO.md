# TODO - Correctif Comportement Balles, Arcs et Machines

## 1. Comportement des Balles & Drag
- [x] Modifier `BallEntity.cs` : Toutes les balles retournent `false` dans `ExecuteFixedUpdate` si `IsBeingDragged` est vrai, SAUF la balle jaune.
- [x] Modifier `YellowBallBehavior.cs` : Forcer le rebuild du network (`EnergyManager.Instance.IsTopologyDirty = true`) dès que la balle bouge, même en drag. (Déjà géré dans le behavior)
- [x] Modifier `YellowBallBehavior.cs` : S'assurer que la balle peut "pomper" (Energy Allocation) même pendant le drag si elle est proche d'une machine. (Géré via EnergyManager.CanConnectInternal)

## 2. Visualisation des Arcs (ElectricArc)
- [x] Modifier `ElectricArc.cs` : Corriger l'override des couleurs du dégradé (Gradient) sur le `LineRenderer`.
- [x] Implémenter les états de couleur :
    - **Gris** : Machine proche non connectée OU machine en attente de tick de pompage.
    - **Jaune** : Flux d'énergie actif (pompage en cours).
- [x] Ajouter des logs stratégiques pour debugger le changement de couleur si nécessaire.

## 3. Bug de Pumping (Regression)
- [x] Analyser `MachineEntity.cs` et `PowerTickManager.cs`.
- [x] Correction : Empêcher le remplissage instantané à la connexion (Limitation de la demande au besoin réel).
- [x] Garantir que la machine commence son cycle de pompage UNIQUEMENT si elle peut atteindre son prochain tick sans interruption.

## 4. Finalisation
- [x] Vérifier la compilation.
- [x] Mettre à jour `DEVELOPMENT_LOG.md`.
- [x] Supprimer les logs de debug temporaires.
