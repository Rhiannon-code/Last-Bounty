using FPSParkour.Perks;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [DisallowMultipleComponent]
    public class ContractProgression : MonoBehaviour
    {
        [SerializeField] BountyRegistry registry;
        [SerializeField] PerkTree tree;
        [SerializeField, Min(0)] int pointsOnCapture = 2;
        [SerializeField, Min(0)] int pointsOnKill = 1;
        [SerializeField, Min(0)] int pointsOnRelease;

        void OnEnable()
        {
            if (registry != null)
                registry.ContractResolved += OnResolved;
        }

        void OnDisable()
        {
            if (registry != null)
                registry.ContractResolved -= OnResolved;
        }

        void OnResolved(BountyContract contract, BountyOutcome outcome, int paid)
        {
            tree?.AwardSkillPoints(PointsFor(outcome));
        }

        public int PointsFor(BountyOutcome outcome)
        {
            switch (outcome)
            {
                case BountyOutcome.Captured: return pointsOnCapture;
                case BountyOutcome.Killed: return pointsOnKill;
                case BountyOutcome.Released: return pointsOnRelease;
                default: return 0;
            }
        }
    }
}
