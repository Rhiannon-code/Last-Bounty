using FPSParkour.AI;
using FPSParkour.Combat;
using FPSParkour.Core;
using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.Presentation
{
    [DisallowMultipleComponent]
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Subduable subduable;
        [SerializeField] Health health;
        [SerializeField] float speedSmoothing = 8f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int DownedId = Animator.StringToHash("Downed");
        static readonly int HitId = Animator.StringToHash("Hit");

        float speed;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (subduable == null) subduable = GetComponent<Subduable>();
            if (health == null) health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (subduable != null)
            {
                subduable.Downed += OnDowned;
                subduable.Revived += OnRevived;
            }

            if (health != null)
                health.OnDamaged += OnDamaged;
        }

        void OnDisable()
        {
            if (subduable != null)
            {
                subduable.Downed -= OnDowned;
                subduable.Revived -= OnRevived;
            }

            if (health != null)
                health.OnDamaged -= OnDamaged;
        }

        void Update()
        {
            if (animator == null || agent == null)
                return;

            float measured = agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            speed = Mathf.Lerp(speed, measured, speedSmoothing * Time.deltaTime);
            animator.SetFloat(SpeedId, speed);
        }

        void OnDowned()
        {
            animator?.SetBool(DownedId, true);
            speed = 0f;
        }

        void OnRevived() => animator?.SetBool(DownedId, false);

        void OnDamaged(float dealt, DamageInfo info) => animator?.SetTrigger(HitId);
    }
}
