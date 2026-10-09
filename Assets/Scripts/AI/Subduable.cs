using System;
using FPSParkour.Combat;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Subduable : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField, Range(0.01f, 0.9f)] float downAtHealthFraction = 0.12f;
        [SerializeField] Behaviour[] disableWhenDowned;

        public event Action Downed;
        public event Action Revived;

        public bool IsDowned { get; private set; }

        void Awake()
        {
            if (health == null)
                health = GetComponent<Health>();

            health.MinimumHealth = Mathf.Max(1f, health.Max * downAtHealthFraction);
            health.OnDamaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (health != null)
                health.OnDamaged -= OnDamaged;
        }

        void OnDamaged(float dealt, DamageInfo info)
        {
            if (IsDowned || health.Current > health.MinimumHealth)
                return;

            GoDown();
        }

        public void GoDown()
        {
            if (IsDowned)
                return;

            IsDowned = true;

            if (disableWhenDowned != null)
            {
                foreach (Behaviour behaviour in disableWhenDowned)
                {
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }

            Downed?.Invoke();
        }

        public void Revive()
        {
            if (!IsDowned)
                return;

            IsDowned = false;
            health.Heal(health.Max);

            if (disableWhenDowned != null)
            {
                foreach (Behaviour behaviour in disableWhenDowned)
                {
                    if (behaviour != null)
                        behaviour.enabled = true;
                }
            }

            Revived?.Invoke();
        }

        public void AllowLethal()
        {
            health.MinimumHealth = 0f;
        }
    }
}
