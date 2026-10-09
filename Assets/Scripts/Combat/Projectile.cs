using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Combat
{
    public struct BallisticShot
    {
        public Vector3 Origin;
        public Vector3 Velocity;
        public float Damage;
        public float Range;
        public DamageType Type;
        public GameObject Source;
        public LayerMask Mask;
        public GameObject ImpactPrefab;
        public float GravityScale;
        public float Drag;
    }

    public class Projectile : MonoBehaviour
    {
        [SerializeField] float radius = 0.045f;
        [SerializeField] float impactImpulse = 6f;

        [Header("Ricochet")]
        [SerializeField] float maxRicochetAngle = 22f;
        [SerializeField] int maxRicochets = 2;
        [SerializeField, Range(0f, 1f)] float ricochetSpeedLoss = 0.35f;
        [SerializeField] float minRicochetSpeed = 15f;

        [Header("Life")]
        [SerializeField] float maxLifetime = 8f;
        [SerializeField] bool alignToVelocity = true;

        Vector3 velocity;
        float gravityScale;
        float drag;
        float damage;
        float rangeLeft;
        float diesAt;
        DamageType type;
        GameObject source;
        LayerMask mask;
        GameObject impactPrefab;
        int ricochetsLeft;

        public Vector3 Velocity => velocity;
        public float Speed => velocity.magnitude;

        public void Launch(in BallisticShot shot)
        {
            transform.position = shot.Origin;
            velocity = shot.Velocity;
            damage = shot.Damage;
            rangeLeft = shot.Range;
            type = shot.Type;
            source = shot.Source;
            mask = shot.Mask;
            impactPrefab = shot.ImpactPrefab;
            gravityScale = shot.GravityScale;
            drag = shot.Drag;
            ricochetsLeft = maxRicochets;
            diesAt = Time.time + maxLifetime;

            Face();
        }

        public void Init(float speed, float range, float dmg, DamageType damageType,
            GameObject shooter, LayerMask hitMask, GameObject impact)
        {
            Launch(new BallisticShot
            {
                Origin = transform.position,
                Velocity = transform.forward * speed,
                Damage = dmg,
                Range = range,
                Type = damageType,
                Source = shooter,
                Mask = hitMask,
                ImpactPrefab = impact,
                GravityScale = 0f,
                Drag = 0f
            });
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            velocity += Physics.gravity * (gravityScale * dt);

            if (drag > 0f)
                velocity *= Mathf.Clamp01(1f - drag * dt);

            Vector3 step = velocity * dt;
            float distance = step.magnitude;

            if (distance <= Mathf.Epsilon)
                return;

            if (Physics.SphereCast(transform.position, radius, step / distance, out RaycastHit hit,
                    distance, mask, QueryTriggerInteraction.Ignore))
            {
                if (!ReferenceEquals(hit.collider.gameObject, source))
                {
                    Resolve(hit);
                    return;
                }
            }

            transform.position += step;
            rangeLeft -= distance;
            Face();

            if (rangeLeft <= 0f || Time.time >= diesAt)
                Destroy(gameObject);
        }

        void Resolve(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.ApplyDamage(new DamageInfo(damage, type, hit.point, velocity.normalized, source));
                Impact(hit);
                Destroy(gameObject);
                return;
            }

            if (hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(velocity.normalized * impactImpulse, hit.point, ForceMode.Impulse);

            float grazeAngle = 90f - Vector3.Angle(-velocity.normalized, hit.normal);

            if (ricochetsLeft > 0 && grazeAngle <= maxRicochetAngle && Speed >= minRicochetSpeed)
            {
                ricochetsLeft--;
                transform.position = hit.point + hit.normal * (radius * 1.5f);
                velocity = Vector3.Reflect(velocity, hit.normal) * (1f - ricochetSpeedLoss);
                Face();
                return;
            }

            Impact(hit);
            Destroy(gameObject);
        }

        void Impact(RaycastHit hit)
        {
            if (impactPrefab == null)
                return;

            GameObject effect = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 5f);
        }

        void Face()
        {
            if (alignToVelocity && velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}
