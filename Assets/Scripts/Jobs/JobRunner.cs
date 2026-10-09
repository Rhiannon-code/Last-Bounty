using System;
using System.Collections.Generic;
using FPSParkour.AI;
using FPSParkour.Bounty;
using FPSParkour.Combat;
using FPSParkour.Investigation;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.Jobs
{
    public enum JobState { Idle, Running, Complete, Failed }

    [DisallowMultipleComponent]
    public class JobRunner : MonoBehaviour
    {
        [SerializeField] HuntDirector director;
        [SerializeField] BountyRegistry registry;
        [SerializeField] TargetScanner scanner;
        [SerializeField] GadgetThrower gadget;
        [SerializeField] StoryFlags flags;
        [SerializeField] Transform player;
        [SerializeField] Transform roofStart;

        public event Action<JobDefinition> JobBegun;
        public event Action<int> ObjectiveCompleted;
        public event Action<JobDefinition> JobCompleted;
        public event Action<JobDefinition, FailureKind> JobFailed;
        public event Action<string> Notice;

        public JobDefinition Active { get; private set; }
        public JobState State { get; private set; } = JobState.Idle;
        public int ObjectiveIndex { get; private set; }

        public bool HasObjective => Active != null && ObjectiveIndex < Active.ObjectiveCount;
        public JobObjective Current => Active.Objectives[ObjectiveIndex];

        readonly List<Subduable> guards = new List<Subduable>();

        int hostilesDowned;

        void OnEnable()
        {
            if (scanner != null) scanner.Scanned += OnScanned;
            if (registry != null) registry.ContractResolved += OnResolved;

            if (director != null)
            {
                director.PhaseChanged += OnPhaseChanged;
                director.WrongPerson += OnWrongPerson;

                if (director.Dossier != null)
                {
                    director.Dossier.ClueLearned += OnClueLearned;
                    director.Dossier.TargetIdentified += OnIdentified;
                }

                if (director.Heat != null) director.Heat.Maxed += OnHeatMaxed;
                if (director.Target != null) director.Target.StateChanged += OnTargetState;
            }
        }

        void OnDisable()
        {
            if (scanner != null) scanner.Scanned -= OnScanned;
            if (registry != null) registry.ContractResolved -= OnResolved;

            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.WrongPerson -= OnWrongPerson;

                if (director.Dossier != null)
                {
                    director.Dossier.ClueLearned -= OnClueLearned;
                    director.Dossier.TargetIdentified -= OnIdentified;
                }

                if (director.Heat != null) director.Heat.Maxed -= OnHeatMaxed;
                if (director.Target != null) director.Target.StateChanged -= OnTargetState;
            }

            UnsubscribeGuards();
        }

        public void Begin(JobDefinition job)
        {
            if (job == null)
                return;

            Active = job;
            State = JobState.Running;
            ObjectiveIndex = 0;
            hostilesDowned = 0;

            if (director != null && director.Dossier != null)
            {
                director.Dossier.Configure(job.Contract, job.TargetMarks, job.KnownAtStart,
                    job.MinimumMarksToAccuse);
            }

            if (job.Contract != null)
                registry?.Accept(job.Contract);

            gadget?.Refill();
            ApplyShape(job);
            SubscribeGuards();

            JobBegun?.Invoke(job);
        }

        void ApplyShape(JobDefinition job)
        {
            FleeingBountyBrain target = director != null ? director.Target : null;
            if (target == null)
                return;

            switch (job.Shape)
            {
                case JobShape.RooftopPursuit:
                    if (roofStart != null)
                        Warp(target, roofStart.position);

                    target.Identified = true;
                    target.Spook();
                    break;

                case JobShape.SnatchUnderGuard:

                    CrowdPedestrian wander = target.GetComponent<CrowdPedestrian>();
                    if (wander != null)
                        wander.enabled = false;
                    break;
            }
        }

        static void Warp(FleeingBountyBrain target, Vector3 to)
        {
            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();

            if (agent != null && NavMesh.SamplePosition(to, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        void SubscribeGuards()
        {
            UnsubscribeGuards();

            FleeingBountyBrain target = director != null ? director.Target : null;

            foreach (Subduable subduable in FindObjectsByType<Subduable>(FindObjectsSortMode.None))
            {
                if (target != null && subduable.gameObject == target.gameObject)
                    continue;

                guards.Add(subduable);
                subduable.Downed += OnGuardDowned;
            }
        }

        void UnsubscribeGuards()
        {
            foreach (Subduable subduable in guards)
            {
                if (subduable != null)
                    subduable.Downed -= OnGuardDowned;
            }

            guards.Clear();
        }

        void Update()
        {
            if (State != JobState.Running || !HasObjective)
                return;

            if (Current.Kind == ObjectiveKind.ReachHeight && player != null
                && player.position.y >= Current.Amount)
                Complete(ObjectiveKind.ReachHeight);
        }

        void OnScanned(CrowdIdentity subject, ScanReport report) => Complete(ObjectiveKind.ScanAnyone);

        void OnClueLearned(TraitSlot slot, string value)
        {
            if (!HasObjective || Current.Kind != ObjectiveKind.LearnClues)
                return;

            if (director.Dossier.CluesKnown >= Mathf.RoundToInt(Current.Amount))
                Complete(ObjectiveKind.LearnClues);
        }

        void OnIdentified(CrowdIdentity subject) => Complete(ObjectiveKind.IdentifyTarget);

        void OnGuardDowned()
        {
            hostilesDowned++;

            if (HasObjective && Current.Kind == ObjectiveKind.DownHostiles
                && hostilesDowned >= Mathf.RoundToInt(Current.Amount))
                Complete(ObjectiveKind.DownHostiles);
        }

        void OnTargetState(FleeState state)
        {
            if (state == FleeState.Snared)
                Complete(ObjectiveKind.SnareTarget);

            if (state == FleeState.Downed)
                Complete(ObjectiveKind.DownTarget);
        }

        void OnPhaseChanged(HuntPhase phase)
        {
            switch (phase)
            {
                case HuntPhase.Chase:
                    Complete(ObjectiveKind.ChaseBegins);
                    break;
                case HuntPhase.Escaped:
                    Fail(FailureKind.TargetEscapes,
                        "They got away, unseen and beyond the escape range for long enough. Cut the corner over the roofs instead of following them along the street.");
                    break;
            }
        }

        void OnWrongPerson(CrowdIdentity subject)
        {
            Notice?.Invoke("Wrong person. That cost you heat, and there is no quiet way back, get another mark before you accuse again.");
            Fail(FailureKind.WrongAccusation, null);
        }

        void OnHeatMaxed()
        {
            Notice?.Invoke("The district is fully spooked. Your target is leaving whether or not you ever worked out who they were.");
            Fail(FailureKind.HeatMaxed, null);
        }

        void OnResolved(BountyContract contract, BountyOutcome outcome, int paid)
        {
            if (Active == null || contract != Active.Contract)
                return;

            if (outcome == BountyOutcome.Killed)
                Fail(FailureKind.PlayerKillsTarget, "This one was wanted alive. The payout is halved and it stays on your record.");

            Complete(ObjectiveKind.ResolveContract);
        }

        void Complete(ObjectiveKind kind)
        {
            if (State != JobState.Running || !HasObjective || Current.Kind != kind)
                return;

            ObjectiveCompleted?.Invoke(ObjectiveIndex);
            ObjectiveIndex++;

            if (HasObjective)
                return;

            State = JobState.Complete;

            if (flags != null && Active.FlagsOnComplete != null)
            {
                foreach (string flag in Active.FlagsOnComplete)
                    flags.Set(flag);
            }

            JobCompleted?.Invoke(Active);
        }

        void Fail(FailureKind kind, string notice)
        {
            if (State != JobState.Running || Active.FailsOn != kind)
                return;

            if (!string.IsNullOrEmpty(notice))
                Notice?.Invoke(notice);

            State = JobState.Failed;
            JobFailed?.Invoke(Active, kind);
        }
    }
}
