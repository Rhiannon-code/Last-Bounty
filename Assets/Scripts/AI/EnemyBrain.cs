using FPSParkour.Combat;
using FPSParkour.Core;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.AI
{
    public enum EnemyState { Idle, Alert, Combat, Search, Downed }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class EnemyBrain : MonoBehaviour
    {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] EnemySenses senses;
        [SerializeField] EnemyWeapon weapon;
        [SerializeField] Health health;
        [SerializeField] Subduable subduable;

        [Header("Ranges")]
        [SerializeField] float preferredRange = 12f;
        [SerializeField] float tooCloseRange = 5f;
        [SerializeField] float searchDuration = 8f;

        [Header("Barks")]
        [SerializeField] BarkPlayer barks;
        [SerializeField] BarkSet onSpotted;
        [SerializeField] BarkSet onLostTarget;
        [SerializeField] BarkSet onDowned;

        float searchEndsAt;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (senses == null) senses = GetComponent<EnemySenses>();
            if (subduable == null) subduable = GetComponent<Subduable>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            if (subduable != null)
                subduable.Downed += OnDowned;

            health.OnDied += OnDied;
        }

        void OnDestroy()
        {
            if (subduable != null)
                subduable.Downed -= OnDowned;

            if (health != null)
                health.OnDied -= OnDied;
        }

        void Update()
        {
            if (State == EnemyState.Downed || senses == null)
                return;

            if (senses.HasLineOfSight)
            {
                if (State != EnemyState.Combat)
                {
                    Enter(EnemyState.Combat);
                    barks?.TryPlay(onSpotted);
                }

                Fight();
                return;
            }

            if (senses.IsAware)
            {
                Enter(EnemyState.Alert);
                MoveTo(senses.LastKnownPosition);
                searchEndsAt = Time.time + searchDuration;
                return;
            }

            if (State == EnemyState.Combat || State == EnemyState.Alert)
            {
                Enter(EnemyState.Search);
                barks?.TryPlay(onLostTarget);
            }

            if (State != EnemyState.Search)
                return;

            if (Time.time >= searchEndsAt)
                Enter(EnemyState.Idle);
            else
                MoveTo(senses.LastKnownPosition);
        }

        void Fight()
        {
            Vector3 targetPosition = senses.Target.position;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > preferredRange)
                MoveTo(targetPosition);
            else if (distance < tooCloseRange)
                MoveTo(transform.position + (transform.position - targetPosition).normalized * tooCloseRange);
            else
                Stop();

            Vector3 flatDirection = targetPosition - transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDirection), 8f * Time.deltaTime);

            weapon?.TryFire(targetPosition + Vector3.up, senses.TargetVelocity);
        }

        void MoveTo(Vector3 position)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(position);
        }

        void Stop()
        {
            if (agent != null && agent.isOnNavMesh)
                agent.ResetPath();
        }

        void Enter(EnemyState next) => State = next;

        void OnDowned()
        {
            Enter(EnemyState.Downed);
            Stop();

            if (agent != null)
                agent.enabled = false;

            barks?.TryPlay(onDowned);
        }

        void OnDied(DamageInfo info)
        {
            Enter(EnemyState.Downed);

            if (agent != null)
                agent.enabled = false;

            enabled = false;
        }
    }
}
