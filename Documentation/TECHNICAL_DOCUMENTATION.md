# Documentation Technique de "Out Of Nothing"

## État Actuel du Projet
*Dernière mise à jour : 2026-04-10*

### Architecture Globale
- Moteur : Unity
- Langage : C#
- Principes : KISS, SSOT, CoreFX Coding Standard.

### Modules Analysés
*Aucun pour l'instant. L'analyse débutera lors de la première interaction avec le code source.*

### Systèmes de Tiers
- Odin Inspector (détecté via .csproj)
- Shapes (détecté via .csproj)

### Système de Physique (Balles & Rebond)
- **Problématique EdgeCollider2D :** Le moteur physique d'Unity gère mal les impacts à haute vitesse sur les arêtes d'un EdgeCollider2D (création de forces de dépénétration latérales aberrantes et normales faussées).
- **Contournement mis en place :**
  - **`BallPhysicsPassport` :** La variable `TrueVelocity` capture la vélocité via `FixedUpdate` avant que le moteur de collision d'Unity ne s'exécute. C'est l'unique source de vérité pour le rebond.
  - **Rebond Géométrique (`GameZone.GetNearestSide`) :** Plutôt que de se fier aux normales de collision d'Unity, les vecteurs normaux des murs sont calculés purement via la géométrie relative (distance au MinX/MaxX/MinY/MaxY en espace local).
  - **`ConstantBounceSurface` :** Ce script gère désormais intégralement les collisions des murs, appliquant une vélocité forcée et gérant les balles ayant une vitesse inférieure au `_thresholdSpeed`.
  - **Dépénétration Manuelle :** Lors du calcul du rebond, on déplace physiquement le `Rigidbody2D` vers l'extérieur (le long de la normale) en fonction de la pénétration `separation` pour empêcher Unity d'appliquer son impulse correctif défectueux au frame suivant.

### Visual Effects & Animations (Added 2026-05-21)
- **`InfiniteRotate.cs`**:
  - Animates the `DashOffset` of any `Shapes.Disc` component to create a seamless rotation effect.
  - Implements a precise modulo wrapping mechanism based on the dash period (`DashSize + DashSpacing`) to ensure that the offset resets perfectly to `0` upon completing a full cycle. This prevents floating-point precision loss and jitter that typically accumulates over long gameplay sessions.

