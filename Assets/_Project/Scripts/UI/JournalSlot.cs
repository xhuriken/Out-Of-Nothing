using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // INDISPENSABLE pour le clic et le survol
using DG.Tweening;

public class JournalSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Structure")]
    public Transform previewContainer;
    public Image questionMarkImage;

    [Header("Animations Survol (Hover)")]
    public float hoverScale = 1.15f;
    public float hoverDuration = 0.2f;
    public Ease hoverEase = Ease.OutBack;

    private GameObject currentPreview;
    private Vector3 originalSlotScale;
    private readonly Vector3 targetPreviewScale = Vector3.one;

    // Sauvegarde locale des données de ce slot
    private ItemData itemData;
    private JournalManager manager;

    private void Awake()
    {
        originalSlotScale = transform.localScale;
    }

    // On passe maintenant le manager en paramètre pour pouvoir l'avertir du clic
    public void Setup(ItemData data, JournalManager journalManager)
    {
        itemData = data;
        manager = journalManager;

        if (currentPreview != null) Destroy(currentPreview);

        if (data.previewPrefab != null)
        {
            // On utilise ", false" ici aussi pour que l'instanciation respecte le prefab
            currentPreview = Instantiate(data.previewPrefab, previewContainer, false);
            RectTransform previewRt = currentPreview.GetComponent<RectTransform>();
            if (previewRt != null)
            {
                previewRt.anchoredPosition = Vector2.zero;

                // --- LA CORRECTION EST ICI ---
                // Au lieu d'imposer Vector3.one, on va lire l'échelle configurée sur ton PREFAB visuel
                Vector3 originalPrefabScale = data.previewPrefab.transform.localScale;

                // On applique cette échelle d'origine pour que le visuel garde sa vraie taille dans le slot
                previewRt.localScale = originalPrefabScale;
            }
        }

        // Gestion des états (Débloqué / Bloqué)
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

        // Animation d'apparition du slot
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(originalSlotScale, 0.4f)
            .SetEase(Ease.OutBack)
            .SetDelay(Random.Range(0f, 0.15f));
    }

    // --- DETECTION DU CLIC ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // On n'autorise le clic que si l'objet est débloqué !
        if (itemData != null && itemData.isUnlocked && manager != null)
        {
            // Petit effet visuel de "pression" au clic
            transform.DOScale(originalSlotScale * 0.9f, 0.1f).OnComplete(() =>
            {
                transform.DOScale(originalSlotScale * hoverScale, 0.1f);
            });

            // On envoie les données de cet objet au manager du journal
            manager.SelectItem(itemData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemData != null && !itemData.isUnlocked) return; // Pas de survol si bloqué

        transform.DOKill();
        transform.DOScale(originalSlotScale * hoverScale, hoverDuration).SetEase(hoverEase);

       
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalSlotScale, hoverDuration).SetEase(Ease.OutCubic);

       
    }

    private void OnDisable()
    {
        transform.DOKill();
        if (currentPreview != null) currentPreview.transform.DOKill();
    }
}