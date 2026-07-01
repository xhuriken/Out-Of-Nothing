using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation Settings")]
    [SerializeField] private float _hoverScaleMultiplier = 1.08f;
    [SerializeField] private float _clickScaleMultiplier = 0.92f;
    [SerializeField] private float _animationDuration = 0.25f;
    [SerializeField] private Ease _hoverEase = Ease.OutBack;
    [SerializeField] private Ease _exitEase = Ease.OutQuad;

    [Header("Tilt / Angle Animation Settings")]
    [SerializeField] private bool _enableTilt = true;
    [SerializeField] private float _maxTiltAngle = 1.5f;

    private Vector3 _originalScale;
    private Vector3 _originalEuler;
    private bool _isHovered = false;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _originalEuler = transform.localEulerAngles;
    }

    private void OnDisable()
    {
        // Reset state immediately if disabled
        transform.DOKill();
        transform.localScale = _originalScale;
        transform.localEulerAngles = _originalEuler;
        _isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        transform.DOKill();

        // 1. Elastic/bounce scale up
        transform.DOScale(_originalScale * _hoverScaleMultiplier, _animationDuration)
            .SetEase(_hoverEase)
            .SetUpdate(true); // Works even when paused

        // 2. Soft organic rotation tilt
        if (_enableTilt)
        {
            float randomTilt = Random.Range(-_maxTiltAngle, _maxTiltAngle);
            transform.DOLocalRotate(new Vector3(0f, 0f, _originalEuler.z + randomTilt), _animationDuration)
                .SetEase(Ease.OutSine)
                .SetUpdate(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        transform.DOKill();

        // Smoothly restore scale and rotation
        transform.DOScale(_originalScale, _animationDuration * 0.8f)
            .SetEase(_exitEase)
            .SetUpdate(true);

        transform.DOLocalRotate(_originalEuler, _animationDuration * 0.8f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();

        // Squish button down on click
        transform.DOScale(_originalScale * _clickScaleMultiplier, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();

        if (_isHovered)
        {
            // Bounce back to hover size
            transform.DOScale(_originalScale * _hoverScaleMultiplier, _animationDuration)
                .SetEase(Ease.OutElastic)
                .SetUpdate(true);
        }
        else
        {
            // Reset to original size
            transform.DOScale(_originalScale, _animationDuration)
                .SetEase(_exitEase)
                .SetUpdate(true);
        }
    }
}
