using UnityEngine;

namespace FPSParkour.Config
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "FPS Parkour/Movement Config", order = 1)]
    public class MovementConfig : ScriptableObject
    {
        [Header("Ground locomotion")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 9.5f;
        public float crouchSpeed = 3.5f;
        public float groundAccel = 90f;
        public float groundFriction = 12f;

        [Header("Air locomotion (momentum/bhop feel)")]
        public float airAccel = 45f;
        public float airStrafeMaxSpeed = 1.4f;

        [Header("Gravity & jump")]
        public float gravity = -24f;
        public float maxFallSpeed = -55f;
        public float jumpHeight = 1.6f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Crouch/capsule")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchLerpSpeed = 12f;

        [Header("Slide")]
        public float slideEntrySpeedBoost = 4f;
        public float slideFriction = 3.5f;
        public float slideMinSpeed = 4f;
        public float slideSteerAccel = 14f;
        public float slideMaxTime = 1.6f;
        public float slopeSlideAccel = 20f;

        [Header("Wall run (BASE ability)")]
        public float wallCheckDistance = 0.75f;
        public float wallRunMinEntrySpeed = 4f;
        public float wallRunSpeed = 9.5f;
        public float wallRunMaxTime = 1.8f;
        public float wallRunGravity = 6f;
        public float wallStickForce = 6f;
        public float wallRunCameraTilt = 15f;
        public float wallRunCooldown = 0.25f;

        [Header("Wall jump (BASE ability)")]
        public float wallJumpUp = 6.5f;
        public float wallJumpSide = 8f;
        public float wallJumpForward = 3f;

        [Header("Double jump (perk)")]
        public int maxAirJumps = 1;
        public float airJumpHeight = 1.4f;

        [Header("Air dash (perk)")]
        public float dashSpeed = 22f;
        public float dashDuration = 0.16f;
        public int dashCharges = 1;
        public float dashRechargeTime = 1.6f;
        public float dashMomentumKeep = 0.6f;

        [Header("Ground slam (perk)")]
        public float slamSpeed = 42f;
        public float slamLandBoost = 6f;
        public float slamMinAirTime = 0.15f;

        [Header("Grapple (perk)")]
        public float grappleRange = 35f;
        public float grappleReelSpeed = 12f;
        public float grappleSpring = 40f;
        public float grappleDamper = 4f;
        public float grappleMaxSpeed = 28f;
        public float grappleMinRope = 3f;
        public LayerMask grappleMask = ~0;

        [Header("Mantle (BASE ability)")]
        public float mantleMaxHeight = 2.2f;
        public float mantleForwardReach = 1.0f;
        public float mantleDuration = 0.28f;

        [Header("Collision")]
        public LayerMask worldMask = ~0;
    }
}
