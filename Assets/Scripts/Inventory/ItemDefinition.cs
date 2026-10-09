using UnityEngine;

namespace FPSParkour.Inventory
{
    public enum ItemCategory
    {
        Weapon,
        Gadget,
        Augment,     // Installable perk granting chip/mod
        Consumable,
        Ammo,
        Resource,    // Crafting/currency material
        QuestItem,
    }

    [CreateAssetMenu(fileName = "Item_", menuName = "FPS Parkour/Item", order = 2)]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public ItemCategory category = ItemCategory.Resource;

        [Header("Grid footprint (RPG inventory)")]
        [Min(1)] public int gridWidth = 1;
        [Min(1)] public int gridHeight = 1;

        [Header("Stacking")]
        [Min(1)] public int maxStack = 1;
        public float weight = 1f;

        [Header("Quick-bar")]
        public bool quickSlottable = false;
        public GameObject worldPrefab;

        public bool IsStackable => maxStack > 1;
    }
}
