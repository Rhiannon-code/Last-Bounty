using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class SnarePod : MonoBehaviour
    {
        [SerializeField] float burstRadius = 4.5f;
        [SerializeField] float snareSeconds = 3.5f;
        [SerializeField] float armAfterSeconds = 0.15f;
        [SerializeField] float selfDestructAfter = 12f;
        [SerializeField] LayerMask catchMask = ~0;
        [SerializeField] GameObject burstPrefab;

        float armedAt;
        float durationScale = 1f;
        bool spent;

        void Awake()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            armedAt = Time.time + armAfterSeconds;
            Destroy(gameObject, selfDestructAfter);
        }

        public void Throw(Vector3 velocity, float snareDurationScale)
        {
            durationScale = Mathf.Max(0.1f, snareDurationScale);
            GetComponent<Rigidbody>().linearVelocity = velocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (spent || Time.time < armedAt)
                return;

            Burst();
        }

        void Burst()
        {
            spent = true;

            Collider[] caught = Physics.OverlapSphere(transform.position, burstRadius, catchMask, QueryTriggerInteraction.Ignore);
            ISnareable last = null;

            foreach (Collider collider in caught)
            {
                ISnareable snareable = collider.GetComponentInParent<ISnareable>();

                if (snareable == null || ReferenceEquals(snareable, last))
                    continue;

                snareable.Snare(snareSeconds * durationScale, transform.position);
                last = snareable;
            }

            if (burstPrefab != null)
                Destroy(Instantiate(burstPrefab, transform.position, Quaternion.identity), 4f);

            Destroy(gameObject);
        }
    }
}
