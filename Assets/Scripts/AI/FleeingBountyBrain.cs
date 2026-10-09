using System;
using FPSParkour.Combat;
using FPSParkour.Core;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.AI
{
    public enum FleeState { Blending, Fleeing, Winded, Snared, Downed, Escaped }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class FleeingBountyBrain : MonoBehaviour, ISnareable
    {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Health health;
        [SerializeField] Subduable subduable;
        [SerializeField] EnemySenses senses;
        [SerializeField] Transform[] fleeNodes;

        [Header("Speed")]
        [SerializeField] float sprintSpeed = 7.4f;
        [SerializeField] float windedSpeed = 2.8f;
        [SerializeField] float arriveDistance = 4f;

        [Header("Stamina: the window the player is actually hunting for")]
        [SerializeField] float sprintSeconds = 13f;
        [SerializeField] float windedSeconds = 4.5f;
        [SerializeField, Range(0f, 1f)] float recoveredFraction = 0.65f;

        [Header("Spooking")]
        [SerializeField] float spookDistance = 12f;
        [SerializeField] Behaviour[] disableWhenFleeing;
        [SerializeField, Min(0f)] float levelChangeBonus = 1.4f;

        [Header("Escape")]
        [SerializeField] float escapeDistance = 110f;
        [SerializeField] float escapeAfterSeconds = 14f;

        [Header("Barks")]
        [SerializeField] BarkPlayer barks;
        [SerializeField] BarkSet onSpooked;
        [SerializeField] BarkSet onWinded;

        float stamina;
        float recoverAt;
        float snaredUntil;
        float unseenSince = -1f;
        int nodeIndex = -1;

        public event Action<FleeState> StateChanged;
        public event Action Spooked;
        public event Action Escaped;

        public FleeState State { get; private set; } = FleeState.Blending;
        public bool IsSnared => State == FleeState.Snared;
        public bool IsRunning => State == FleeState.Fleeing || State == FleeState.Winded || State == FleeState.Snared;
        public float StaminaFraction => sprintSeconds > 0f ? Mathf.Clamp01(stamina / sprintSeconds) : 0f;
        public bool Identified { get; set; }

        public Transform Pursuer => senses != null ? senses.Target : null;

        public float DistanceToPursuer =>
            Pursuer != null ? Vector3.Distance(transform.position, Pursuer.position) : float.PositiveInfinity;

        void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (health == null) health = GetComponent<Health>();
            if (subduable == null) subduable = GetComponent<Subduable>();
            if (senses == null) senses = GetComponent<EnemySenses>();

            stamina = sprintSeconds;

            if (subduable != null)
                subduable.Downed += OnDowned;

            health.OnDied += OnDied;
        }

        void OnDestroy()
        {
            if (subduable != null) subduable.Downed -= OnDowned;
            if (health != null) health.OnDied -= OnDied;
        }

        void Update()
        {
            if (State == FleeState.Downed || State == FleeState.Escaped)
                return;

            if (State == FleeState.Blending)
            {
                if (Identified && DistanceToPursuer <= spookDistance)
                    Spook();

                return;
            }

            if (State == FleeState.Snared)
            {
                if (Time.time < snaredUntil)
                    return;

                Resume();
            }

            TickEscape();
            TickStamina();
            Route();
        }

        public void Spook()
        {
            if (State != FleeState.Blending)
                return;

            if (disableWhenFleeing != null)
            {
                foreach (Behaviour behaviour in disableWhenFleeing)
                {
                    if (behaviour != null)
                        behaviour.enabled = false;
                }
            }

            Enter(FleeState.Fleeing);
            barks?.TryPlay(onSpooked);
            Spooked?.Invoke();
            PickNode();
        }

        public void Snare(float seconds, Vector3 from)
        {
            if (State == FleeState.Downed || State == FleeState.Escaped)
                return;

            if (State == FleeState.Blending)
                Spook();

            snaredUntil = Mathf.Max(snaredUntil, Time.time + seconds);
            Enter(FleeState.Snared);
            Stop();
        }

        void Resume()
        {
            Enter(stamina > 0f ? FleeState.Fleeing : FleeState.Winded);
            PickNode();
        }

        void TickStamina()
        {
            if (State == FleeState.Winded)
            {
                agent.speed = windedSpeed;

                if (Time.time < recoverAt)
                    return;

                stamina = sprintSeconds * recoveredFraction;
                Enter(FleeState.Fleeing);
                return;
            }

            agent.speed = sprintSpeed;
            stamina -= Time.deltaTime;

            if (stamina > 0f)
                return;

            recoverAt = Time.time + windedSeconds;
            Enter(FleeState.Winded);
            barks?.TryPlay(onWinded);
        }

        void TickEscape()
        {
            bool seen = senses != null && senses.HasLineOfSight;
            bool close = DistanceToPursuer <= escapeDistance;

            if (seen || close)
            {
                unseenSince = -1f;
                return;
            }

            if (unseenSince < 0f)
                unseenSince = Time.time;
            else if (Time.time - unseenSince >= escapeAfterSeconds)
                GetAway();
        }

        void Route()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            if (nodeIndex < 0 || (!agent.pathPending && agent.remainingDistance <= arriveDistance))
                PickNode();
        }

        void PickNode()
        {
            if (fleeNodes == null || fleeNodes.Length == 0 || agent == null || !agent.isOnNavMesh)
                return;

            Vector3 pursuer = Pursuer != null ? Pursuer.position : transform.position - transform.forward * 20f;
            float ownGap = Vector3.Distance(transform.position, pursuer);

            int best = -1;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < fleeNodes.Length; i++)
            {
                if (fleeNodes[i] == null || i == nodeIndex)
                    continue;

                Vector3 node = fleeNodes[i].position;
                float gap = Vector3.Distance(node, pursuer);

                if (gap <= ownGap)
                    continue;

                float score = gap - 0.6f * Vector3.Distance(node, transform.position);

                score += Mathf.Min(Mathf.Abs(node.y - pursuer.y), 30f) * levelChangeBonus;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = i;
            }

            if (best < 0)
                best = UnityEngine.Random.Range(0, fleeNodes.Length);

            nodeIndex = best;

            if (NavMesh.SamplePosition(fleeNodes[best].position, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        void GetAway()
        {
            Enter(FleeState.Escaped);
            Stop();
            Escaped?.Invoke();
        }

        void OnDowned()
        {
            Enter(FleeState.Downed);
            Stop();

            if (agent != null)
                agent.enabled = false;
        }

        void OnDied(DamageInfo info)
        {
            Enter(FleeState.Downed);

            if (agent != null)
                agent.enabled = false;

            enabled = false;
        }

        void Stop()
        {
            if (agent != null && agent.isOnNavMesh)
                agent.ResetPath();
        }

        void Enter(FleeState next)
        {
            if (State == next)
                return;

            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
