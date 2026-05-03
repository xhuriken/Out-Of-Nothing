# TODO - Correctif Comportement Balles, Arcs et Machines

## 1. Comportement des Balles & Drag
- [x] Modifier `BallEntity.cs` : Toutes les balles retournent `false` dans `ExecuteFixedUpdate` si `IsBeingDragged` est vrai, SAUF la balle jaune.
- [x] Modifier `YellowBallBehavior.cs` : Forcer le rebuild du network (`EnergyManager.Instance.IsTopologyDirty = true`) dès que la balle bouge, même en drag.
- [x] Modifier `YellowBallBehavior.cs` : S'assurer que la balle peut "pomper" (Energy Allocation) même pendant le drag si elle est proche d'une machine.

## 2. Visualisation des Arcs (ElectricArc)
- [x] Modifier `ElectricArc.cs` : Corriger l'override des couleurs du dégradé (Gradient) sur le `LineRenderer`.
- [x] Implémenter les états de couleur : Jaune (Actif), Gris (Inactif).

## 3. Bug de Pumping (Regression)
- [x] Analyser `MachineEntity.cs` et `PowerTickManager.cs`.
- [x] Correction : Empêcher le remplissage instantané à la connexion.

## 4. Organisation du Projet (Nettoyage)
- [x] Créer un dossier `Documentation/` à la racine pour les fichiers MD.
- [x] Supprimer le dossier `Assets/_Recovery`.
- [x] Refondre l'architecture `Assets/` vers `Assets/_Project/`.
- [x] Renommer et classer les scripts sans préfixes numériques (Core, Entities, Physics, Data).
- [x] Regrouper les plugins dans `Assets/Plugins`.
- [x] Ranger les matériaux de physique et réglages d'input.
