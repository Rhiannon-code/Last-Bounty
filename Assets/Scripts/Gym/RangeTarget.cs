using System;
using FPSParkour.Combat;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class RangeTarget : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] float respawnAfter = 2f;
        [SerializeField] Renderer tint;
        [SerializeField] Color aliveColour = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] Color downedColour = new Color(0.8f, 0.2f, 0.2f);

        float respawnAt = -1f;

        public event Action<RangeTarget, bool> Hit;

        public int TimesDowned { get; private set; }
        public int CriticalHits { get; private set; }

        void Awake()
        {
            if (health == null)
                health = GetComponent<Health>();

            health.OnDamaged += OnDamaged;
            health.OnDied += OnDied;
            Paint(aliveColour);
        }

        void OnDestroy()
        {
            if (health == null)
                return;

            health.OnDamaged -= OnDamaged;
            health.OnDied -= OnDied;
        }

        void Update()
        {
            if (respawnAt < 0f || Time.time < respawnAt)
                return;

            respawnAt = -1f;
            health.Heal(health.Max);
            Paint(aliveColour);
        }

        void OnDamaged(float dealt, DamageInfo info)
        {
            if (info.isCritical)
                CriticalHits++;

            Hit?.Invoke(this, info.isCritical);
        }

        void OnDied(DamageInfo info)
        {
            TimesDowned++;
            respawnAt = Time.time + respawnAfter;
            Paint(downedColour);
        }

        void Paint(Color colour)
        {
            if (tint != null)
                tint.material.color = colour;
        }
    }
}
