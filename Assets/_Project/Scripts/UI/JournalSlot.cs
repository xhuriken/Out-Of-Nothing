using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class JournalSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Structure")]
    public Transform previewContainer;
    public Image questionMarkImage;

    [Header("Animations Survol (Hover) du SLOT")]
    public float hoverScale = 1.15f;
    public float hoverDuration = 0.2f;
    public Ease hoverEase = Ease.OutBack;

    private GameObject currentPreview;
    private Vector3 originalSlotScale;

    // --- CORRECTION : ÉCHELLE DE PRÉVISUALISATION ---
    // On force l'échelle cible de la prévisualisation à (1,1,1) au repos.
    private readonly Vector3 targetPreviewScale = Vector3.one;

    private void Awake()
    {
        // On sauvegarde l'échelle d'origine de ton SLOT parent (habituellement 1,1,1)
        originalSlotScale = transform.localScale;
    }

    public void Setup(ItemData data)
    {
        // Nettoyage avant réutilisation
        if (currentPreview != null) Destroy(currentPreview);

        if (data.previewPrefab != null)
        {
            currentPreview = Instantiate(data.previewPrefab, previewContainer);
            RectTransform previewRt = currentPreview.GetComponent<RectTransform>();
            if (previewRt != null)
            {
                previewRt.anchoredPosition = Vector2.zero;
                // --- CORRECTION ---
                // On force l'échelle initiale à 1 pour être sûr que la prévisualisation
                // est visible dès l'instanciation.
                previewRt.localScale = targetPreviewScale;
            }
        }

        // --- GESTION DES ÉTATS ---
        if (data.isUnlocked)
        {
            if (currentPreview != null) currentPreview.SetActive(true);
            questionMarkImage.gameObject.SetActive(false);
        }
        else
        {
            if (currentPreview != null) currentPreview.SetActive(false);
            questionMarkImage.gameObject.SetActive(true);
        }

        // --- ANIMATION D'APPARITION (Pop-in) DU SLOT ---
        // Le slot parent grandit, la prévisualisation reste fixe à sa taille cible
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(originalSlotScale, 0.4f)
            .SetEase(Ease.OutBack)
            .SetDelay(Random.Range(0f, 0.15f));
    }

    // --- DETECTION SOURIS : ENTRÉE ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        // Grossissement du SLOT (pas de la prévisualisation)
        transform.DOScale(originalSlotScale * hoverScale, hoverDuration).SetEase(hoverEase);

     
    }

    // --- DETECTION SOURIS : SORTIE ---
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        // Le SLOT reprend sa taille d'origine
        transform.DOScale(originalSlotScale, hoverDuration).SetEase(Ease.OutCubic);

    }

    private void OnDisable()
    {
        // Sécurité DOTween
        transform.DOKill();
        if (currentPreview != null) currentPreview.transform.DOKill();
    }
}