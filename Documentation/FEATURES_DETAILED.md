# FEATURES_DETAILED.md - Single Source of Truth

This document provides a comprehensive technical audit of the "Out-Of-Nothing" project. It serves as the authoritative reference for understanding systems architecture and component logic.

---

## 1. Global Architecture

### Core Patterns
- **Singleton Pattern**: Used for centralized management systems requiring global access.
    - `BallPoolManager`: Handles object pooling for all ball entities.
    - `EnergyManager`: Orchestrates the energy grid topology and recalculations.
    - `GameInputManager`: Routes unified input to world entities.
- **Strategy & Prototype Pattern**: Used for Ball logic. `BallDataSO` holds a `BallBehavior` template which is cloned by each instance of `BallEntity`. This ensures logic is separate from physical representation and avoids shared state issues.
- **Graph-Based Distribution**: The energy system uses a Flood Fill (BFS) algorithm to detect isolated clusters of connected machines/balls, forming independent `EnergyNetwork` instances.
- **Proxy Pattern**: Machines (e.g., `PressMachine`, `BumperMachine`) use `MachineColliderProxy` to forward physics events from child objects to the main machine logic.

### Data Flow & Interactions
1. **Input Flow**: `GameInputManager` (Input System) -> `IDraggable` or `BallEntity`.
2. **Physics Flow**: Unity Physics2D -> `BallEntity` / `MachineEntity` -> `EnergyManager` (Dirty Flag).
3. **Energy Flow**: `EnergyManager` Ticks `EnergyNetwork` -> `IEnergyProducer` (Gather) -> `IEnergyConsumer` (Distribute) -> `IEnergyStorage` (Buffer).
4. **Lifecycle**: `BallPoolManager` -> `BallEntity` -> `BallBehavior` (Clone).

---

## 2. Detailed Feature Index

### 2.1 Ball System

#### **BallEntity.cs** (Core Controller)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Manages physical representation, handles interactions, and delegates logic to `BallBehavior`. |
| **Logic & Algorithms** | Uses `DOTween` for scaling, click punch animations, and advanced mitosis-style cell duplication animations. Aligns transforms along a randomized split direction, triggers vibratory shakes, disables collision between duplicating halves using `Physics2D.IgnoreCollision`, moves kinematic rigidbodies via `DOMove` (pushing dynamic entities aside), and recovers shape with dynamic elastic overshoots. |
| **Exposed Members** | `_data` (BallDataSO), `_renderer` (Disc), `_dragForceMultiplier` (float), `_prepDuration` (float), `_splitDuration` (float), `_splitDistance` (float), `_vibrationIntensity` (float), `_maxStretch` (float), `_minSquash` (float), `_partingImpulse` (float), `_splitEase` (Ease), `_scaleEase` (Ease). |
| **Dependencies** | `Rigidbody2D`, `CircleCollider2D`, `BallPhysicsPassport`, `BallDataSO`, `BallPoolManager`, `DOTween`. |
| **Debug Logs** | N/A |

#### **BallPoolManager.cs** (Utility)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Centralized multi-pool for ball entities to avoid garbage collection. |
| **Logic & Algorithms** | Uses `UnityEngine.Pool.ObjectPool` per ball ID (Dictionary-backed). |
| **Exposed Members** | `_defaultCapacity` (int), `_maxSize` (int). |
| **Dependencies** | `BallEntity`, `BallDataSO`. |
| **Debug Logs** | `[BallPoolManager] Cannot spawn ball: Data or Prefab is null.` |

#### **BlueBallBehavior.cs** (Logic)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Oscillation logic with a pause mechanic on collision. |
| **Logic & Algorithms** | Updates `linearVelocity.y` using a `Mathf.Cos` oscillation. Pauses for `_pauseDuration` on collision. |
| **Exposed Members** | `_pauseDuration`, `_amplitude`, `_speed`. |
| **Dependencies** | `BallEntity`. |
| **Debug Logs** | N/A |

---

### 2.2 Machine System

