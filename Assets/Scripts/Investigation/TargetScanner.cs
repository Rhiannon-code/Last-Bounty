using System;
using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.Investigation
{
    [DisallowMultipleComponent]
    public class TargetScanner : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform aimOrigin;
        [SerializeField] Dossier dossier;
        [SerializeField] DistrictHeat heat;
        [SerializeField] AbilityUnlocks unlocks;
        [SerializeField] PlayerStats stats;

        [SerializeField] float range = 45f;
        [SerializeField] float radius = 0.6f;
        [SerializeField] LayerMask mask = ~0;
        [SerializeField, Min(0.1f)] float holdSeconds = 1.1f;
        [SerializeField] float heatPerScan = 0.05f;

        CrowdIdentity subject;
        float progress;

        public event Action<CrowdIdentity, ScanReport> Scanned;

        public CrowdIdentity Subject => subject;
        public float Progress => HoldTime > 0f ? Mathf.Clamp01(progress / HoldTime) : 0f;
        public bool IsScanning => subject != null && input != null && input.ScanHeld;
        public bool IsUnlocked => unlocks == null || unlocks.Has(AbilityId.Scanner);

        public float Range => stats != null ? range * stats.Mult(StatType.ScanRangeMult) : range;

        public float HoldTime => stats != null
            ? holdSeconds / Mathf.Max(0.1f, stats.Mult(StatType.ScanSpeedMult))
            : holdSeconds;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (unlocks == null) unlocks = GetComponent<AbilityUnlocks>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (aimOrigin == null && Camera.main != null) aimOrigin = Camera.main.transform;
        }

        void Update()
        {
            if (input == null || aimOrigin == null || dossier == null || !IsUnlocked)
                return;

            CrowdIdentity found = FindSubject();

            if (found != subject)
            {
                subject = found;
                progress = 0f;
            }

            if (subject == null || !input.ScanHeld)
            {
                progress = 0f;
                return;
            }

            if (subject.WasScanned)
                return;

            progress += Time.deltaTime;

            if (progress < HoldTime)
                return;

            progress = 0f;
            Complete(subject);
        }

        CrowdIdentity FindSubject()
        {
            if (!Physics.SphereCast(aimOrigin.position, radius, aimOrigin.forward, out RaycastHit hit,
                    Range, mask, QueryTriggerInteraction.Ignore))
                return null;

            return hit.collider.GetComponentInParent<CrowdIdentity>();
        }

        void Complete(CrowdIdentity target)
        {
            ScanReport report = dossier.Inspect(target);
            heat?.Add(heatPerScan);
            Scanned?.Invoke(target, report);
        }
    }
}
