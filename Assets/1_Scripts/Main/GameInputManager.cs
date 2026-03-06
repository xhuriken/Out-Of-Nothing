using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centralized input manager to handle clicks and prevent spam.
/// </summary>
public class GameInputManager : MonoBehaviour
{
    [SerializeField]
    private LayerMask _ballLayerMask;

    private Camera _mainCamera;
    private IDraggable _currentDraggedObject;
    private void Awake()
    {
        _mainCamera = Camera.main; // TODO: STOP USING CAMERA.MAIN, CACHE THAT IN A SINGLETON OR PASS IT VIA INSPECTOR
    }

    private void Update()
    {
        if (_currentDraggedObject != null)
        {
            _currentDraggedObject.OnDragUpdate(GetMouseWorldPosition());
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed && _currentDraggedObject == null)
        {
            HandleClick();
        }
    }

    /// <summary>
    /// Handles the Drag input action. Starts or ends the drag state.
    /// </summary>
    public void OnDrag(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 mousePosition = GetMouseWorldPosition();
            LayerMask allMask = ~0;
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, 0f, allMask);

            if (hit.collider != null && hit.collider.TryGetComponent(out IDraggable draggable))
            {
                _currentDraggedObject = draggable;
                _currentDraggedObject.OnDragStart();
            }
        }
        else if (context.canceled)
        {
            if (_currentDraggedObject != null)
            {
                _currentDraggedObject.OnDragEnd();
                _currentDraggedObject = null;
            }
        }
    }

    /// <summary>
    /// Handles the Scroll input action to rotate the currently dragged object.
    /// </summary>
    public void OnScroll(InputAction.CallbackContext context)
    {
        if (_currentDraggedObject != null && context.performed)
        {
            // The scroll wheel is the Y axis
            float scrollValue = context.ReadValue<Vector2>().y;

            //if (Mathf.Abs(scrollValue) > 0.01f)
            //{
            _currentDraggedObject.OnDragRotate(scrollValue);
            //}
        }
    }

    public void OnCraft(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //Start Crafting logic
        }
    }

    public void OnCodex(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //Open Condex UI
        }
    }

    /// <summary>
    /// Casts a ray to detect clickable entities.
    /// </summary>
    private void HandleClick()
    {
        Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Single Raycast optimized by LayerMask
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, 0f, _ballLayerMask);

        if (hit.collider != null && hit.collider.TryGetComponent(out BallEntity ball))
        {
            ball.ReceiveClick();
        }
    }

    #region Helpers
    /// <summary>
    /// Converts current mouse screen position to world coordinates.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        return _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }
    #endregion

}