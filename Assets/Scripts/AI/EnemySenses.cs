using UnityEngine;

namespace FPSParkour.AI
{
    [DisallowMultipleComponent]
    public class EnemySenses : MonoBehaviour
    {
        [SerializeField] Transform eyes;
        [SerializeField] string targetTag = "Player";
        [SerializeField] float sightRange = 30f;
        [SerializeField, Range(1f, 360f)] float fieldOfView = 120f;
        [SerializeField] float hearingRange = 14f;
        [SerializeField] LayerMask blockers = ~0;
        [SerializeField] float loseTargetAfter = 4f;

        float lastSeenAt = -999f;

        public Transform Target { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public Vector3 TargetVelocity { get; private set; }
        public bool HasLineOfSight { get; private set; }
        public bool IsAware => Time.time - lastSeenAt <= loseTargetAfter;

        void Awake()
        {
            if (eyes == null)
                eyes = transform;
        }

        void Start()
        {
            GameObject found = GameObject.FindGameObjectWithTag(targetTag);
            if (found != null)
                Target = found.transform;
        }

        Vector3 previousTargetPosition;
        bool hasPreviousTarget;

        void Update()
        {
            HasLineOfSight = false;

            if (Target == null)
                return;

            TrackVelocity();

            Vector3 toTarget = Target.position - eyes.position;
            float distance = toTarget.magnitude;

            if (distance > sightRange)
                return;

            bool withinCone = Vector3.Angle(eyes.forward, toTarget) <= fieldOfView * 0.5f;
            bool closeEnoughToHear = distance <= hearingRange;

            if (!withinCone && !closeEnoughToHear)
                return;

            if (Physics.Raycast(eyes.position, toTarget.normalized, out RaycastHit hit, distance, blockers, QueryTriggerInteraction.Ignore)
                && hit.transform != Target && !hit.transform.IsChildOf(Target))
                return;

            HasLineOfSight = true;
            LastKnownPosition = Target.position;
            lastSeenAt = Time.time;
        }

        void TrackVelocity()
        {
            if (Time.deltaTime <= 0f)
                return;

            if (hasPreviousTarget)
            {
                Vector3 measured = (Target.position - previousTargetPosition) / Time.deltaTime;
                TargetVelocity = Vector3.Lerp(TargetVelocity, measured, 0.35f);
            }

            previousTargetPosition = Target.position;
            hasPreviousTarget = true;
        }

        public void ReportNoise(Vector3 position)
        {
            LastKnownPosition = position;
            lastSeenAt = Time.time;
        }
    }
}
