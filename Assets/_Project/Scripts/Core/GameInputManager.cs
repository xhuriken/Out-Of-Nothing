using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

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

    private Camera _mainCamera;
    private IDraggable _currentDraggedObject;
    private BallShop _hoveredBallShop;

    public IDraggable CurrentDraggedObject => _currentDraggedObject;
    public float CursorActionRadius => _cursorActionRadius;
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
            return;
        }

        if (Application.isPlaying)
        {
            UpdateHoverState();
        }

        if (_currentDraggedObject != null)
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
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (MenuController.Instance != null && MenuController.Instance.IsOpen) return;

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
}