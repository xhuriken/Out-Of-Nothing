using UnityEngine;
using System.Collections.Generic;

public class JournalManager : MonoBehaviour
{
    [Header("Configuration UI")]
    public Transform gridLayoutGroup; // Le parent qui contient le Grid Layout Group
    public GameObject slotPrefab;     // Ton prefab de slot carré avec le script JournalSlot

    [Header("Données du Jeu")]
    // Glisse ici TOUTES tes boules et TOUTES tes machines (Scriptable Objects)
    public List<ItemData> allItems;

    // Liste pour recycler les slots existants et éviter le lag de l'Instantiate/Destroy
    private List<JournalSlot> spawnedSlots = new List<JournalSlot>();

    private void Start()
    {
        // Par défaut au démarrage, on affiche les boules
        ShowCategory(ItemData.ItemType.Ball);
    }

    // --- FONCTIONS PUBLIQUES POUR TES BOUTONS ---

    public void OnClickBallsButton()
    {
        ShowCategory(ItemData.ItemType.Ball);
    }

    public void OnClickMachinesButton()
    {
        ShowCategory(ItemData.ItemType.Machine);
    }

    // --- LOGIQUE D'AFFICHAGE ---

    private void ShowCategory(ItemData.ItemType targetType)
    {
        // 1. On filtre notre grande liste pour ne garder que la catégorie voulue
        List<ItemData> filteredItems = allItems.FindAll(item => item.type == targetType);

        // 2. On ajuste le nombre de slots actifs dans la grille
        for (int i = 0; i < filteredItems.Count; i++)
        {
            JournalSlot slot;

            // Si on a déjà un slot créé précédemment dans la liste, on le réutilise
            if (i < spawnedSlots.Count)
            {
                slot = spawnedSlots[i];
                slot.gameObject.SetActive(true);
            }
            // Sinon, on en instancie un nouveau
            else
            {
                GameObject newSlotObj = Instantiate(slotPrefab, gridLayoutGroup);
                slot = newSlotObj.GetComponent<JournalSlot>();
                spawnedSlots.Add(slot);
            }

            // On met à jour le visuel du slot avec les données de la boule/machine
            slot.Setup(filteredItems[i]);
        }

        // 3. Désactiver les slots en trop s'il y en avait plus dans l'ancienne catégorie
        for (int i = filteredItems.Count; i < spawnedSlots.Count; i++)
        {
            spawnedSlots[i].gameObject.SetActive(false);
        }
    }
}