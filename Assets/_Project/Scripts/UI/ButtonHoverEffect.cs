using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Components")]
    public RectTransform textRect;
    public CanvasGroup textCanvasGroup;

    [Header("Hover Settings")]
    public float moveDistance = 20f;
    public float scalePunch = 1.05f;   // Grossit de 5% par rapport à sa taille de base
    public float duration = 0.25f;

    private Vector2 originalTextPos;
    private Vector3 baseScale; // Va stocker la vraie taille de ton bouton
    private RectTransform buttonRect;
    private bool hasOriginalPos = false;

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    // On utilise OnEnable pour choper la taille une fois que le MenuController 
    // a fini de lui donner sa taille finale de déploiement.
    private void OnEnable()
    {
        // On attend la fin de la frame pour lire la vraie taille scale après DOTween
        DOVirtual.DelayedCall(0.01f, () => {
            if (this == null || buttonRect == null) return;

            baseScale = buttonRect.localScale;

            if (textRect != null && !hasOriginalPos)
            {
                originalTextPos = textRect.anchoredPosition;
                hasOriginalPos = true;
            }
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textRect.DOKill();
        buttonRect.DOKill();

        // Glissement du texte
        textRect.DOAnchorPosX(originalTextPos.x + moveDistance, duration).SetEase(Ease.OutCubic);

        // Grossissement basé sur sa VRAIE taille (baseScale)
        buttonRect.DOScale(baseScale * scalePunch, duration).SetEase(Ease.OutCubic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textRect.DOKill();
        buttonRect.DOKill();

        // Le texte et le bouton reviennent à leur état d'origine exact
        textRect.DOAnchorPosX(originalTextPos.x, duration).SetEase(Ease.OutCubic);
        buttonRect.DOScale(baseScale, duration).SetEase(Ease.OutCubic);
    }

    private void OnDisable()
    {
        textRect.DOKill();
        buttonRect.DOKill();

        // On remet les valeurs d'origine proprement pour la prochaine ouverture
        if (hasOriginalPos && textRect != null) textRect.anchoredPosition = originalTextPos;
        if (buttonRect != null && baseScale != Vector3.zero) buttonRect.localScale = baseScale;
    }
}