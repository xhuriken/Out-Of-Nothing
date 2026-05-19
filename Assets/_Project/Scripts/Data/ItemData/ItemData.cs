using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Journal/Item Data (Prefab)")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [Tooltip("Le prefab de prévisualisation de la boule ou machine (contenant les shapes)")]
    public GameObject previewPrefab;  // Référence au prefab visuel
    public bool isUnlocked;            // Est-ce que le joueur l'a débloquée ?

    public enum ItemType { Ball, Machine }
    public ItemType type;              // Permet de filtrer Boule ou Machine
}