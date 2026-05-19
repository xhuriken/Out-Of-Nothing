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

    private void Start()
    {
        if (bouleRect == null) bouleRect = GetComponent<RectTransform>();

        // On force la position de départ à gauche au lancement
        bouleRect.anchoredPosition = new Vector2(leftPositionX, bouleRect.anchoredPosition.y);
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

        // Optionnel : Un mini effet de "stretch" (écrasement) pendant qu'elle bouge pour faire plus "jus de jeu"
        bouleRect.DOScaleX(1.2f, animationDuration * 0.5f).SetLoops(2, LoopType.Yoyo);
    }
}