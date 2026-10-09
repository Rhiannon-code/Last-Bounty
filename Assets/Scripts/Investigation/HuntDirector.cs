using System;
using FPSParkour.AI;
using FPSParkour.Bounty;
using UnityEngine;

namespace FPSParkour.Investigation
{
    public enum HuntPhase { Investigating, Chase, Resolved, Escaped }

    [DisallowMultipleComponent]
    public class HuntDirector : MonoBehaviour
    {
        [SerializeField] Dossier dossier;
        [SerializeField] DistrictHeat heat;
        [SerializeField] BountyRegistry registry;
        [SerializeField] FleeingBountyBrain target;

        [Header("Cost of accusing the wrong person")]
        [SerializeField, Range(0f, 1f)] float wrongPersonHeat = 0.34f;

        public event Action<HuntPhase> PhaseChanged;
        public event Action<CrowdIdentity> WrongPerson;

        public HuntPhase Phase { get; private set; } = HuntPhase.Investigating;
        public Dossier Dossier => dossier;
        public DistrictHeat Heat => heat;
        public FleeingBountyBrain Target => target;

        void OnEnable()
        {
            if (dossier != null) dossier.TargetIdentified += OnIdentified;
            if (heat != null) heat.Maxed += OnHeatMaxed;
            if (registry != null) registry.ContractResolved += OnResolved;

            if (target != null)
            {
                target.Spooked += OnSpooked;
                target.Escaped += OnEscaped;
            }
        }

        void OnDisable()
        {
            if (dossier != null) dossier.TargetIdentified -= OnIdentified;
            if (heat != null) heat.Maxed -= OnHeatMaxed;
            if (registry != null) registry.ContractResolved -= OnResolved;

            if (target != null)
            {
                target.Spooked -= OnSpooked;
                target.Escaped -= OnEscaped;
            }
        }

        public void Accuse(CrowdIdentity subject)
        {
            if (subject == null || Phase != HuntPhase.Investigating)
                return;

            if (subject.IsBountyTarget)
            {
                dossier.Identify(subject);
                return;
            }

            heat?.Add(wrongPersonHeat);
            WrongPerson?.Invoke(subject);
        }

        void OnIdentified(CrowdIdentity subject)
        {
            if (target != null)
            {
                target.Identified = true;
                target.Spook();
            }
        }

        void OnHeatMaxed()
        {
            if (Phase == HuntPhase.Investigating)
                target?.Spook();
        }

        void OnSpooked() => Enter(HuntPhase.Chase);

        void OnEscaped() => Enter(HuntPhase.Escaped);

        void OnResolved(BountyContract contract, BountyOutcome outcome, int paid)
        {
            if (dossier == null || contract == dossier.Contract)
                Enter(HuntPhase.Resolved);
        }

        void Enter(HuntPhase next)
        {
            if (Phase == next || Phase == HuntPhase.Resolved)
                return;

            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
