using UnityEngine;
using FPSParkour.Config;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public enum MovementState
    {
        Grounded,
        Airborne,
        Sliding,
        WallRunning,
        Dashing,
        Slamming,
        Grappling,
        Mantling,
    }

    [RequireComponent(typeof(PlayerMotor))]
    public partial class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private MovementConfig config;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private AbilityUnlocks unlocks;

        public MovementState State { get; private set; }
        public Vector3 Velocity => _velocity;

        private Vector3 _velocity;
        private float _coyote;
        private float _jumpBuffer;
        private float _desiredRoll;
        private float _fovBonus;
        private bool _controlClaimed;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (unlocks == null) unlocks = GetComponent<AbilityUnlocks>();
            if (config != null) motor.SetGroundMask(config.worldMask);

            InitGrapple();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            _velocity = motor.Velocity;
            _desiredRoll = 0f;
            _controlClaimed = false;

            UpdateTimers(dt);
            UpdateCrouch(dt);

            if (_mantling)
            {
                State = MovementState.Mantling;
                ApplyCameraFx();
                return;
            }

            if (TickDash(dt)) { Finish(dt); return; }
            if (TickSlam(dt)) { Finish(dt); return; }
            if (TickWallRun(dt)) { Finish(dt); return; }
            if (TickGrapple(dt)) { Finish(dt); return; }

            if (motor.IsGrounded && _velocity.y <= 0.5f)
            {
                _coyote = config.coyoteTime;
                _airJumpsUsed = 0;

                if (TickSlide(dt))
                    State = MovementState.Sliding;
                else
                {
                    GroundMove(dt);
                    State = MovementState.Grounded;
                }
            }
            else
            {
                AirMove(dt);
                State = MovementState.Airborne;
            }

            HandleJump();
            TryStartWallRun();
            HandleAirActions();
            TryStartMantle();
            ApplyGravity(dt);

            Finish(dt);
        }

        private void Finish(float dt)
        {
            motor.Move(_velocity, dt);
            ApplyCameraFx();
        }

        private void UpdateTimers(float dt)
        {
            _coyote -= dt;
            _jumpBuffer -= dt;
            if (input.JumpPressed) _jumpBuffer = config.jumpBufferTime;

            TickDashCooldown(dt);
            TickWallRunCooldown(dt);
        }

        private void GroundMove(float dt)
        {
            Vector3 wish = WishDir();
            float maxSpeed = GroundTargetSpeed();

            Vector3 h = new(_velocity.x, 0f, _velocity.z);

            float speed = h.magnitude;
            if (speed > 0.0001f)
            {
                float drop = speed * config.groundFriction * dt;
                h *= Mathf.Max(speed - drop, 0f) / speed;
            }

            h = Accelerate(h, wish, config.groundAccel, maxSpeed, dt);

            _velocity.x = h.x;
            _velocity.z = h.z;
            if (_velocity.y < 0f) _velocity.y = -2f; // Stick to ground
        }

        private void AirMove(float dt)
        {
            Vector3 wish = WishDir();
            Vector3 h = new(_velocity.x, 0f, _velocity.z);
            h = Accelerate(h, wish, config.airAccel, config.airStrafeMaxSpeed, dt);
            _velocity.x = h.x;
            _velocity.z = h.z;
        }

        private void ApplyGravity(float dt)
        {
            if (motor.IsGrounded && _velocity.y <= 0f) return;
            float g = config.gravity * stats.Mult(StatType.GravityMult);
            _velocity.y += g * dt;
            if (_velocity.y < config.maxFallSpeed)
                _velocity.y = config.maxFallSpeed;
        }

        private void HandleJump()
        {
            if (_jumpBuffer <= 0f) return;

            if (_coyote > 0f)
            {
                DoJump(config.jumpHeight);
                _coyote = 0f;
                _jumpBuffer = 0f;
                _airJumpsUsed = 0;
                return;
            }

            if (TryAirJump())
                _jumpBuffer = 0f;
        }

        private bool Can(AbilityId id) => unlocks != null && unlocks.Has(id);

        private float GroundTargetSpeed()
        {
            if (_isCrouched) return config.crouchSpeed * stats.Mult(StatType.MoveSpeedMult);

            bool sprinting = input.SprintHeld && Can(AbilityId.Sprint) && input.Move.y > 0.1f;
            if (sprinting)
                return config.sprintSpeed * stats.Mult(StatType.SprintSpeedMult);

            return config.walkSpeed * stats.Mult(StatType.MoveSpeedMult);
        }

        private Vector3 WishDir()
        {
            Vector3 f = transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = transform.right; r.y = 0f; r.Normalize();
            Vector3 dir = f * input.Move.y + r * input.Move.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private static Vector3 Accelerate(Vector3 vel, Vector3 wishDir, float accel, float maxSpeed, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f) return vel;
            float current = Vector3.Dot(vel, wishDir);
            float add = maxSpeed - current;
            if (add <= 0f) return vel;
            float accelSpeed = Mathf.Min(accel * dt * maxSpeed, add);
            return vel + wishDir * accelSpeed;
        }

        private void DoJump(float height)
        {
            float g = Mathf.Abs(config.gravity * stats.Mult(StatType.GravityMult));
            float h = height * stats.Mult(StatType.JumpHeightMult);
            _velocity.y = Mathf.Sqrt(2f * g * h);
        }

        private void ApplyCameraFx()
        {
            if (look == null) return;
            look.SetRollTarget(_desiredRoll);

            float hSpeed = new Vector2(_velocity.x, _velocity.z).magnitude;
            float over = Mathf.Max(0f, hSpeed - config.sprintSpeed);
            look.SetFovBonus(_fovBonus + Mathf.Clamp(over * 0.8f, 0f, 15f));
        }
    }
}
