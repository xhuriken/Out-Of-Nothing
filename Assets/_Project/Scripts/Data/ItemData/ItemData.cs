using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewItem", menuName = "Journal/Item Data Complete")]
public class ItemData : ScriptableObject
{
    [Header("Informations de Base")]
    public string itemName;           
    [TextArea(3, 5)]
    public string description;         

    [Header("Visuel")]
    [Tooltip("Le prefab de prévisualisation (contenant les shapes)")]
    public GameObject previewPrefab;
    public bool isUnlocked;            

    public enum ItemType { Ball, Machine }
    [Header("Catégorie")]
    public ItemType type;            

    [Header("Recette de Craft (Machines Uniquement)")]
    [Tooltip("Glisse ici les boules nécessaires pour crafter cette machine. Ex: si besoin d'1 rouge et 2 bleues, glisse la rouge 1 fois et la bleue 2 fois.")]
    public List<ItemData> craftRecipe; 
}