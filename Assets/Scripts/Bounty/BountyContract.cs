using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.Bounty
{
    public enum BountyOutcome { Unresolved, Captured, Killed, Released }

    [CreateAssetMenu(fileName = "Bounty_", menuName = "FPS Parkour/Bounty Contract", order = 20)]
    public class BountyContract : ScriptableObject
    {
        [SerializeField] string id = "bounty.unnamed";
        [SerializeField] string targetName = "Unnamed";
        [SerializeField] string alias;
        [SerializeField] string species;
        [SerializeField] string issuingFactionId;

        [Header("Dossier: the player must be able to know who this is BEFORE deciding")]
        [SerializeField, TextArea(3, 8)] string charges;
        [SerializeField, TextArea(3, 8)] string whoTheyActuallyAre;

        [Header("Payout")]
        [SerializeField, Min(0)] int payoutAlive = 1000;

        [Header("Availability")]
        [SerializeField] FlagCondition availability = new FlagCondition();

        [Header("Flags set by the outcome")]
        [SerializeField] string[] flagsOnCapture;
        [SerializeField] string[] flagsOnKill;
        [SerializeField] string[] flagsOnRelease;

        [Header("Standing")]
        [SerializeField] int standingOnCapture = 3;
        [SerializeField] int standingOnKill = 1;
        [SerializeField] int standingOnRelease = -2;

        public string Id => id;
        public string TargetName => targetName;
        public string Alias => alias;
        public string Species => species;
        public string IssuingFactionId => issuingFactionId;
        public string Charges => charges;
        public string WhoTheyActuallyAre => whoTheyActuallyAre;
        public FlagCondition Availability => availability;

        public string[] FlagsOnCapture => flagsOnCapture;
        public string[] FlagsOnKill => flagsOnKill;
        public string[] FlagsOnRelease => flagsOnRelease;
        public int StandingOnCapture => standingOnCapture;
        public int StandingOnKill => standingOnKill;
        public int StandingOnRelease => standingOnRelease;
        public const float DeadPayoutFraction = 0.5f;
        public int PayoutAlive => payoutAlive;
        public int PayoutDead => Mathf.RoundToInt(payoutAlive * DeadPayoutFraction);
        public int PayoutReleased => 0;

        public int PayoutFor(BountyOutcome outcome)
        {
            switch (outcome)
            {
                case BountyOutcome.Captured: return PayoutAlive;
                case BountyOutcome.Killed: return PayoutDead;
                default: return 0;
            }
        }

        public string[] FlagsFor(BountyOutcome outcome)
        {
            switch (outcome)
            {
                case BountyOutcome.Captured: return flagsOnCapture;
                case BountyOutcome.Killed: return flagsOnKill;
                case BountyOutcome.Released: return flagsOnRelease;
                default: return null;
            }
        }

        public int StandingFor(BountyOutcome outcome)
        {
            switch (outcome)
            {
                case BountyOutcome.Captured: return standingOnCapture;
                case BountyOutcome.Killed: return standingOnKill;
                case BountyOutcome.Released: return standingOnRelease;
                default: return 0;
            }
        }
    }
}
