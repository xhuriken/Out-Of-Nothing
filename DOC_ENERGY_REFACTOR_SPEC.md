# Spécification Technique : Refonte du Flux d'Énergie et de la Charge Réseau (Phase 5)

## 1. Problématique Actuelle
Actuellement, le système d'énergie est fragmenté : le flux d'énergie (FixedUpdate) transfère des paquets d'énergie absolus, sans considération stricte pour la durée du Tick global, et de manière locale (nœud à nœud). Cela entraîne une sensation "saccadée" ou "incohérente" quand les machines tentent de se vider/remplir sur un cycle, et empêche un véritable Load Balancing global (plusieurs consommateurs sur un ou plusieurs générateurs).

## 2. Objectifs de la Refonte

### A. Intégration Tick-Flow (Ratio de Flux)
L'énergie doit être "coulante" (fluide visuellement) mais fondamentalement dépendante du métronome du `PowerTickManager`.

**L'Équation du Flux :**
Au lieu de définir qu'une machine transfère "1 unité par frame", nous définissons qu'une machine transfère "X unités par **Tick**".
Le `EnergyManager` (dans son `FixedUpdate`) va interpoler ce transfert pour qu'il soit lisse.

```csharp
// Pour une frame donnée, l'énergie allouée à transférer est :
float tickRate = PowerTickManager.Instance.TickRate; // Ex: 1.0 sec
float energyPerTick = machine.TransferSpeed;         // Ex: 5 unités
float energyPerFixedUpdate = (energyPerTick / tickRate) * Time.fixedDeltaTime;
```
*Bénéfice :* Si on accélère le jeu (Tick = 0.5s), l'énergie coule 2 fois plus vite à l'écran, sans aucune modification de la logique de transfert interne des machines.

### B. Dynamique de Charge du Network (Global Solver)
L'énergie ne peut plus se contenter de couler vers le voisin le plus proche. Le réseau doit se comporter de manière intelligente.

**Algorithme (O(N) par Tick) :**
À chaque début de Tick, l'`EnergyNetwork` agit comme un comptable central :
1. **Bilan de l'Offre :** Somme de toutes les `OutputTransferSpeed` des `IEnergyProducer` (bridé par leur énergie actuelle).
2. **Bilan de la Demande :** Somme de toutes les `InputTransferSpeed` des `IEnergyConsumer` (bridé par leur espace libre).
3. **Load Balancing (Équilibrage) :**
    *   **Abondance (Offre >= Demande) :** Chaque consommateur reçoit 100% de sa demande. Les producteurs ne débitent que l'énergie effectivement consommée, au prorata de leur capacité maximale.
    *   **Pénurie (Offre < Demande) :** Chaque consommateur reçoit un pourcentage strict de l'énergie disponible, calculé selon le ratio global (`Ratio = TotalOffre / TotalDemande`). Le prorata est respecté (si un consommateur demande 2x plus, il recevra 2x plus que l'autre, mais tous deux seront réduits par le Ratio).
4. **Mise en Cache :** Le réseau stocke la variable `EnergyAllocatedPerFixedUpdate` pour chaque machine.
5. **Fluidité :** Le `FixedUpdate` se contente d'appliquer `CurrentEnergy += EnergyAllocatedPerFixedUpdate` en douceur tout au long du cycle, sans calculs lourds.

### C. Évolutivité & Anticipation (Nomenclature des Variables)
Pour rester KISS (Keep It Simple, Stupid) et SSOT (Single Source Of Truth), l'architecture des propriétés doit être claire, exposée (Inspector) et normalisée (Microsoft CoreFX).

#### Interface de base (`IEnergyNode`)
*À étendre dans MachineEntity :*
- `float MaxStorage` : Capacité interne maximale (ex: 5).
- `float CurrentEnergy` : Énergie actuellement dans le buffer.

#### Producteur (`IEnergyProducer` -> `GeneratorMachine`)
- `float ProductionPerTick` : Énergie "créée de nulle part" par cycle.
- `float OutputTransferSpeed` : Quantité max que la machine peut **pousser** dans le réseau par Tick.

#### Consommateur (`IEnergyConsumer` -> `RedMaterialisatorMachine`, etc.)
- `float InputTransferSpeed` : Quantité max que la machine peut **aspirer** du réseau par Tick.
- `float ConsumptionPerAction` : Coût d'une action spécifique (ex: l'ancien `EnergyRequiredPerSpawn`).

#### Stockeur (`IEnergyStocker` -> Futurs objets de batterie)
- Cumule `InputTransferSpeed` et `OutputTransferSpeed`. Ne produit rien.

## 3. Cas d'Utilisation

**Cas A : Pénurie avec Load Balancing**
- *Situation :* 1 Générateur (Offre 10/Tick). 2 Consommateurs (Le A demande 10/Tick, le B demande 20/Tick).
- *Résultat du Bilan (Début de Tick) :* Offre totale = 10. Demande totale = 30. Pénurie détectée.
- *Ratio :* `10 / 30 = 0.3333`.
- *Attribution :* Le consommateur A recevra `10 * 0.3333 = 3.3333` unités. Le consommateur B recevra `20 * 0.3333 = 6.6666` unités.
- *Visuel :* En `FixedUpdate`, sur la durée du Tick, la jauge de A montera de 3.3333 et celle de B montera de 6.6666 fluidement.

**Cas B : Accélération Temporelle**
- *Situation :* Le joueur divise le `TickRate` par 2 (passe de 1.0s à 0.5s). Un Stocker demande 10/Tick.
- *Résultat :* L'équation `(10 / 0.5) * 0.02` calculera un transfert de 0.4 unités par frame physique au lieu de 0.2. 
- *Visuel :* La jauge d'énergie monte deux fois plus vite, l'animation s'accélère mécaniquement. Le `PowerTickManager` continue de dicter le pas.

## 4. Bénéfices Techniques
1. **Performances :** Plus de calculs complexes de transfert à chaque `FixedUpdate`. La répartition se fait une fois par Tick, le `FixedUpdate` n'est qu'une simple addition flottante. Parfait pour `500+ networks`.
2. **Design System :** Les valeurs dans l'Inspecteur (`TransferSpeed`, `ProductionPerTick`) prennent tout leur sens et permettent des mécaniques d'upgrades claires pour le joueur.
3. **Stabilité :** Aucun risque de dépassement de buffer. Le Load Balancing garantit la cohérence des nombres avant le transfert visuel.
