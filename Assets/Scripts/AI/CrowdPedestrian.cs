using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.AI
{
    public enum PedestrianState { Strolling, Waiting, Fleeing }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CrowdPedestrian : MonoBehaviour
    {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] float wanderRadius = 25f;
        [SerializeField] Vector2 waitSeconds = new Vector2(1f, 4f);
        [SerializeField] float arriveDistance = 1.2f;

        [Header("Panic")]
        [SerializeField] float fleeSpeedMultiplier = 2.2f;
        [SerializeField] float fleeDistance = 30f;
        [SerializeField] float calmAfterSeconds = 7f;

        Vector3 home;
        float resumeAt;
        float calmAt;
        float strollSpeed;

        public PedestrianState State { get; private set; } = PedestrianState.Strolling;

        void Awake()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            home = transform.position;
            strollSpeed = agent.speed;
        }

        void OnEnable()
        {
            if (CityAlarm.Instance != null)
                CityAlarm.Instance.Raised += OnAlarm;
        }

        void OnDisable()
        {
            if (CityAlarm.Instance != null)
                CityAlarm.Instance.Raised -= OnAlarm;
        }

        void Start() => PickNewDestination();

        void Update()
        {
            if (!agent.isOnNavMesh)
                return;

            if (State == PedestrianState.Fleeing)
            {
                if (Time.time < calmAt)
                    return;

                State = PedestrianState.Strolling;
                agent.speed = strollSpeed;
                PickNewDestination();
                return;
            }

            if (State == PedestrianState.Waiting)
            {
                if (Time.time >= resumeAt)
                    PickNewDestination();

                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            {
                State = PedestrianState.Waiting;
                resumeAt = Time.time + Random.Range(waitSeconds.x, waitSeconds.y);
            }
        }

        void OnAlarm(Vector3 position, float radius)
        {
            if (!agent.isOnNavMesh || Vector3.Distance(transform.position, position) > radius)
                return;

            Vector3 away = (transform.position - position).normalized * fleeDistance;

            if (!TrySample(transform.position + away, fleeDistance, out Vector3 destination))
                return;

            State = PedestrianState.Fleeing;
            calmAt = Time.time + calmAfterSeconds;
            agent.speed = strollSpeed * fleeSpeedMultiplier;
            agent.SetDestination(destination);
        }

        void PickNewDestination()
        {
            State = PedestrianState.Strolling;

            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);

            if (TrySample(candidate, wanderRadius, out Vector3 destination))
                agent.SetDestination(destination);
            else
                resumeAt = Time.time + 1f;
        }

        static bool TrySample(Vector3 around, float range, out Vector3 result)
        {
            if (NavMesh.SamplePosition(around, out NavMeshHit hit, range, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = around;
            return false;
        }
    }
}