#### **MachineEntity.cs** (Base)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Base contract for all machines. Handles dragging and energy node registration. |
| **Logic & Algorithms** | Implements rotation modes (Fixed 90° or Free). Stops logic (`_isRunning = false`) while dragging. |
| **Exposed Members** | `_rotationMode`, `_connectionRadius`, `_freeRotationSpeed`. |
| **Dependencies** | `IEnergyNode`, `IDraggable`. |
| **Debug Logs** | N/A |

#### **GeneratorMachine.cs** (Energy Source)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Constant energy production and buffering. |
| **Logic & Algorithms** | Linear accumulation: `_currentEnergy += _productionRate * deltaTime`. |
| **Exposed Members** | `_productionRate`, `_maxCapacity`, `_energyRenderer` (Shapes.Rectangle). |
| **Dependencies** | `IEnergyProducer`, `IEnergyStorage`. |
| **Debug Logs** | N/A |

#### **PressMachine.cs** (Consumer - WIP)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Processes specific ball types (Capture -> Animation -> Eject). |
| **Logic & Algorithms** | Uses `OnPartTriggerEnter` to capture "RedBall". Moves ball via `DOMove`. |
| **Exposed Members** | `_ballInside`, `_ballOut`, `_TargetTransformBall`. |
| **Dependencies** | `IEnergyConsumer` (Not Implementied), `BallPoolManager`. |
| **Debug Logs** | `[PressMachine] Capture animation completed ! Ready for ejection`. |

#### **Shop.cs** (Independent Machine)
| Feature | Details |
| :--- | :--- |
| **Purpose** | A machine that acts as a purchasable spawner for balls. It spawns selectable ball slots in a circle and ejects the purchased ball. |
| **Logic & Algorithms** | Inherits directly from `MonoBehaviour` and implements `IDraggable` (decoupled from the energy grid). Uses the `[ExecuteAlways]` attribute to update visuals and parameters dynamically both in the editor scene view and at runtime. Uses `DOTween` for spawning slots and hiding slots in **local space** (ensuring they follow the parent Shop transform perfectly when dragged). Performs currency verification with `IncrementManager.Instance.Points`. Ejection instantiates the purchased ball at the center, sets its scale initially to zero, applies a temporary heavy mass via `SetTemporaryHeavyMass(2f, 50f)` on the `BallEntity`, applies a physical dynamic impulse force via `Rb.AddForce` in the direction of the purchased slot, and tweens its scale to normal size (`Vector3.one`) over `0.5s`. Synchronizes `_gRadius` in `OnValidate`, `Start`, and `Update` to the visual discs, colliders, and shader properties. Continuously tracks `_lastPosition` to dynamically update the shader's `_ReflectCenter` property block in `LateUpdate()` whenever the Shop moves. Closes the shop interface when dragged or attracted by the black hole, and reopens it when the drag ends (if it was previously open and is not being expelled/attracted). |
| **Exposed Members** | `_ballShopContainer` (GameObject), `_discComponent` (Disc), `_backgroundDisc` (Disc), `_shaderRenderer` (SpriteRenderer), `_reflectRenderer` (SpriteRenderer), `_gRadius` (float), `_mainDiscOffset` (float), `_backgroundOffset` (float), `_shaderOffset` (float), `_reflectShaderOffset` (float), `_radius` (float) (spawner layout multiplier), `_moveDuration` (float), `_spawnDelay` (float), `_hideDelay` (float), `_postHideDelay` (float), `_shopDetectionRadius` (float), `_expelForce` (float), `_dragForceMultiplier` (float), `_maxDragSpeed` (float), `_wasOpenBeforeDrag` (bool). |
| **Dependencies** | `MonoBehaviour`, `IDraggable`, `BallShop`, `IncrementManager`, `BallPoolManager`, `DOTween`, `BallEntity`, `Shapes.Disc`, `SpriteRenderer`. |
| **Debug Logs** | `[Shop] BallShopContainer is not assigned!` |

