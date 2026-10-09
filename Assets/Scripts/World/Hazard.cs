using System.Collections.Generic;
using FPSParkour.AI;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.World
{
    public enum HazardMode
    {
        Continuous, // Electrified rail, coolant spill
        Pulsed,     // Steam vent, safe between bursts, so it is a timing problem not a wall
        OnEnter,    // Tram strike, crusher
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] HazardMode mode = HazardMode.Continuous;
        [SerializeField] DamageType damageType = DamageType.Energy;
        [SerializeField] float damagePerSecond = 18f;
        [SerializeField] float damagePerHit = 45f;

        [Header("Pulsed")]
        [SerializeField, Min(0.1f)] float cycleSeconds = 4f;
        [SerializeField, Min(0.1f)] float activeSeconds = 1.2f;
        [SerializeField] float phaseOffset;

        [Header("Tell")]
        [SerializeField] Renderer tell;
        [SerializeField] Color idleColour = new Color(0.35f, 0.32f, 0.2f);
        [SerializeField] Color liveColour = new Color(1f, 0.45f, 0.15f);

        [Header("Noise")]
        [SerializeField] bool raisesAlarm;
        [SerializeField] float alarmRadius = 25f;

        readonly List<IDamageable> occupants = new List<IDamageable>();

        MaterialPropertyBlock block;
        bool wasLive;

        public bool IsLive => mode != HazardMode.Pulsed
                              || Mathf.Repeat(Time.time + phaseOffset, cycleSeconds) < activeSeconds;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void Update()
        {
            bool live = IsLive;

            if (live != wasLive)
            {
                wasLive = live;
                Tint(live);

                if (live && raisesAlarm && CityAlarm.Instance != null)
                    CityAlarm.Instance.Raise(transform.position, alarmRadius);
            }

            if (mode == HazardMode.OnEnter || !live || occupants.Count == 0)
                return;

            float amount = damagePerSecond * Time.deltaTime;

            for (int i = occupants.Count - 1; i >= 0; i--)
            {
                if (occupants[i] == null)
                    occupants.RemoveAt(i);
                else
                    Hit(occupants[i], amount);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();

            if (target == null || occupants.Contains(target))
                return;

            if (mode == HazardMode.OnEnter)
            {
                if (IsLive)
                    Hit(target, damagePerHit);

                return;
            }

            occupants.Add(target);
        }

        void OnTriggerExit(Collider other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();

            if (target != null)
                occupants.Remove(target);
        }

        void Hit(IDamageable target, float amount)
        {
            target.ApplyDamage(new DamageInfo(amount, damageType, transform.position, Vector3.up, gameObject));
        }

        void Tint(bool live)
        {
            if (tell == null)
                return;

            block ??= new MaterialPropertyBlock();
            tell.GetPropertyBlock(block);
            block.SetColor("_BaseColor", live ? liveColour : idleColour);
            tell.SetPropertyBlock(block);
        }
    }
}
