using UnityEngine;
using UnityEngine.UI;
using TMPro; // Indispensable pour tes textes
using System.Collections.Generic;
using DG.Tweening;

public class JournalManager : MonoBehaviour
{
    [Header("Menu Controller Link")]
    public MenuController menuController; // Référence au script principal

    [Header("Configuration UI - Grille Principale (Panel 1)")]
    public Transform gridLayoutGroup;
    public GameObject slotPrefab;

    [Header("Configuration UI - Détails (Panel 2)")]
    public GameObject titreJournal;       // L'objet texte "JOURNAL"
    public GameObject zoneDetails;         // Un parent vide unique qui contient (Nom, Desc, Image, Craft) pour les masquer/afficher d'un coup
    public TextMeshProUGUI textNom;
    public TextMeshProUGUI textDescription;
    public Transform previewContainerPanel2; // Là où on va instancier le visuel en gros
    public Transform craftGridLayoutGroup;  // La grille pour les slots de craft
    public GameObject craftSlotPrefab;      // Le prefab utilisé pour afficher les ingrédients (peut être le même slotPrefab)

    [Header("Données du Jeu")]
    public List<ItemData> allItems;

    private List<JournalSlot> spawnedSlots = new List<JournalSlot>();
    private List<GameObject> spawnedCraftSlots = new List<GameObject>();
    private GameObject currentLargePreview;
    private ItemData currentSelectedItem;

    private void Start()
    {
        // Au tout début, on affiche le titre "Journal" et on cache les détails
        titreJournal.SetActive(true);
        zoneDetails.SetActive(false);
        ShowCategory(ItemData.ItemType.Ball);
    }

    public void OnClickBallsButton()
    {
        ResetPanel2(); // Si on change de catégorie, on réinitialise le panneau du haut
        ShowCategory(ItemData.ItemType.Ball);
    }

    public void OnClickMachinesButton()
    {
        ResetPanel2();
        ShowCategory(ItemData.ItemType.Machine);
    }

    private void ShowCategory(ItemData.ItemType targetType)
    {
        foreach (Transform child in gridLayoutGroup)
        {
            Destroy(child.gameObject);
        }
        spawnedSlots.Clear();

        List<ItemData> filteredItems = allItems.FindAll(item => item.type == targetType);

        for (int i = 0; i < filteredItems.Count; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, gridLayoutGroup);
            JournalSlot slot = newSlotObj.GetComponent<JournalSlot>();
            spawnedSlots.Add(slot);

            // On lui passe "this" (ce manager) en plus pour activer la détection du clic
            slot.Setup(filteredItems[i], this);
        }
    }

    public void SelectItem(ItemData data)
    {
        if (currentSelectedItem == data) return;
        currentSelectedItem = data;

        // 1. Nettoyage
        if (currentLargePreview != null) Destroy(currentLargePreview);
        foreach (GameObject slot in spawnedCraftSlots) Destroy(slot);
        spawnedCraftSlots.Clear();

        // 2. Attribution des textes
        textNom.text = data.itemName;
        textDescription.text = data.description;

        // 3. GRANDE PREVIEW : Conservation STRICTE du scale du prefab
        if (data.previewPrefab != null)
        {
            // --- CORRECTIF ICI ---
            // Le ", false" à la fin dit à Unity : "Ne touche pas au scale, laisse celui du prefab !"
            currentLargePreview = Instantiate(data.previewPrefab, previewContainerPanel2, false);

            RectTransform rt = currentLargePreview.GetComponent<RectTransform>();
            if (rt != null)
            {
                // On le recentre, mais on ne touche SURTOUT PLUS à son localScale
          
                rt.localScale = rt.localScale * 3.5f;
            }
        }

        // 4. Recette de craft
        UpdateCraftRecipes(data);

        // 5. Animation d'apparition
        if (titreJournal.activeSelf)
        {
            titreJournal.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() => {
                titreJournal.SetActive(false);
                zoneDetails.transform.localScale = Vector3.one;
                zoneDetails.SetActive(true);

                zoneDetails.transform.DOKill();
                zoneDetails.transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0.05f), 0.25f, 5, 1f);
            });
        }
        else
        {
            zoneDetails.transform.DOKill();
            zoneDetails.transform.localScale = Vector3.one;
            zoneDetails.transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0.05f), 0.2f, 5, 1f);
        }

        if (menuController != null)
        {
            menuController.AnimateJournalDetailsOpen();
        }
    }

    private void UpdateCraftRecipes(ItemData data)
    {
        if (craftGridLayoutGroup == null) return;

        foreach (GameObject slotObj in spawnedCraftSlots)
        {
            if (slotObj != null) Destroy(slotObj);
        }
        spawnedCraftSlots.Clear();

        if (data.type == ItemData.ItemType.Machine && data.craftRecipe != null && data.craftRecipe.Count > 0)
        {
            craftGridLayoutGroup.gameObject.SetActive(true);

            foreach (ItemData ingredient in data.craftRecipe)
            {
                GameObject craftSlot = Instantiate(craftSlotPrefab, craftGridLayoutGroup);
                spawnedCraftSlots.Add(craftSlot);

                JournalSlot slotScript = craftSlot.GetComponent<JournalSlot>();
                if (slotScript != null)
                {
                    // On appelle le Setup qui va initialiser le prefab interne
                    slotScript.Setup(ingredient, null);

                    // IMPORTANT : On tue TOUS les tweens qui pourraient tourner sur ce slot
                    // et on force son échelle locale à (1,1,1)
                    slotScript.transform.DOKill();
                    slotScript.transform.localScale = Vector3.one;
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(craftGridLayoutGroup.GetComponent<RectTransform>());
        }
        else
        {
            craftGridLayoutGroup.gameObject.SetActive(false);
        }
    }



    public void ResetPanel2()
    {
        currentSelectedItem = null;

        // Nettoyage complet
        if (currentLargePreview != null) Destroy(currentLargePreview);
        foreach (GameObject slot in spawnedCraftSlots) Destroy(slot);
        spawnedCraftSlots.Clear();

        // On coupe TOUT le bloc de détails d'un coup
        zoneDetails.SetActive(false);

        // On réactive proprement le titre "JOURNAL" tout seul
        titreJournal.SetActive(true);
        titreJournal.transform.localScale = Vector3.one;

        // La mâchoire reprend sa position normale
        if (menuController != null)
        {
            menuController.AnimateJournalDetailsClose();
        }
    }
}