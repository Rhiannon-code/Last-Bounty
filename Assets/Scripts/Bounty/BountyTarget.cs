using System;
using FPSParkour.AI;
using FPSParkour.Combat;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Subduable))]
    public class BountyTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] BountyContract contract;
        [SerializeField] BountyRegistry registry;
        [SerializeField] Subduable subduable;
        [SerializeField] Health health;
        [SerializeField] float destroyDelay = 1.5f;

        public event Action<BountyTarget> ResolutionRequested;
        public static event Action<BountyTarget> AnyResolutionRequested;
        public BountyContract Contract => contract;
        public bool IsDowned => subduable != null && subduable.IsDowned;
        public bool CanRelease => registry != null && registry.ReleaseUnlocked;

        public string Prompt => IsDowned ? $"Capture {contract?.TargetName}" : string.Empty;

        void Awake()
        {
            if (subduable == null) subduable = GetComponent<Subduable>();
            if (health == null) health = GetComponent<Health>();
            if (health != null) health.OnDied += OnDied;
        }

        void OnDestroy()
        {
            if (health != null)
                health.OnDied -= OnDied;
        }

        public bool CanInteract(GameObject actor) => IsDowned && registry != null && !registry.IsResolved(contract);

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor))
                return;

            if (ResolutionRequested == null && AnyResolutionRequested == null)
            {
                Capture();
                return;
            }

            ResolutionRequested?.Invoke(this);
            AnyResolutionRequested?.Invoke(this);
        }

        public bool Capture() => Resolve(BountyOutcome.Captured);
        public bool Release() => CanRelease && Resolve(BountyOutcome.Released);
        public bool Execute()
        
        {
            if (!IsDowned || health == null)
                return false;

            subduable.AllowLethal();
            health.Kill(new DamageInfo(health.Max, DamageType.Kinetic, transform.position, Vector3.zero, gameObject));
            return true;
        }

        void OnDied(DamageInfo info) => Resolve(BountyOutcome.Killed);

        bool Resolve(BountyOutcome outcome)
        {
            if (registry == null || contract == null || registry.IsResolved(contract))
                return false;

            if (!registry.Resolve(contract, outcome))
                return false;

            if (outcome != BountyOutcome.Killed)
                Destroy(gameObject, destroyDelay);

            return true;
        }
    }
}
