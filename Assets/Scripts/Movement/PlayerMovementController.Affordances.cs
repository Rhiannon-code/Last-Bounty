using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        public bool IsGrounded => motor != null && motor.IsGrounded;

        public bool Unlocked(AbilityId id) => Can(id);

        public float HorizontalSpeed
        {
            get
            {
                Vector3 v = _velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }
        }

        public bool LedgeAhead => !_mantling && FindMantleTarget(out _);

        public bool WallBeside => WallCheck(out _, out _);
        public bool CanWallRunNow =>
            !_wallRunning && Can(AbilityId.WallRun) && motor != null && !motor.IsGrounded
            && _wallRunCooldownTimer <= 0f
            && HorizontalSpeed >= config.wallRunMinEntrySpeed
            && WallBeside;

        public bool IsWallRunning => _wallRunning;

        public float WallRunEntrySpeed => config != null ? config.wallRunMinEntrySpeed : 0f;

        public bool GrappleAnchorInRange => Can(AbilityId.Grapple) && !_grappleAttached && TryFindGrapplePoint(out _);

        public bool IsGrappling => _grappleAttached;

        public int DashCharges => Mathf.Max(0, _dashChargesLeft);

        public int DashChargesMax => config != null ? MaxDashCharges : 0;

        public bool CanDashNow => Can(AbilityId.AirDash) && _dashTimer <= 0f && _dashChargesLeft > 0;

        public int AirJumpsLeft =>
            Can(AbilityId.DoubleJump) ? Mathf.Max(0, config.maxAirJumps - _airJumpsUsed) : 0;

        public bool CanAirJumpNow => AirJumpsLeft > 0 && motor != null && !motor.IsGrounded;

        public bool CanSlamNow =>
            Can(AbilityId.GroundSlam) && !_slamming && motor != null && !motor.IsGrounded
            && motor.TimeSinceGrounded > config.slamMinAirTime;

        public bool CanSlideNow =>
            Can(AbilityId.Slide) && !_sliding && motor != null && motor.IsGrounded
            && HorizontalSpeed >= config.slideMinSpeed;

        public float SlideEntrySpeed => config != null ? config.slideMinSpeed : 0f;

        public bool IsSliding => _sliding;
    }
}
