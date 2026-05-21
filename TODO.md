# TODO - Crafting System Polish & Interaction Fixes

## 1. Feature Implementation
- [x] Allow Drag & Drop on currently selected craft balls by relaxing the `_isProcessing` restriction in `BallEntity.OnDragStart`.
- [x] Integrate `_additionalPreviewObject` support in `CraftingManager.cs` to mirror the appearance/disappearance lifecycle of the shadow preview.
- [x] Fix determinism bug in cellular duplication by storing and restoring `Random.state` around the `NetworkServer` Gizmos inside `EnergyManager.cs`.

## 2. Bug Fixes
- [x] Implement rigorous NaN checking on `Mouse.current.position.ReadValue()` across input/cursor scripts to prevent `ScreenToWorldPoint` frustum errors caused by the new Input System.
- [x] Prevent `DOTween` sequence errors in `GameCursor.cs` by decoupling infinite vibration loops from sequenced tweens.
- [x] Prevent `DOPunchScale` infinite growth bug when spam-clicking objects in `CraftingManager.cs` by safely executing `DOKill()` and reverting the scale before animation.
- [x] Ensure `_additionalPreviewObject` prefabs are properly instantiated in `CraftingManager.Awake()` rather than directly altering asset data.

## 3. Verification & Safety
- [x] Compile the C# project using `dotnet build` to ensure 0 errors.
- [x] Verify that all comments are in English.
- [x] Update `DEVELOPMENT_LOG.md` with detailed records of the implemented feature and technical rationale.