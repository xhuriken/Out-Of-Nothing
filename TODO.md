# TODO - Game Cursor Implementation

## 1. Core Mechanics
- [x] Create `GameCursor.cs` script.
- [x] Make it a Singleton to be easily accessible.
- [x] Hide the default system cursor.
- [x] Implement smooth following logic using `Vector3.SmoothDamp` with adjustable parameters (speed, smoothing time).

## 2. Visuals & Integration
- [ ] Add Freya Holmér's Shapes components (e.g., `ShapeRenderer`, `Disc`, `Line`) to the `GameCursor` GameObject in the scene.
- [ ] Tweak visual parameters (color, thickness) based on game state (e.g., interacting, dragging).
- [ ] Ensure it renders on top of everything (UI layer or sorting order).
