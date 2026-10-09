using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovementController))]
    public class GroundSlamImpact : MonoBehaviour
    {
        [SerializeField] PlayerMovementController controller;
        [SerializeField] float radius = 6f;
        [SerializeField] float damage = 45f;
        [SerializeField] DamageType damageType = DamageType.Kinetic;
        [SerializeField, Range(0f, 1f)] float minimumFalloff = 0.25f;
        [SerializeField] LayerMask hitMask = ~0;

        readonly Collider[] overlap = new Collider[32];

        void Awake()
        {
            if (controller == null)
                controller = GetComponent<PlayerMovementController>();

            controller.OnGroundSlamImpact += OnImpact;
        }

        void OnDestroy()
        {
            if (controller != null)
                controller.OnGroundSlamImpact -= OnImpact;
        }

        void OnImpact(Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, overlap, hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                IDamageable target = overlap[i].GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, GetComponent<IDamageable>()))
                    continue;

                Vector3 point = overlap[i].ClosestPoint(position);
                float falloff = Mathf.Lerp(1f, minimumFalloff, Mathf.Clamp01(Vector3.Distance(position, point) / radius));

                target.ApplyDamage(new DamageInfo(damage * falloff, damageType, point, (point - position).normalized, gameObject));
            }
        }
    }
}
