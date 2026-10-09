using FPSParkour.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class MomentumView : MonoBehaviour
    {
        [SerializeField] PlayerMovementController movement;

        [Header("Widgets")]
        [SerializeField] Text stateLabel;
        [SerializeField] RectTransform speedFill;
        [SerializeField] Image speedFillImage;
        [SerializeField] RectTransform slideMark;
        [SerializeField] RectTransform wallRunMark;
        [SerializeField, Min(1f)] float fullScaleSpeed = 20f;
        [SerializeField] Color belowThreshold = new Color(0.78f, 0.78f, 0.78f, 0.9f);
        [SerializeField] Color aboveThreshold = new Color(0.55f, 0.91f, 0.63f, 0.95f);
        [SerializeField] Color active = new Color(1f, 0.78f, 0.35f, 0.95f);

        void Start()
        {
            if (movement == null)
                return;

            PlaceMark(slideMark, movement.SlideEntrySpeed);
            PlaceMark(wallRunMark, movement.WallRunEntrySpeed);
        }

        void PlaceMark(RectTransform mark, float speed)
        {
            if (mark == null)
                return;

            float x = Mathf.Clamp01(speed / fullScaleSpeed);
            mark.anchorMin = new Vector2(x, 0f);
            mark.anchorMax = new Vector2(x, 1f);
            mark.anchoredPosition = Vector2.zero;
        }

        void Update()
        {
            if (movement == null)
                return;

            float speed = movement.HorizontalSpeed;

            if (stateLabel != null)
                stateLabel.text = $"{Describe(movement.State)}    {speed:F1} m/s";

            if (speedFill != null)
                speedFill.anchorMax = new Vector2(Mathf.Clamp01(speed / fullScaleSpeed), 1f);

            if (speedFillImage != null)
                speedFillImage.color = FillColour(speed);
        }

        Color FillColour(float speed)
        {
            switch (movement.State)
            {
                case MovementState.Sliding:
                case MovementState.WallRunning:
                case MovementState.Dashing:
                case MovementState.Grappling:
                case MovementState.Slamming:
                    return active;
                default:
                    return speed >= movement.SlideEntrySpeed ? aboveThreshold : belowThreshold;
            }
        }

        static string Describe(MovementState state)
        {
            switch (state)
            {
                case MovementState.Grounded: return "ON FOOT";
                case MovementState.Airborne: return "AIRBORNE";
                case MovementState.Sliding: return "SLIDING";
                case MovementState.WallRunning: return "WALL-RUNNING";
                case MovementState.Dashing: return "DASHING";
                case MovementState.Slamming: return "SLAMMING";
                case MovementState.Grappling: return "GRAPPLING";
                case MovementState.Mantling: return "MANTLING";
                default: return state.ToString().ToUpperInvariant();
            }
        }
    }
}
