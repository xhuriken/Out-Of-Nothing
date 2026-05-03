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
| **Purpose** | Manages physical representation and delegates logic to `BallBehavior`. |
| **Logic & Algorithms** | Uses `DOTween` for scaling animations. Synchronizes `Shapes.Disc` visuals with `BallDataSO`. |
| **Exposed Members** | `_data` (BallDataSO), `_renderer` (Disc), `_dragForceMultiplier` (float). |
| **Dependencies** | `Rigidbody2D`, `CircleCollider2D`, `BallDataSO`. |
| **Debug Logs** | `Duplicating {id}`, `Trying to drag ball {id}`. |

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
