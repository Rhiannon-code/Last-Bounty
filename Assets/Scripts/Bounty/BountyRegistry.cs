using System;
using System.Collections.Generic;
using FPSParkour.Core;
using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [Serializable]
    public struct ResolvedBounty
    {
        public string ContractId;
        public int Outcome;
        public int Paid;
    }

    [Serializable]
    public struct BountyRegistryState
    {
        public string[] AcceptedIds;
        public ResolvedBounty[] Resolved;
    }

    [DisallowMultipleComponent]
    public class BountyRegistry : MonoBehaviour, ISaveable
    {
        [SerializeField] BountyContract[] roster;
        [SerializeField] StoryFlags flags;
        [SerializeField] CreditsWallet wallet;
        [SerializeField] ReputationSystem reputation;
        [SerializeField] string saveKey = "bounties";

        [Header("Release gate")]
        [SerializeField] string releaseUnlockedFlag = "flag_ch13_seen";

        readonly HashSet<string> accepted = new HashSet<string>();
        readonly Dictionary<string, ResolvedBounty> resolved = new Dictionary<string, ResolvedBounty>();

        public event Action<BountyContract> ContractAccepted;
        public event Action<BountyContract, BountyOutcome, int> ContractResolved;
        public string SaveKey => saveKey;
        public IReadOnlyList<BountyContract> Roster => roster;
        public bool ReleaseUnlocked => flags != null && flags.Has(releaseUnlockedFlag);
        public int CapturedCount { get; private set; }
        public int KilledCount { get; private set; }
        public int ReleasedCount { get; private set; }

        public bool IsAccepted(BountyContract contract) => contract != null && accepted.Contains(contract.Id);
        public bool IsResolved(BountyContract contract) => contract != null && resolved.ContainsKey(contract.Id);

        public bool IsAvailable(BountyContract contract)
        {
            return contract != null
                   && !IsResolved(contract)
                   && !IsAccepted(contract)
                   && contract.Availability.IsMet(flags);
        }

        public void CollectAvailable(List<BountyContract> into)
        {
            into.Clear();

            if (roster == null)
                return;

            foreach (BountyContract contract in roster)
            {
                if (IsAvailable(contract))
                    into.Add(contract);
            }
        }

        public bool Accept(BountyContract contract)
        {
            if (!IsAvailable(contract))
                return false;

            accepted.Add(contract.Id);
            ContractAccepted?.Invoke(contract);
            return true;
        }

        public bool CanResolve(BountyContract contract, BountyOutcome outcome)
        {
            if (contract == null || outcome == BountyOutcome.Unresolved || IsResolved(contract))
                return false;

            return outcome != BountyOutcome.Released || ReleaseUnlocked;
        }

        public bool Resolve(BountyContract contract, BountyOutcome outcome)
        {
            if (!CanResolve(contract, outcome))
                return false;

            int paid = contract.PayoutFor(outcome);

            accepted.Remove(contract.Id);
            resolved[contract.Id] = new ResolvedBounty
            {
                ContractId = contract.Id,
                Outcome = (int)outcome,
                Paid = paid
            };

            Tally(outcome, 1);

            if (paid > 0)
                wallet?.Add(paid);

            flags?.SetAll(contract.FlagsFor(outcome));
            reputation?.Modify(contract.IssuingFactionId, contract.StandingFor(outcome));

            ContractResolved?.Invoke(contract, outcome, paid);
            return true;
        }

        void Tally(BountyOutcome outcome, int delta)
        {
            switch (outcome)
            {
                case BountyOutcome.Captured: CapturedCount += delta; break;
                case BountyOutcome.Killed: KilledCount += delta; break;
                case BountyOutcome.Released: ReleasedCount += delta; break;
            }
        }

        public string CaptureJson()
        {
            string[] acceptedIds = new string[accepted.Count];
            accepted.CopyTo(acceptedIds);

            ResolvedBounty[] resolvedArray = new ResolvedBounty[resolved.Count];
            resolved.Values.CopyTo(resolvedArray, 0);

            return JsonUtility.ToJson(new BountyRegistryState { AcceptedIds = acceptedIds, Resolved = resolvedArray });
        }

        public void RestoreJson(string json)
        {
            accepted.Clear();
            resolved.Clear();
            CapturedCount = 0;
            KilledCount = 0;
            ReleasedCount = 0;

            BountyRegistryState state = JsonUtility.FromJson<BountyRegistryState>(json);

            if (state.AcceptedIds != null)
            {
                foreach (string id in state.AcceptedIds)
                    accepted.Add(id);
            }

            if (state.Resolved == null)
                return;

            foreach (ResolvedBounty entry in state.Resolved)
            {
                resolved[entry.ContractId] = entry;
                Tally((BountyOutcome)entry.Outcome, 1);
            }
        }
    }
}
