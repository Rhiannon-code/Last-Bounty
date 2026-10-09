using UnityEngine;

namespace FPSParkour.Player
{
    public class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private Transform playerBody;   // Yaw
        [SerializeField] private Transform cameraPivot;  // Pitch + roll
        [SerializeField] private Camera cam;
        [SerializeField] private PlayerInputReader input;

        [Header("Look")]
        [SerializeField] private float sensitivity = 0.08f;
        [SerializeField] private float pitchClamp = 89f;

        [Header("Camera juice")]
        [SerializeField] private float baseFov = 90f;
        [SerializeField] private float rollLerpSpeed = 10f;
        [SerializeField] private float fovLerpSpeed = 8f;
        [SerializeField] private float recoilRecoverSpeed = 12f;

        private float _pitch;
        private float _rollTarget;
        private float _roll;
        private float _speedFovBonus;  // From movement (speed lines / dash)
        private float _aimFovOffset;   // From weapon ADS (usually negative)
        private float _recoilPitch;
        private float _recoilYaw;

        public void SetRollTarget(float degrees) => _rollTarget = degrees;
        public void SetFovBonus(float bonus) => _speedFovBonus = bonus;
        public void SetAimFovOffset(float offset) => _aimFovOffset = offset;
        public void AddRecoilKick(float pitch, float yaw)
        {
            _recoilPitch += pitch;
            _recoilYaw += yaw;
        }

        public Transform CameraTransform => cameraPivot;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) cam.fieldOfView = baseFov;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            Vector2 look = input.Look * sensitivity;

            playerBody.Rotate(Vector3.up, look.x, Space.Self);

            _pitch = Mathf.Clamp(_pitch - look.y, -pitchClamp, pitchClamp);
            _roll = Mathf.Lerp(_roll, _rollTarget, rollLerpSpeed * Time.deltaTime);

            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, recoilRecoverSpeed * Time.deltaTime);
            _recoilYaw = Mathf.Lerp(_recoilYaw, 0f, recoilRecoverSpeed * Time.deltaTime);

            cameraPivot.localRotation = Quaternion.Euler(_pitch - _recoilPitch, _recoilYaw, _roll);

            if (cam != null)
            {
                float target = baseFov + _speedFovBonus + _aimFovOffset;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, fovLerpSpeed * Time.deltaTime);
            }
        }
    }
}
