using System;
using System.Collections.Generic;
using UnityEngine;
using FPSParkour.Core;

namespace FPSParkour.Player
{
    public enum StatType
    {
        MaxHealth,
        MaxStamina,
        MoveSpeedMult,
        SprintSpeedMult,
        SlideDurationMult,
        JumpHeightMult,
        GravityMult,
        WallRunDurationMult,
        DashCharges,        // Flat +N charges
        DashCooldownMult,
        GrappleRangeMult,
        FallDamageResist,   // 0..1, fraction of fall damage ignored
        ScanRangeMult,
        ScanSpeedMult,
        SnareCharges,       // Flat +N charges
        SnareDurationMult,
        HeatGainMult,       // How loudly the district reads you working
    }

    public enum WeaponStatType
    {
        DamageMult,
        FireRateMult,
        ReloadSpeedMult,
        MagSizeAdd,
        SpreadMult,
        ReserveAmmoMult,
    }

    public enum ModifierOp
    {
        Flat,        // Added to the base value
        PercentAdd,  // Summed with other PercentAdd, then applied as (1 + sum)
        PercentMult, // Applied multiplicatively, one after another
    }

    [Serializable]
    public struct StatModifier
    {
        public ModifierOp Op;
        public float Value;

        public StatModifier(ModifierOp op, float value)
        {
            Op = op;
            Value = value;
        }
    }

    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Base resource values (before modifiers)")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseMaxStamina = 100f;

        private readonly ModifierSet _stats = new();
        private readonly ModifierSet _weapon = new();

        public float Health { get; private set; }
        public float Stamina { get; private set; }

        public event Action OnStatsChanged;
        public event Action<float, float> OnHealthChanged; // Current, max

        private void Awake()
        {
            Health = MaxHealth;
            Stamina = MaxStamina;
        }

        public float MaxHealth => Modify(StatType.MaxHealth, baseMaxHealth);
        public float MaxStamina => Modify(StatType.MaxStamina, baseMaxStamina);


        public void AddModifier(StatType stat, StatModifier mod, object source)
        {
            _stats.Add((int)stat, mod, source);
            OnStatsChanged?.Invoke();
        }

        public void AddWeaponModifier(WeaponStatType stat, StatModifier mod, object source)
        {
            _weapon.Add((int)stat, mod, source);
            OnStatsChanged?.Invoke();
        }

        public void RemoveSource(object source)
        {
            _stats.RemoveSource(source);
            _weapon.RemoveSource(source);
            OnStatsChanged?.Invoke();
        }

        public float Modify(StatType stat, float baseValue) => _stats.Apply((int)stat, baseValue);
        public float ModifyWeapon(WeaponStatType stat, float baseValue) => _weapon.Apply((int)stat, baseValue);
        public float Mult(StatType stat) => Modify(stat, 1f);

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            OnHealthChanged?.Invoke(Health, MaxHealth);
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
            OnHealthChanged?.Invoke(Health, MaxHealth);
        }

        public void ApplyDamage(in DamageInfo info) => ApplyDamage(info.amount);

        private class ModifierSet
        {
            private readonly Dictionary<int, List<(StatModifier mod, object source)>> _map = new();

            public void Add(int key, StatModifier mod, object source)
            {
                if (!_map.TryGetValue(key, out var list))
                {
                    list = new List<(StatModifier, object)>();
                    _map[key] = list;
                }
                list.Add((mod, source));
            }

            public void RemoveSource(object source)
            {
                foreach (var list in _map.Values)
                    list.RemoveAll(e => ReferenceEquals(e.source, source));
            }

            public float Apply(int key, float baseValue)
            {
                if (!_map.TryGetValue(key, out var list) || list.Count == 0)
                    return baseValue;

                float flat = 0f;
                float percentAdd = 0f;
                float result = baseValue;

                foreach (var (mod, _) in list)
                {
                    switch (mod.Op)
                    {
                        case ModifierOp.Flat: flat += mod.Value; break;
                        case ModifierOp.PercentAdd: percentAdd += mod.Value; break;
                    }
                }

                result = (baseValue + flat) * (1f + percentAdd);

                foreach (var (mod, _) in list)
                    if (mod.Op == ModifierOp.PercentMult)
                        result *= mod.Value;

                return result;
            }
        }
    }
}
