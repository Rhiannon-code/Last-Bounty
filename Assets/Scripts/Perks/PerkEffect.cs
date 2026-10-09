using System;
using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Perks
{
    public enum PerkEffectKind
    {
        GrantAbility,   // Unlock a movement AbilityId
        PlayerStat,     // Modify a StatType on PlayerStats
        WeaponStat,     // Modify a WeaponStatType on PlayerStats
    }

    [Serializable]
    public class PerkEffect
    {
        public PerkEffectKind kind = PerkEffectKind.PlayerStat;

        [Header("GrantAbility")]
        public AbilityId ability;

        [Header("PlayerStat/WeaponStat")]
        public StatType playerStat;
        public WeaponStatType weaponStat;
        public ModifierOp op = ModifierOp.PercentAdd;
        public float value = 0.1f;
    }
}
