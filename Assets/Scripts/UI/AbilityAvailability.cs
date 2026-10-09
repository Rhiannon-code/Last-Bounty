using FPSParkour.Combat;
using FPSParkour.Investigation;
using FPSParkour.Movement;
using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class AbilityAvailability : MonoBehaviour
    {
        [SerializeField] AbilityUnlocks unlocks;
        [SerializeField] PlayerMovementController movement;
        [SerializeField] GadgetThrower gadget;
        [SerializeField] TargetScanner scanner;

        public bool Owns(AbilityId id) => unlocks == null || unlocks.Has(id);

        public bool UsableNow(AbilityId id)
        {
            if (!Owns(id))
                return false;

            switch (id)
            {
                case AbilityId.Slide: return movement != null && movement.CanSlideNow;
                case AbilityId.Mantle: return movement != null && movement.LedgeAhead;
                case AbilityId.WallRun: return movement != null && movement.CanWallRunNow;
                case AbilityId.WallJump: return movement != null && movement.IsWallRunning;
                case AbilityId.DoubleJump: return movement != null && movement.CanAirJumpNow;
                case AbilityId.AirDash: return movement != null && movement.CanDashNow;
                case AbilityId.Grapple: return movement != null && (movement.GrappleAnchorInRange || movement.IsGrappling);
                case AbilityId.GroundSlam: return movement != null && movement.CanSlamNow;
                case AbilityId.SnareLauncher: return gadget != null && gadget.Ready;
                case AbilityId.Scanner: return scanner != null && scanner.Subject != null;
                default: return true;
            }
        }

        public string StateText(AbilityId id)
        {
            switch (id)
            {
                case AbilityId.AirDash:
                    return movement != null ? $"{movement.DashCharges}/{movement.DashChargesMax}" : string.Empty;
                case AbilityId.DoubleJump:
                    return movement != null ? $"{movement.AirJumpsLeft} left" : string.Empty;
                case AbilityId.SnareLauncher:
                    return gadget != null ? $"{gadget.Charges}/{gadget.MaxCharges}" : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
