using System;
using UnityEngine;

namespace FPSParkour.Jobs
{
    public enum ObjectiveKind
    {
        ScanAnyone,         // One completed scan, whoever it was
        LearnClues,         // Amount clues in the dossier
        IdentifyTarget,     // A correct accusation
        ChaseBegins,        // The target's cover breaks, however it broke
        ReachHeight,        // Stand Amount metres up, the vertical layer
        SnareTarget,        // The target is rooted
        DownTarget,         // The target is downed, by snare or by fire
        ResolveContract,    // Capture/kill/release chosen at the prompt
        DownHostiles,       // Amount hostiles downed, the guarded snatch
    }

    public enum FailureKind
    {
        None,
        HeatMaxed,          // The stakeout, being noticed IS the loss
        TargetEscapes,
        WrongAccusation,
        PlayerKillsTarget,  // Some contracts are alive only
    }

    [Serializable]
    public struct JobObjective
    {
        public ObjectiveKind Kind;
        public string Description;
        public float Amount;
        [TextArea(2, 4)]
        public string Hint;
    }
}
