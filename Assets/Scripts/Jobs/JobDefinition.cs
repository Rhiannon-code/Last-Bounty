using FPSParkour.Bounty;
using FPSParkour.Investigation;
using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.Jobs
{
    public enum JobShape
    {
        StalkAndChase,      // The full loop, read a crowd, then catch them
        RooftopPursuit,     // Starts spooked and already on the roof network
        Stakeout,           // Identify without being noticed, heat is the fail, not the pressure
        SnatchUnderGuard,   // Stationary target, hostiles around them
    }

    [CreateAssetMenu(fileName = "Job_", menuName = "FPS Parkour/Job", order = 21)]
    public class JobDefinition : ScriptableObject
    {
        [SerializeField] string id = "job.unnamed";
        [SerializeField] string title = "Untitled job";
        [SerializeField] JobShape shape = JobShape.StalkAndChase;
        [SerializeField] BountyContract contract;

        [Header("Briefing")]
        [TextArea(3, 8)]
        [SerializeField] string briefing;
        [SerializeField] string showcases;

        [Header("The job")]
        [SerializeField] JobObjective[] objectives;
        [SerializeField] FailureKind failsOn = FailureKind.None;

        [Header("Dossier")]
        [SerializeField] IdentityMark[] targetMarks;
        [SerializeField] TraitSlot[] knownAtStart;
        [SerializeField, Min(1)] int minimumMarksToAccuse = 2;

        [Header("Availability")]
        [SerializeField] FlagCondition availability = new FlagCondition();
        [SerializeField] string[] flagsOnComplete;

        public string Id => id;
        public string Title => title;
        public JobShape Shape => shape;
        public BountyContract Contract => contract;
        public string Briefing => briefing;
        public string Showcases => showcases;
        public JobObjective[] Objectives => objectives;
        public FailureKind FailsOn => failsOn;
        public IdentityMark[] TargetMarks => targetMarks;
        public TraitSlot[] KnownAtStart => knownAtStart;
        public int MinimumMarksToAccuse => minimumMarksToAccuse;
        public FlagCondition Availability => availability;
        public string[] FlagsOnComplete => flagsOnComplete;

        public int ObjectiveCount => objectives != null ? objectives.Length : 0;
    }
}
