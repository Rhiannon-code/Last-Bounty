using FPSParkour.Combat;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.AI
{
    [DisallowMultipleComponent]
    public class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] GameObject projectilePrefab;

        [Header("Round")]
        [SerializeField] float damage = 8f;
        [SerializeField] DamageType damageType = DamageType.Energy;
        [SerializeField] float projectileSpeed = 70f;
        [SerializeField] float gravityScale = 0.6f;
        [SerializeField] float drag = 0.02f;
        [SerializeField] float range = 120f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Behaviour")]
        [SerializeField] float shotsPerSecond = 1.8f;
        [SerializeField] float spreadDegrees = 2.5f;
        [SerializeField, Range(0f, 1f)] float leadAccuracy = 0.8f;
        [SerializeField] int leadIterations = 3;

        float nextShotAt;
        public bool IsReady => Time.time >= nextShotAt;
        public float LeadAccuracy
        {
            get => leadAccuracy;
            set => leadAccuracy = Mathf.Clamp01(value);
        }

        public float ProjectileSpeed
        {
            get => projectileSpeed;
            set => projectileSpeed = Mathf.Max(1f, value);
        }

        public float ShotsPerSecond
        {
            get => shotsPerSecond;
            set => shotsPerSecond = Mathf.Max(0.05f, value);
        }

        public float SpreadDegrees
        {
            get => spreadDegrees;
            set => spreadDegrees = Mathf.Max(0f, value);
        }

        void Awake()
        {
            if (muzzle == null)
                muzzle = transform;
        }

        public bool TryFire(Vector3 targetPosition) => TryFire(targetPosition, Vector3.zero);

        public bool TryFire(Vector3 targetPosition, Vector3 targetVelocity)
        {
            if (!IsReady)
                return false;

            nextShotAt = Time.time + 1f / Mathf.Max(0.01f, shotsPerSecond);

            Vector3 aimPoint = Solve(targetPosition, targetVelocity);
            Vector3 direction = Scatter((aimPoint - muzzle.position).normalized);

            if (projectilePrefab == null)
                return FireRayFallback(direction);

            GameObject spawned = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(direction));

            if (spawned.TryGetComponent(out Projectile projectile))
            {
                projectile.Launch(new BallisticShot
                {
                    Origin = muzzle.position,
                    Velocity = direction * projectileSpeed,
                    Damage = damage,
                    Range = range,
                    Type = damageType,
                    Source = gameObject,
                    Mask = hitMask,
                    ImpactPrefab = null,
                    GravityScale = gravityScale,
                    Drag = drag
                });
            }

            return true;
        }

        Vector3 Solve(Vector3 targetPosition, Vector3 targetVelocity)
        {
            float speed = Mathf.Max(1f, projectileSpeed);
            float flightTime = Vector3.Distance(muzzle.position, targetPosition) / speed;

            for (int i = 0; i < leadIterations; i++)
            {
                Vector3 predicted = targetPosition + targetVelocity * flightTime;
                flightTime = Vector3.Distance(muzzle.position, predicted) / speed;
            }

            Vector3 lead = targetPosition + targetVelocity * (flightTime * leadAccuracy);
            float drop = 0.5f * Mathf.Abs(Physics.gravity.y) * gravityScale * flightTime * flightTime;

            return lead + Vector3.up * drop;
        }

        Vector3 Scatter(Vector3 direction)
        {
            if (spreadDegrees <= 0f)
                return direction;

            return Quaternion.Euler(
                Random.Range(-spreadDegrees, spreadDegrees),
                Random.Range(-spreadDegrees, spreadDegrees),
                0f) * direction;
        }

        bool FireRayFallback(Vector3 direction)
        {
            if (!Physics.Raycast(muzzle.position, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
                return true;

            hit.collider.GetComponentInParent<IDamageable>()?
                .ApplyDamage(new DamageInfo(damage, damageType, hit.point, direction, gameObject));

            return true;
        }
    }
}