#### **ShopRepulsion.cs** (Repulsion Field Component)
| Feature | Details |
| :--- | :--- |
| **Purpose** | A separate component dedicated to repelling balls and other machines away from the shop when the shop GUI is inactive. |
| **Logic & Algorithms** | Runs in `FixedUpdate`. Searches for colliders in a radius defined by the Shop's `GRadius` and `_repelRadiusOffset`. Repels dynamic rigidbodies (balls) via `AddForce` and kinematic rigidbodies (machines) via `MovePosition`. To prevent machines from overlapping or crossing out of bounds when repelled, kinematic bodies perform a `Rigidbody2D.Cast` sweep along their repulsion vector, shortening their movement to stop at the first obstacle contact point. Additionally, their target positions are mathematically clamped within the boundaries of `GameZone` using their physical radius. |
| **Exposed Members** | `_repelForce` (float), `_repelRadiusOffset` (float), `_repelLayerMask` (LayerMask). |
| **Dependencies** | `Shop`, `BallEntity`, `Rigidbody2D`, `Physics2D`. |
| **Debug Logs** | N/A |

#### **BallShop.cs** (Purchase Slot Component)
| Feature | Details |
| :--- | :--- |
| **Purpose** | An individual purchasable ball slot child of the Shop. |
| **Logic & Algorithms** | Exposes pricing and `BallDataSO` templates. Animates scale/movement during spawn/hide. Features an `IsInteractive` state flag that prevents clicks and hovers until deployment animations are fully complete. Exposes hover control via `SetHovered(bool)` (disabling Unity's native mouse messages). Features a visual shake and flash color feedback if funds are insufficient (`FlashPriceTextRed`). |
| **Exposed Members** | `_identity` (BallIdentityData), `_priceText` (TMP_Text), `_visualDisc` (Disc). |
| **Dependencies** | `BallDataSO`, `TextMeshPro`, `DOTween`, `Shapes.Disc`. |
| **Debug Logs** | N/A |

---

### 2.3 Energy System

#### **EnergyManager.cs** (Orchestrator)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Computes the global grid topology. |
| **Logic & Algorithms** | **BFS Cluster Detection**: Uses `Physics2D.OverlapCircleNonAlloc` to find neighbors. Groups them into `EnergyNetwork`. |
| **Exposed Members** | `_neighborBuffer` (Fixed array of 16). |
| **Dependencies** | `IEnergyNode`. |
| **Debug Logs** | `[EnergyManager] Rebuild complete. Found {count} independent networks.` |

#### **EnergyNetwork.cs** (Container)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Handles energy distribution within a connected cluster. |
| **Logic & Algorithms** | **Priority distribution**: 1. Direct Production -> 2. Storage Extraction. Respects `MaxFlowRate` per consumer. |
| **Exposed Members** | Internal lists of `IEnergyConsumer`, `IEnergyProducer`, `IEnergyStorage`. |
| **Dependencies** | N/A |
| **Debug Logs** | `[EnergyNetwork] Provided {amount} energy directly from producers.` |

---

### 2.4 Black Hole System

#### **BlackHole.cs** (Core Controller)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Central anomaly coordinator managing growth, consume flash, and the multi-phase "ImploseNothing" animation sequence. |
| **Logic & Algorithms** | Exposes target GRadius and easing curves. The **ImploseNothing** animation follows a 4-phase sequence: Phase 1 shrinks `GRadius` to `_implodeGRadiusTarget`; Phase 2 shrinks the main disc and increases thickness to keep its outer boundary stationary; Phase 3 expands the visual/physics attraction range to cover the GameZone (+3.0f margin) while growing the central black hole to a target percentage (`_implodeGRadiusGrowthPercent`) of the screen; Phase 4 restores all parameters back to their pre-implosion values (`preImplodeGRadius`). Shaking is applied dynamically via a decaying sinusoidal offset on `GRadius`. Bypasses shared materials using a `_flashIntensityMultiplier` to overlay flash pulses without overriding colors. |
| **Exposed Members** | `_implodeGRadiusTarget` (float), `_xDuration` (float), `_yDuration` (float), `_zDuration` (float), `_returnDuration` (float), `_implodeGRadiusGrowthPercent` (float), `_shakeAmplitude` (float), `_shakeFrequency` (float). |
| **Dependencies** | `Disc` (Shapes), `BlackHoleVisuals`, `BlackHolePhysics`, `BlackHoleVisualGlitch`, `GameZone`, `IncrementManager`, `DOTween`. |
| **Debug Logs** | N/A |

#### **BlackHolePhysics.cs** (Physics Attraction)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Handles gravity attraction forces and event horizon consumption. Excludes the Shop machine from consumption. |
| **Logic & Algorithms** | **Hybrid Pull**: Applies a constant force to dynamic Rigidbody2D balls, and translates kinematic Rigidbody2D machines directly using `MovePosition`. Consumption is checked center-to-center (`distanceToCenter <= GRadius`) while attraction range is checked edge-to-edge. Reads `CurrentAttractPhysicsRadius` during overrides. |
| **Exposed Members** | `_attractForce` (float), `_attractRadiusOffset` (float), `_targetLayerMask` (LayerMask). |
| **Dependencies** | `BlackHole`, `Rigidbody2D`, `Collider2D`. |
| **Debug Logs** | N/A |

#### **BlackHoleVisuals.cs** (Visual Sync)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Controls visual scaling and parameters of the background Shapes Discs and SpriteRenderer distortion shaders. |
| **Logic & Algorithms** | Automatically synchronizes the main disc, background disc, and shaders using predefined offsets relative to `GRadius` when `OverrideMainDisc` and `OverrideAttractShader` are false. Features `SetAttractShaderRadius` to set the shader parameter directly using `MaterialPropertyBlock` instances to prevent cross-material leakage. Includes a self-healing child reference detection `AutoFindReferences()`. |
| **Exposed Members** | `_mainDiscOffset` (float), `_backgroundOffset` (float), `_shaderOffset` (float), `_attractShaderOffset` (float). |
| **Dependencies** | `Disc` (Shapes), `SpriteRenderer`, `MaterialPropertyBlock`, `BlackHole`. |
| **Debug Logs** | N/A |

#### **BlackHoleVisualGlitch.cs** (Spaghettification & Jitter)
| Feature | Details |
| :--- | :--- |
| **Purpose** | Applies spaghettification and jitter/glitch rendering to attracted entities. |
| **Logic & Algorithms** | Processes attracted entities inside the capture zone. Computes non-linear scale squashing/stretching based on target depth raised to `_shrinkPower`. Jitters scale and rotation at independent frequencies (`_glitchFrequencyBalls` / `_glitchFrequencyMachines`) to create organic glitches. |
| **Exposed Members** | `_maxGlitchIntensityBalls` (float), `_maxGlitchIntensityMachines` (float), `_glitchFrequencyBalls` (float), `_glitchFrequencyMachines` (float), `_shrinkPower` (float). |
| **Dependencies** | `BlackHole`, `BlackHolePhysics`. |
| **Debug Logs** | N/A |

---

## 3. Current State & Traceability

### Current State (Operational)
- **Object Pooling**: Fully functional and ready for stress testing.
- **Drag & Drop**: Smooth interaction with rotation and physics preservation.
- **Energy Topology**: Dynamic merging and splitting of networks works via proximity.
- **Ball Behaviors**: Base system finished, `BlueBallBehavior` serves as a stable reference.

### Technical Debt (KISS Audit)
1. **PressMachine Implementation**: `IEnergyConsumer` methods throw `NotImplementedException`.
2. **Camera Access**: `GameInputManager` uses `Camera.main` in a hot path. Requires caching.
3. **Machine Colliders**: Raycasting for dragging triggers `GetComponentInParent`, which may be slow if complex hierarchies are used.
4. **Energy Visuals**: Connection Gizmos are strictly for Editor. A runtime visual system (cables/glow) is missing.
5. **Unity References**: `DefaultExecutionOrder` on `EnergyManager` is a magic constant.

### Fix History (Detected Modifications)
- **2026-04-10**: Initialized `UNITY_DEVELOPMENT_RULES.md` and `DEVELOPMENT_LOG.md`.
- **Logic Sync**: Added `IsProcessing` state to `BallEntity` to prevent interaction during machine animations.
- **Energy Node Identification**: Fixed `EnergyManager` neighbor detection to support both `MachineEntity` and `BallEntity` (via behavior).
