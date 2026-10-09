using System;
using System.Collections.Generic;
using FPSParkour.Bounty;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Investigation
{
    public struct ScanReport
    {
        public int Matched;
        public int Known;
        public bool Conclusive; // every mark the hunter knows lines up

        public override string ToString() =>
            Known == 0 ? "no marks known" : $"matches {Matched}/{Known} known marks";
    }

    [Serializable]
    public struct DossierState
    {
        public int[] Known;
        public bool Identified;
    }

    [DisallowMultipleComponent]
    public class Dossier : MonoBehaviour, ISaveable
    {
        [SerializeField] string saveKey = "dossier";
        [SerializeField] BountyContract contract;
        [SerializeField] IdentityCatalog catalog;
        [SerializeField] IdentityMark[] targetMarks;
        [SerializeField, Min(1)] int minimumMarksToAccuse = 2;
        [SerializeField] TraitSlot[] knownAtStart;

        readonly HashSet<TraitSlot> known = new HashSet<TraitSlot>();

        public event Action<TraitSlot, string> ClueLearned;
        public event Action<CrowdIdentity> TargetIdentified;

        public BountyContract Contract => contract;
        public string TargetName => contract != null ? contract.TargetName : "the target";
        public int CluesKnown => known.Count;
        public int TotalMarks => targetMarks != null ? targetMarks.Length : 0;
        public int MinimumMarksToAccuse => minimumMarksToAccuse;
        public bool CanAccuse => known.Count >= minimumMarksToAccuse;
        public bool IsIdentified { get; private set; }
        public string SaveKey => saveKey;

        void Awake()
        {
            if (knownAtStart != null)
            {
                foreach (TraitSlot slot in knownAtStart)
                    known.Add(slot);
            }

            WarnIfTargetIsIndistinguishable();
        }

        public void Configure(BountyContract next, IdentityMark[] marks, TraitSlot[] free, int minimumToAccuse)
        {
            contract = next;
            targetMarks = marks;
            knownAtStart = free;
            minimumMarksToAccuse = Mathf.Max(1, minimumToAccuse);

            known.Clear();
            IsIdentified = false;

            if (free != null)
            {
                foreach (TraitSlot slot in free)
                    known.Add(slot);
            }

            WarnIfTargetIsIndistinguishable();
        }

        public bool Knows(TraitSlot slot) => known.Contains(slot);

        public bool TryGetExpected(TraitSlot slot, out string value)
        {
            if (targetMarks != null)
            {
                foreach (IdentityMark mark in targetMarks)
                {
                    if (mark.Slot != slot)
                        continue;

                    value = mark.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool LearnClue(TraitSlot slot)
        {
            if (!known.Add(slot))
                return false;

            TryGetExpected(slot, out string value);
            ClueLearned?.Invoke(slot, value);
            return true;
        }

        public bool TryNextUnknownSlot(out TraitSlot slot)
        {
            if (targetMarks != null)
            {
                foreach (IdentityMark mark in targetMarks)
                {
                    if (known.Contains(mark.Slot))
                        continue;

                    slot = mark.Slot;
                    return true;
                }
            }

            slot = default;
            return false;
        }

        public ScanReport Inspect(CrowdIdentity subject)
        {
            ScanReport report = new ScanReport { Known = known.Count };

            if (subject == null)
                return report;

            foreach (TraitSlot slot in known)
            {
                if (TryGetExpected(slot, out string expected)
                    && subject.TryGetValue(slot, out string actual)
                    && expected == actual)
                    report.Matched++;
            }

            report.Conclusive = report.Known > 0 && report.Matched == report.Known;
            subject.RecordScan(report.Matched, report.Known);
            return report;
        }

        public void Identify(CrowdIdentity subject)
        {
            if (IsIdentified || subject == null || !subject.IsBountyTarget)
                return;

            IsIdentified = true;
            TargetIdentified?.Invoke(subject);
        }

        public string CaptureJson()
        {
            int[] slots = new int[known.Count];
            int i = 0;

            foreach (TraitSlot slot in known)
                slots[i++] = (int)slot;

            return JsonUtility.ToJson(new DossierState { Known = slots, Identified = IsIdentified });
        }

        public void RestoreJson(string json)
        {
            DossierState state = JsonUtility.FromJson<DossierState>(json);

            known.Clear();
            IsIdentified = state.Identified;

            if (state.Known == null)
                return;

            foreach (int slot in state.Known)
                known.Add((TraitSlot)slot);
        }

        void WarnIfTargetIsIndistinguishable()
        {
            if (catalog == null || targetMarks == null || targetMarks.Length == 0)
                return;

            foreach (IdentityMark mark in targetMarks)
            {
                if (!catalog.Contains(mark.Slot, mark.Value))
                    return;
            }

            Debug.LogWarning($"Dossier for '{TargetName}': every target mark is also a common crowd value, " +
                             "so the target can be found by chance. Give them one mark outside the catalog.", this);
        }
    }
}
