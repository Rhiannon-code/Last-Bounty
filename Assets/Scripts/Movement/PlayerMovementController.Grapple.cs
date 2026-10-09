using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        private bool _grappleAttached;
        private Vector3 _grapplePoint;
        private LineRenderer _rope;

        private void InitGrapple()
        {
            _rope = GetComponent<LineRenderer>();
            if (_rope != null)
            {
                _rope.positionCount = 2;
                _rope.enabled = false;
            }
        }

        private Vector3 RopeAnchor()
        {
            var cam = look != null ? look.CameraTransform : null;
            return cam != null ? cam.position : transform.position + Vector3.up * 1.4f;
        }

        private bool TryFindGrapplePoint(out Vector3 point)
        {
            point = default;
            Transform cam = look != null ? look.CameraTransform : transform;
            float range = config.grappleRange * stats.Mult(StatType.GrappleRangeMult);

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range,
                    config.grappleMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }
            return false;
        }

        private bool TickGrapple(float dt)
        {
            if (!_grappleAttached)
            {
                if (input.GrapplePressed && Can(AbilityId.Grapple) && TryFindGrapplePoint(out Vector3 p))
                {
                    _grapplePoint = p;
                    _grappleAttached = true;
                    if (_rope != null) _rope.enabled = true;
                }
                else return false;
            }

            if (!input.GrappleHeld)
            {
                DetachGrapple();
                return false;
            }

            Vector3 anchor = RopeAnchor();
            Vector3 toPoint = _grapplePoint - anchor;
            float dist = toPoint.magnitude;
            Vector3 dir = dist > 0.001f ? toPoint / dist : Vector3.zero;

            _velocity.y += config.gravity * stats.Mult(StatType.GravityMult) * dt;
            _velocity += dir * (config.grappleSpring * dt);
            if (dist > config.grappleMinRope)
                _velocity += dir * (config.grappleReelSpeed * dt);

            _velocity *= 1f - config.grappleDamper * dt;
            if (_velocity.magnitude > config.grappleMaxSpeed)
                _velocity = _velocity.normalized * config.grappleMaxSpeed;

            if (_rope != null)
            {
                _rope.SetPosition(0, anchor);
                _rope.SetPosition(1, _grapplePoint);
            }

            State = MovementState.Grappling;

            if (dist <= config.grappleMinRope)
            {
                DetachGrapple();
                return false; // Keep momentum, hand back to normal locomotion
            }
            return true;
        }

        private void DetachGrapple()
        {
            _grappleAttached = false;
            if (_rope != null) _rope.enabled = false;
        }
    }
}
