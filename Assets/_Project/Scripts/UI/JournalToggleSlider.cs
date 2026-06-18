using UnityEngine;
using DG.Tweening;

public class JournalToggleSlider : MonoBehaviour
{
    [Header("Configuration")]
    public JournalManager journalManager; // Référence à ton manager de journal
    public RectTransform bouleRect;       // Le RectTransform de cette boule

    [Header("Réglages Positions")]
    [Tooltip("La position X locale quand la boule est à gauche (Mode Boules)")]
    public float leftPositionX = -50f;
    [Tooltip("La position X locale quand la boule est à droite (Mode Machines)")]
    public float rightPositionX = 50f;

    [Header("Animation")]
    public float animationDuration = 0.25f;
    public Ease animationEase = Ease.OutBack; // Un petit effet de ressort sympa

    private bool isAtRight = false; // False = Gauche (Balls), True = Droite (Machines)
    private bool isAnimating = false;

    private Vector3 initialScale = Vector3.one;
    private bool hasCachedScale = false;

    private void Awake()
    {
        CacheScaleIfNeeded();
    }

    private void CacheScaleIfNeeded()
    {
        if (hasCachedScale) return;
        if (bouleRect == null) bouleRect = GetComponent<RectTransform>();
        if (bouleRect != null)
        {
            initialScale = bouleRect.localScale;
            hasCachedScale = true;
        }
    }

    private void Start()
    {
        CacheScaleIfNeeded();
     

        // On force la position de départ à gauche au lancement
        if (bouleRect != null)
        {
            bouleRect.anchoredPosition = new Vector2(leftPositionX, bouleRect.anchoredPosition.y);
        }
    }

    private void OnEnable()
    {
        ResetSliderState();
    }

    private void OnDisable()
    {
        ResetSliderState();
    }

    public void ResetSliderState()
    {
        isAnimating = false;
        CacheScaleIfNeeded();
        if (bouleRect != null)
        {
            bouleRect.DOKill();
            float targetX = isAtRight ? rightPositionX : leftPositionX;
            bouleRect.anchoredPosition = new Vector2(targetX, bouleRect.anchoredPosition.y);
            bouleRect.localScale = initialScale;
        }
    }

    // Cette fonction sera appelée quand on clique sur la boule
    public void ToggleSlider()
    {
        Debug.Log("press");
        // Sécurité pour éviter le spam pendant l'animation
        if (isAnimating) return;
        
        isAnimating = true;
        bouleRect.DOKill();

        // On inverse l'état
        isAtRight = !isAtRight;

        // On calcule la destination
        float targetX = isAtRight ? rightPositionX : leftPositionX;

        // Animation de déplacement de la boule
        bouleRect.DOAnchorPosX(targetX, animationDuration)
            .SetEase(animationEase)
            .OnComplete(() => {
                isAnimating = false;

                // Une fois la boule arrivée, on demande au manager de changer de mode
                if (isAtRight)
                {
                    journalManager.OnClickMachinesButton();
                }
                else
                {
                    journalManager.OnClickBallsButton();
                }
            });

        // Optionnel : Un mini effet de "stretch" (écrasement) proportionnel à son scale d'origine
        bouleRect.DOScaleX(initialScale.x * 1.2f, animationDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
    }
}