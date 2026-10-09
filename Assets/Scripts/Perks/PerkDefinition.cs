using UnityEngine;

namespace FPSParkour.Perks
{
    public enum PerkCategory
    {
        Augment,   // body/cybernetic upgrades: stats, resistances
        Movement,  // unlock/upgrade traversal abilities
        Weapon,    // weapon behaviour mods
        Utility,   // gadgets, scanning, economy, etc.
    }

    [CreateAssetMenu(fileName = "Perk_", menuName = "FPS Parkour/Perk", order = 0)]
    public class PerkDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "New Perk";
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public PerkCategory category = PerkCategory.Augment;

        [Header("Tree placement")]
        public int cost = 1;
        public int tier = 0;
        public PerkDefinition[] prerequisites;

        [Header("What it does")]
        public PerkEffect[] effects;
    }
}
