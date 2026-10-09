using System.Collections;
using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Movement
{
    public partial class PlayerMovementController
    {
        private bool _mantling;

        private void TryStartMantle()
        {
            if (_mantling || input.Move.y < 0.1f) return; // Only when moving into the ledge
            if (!FindMantleTarget(out Vector3 target)) return;

            StartCoroutine(MantleRoutine(target));
        }

        private bool FindMantleTarget(out Vector3 target)
        {
            target = default;
            if (!Can(AbilityId.Mantle)) return false;

            var cc = motor.Controller;
            Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
            Vector3 chest = transform.position + Vector3.up * (cc.height * 0.5f);
            float reach = config.mantleForwardReach + cc.radius;

            if (!Physics.Raycast(chest, forward, out RaycastHit wallHit, reach,
                    config.worldMask, QueryTriggerInteraction.Ignore))
                return false;
            if (!IsWall(wallHit.normal)) return false;

            Vector3 probe = transform.position + forward * reach + Vector3.up * config.mantleMaxHeight;
            if (!Physics.Raycast(probe, Vector3.down, out RaycastHit topHit, config.mantleMaxHeight,
                    config.worldMask, QueryTriggerInteraction.Ignore))
                return false;

            float ledgeHeight = topHit.point.y - transform.position.y;
            if (ledgeHeight < 0.2f || ledgeHeight > config.mantleMaxHeight) return false;

            target = topHit.point + Vector3.up * 0.05f;
            return !Physics.CheckSphere(target + Vector3.up * cc.radius, cc.radius * 0.9f,
                config.worldMask, QueryTriggerInteraction.Ignore);
        }

        private IEnumerator MantleRoutine(Vector3 target)
        {
            _mantling = true;
            _velocity = Vector3.zero;
            motor.Velocity = Vector3.zero;

            Vector3 start = transform.position;
            float t = 0f;
            float dur = Mathf.Max(0.01f, config.mantleDuration);

            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);

                Vector3 pos = Vector3.Lerp(start, target, k);
                pos.y = Mathf.Lerp(start.y, target.y, Mathf.Clamp01(k * 1.6f));
                motor.SetPosition(pos);
                yield return null;
            }

            motor.SetPosition(target);
            motor.Velocity = Vector3.zero;
            _mantling = false;
        }
    }
}
