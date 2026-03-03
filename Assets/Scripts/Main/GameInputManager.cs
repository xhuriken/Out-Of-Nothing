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

    private void Awake()
    {
        _mainCamera = Camera.main; // TODO: STOP USING CAMERA.MAIN, CACHE THAT IN A SINGLETON OR PASS IT VIA INSPECTOR
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleClick();
        }
    }

    public void OnDrag(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //Start drag
        }
        if(context.canceled)
        {
            //End drag
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
}