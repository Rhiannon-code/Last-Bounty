using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        private bool _isCrouched;
        private bool _sliding;
        private float _slideTimer;

        private void UpdateCrouch(float dt)
        {
            var cc = motor.Controller;
            bool wantCrouch = input.CrouchHeld;

            if (!wantCrouch && CeilingBlocked())
                wantCrouch = true;

            float target = wantCrouch ? config.crouchHeight : config.standHeight;
            cc.height = Mathf.Lerp(cc.height, target, config.crouchLerpSpeed * dt);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            _isCrouched = cc.height < (config.standHeight + config.crouchHeight) * 0.5f;
        }

        private bool CeilingBlocked()
        {
            var cc = motor.Controller;
            Vector3 origin = transform.position + Vector3.up * cc.radius;
            float dist = config.standHeight - cc.radius;
            return Physics.SphereCast(origin, cc.radius * 0.9f, Vector3.up,
                out _, dist, config.worldMask, QueryTriggerInteraction.Ignore);
        }

        private bool TickSlide(float dt)
        {
            Vector3 h = new(_velocity.x, 0f, _velocity.z);
            float speed = h.magnitude;

            if (!_sliding)
            {
                bool canStart = Can(AbilityId.Slide) && input.CrouchHeld && speed >= config.slideMinSpeed;
                if (!canStart) return false;

                Vector3 dir = speed > 0.01f ? h / speed : transform.forward;
                _velocity += dir * config.slideEntrySpeedBoost;
                _slideTimer = config.slideMaxTime * stats.Mult(StatType.SlideDurationMult);
                _sliding = true;
            }

            _slideTimer -= dt;

            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, motor.GroundNormal).normalized;
            float steep = 1f - motor.GroundNormal.y; // 0 flat … 1 wall
            h += downhill * (config.slopeSlideAccel * steep * dt);

            speed = h.magnitude;
            if (speed > 0.0001f)
            {
                float drop = speed * config.slideFriction * dt;
                h *= Mathf.Max(speed - drop, 0f) / speed;
            }

            h = Accelerate(h, WishDir(), config.slideSteerAccel, h.magnitude, dt);

            _velocity.x = h.x;
            _velocity.z = h.z;
            if (_velocity.y < 0f) _velocity.y = -2f;

            if (!input.CrouchHeld || _slideTimer <= 0f || h.magnitude < config.slideMinSpeed * 0.6f)
                _sliding = false;

            return _sliding;
        }
    }
}
