using System;
using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.Combat
{
    [DisallowMultipleComponent]
    public class GadgetThrower : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] AbilityUnlocks unlocks;
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform aimOrigin;
        [SerializeField] SnarePod podPrefab;
        [SerializeField] int maxCharges = 3;
        [SerializeField] float throwSpeed = 22f;
        [SerializeField] float upwardBias = 0.16f;
        [SerializeField] float cooldownSeconds = 1.2f;

        float readyAt;

        public event Action<int, int> ChargesChanged;

        public int Charges { get; private set; }

        public int MaxCharges => maxCharges + (stats != null
            ? Mathf.RoundToInt(stats.Modify(StatType.SnareCharges, 0f))
            : 0);
            
        public bool IsUnlocked => unlocks == null || unlocks.Has(AbilityId.SnareLauncher);

        public bool Ready => IsUnlocked && Charges > 0 && Time.time >= readyAt;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (unlocks == null) unlocks = GetComponent<AbilityUnlocks>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (aimOrigin == null && Camera.main != null) aimOrigin = Camera.main.transform;

            Charges = MaxCharges;
        }

        void Update()
        {
            if (input != null && input.GadgetPressed)
                TryThrow();
        }

        public bool TryThrow()
        {
            if (!Ready || podPrefab == null || aimOrigin == null)
                return false;

            Charges--;
            readyAt = Time.time + cooldownSeconds;

            Vector3 direction = (aimOrigin.forward + Vector3.up * upwardBias).normalized;
            SnarePod pod = Instantiate(podPrefab, aimOrigin.position + aimOrigin.forward * 0.8f, Quaternion.identity);
            pod.Throw(direction * throwSpeed, stats != null ? stats.Mult(StatType.SnareDurationMult) : 1f);

            ChargesChanged?.Invoke(Charges, MaxCharges);
            return true;
        }

        public void Refill()
        {
            Charges = MaxCharges;
            ChargesChanged?.Invoke(Charges, MaxCharges);
        }
    }
}
