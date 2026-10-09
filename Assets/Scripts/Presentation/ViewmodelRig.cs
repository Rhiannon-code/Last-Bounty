using FPSParkour.Combat;
using FPSParkour.Movement;
using UnityEngine;

namespace FPSParkour.Presentation
{
    [DisallowMultipleComponent]
    public class ViewmodelRig : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] PlayerMovementController movement;
        [SerializeField] WeaponController weapons;

        [Header("Sway: the cheapest thing that stops arms looking pasted on")]
        [SerializeField] Transform swayPivot;
        [SerializeField] float swayDegrees = 3.5f;
        [SerializeField] float swayResponse = 9f;
        [SerializeField] float bobHeight = 0.012f;
        [SerializeField] float bobSpeed = 9f;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int StateId = Animator.StringToHash("State");
        static readonly int FireId = Animator.StringToHash("Fire");
        static readonly int ReloadId = Animator.StringToHash("Reload");

        Vector3 restPosition;
        Quaternion restRotation;
        float bobPhase;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (swayPivot == null)
                swayPivot = transform;

            restPosition = swayPivot.localPosition;
            restRotation = swayPivot.localRotation;
        }

        void OnEnable()
        {
            if (weapons == null)
                return;

            weapons.OnFired += OnFired;
            weapons.OnReloadStarted += OnReloadStarted;
        }

        void OnDisable()
        {
            if (weapons == null)
                return;

            weapons.OnFired -= OnFired;
            weapons.OnReloadStarted -= OnReloadStarted;
        }

        void LateUpdate()
        {
            if (movement == null)
                return;

            Vector3 velocity = movement.Velocity;
            float horizontal = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            if (animator != null)
            {
                animator.SetFloat(SpeedId, horizontal);
                animator.SetBool(GroundedId, movement.State == MovementState.Grounded);
                animator.SetInteger(StateId, (int)movement.State);
            }

            Sway(horizontal);
        }

        void Sway(float horizontalSpeed)
        {
            if (swayPivot == null)
                return;

            Vector3 local = swayPivot.parent != null
                ? swayPivot.parent.InverseTransformDirection(movement.Velocity)
                : movement.Velocity;

            Quaternion target = restRotation * Quaternion.Euler(
                Mathf.Clamp(local.y * 0.4f, -swayDegrees, swayDegrees),
                Mathf.Clamp(local.x * 0.4f, -swayDegrees, swayDegrees),
                Mathf.Clamp(-local.x * 0.6f, -swayDegrees, swayDegrees));

            swayPivot.localRotation = Quaternion.Slerp(swayPivot.localRotation, target, swayResponse * Time.deltaTime);

            bobPhase += horizontalSpeed * bobSpeed * Time.deltaTime;
            float bob = Mathf.Sin(bobPhase) * bobHeight * Mathf.Clamp01(horizontalSpeed / 6f);
            swayPivot.localPosition = restPosition + new Vector3(0f, bob, 0f);
        }

        void OnFired(WeaponInstance instance) => animator?.SetTrigger(FireId);

        void OnReloadStarted() => animator?.SetTrigger(ReloadId);
    }
}
