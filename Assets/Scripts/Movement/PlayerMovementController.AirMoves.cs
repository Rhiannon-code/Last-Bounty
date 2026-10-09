using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        private int _airJumpsUsed;

        private float _dashTimer;
        private float _dashCooldownTimer;
        private int _dashChargesLeft = -1; // -1 = not yet initialised
        private Vector3 _dashDir;

        private bool _slamming;

        private int MaxDashCharges =>
            config.dashCharges + Mathf.RoundToInt(stats.Modify(StatType.DashCharges, 0f));

        private void TickDashCooldown(float dt)
        {
            if (_dashChargesLeft < 0) _dashChargesLeft = MaxDashCharges; // Lazy init

            if (_dashChargesLeft >= MaxDashCharges) return;

            _dashCooldownTimer -= dt;
            if (_dashCooldownTimer <= 0f)
            {
                _dashChargesLeft++;
                _dashCooldownTimer = config.dashRechargeTime * stats.Mult(StatType.DashCooldownMult);
            }
        }

        private bool TryAirJump()
        {
            if (!Can(AbilityId.DoubleJump)) return false;
            if (_airJumpsUsed >= config.maxAirJumps) return false;

            _airJumpsUsed++;
            _velocity.y = 0f;
            DoJump(config.airJumpHeight);

            Vector3 wish = WishDir();
            if (wish.sqrMagnitude > 0.01f)
            {
                Vector3 h = new(_velocity.x, 0f, _velocity.z);
                float sp = Mathf.Max(h.magnitude, config.walkSpeed);
                h = Vector3.Lerp(h, wish * sp, 0.5f);
                _velocity.x = h.x;
                _velocity.z = h.z;
            }
            return true;
        }

        private void HandleAirActions()
        {
            if (input.DashPressed && Can(AbilityId.AirDash) && _dashTimer <= 0f && _dashChargesLeft > 0)
                StartDash();

            if (input.CrouchHeld && !motor.IsGrounded && !_slamming
                && Can(AbilityId.GroundSlam)
                && motor.TimeSinceGrounded > config.slamMinAirTime
                && _velocity.y < 2f)
                _slamming = true;
        }

        private void StartDash()
        {
            Vector3 dir = WishDir();
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            _dashDir = dir.normalized;
            _dashTimer = config.dashDuration;
            _dashChargesLeft--;
            _fovBonus = 12f;
        }

        private bool TickDash(float dt)
        {
            if (_dashTimer <= 0f) return false;

            _dashTimer -= dt;
            _velocity = _dashDir * config.dashSpeed;
            State = MovementState.Dashing;

            if (_dashTimer <= 0f) // Dash ended, keep a fraction as momentum
                _velocity = _dashDir * config.dashSpeed * config.dashMomentumKeep;

            return true;
        }

        private bool TickSlam(float dt)
        {
            if (!_slamming) return false;

            _velocity.x *= 0.1f;
            _velocity.z *= 0.1f;
            _velocity.y = -config.slamSpeed;
            State = MovementState.Slamming;

            if (motor.IsGrounded)
            {
                _slamming = false;
                Vector3 f = transform.forward; f.y = 0f; f.Normalize();
                _velocity = f * config.slamLandBoost;

                OnGroundSlamImpact?.Invoke(transform.position);
            }
            return true;
        }

        public System.Action<Vector3> OnGroundSlamImpact;
    }
}
