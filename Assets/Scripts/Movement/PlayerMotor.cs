using UnityEngine;

namespace FPSParkour.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundSnapProbe = 0.2f;

        public CharacterController Controller { get; private set; }
        public Vector3 Velocity;

        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float TimeSinceGrounded { get; private set; }
        public bool HitCeiling { get; private set; }

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
        }

        public void SetGroundMask(LayerMask mask) => groundMask = mask;

        public void Move(Vector3 velocity, float dt)
        {
            Velocity = velocity;
            CollisionFlags flags = Controller.Move(velocity * dt);
            HitCeiling = (flags & CollisionFlags.Above) != 0;
            UpdateGrounded();
        }

        private void UpdateGrounded()
        {
            float radius = Controller.radius * 0.9f;
            Vector3 origin = transform.position + Controller.center
                             - Vector3.up * (Controller.height * 0.5f - Controller.radius);
            float castDist = Controller.radius + Controller.skinWidth + groundSnapProbe;

            bool grounded = Physics.SphereCast(origin, radius, Vector3.down,
                out RaycastHit hit, castDist, groundMask, QueryTriggerInteraction.Ignore);

            if (grounded && Velocity.y <= 0.5f)
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
                TimeSinceGrounded = 0f;
            }
            else
            {
                IsGrounded = false;
                GroundNormal = Vector3.up;
                TimeSinceGrounded += Time.deltaTime;
            }
        }

        public void SetPosition(Vector3 position)
        {
            Controller.enabled = false;
            transform.position = position;
            Controller.enabled = true;
        }
    }
}
