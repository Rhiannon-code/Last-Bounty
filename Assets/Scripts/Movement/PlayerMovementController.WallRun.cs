using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        private bool _wallRunning;
        private float _wallRunTimer;
        private float _wallRunCooldownTimer;
        private Vector3 _wallNormal = Vector3.up;
        private int _wallSide; // -1 = wall on left, +1 = wall on right

        private void TickWallRunCooldown(float dt)
        {
            if (_wallRunCooldownTimer > 0f) _wallRunCooldownTimer -= dt;
        }

        private void TryStartWallRun()
        {
            if (_wallRunning || !Can(AbilityId.WallRun)) return;
            if (motor.IsGrounded || _wallRunCooldownTimer > 0f) return;

            Vector3 h = new(_velocity.x, 0f, _velocity.z);
            if (h.magnitude < config.wallRunMinEntrySpeed) return;
            if (input.Move.y < 0.1f) return;                 // Must be pushing forward
            if (!WallCheck(out Vector3 n, out int side)) return;

            _wallRunning = true;
            _wallNormal = n;
            _wallSide = side;
            _wallRunTimer = config.wallRunMaxTime * stats.Mult(StatType.WallRunDurationMult);
            if (_velocity.y < 0f) _velocity.y = 0f;          // Catch onto the wall
        }

        private bool TickWallRun(float dt)
        {
            if (!_wallRunning) return false;

            if (motor.IsGrounded || !WallCheck(out Vector3 n, out int side))
            {
                EndWallRun();
                return false;
            }

            _wallNormal = n;
            _wallSide = side;
            _wallRunTimer -= dt;
            if (_wallRunTimer <= 0f)
            {
                EndWallRun();
                return false;
            }

            if (_jumpBuffer > 0f && Can(AbilityId.WallJump))
            {
                Vector3 launch = _wallNormal * config.wallJumpSide
                               + Vector3.up * config.wallJumpUp
                               + transform.forward * config.wallJumpForward;
                _velocity = launch;
                _jumpBuffer = 0f;
                _airJumpsUsed = 0;
                EndWallRun();
                return true;
            }

            Vector3 along = Vector3.ProjectOnPlane(transform.forward, _wallNormal).normalized;
            Vector3 vel = along * config.wallRunSpeed;
            vel -= _wallNormal * 1.5f;                        // Gentle stick into the wall
            vel.y = _velocity.y - config.wallRunGravity * dt;
            vel.y = Mathf.Max(vel.y, -config.wallRunSpeed);
            _velocity = vel;

            _desiredRoll = side * config.wallRunCameraTilt;   // Lean into the wall
            _airJumpsUsed = 0;                                // Refresh double jump off a wall
            State = MovementState.WallRunning;
            return true;
        }

        private void EndWallRun()
        {
            _wallRunning = false;
            _wallRunCooldownTimer = config.wallRunCooldown;
        }

        private bool WallCheck(out Vector3 normal, out int side)
        {
            var cc = motor.Controller;
            Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);

            if (Physics.Raycast(origin, transform.right, out RaycastHit hitR,
                    config.wallCheckDistance, config.worldMask, QueryTriggerInteraction.Ignore)
                && IsWall(hitR.normal))
            {
                normal = hitR.normal; side = 1; return true;
            }

            if (Physics.Raycast(origin, -transform.right, out RaycastHit hitL,
                    config.wallCheckDistance, config.worldMask, QueryTriggerInteraction.Ignore)
                && IsWall(hitL.normal))
            {
                normal = hitL.normal; side = -1; return true;
            }

            normal = Vector3.up; side = 0; return false;
        }

        private static bool IsWall(Vector3 normal) => Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.3f;
    }
}
