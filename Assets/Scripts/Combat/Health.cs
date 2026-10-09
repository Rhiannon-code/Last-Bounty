using FPSParkour.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSParkour.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Serializable]
        public struct Resistance
        {
            public DamageType type;
            [Range(-1f, 1f)] public float amount; // 1 = immune, 0 = normal, <0 = extra vulnerable
        }

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool invulnerable;
        [SerializeField] private List<Resistance> resistances = new();

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsAlive => Current > 0f;
        public float MinimumHealth { get; set; }

        public event Action<float, DamageInfo> OnDamaged;
        public static event Action<Health, float, DamageInfo> AnyDamaged;
        public event Action<DamageInfo> OnDied;

        private void Awake() => Current = maxHealth;

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive || invulnerable) return;

            float resist = Mathf.Clamp(GetResistance(info.type), -1f, 1f);
            float dealt = Mathf.Max(0f, info.amount * (1f - resist));
            if (dealt <= 0f) return;

            Current = Mathf.Max(MinimumHealth, Current - dealt);
            OnDamaged?.Invoke(dealt, info);
            AnyDamaged?.Invoke(this, dealt, info);

            if (Current <= 0f)
                OnDied?.Invoke(info);
        }

        public void Kill(in DamageInfo info)
        {
            if (!IsAlive) return;

            float dealt = Current;
            Current = 0f;
            OnDamaged?.Invoke(dealt, info);
            OnDied?.Invoke(info);
        }

        public void Heal(float amount) => Current = Mathf.Min(maxHealth, Current + Mathf.Max(0f, amount));

        private float GetResistance(DamageType type)
        {
            foreach (var r in resistances)
                if (r.type == type) return r.amount;
            return 0f;
        }
    }
}
