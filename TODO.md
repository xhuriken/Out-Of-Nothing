# TODO - Game Cursor Implementation

## 1. Core Mechanics
- [x] Create `GameCursor.cs` script.
- [x] Make it a Singleton to be easily accessible.
- [x] Hide the default system cursor.
- [x] Implement smooth following logic using `Vector3.SmoothDamp` with adjustable parameters (speed, smoothing time).

## 2. Visuals & Integration
- [x] Add Freya Holmér's Shapes components (`Disc` with DOTween integration) to the `GameCursor` GameObject.
- [x] Implement DOTween animations (`PlayClickAnimation`, `SetDragAnimation`) that are resistant to spamming.
- [x] Tweak visual parameters (thickness, scale punching) based on game state (e.g., interacting, dragging).
- [x] Ensure it renders on top of everything (UI layer or sorting order).
