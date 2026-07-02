using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Centralized input manager to handle clicks and prevent spam.
/// </summary>
public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }

    [SerializeField]
    private LayerMask _ballLayerMask;

    [Header("Cursor Settings")]
    [SerializeField]
    [Tooltip("The action/interaction radius for the custom cursor (in world units).")]
    private float _cursorActionRadius = 0.5f;

    [Header("Multi-Selection Settings")]
    [SerializeField]
    private bool _isMultiSelectionUnlocked = true;

    private Camera _mainCamera;
    private IDraggable _currentDraggedObject;
    private BallShop _hoveredBallShop;

    private bool _isSelecting;
    private bool _isMultiDragging;
    private Vector2 _selectionStartMousePos;
    private GameObject _selectionBoxObj;
    private UnityEngine.UI.Image _selectionBoxImage;
    private List<IDraggable> _selectedObjects = new List<IDraggable>();
    private Dictionary<IDraggable, Vector2> _multiDragOffsets = new Dictionary<IDraggable, Vector2>();
    private float _unlockCheckTimer;

    public IDraggable CurrentDraggedObject => _currentDraggedObject;
    public float CursorActionRadius => _cursorActionRadius;
    public bool IsMultiSelectionUnlocked
    {
        get => _isMultiSelectionUnlocked;
        set => _isMultiSelectionUnlocked = value;
    }

    private void Awake()
    { 
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _mainCamera = Camera.main; // TODO: STOP USING CAMERA.MAIN, CACHE THAT IN A SINGLETON OR PASS IT VIA INSPECTOR
    }

    private void Update()
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen)
        {
            if (_currentDraggedObject != null)
            {
                ForceDrop();
            }
            ClearSelection();
            return;
        }

        if (Application.isPlaying)
        {
            UpdateHoverState();
            UpdateMultiSelectionUnlockState();
            HandleMultiSelectionInput();
        }

        if (_currentDraggedObject != null && !_isMultiDragging && !_isSelecting)
        {
            if (_currentDraggedObject as UnityEngine.Object == null)
            {
                ForceDrop();
                return;
            }
            _currentDraggedObject.OnDragUpdate(GetCursorWorldPosition());
        }
    }

    private void OnDisable()
    {
        if (_hoveredBallShop != null)
        {
            _hoveredBallShop.SetHovered(false);
            _hoveredBallShop = null;
        }
        ClearSelection();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;
        if (_isSelecting || _isMultiDragging) return;

        if (context.performed)
        {
            // If crafting is active, route the click ONLY to the crafting system
            if (CraftingManager.Instance != null && CraftingManager.Instance.IsCrafting)
            {
                GameCursor.Instance?.PlayClickAnimation();
                CraftingManager.Instance.OnClickSelection();
                return;
            }

            if (_currentDraggedObject == null || _currentDraggedObject as UnityEngine.Object == null)
            {
                GameCursor.Instance?.PlayClickAnimation();
                HandleClick();
            }
        }
    }

    /// <summary>
    /// Handles the Drag input action. Starts or ends the drag state.
    /// </summary>
    public void OnDrag(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;
        if (_isSelecting || _isMultiDragging) return;

        // Allow dragging even in crafting mode
        if (context.performed)
        {
            Vector2 cursorPos = GetCursorWorldPosition();
            IDraggable draggable = FindClosestTarget<IDraggable>(cursorPos, _cursorActionRadius, ~0);

            if (draggable != null)
            {
                if (draggable.OnDragStart())
                {
                    _currentDraggedObject = draggable;
                    GameCursor.Instance?.SetDragAnimation(true);
                }
            }
        }
        else if (context.canceled)
        {
            ForceDrop();
        }
    }

    /// <summary>
    /// Handles the Scroll input action. Rotates the currently dragged object or zooms the camera.
    /// </summary>
    public void OnScroll(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;

        if (context.performed)
        {
            float scrollValue = 0f;
            if (context.valueType == typeof(Vector2))
            {
                scrollValue = context.ReadValue<Vector2>().y;
            }
            else
            {
                scrollValue = context.ReadValue<float>();
            }

            if (_currentDraggedObject != null)
            {
                if (_currentDraggedObject as UnityEngine.Object == null)
                {
                    ForceDrop();
                    return;
                }
                
                _currentDraggedObject.OnDragRotate(scrollValue);
            }
            else
            {
                // If not dragging any object, scroll zooms the camera
                CameraController.Instance?.AdjustZoom(scrollValue);
            }
        }
    }

    public void OnCraft(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;

        if (context.performed)
        {
            // Toggle mode: if it was hold, now it's toggle.
            // We only care about performed for toggle.
            bool newState = !(CraftingManager.Instance != null && CraftingManager.Instance.IsCrafting);
            CraftingManager.Instance?.OnCraftInput(newState);
        }
    }

    public void OnCodex(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;

        if (context.performed)
        {
            //Open Condex UI
        }
    }

    /// <summary>
    /// Scans around the custom cursor to detect clickable entities (slots, balls, or shop).
    /// </summary>
    private void HandleClick()
    {
        Vector2 cursorPos = GetCursorWorldPosition();

        // 1. Check for BallShop slot click first (only registers if slot is fully interactive/spawned)
        BallShop ballShop = FindClosestTarget<BallShop>(cursorPos, _cursorActionRadius, ~0);
        if (ballShop != null && ballShop.ParentShop != null)
        {
            if (ballShop.IsInteractive)
            {
                ballShop.ParentShop.OnBallSelected(ballShop);
                return;
            }
        }

        // 2. Check for BallEntity click
        BallEntity ball = FindClosestTarget<BallEntity>(cursorPos, _cursorActionRadius, _ballLayerMask);
        if (ball != null)
        {
            ball.ReceiveClick();
            return;
        }

        // 3. Check for Shop click to toggle its interface
        Shop shop = FindClosestTarget<Shop>(cursorPos, _cursorActionRadius, ~0);
        if (shop != null)
        {
            shop.ToggleShopActiveState();
            return;
        }
    }

    #region Helpers
    /// <summary>
    /// Returns the world position of the game cursor.
    /// Falls back to the screen-to-world position of the system mouse if the GameCursor instance is missing.
    /// </summary>
    public Vector2 GetCursorWorldPosition()
    {
        if (GameCursor.Instance != null)
        {
            return GameCursor.Instance.transform.position;
        }
        return GetMouseWorldPosition();
    }

    /// <summary>
    /// Scans a circular area around the center and returns the closest active component of type T.
    /// </summary>
    public T FindClosestTarget<T>(Vector2 center, float radius, LayerMask mask) where T : class
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius, mask);
        if (colliders == null || colliders.Length == 0) return null;

        T closestComponent = null;
        float closestDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col == null || !col.enabled) continue;

            // Exclude proxy colliders from being targets (since they only forward collision events)
            if (col.GetComponent<MachineColliderProxy>() != null) continue;

            T comp = col.GetComponentInParent<T>();
            if (comp != null)
            {
                // Check active state
                if (comp is MonoBehaviour mb && (!mb.gameObject.activeInHierarchy || !mb.enabled))
                {
                    continue;
                }

                // Exclude BallShop slots that are actively hiding/retracting
                if (comp is BallShop bs && bs.IsHiding)
                {
                    continue;
                }

                float dist = Vector2.Distance(center, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestComponent = comp;
                }
            }
        }
        return closestComponent;
    }

    /// <summary>
    /// Scans for the closest BallShop slot under the custom cursor and updates its hover state.
    /// </summary>
    private void UpdateHoverState()
    {
        if (_currentDraggedObject != null)
        {
            if (_hoveredBallShop != null)
            {
                _hoveredBallShop.SetHovered(false);
                _hoveredBallShop = null;
            }
            return;
        }

        Vector2 cursorPos = GetCursorWorldPosition();
        
        // Find closest BallShop slot within the cursor action radius
        BallShop closestBallShop = FindClosestTarget<BallShop>(cursorPos, _cursorActionRadius, ~0);

        if (closestBallShop != _hoveredBallShop)
        {
            if (_hoveredBallShop != null)
            {
                _hoveredBallShop.SetHovered(false);
            }

            _hoveredBallShop = closestBallShop;

            if (_hoveredBallShop != null)
            {
                _hoveredBallShop.SetHovered(true);
            }
        }
    }

    /// <summary>
    /// Clears the currently tracked hovered BallShop slot and resets its hover visual.
    /// Called when a successful purchase retracts the slots.
    /// </summary>
    public void ClearHoveredSlot()
    {
        if (_hoveredBallShop != null)
        {
            _hoveredBallShop.SetHovered(false);
            _hoveredBallShop = null;
        }
    }

    /// <summary>
    /// Converts current mouse screen position to world coordinates.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        if (float.IsNaN(mouseScreenPos.x) || float.IsNaN(mouseScreenPos.y)) return Vector2.zero;

        return _mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
    #endregion

    /// <summary>
    /// Forces the currently dragged object to be dropped. 
    /// Can be called externally by machines claiming a ball.
    /// </summary>
    public void ForceDrop(IDraggable specificObject = null)
    {
        if (_currentDraggedObject != null)
        {
            if (specificObject == null || _currentDraggedObject == specificObject)
            {
                if (_currentDraggedObject as UnityEngine.Object != null)
                {
                    _currentDraggedObject.OnDragEnd();
                }
                
                _currentDraggedObject = null;
                GameCursor.Instance?.SetDragAnimation(false);
            }
        }
    }

    #region Multi-Selection Logic
    private void UpdateMultiSelectionUnlockState()
    {
        _unlockCheckTimer += Time.deltaTime;
        if (_unlockCheckTimer < 0.25f) return;
        _unlockCheckTimer = 0f;

        MachineEntity[] activeMachines = FindObjectsByType<MachineEntity>(FindObjectsSortMode.None);
        bool hasMachine = false;
        foreach (var machine in activeMachines)
        {
            if (machine != null && machine.gameObject.name.Replace("(Clone)", "").Trim() == "MultiSelectorMachine")
            {
                hasMachine = true;
                break;
            }
        }

        if (hasMachine != _isMultiSelectionUnlocked)
        {
            _isMultiSelectionUnlocked = hasMachine;
            if (!_isMultiSelectionUnlocked)
            {
                ClearSelection();
            }
        }
    }

    private void HandleMultiSelectionInput()
    {
        if (!_isMultiSelectionUnlocked) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 cursorPos = GetCursorWorldPosition();

        // 1. Check for starting selection box (Shift + Right Click)
        if (Keyboard.current.shiftKey.isPressed && Mouse.current.rightButton.wasPressedThisFrame && !_isMultiDragging)
        {
            _isSelecting = true;
            _selectionStartMousePos = mousePos;
            UpdateSelectionBox(mousePos);
            return;
        }

        // 2. While drawing selection box
        if (_isSelecting)
        {
            UpdateSelectionBox(mousePos);
            PerformSelectionOverlap(mousePos);

            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _isSelecting = false;
                if (_selectionBoxObj != null)
                {
                    _selectionBoxObj.SetActive(false);
                }
            }
            return;
        }

        // 3. Right Click clicked or drag check
        if (Mouse.current.rightButton.wasPressedThisFrame && !Keyboard.current.shiftKey.isPressed)
        {
            CleanSelectedObjects();

            // Check if clicked on a selected object to start multi-drag
            IDraggable hitDraggable = FindClosestTarget<IDraggable>(cursorPos, _cursorActionRadius, ~0);

            // Exclude NightmareBall
            if (hitDraggable is BallEntity ball && ball.Behavior is NightmareBall)
            {
                hitDraggable = null;
            }

            if (hitDraggable != null && _selectedObjects.Contains(hitDraggable))
            {
                ForceDrop(); // Clear any single-drag state before starting multi-drag!
                
                // Start Multi-Drag!
                _isMultiDragging = true;
                _multiDragOffsets.Clear();
                foreach (var draggable in _selectedObjects)
                {
                    if (draggable is MonoBehaviour mb && mb != null)
                    {
                        _multiDragOffsets[draggable] = (Vector2)mb.transform.position - cursorPos;
                        draggable.OnDragStart();
                    }
                }
            }
            else
            {
                // Clicked in empty space or unselected object -> Deselect all
                ClearSelection();
            }
        }

        // 4. While multi-dragging
        if (_isMultiDragging)
        {
            CleanSelectedObjects();

            if (Mouse.current.rightButton.isPressed)
            {
                foreach (var draggable in _selectedObjects)
                {
                    if (draggable != null && _multiDragOffsets.TryGetValue(draggable, out Vector2 offset))
                    {
                        draggable.OnDragUpdate(cursorPos + offset);
                    }
                }
            }

            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                foreach (var draggable in _selectedObjects)
                {
                    if (draggable != null)
                    {
                        draggable.OnDragEnd();
                    }
                }
                _isMultiDragging = false;
                _multiDragOffsets.Clear();
                ForceDrop(); // Clear any trailing/stuck drag state on release!
            }
        }
    }

    private void PerformSelectionOverlap(Vector2 currentMousePos)
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Vector2 startWorld = _mainCamera.ScreenToWorldPoint(_selectionStartMousePos);
        Vector2 currentWorld = _mainCamera.ScreenToWorldPoint(currentMousePos);

        float minX = Mathf.Min(startWorld.x, currentWorld.x);
        float maxX = Mathf.Max(startWorld.x, currentWorld.x);
        float minY = Mathf.Min(startWorld.y, currentWorld.y);
        float maxY = Mathf.Max(startWorld.y, currentWorld.y);

        Vector2 boxCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 boxSize = new Vector2(maxX - minX, maxY - minY);

        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        HashSet<IDraggable> foundDraggables = new HashSet<IDraggable>();

        foreach (var col in colliders)
        {
            if (col == null || !col.enabled) continue;

            // Skip proxy colliders
            if (col.GetComponent<MachineColliderProxy>() != null) continue;

            IDraggable draggable = col.GetComponentInParent<IDraggable>();
            if (draggable != null)
            {
                if (draggable is MonoBehaviour mb && (!mb.gameObject.activeInHierarchy || !mb.enabled))
                {
                    continue;
                }

                // Exclude NightmareBall
                if (draggable is BallEntity ball && ball.Behavior is NightmareBall)
                {
                    continue;
                }

                foundDraggables.Add(draggable);
            }
        }

        // Remove highlights of things no longer in selection
        foreach (var oldDraggable in _selectedObjects)
        {
            if (oldDraggable != null && !foundDraggables.Contains(oldDraggable))
            {
                if (oldDraggable is MonoBehaviour mb && mb != null)
                {
                    var highlight = mb.GetComponent<SelectionHighlight>();
                    if (highlight != null) Destroy(highlight);
                }
            }
        }

        // Add highlights to new things
        foreach (var newDraggable in foundDraggables)
        {
            if (newDraggable is MonoBehaviour mb && mb != null)
            {
                if (mb.GetComponent<SelectionHighlight>() == null)
                {
                    mb.gameObject.AddComponent<SelectionHighlight>();
                }
            }
        }

        _selectedObjects.Clear();
        _selectedObjects.AddRange(foundDraggables);
    }

    private void CreateSelectionBoxUI()
    {
        Canvas canvas = FindMainCanvas();
        if (canvas == null) return;

        _selectionBoxObj = new GameObject("SelectionBoxUI");
        _selectionBoxObj.transform.SetParent(canvas.transform, false);
        
        _selectionBoxImage = _selectionBoxObj.AddComponent<UnityEngine.UI.Image>();
        _selectionBoxImage.color = new Color(1f, 0.92f, 0.016f, 0.12f); // Weak yellow fill

        var outline = _selectionBoxObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1f, 0.92f, 0.016f, 0.85f); // Yellow outline border
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = _selectionBoxObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        _selectionBoxObj.SetActive(false);
    }

    private void UpdateSelectionBox(Vector2 currentMousePos)
    {
        if (_selectionBoxObj == null)
        {
            CreateSelectionBoxUI();
        }

        if (_selectionBoxObj == null) return;

        _selectionBoxObj.SetActive(true);

        Canvas canvas = FindMainCanvas();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localStart, localCurrent;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, _selectionStartMousePos, canvas.worldCamera, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, currentMousePos, canvas.worldCamera, out localCurrent);

        float minX = Mathf.Min(localStart.x, localCurrent.x);
        float maxX = Mathf.Max(localStart.x, localCurrent.x);
        float minY = Mathf.Min(localStart.y, localCurrent.y);
        float maxY = Mathf.Max(localStart.y, localCurrent.y);

        RectTransform rect = _selectionBoxObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        rect.sizeDelta = new Vector2(maxX - minX, maxY - minY);
    }

    public void ClearSelection()
    {
        CleanSelectedObjects();
        foreach (var draggable in _selectedObjects)
        {
            if (draggable is MonoBehaviour mb && mb != null)
            {
                var highlight = mb.GetComponent<SelectionHighlight>();
                if (highlight != null)
                {
                    Destroy(highlight);
                }
            }
        }
        _selectedObjects.Clear();
        _multiDragOffsets.Clear();
    }

    private void CleanSelectedObjects()
    {
        for (int i = _selectedObjects.Count - 1; i >= 0; i--)
        {
            var obj = _selectedObjects[i];
            if (obj == null || (obj is MonoBehaviour mb && mb == null))
            {
                _selectedObjects.RemoveAt(i);
            }
        }
    }

    private Canvas FindMainCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        // Phase 1: Prioritize exact matches for "Canvas", "HUD", or "HUDCanvas" with ScreenSpaceOverlay
        foreach (Canvas c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (c.name == "Canvas" || c.name == "HUD" || c.name == "HUDCanvas")
                {
                    return c;
                }
            }
        }

        // Phase 2: Prioritize standard UI/HUD canvases (contains HUD, Main, or UI) with ScreenSpaceOverlay
        foreach (Canvas c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (c.name.Contains("UI") || c.name.Contains("HUD") || c.name.Contains("Main"))
                {
                    return c;
                }
            }
        }

        // Phase 3: Fallback to first ScreenSpaceOverlay canvas found
        foreach (Canvas c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }

        // Phase 4: Fallback to any active canvas in the hierarchy
        foreach (Canvas c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy)
            {
                return c;
            }
        }

        return null;
    }
    #endregion
}