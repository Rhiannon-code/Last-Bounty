using FPSParkour.Core;
using FPSParkour.Investigation;
using FPSParkour.Movement;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class ContextPromptView : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMovementController movement;
        [SerializeField] TargetScanner scanner;

        [Header("Widgets")]
        [SerializeField] Text label;

        [SerializeField, Min(0.05f)] float refreshEvery = 0.1f;

        float nextRefreshAt;
        string focusPrompt;

        void OnEnable()
        {
            if (interactor != null) interactor.FocusChanged += OnFocusChanged;
        }

        void OnDisable()
        {
            if (interactor != null) interactor.FocusChanged -= OnFocusChanged;
        }

        void OnFocusChanged(IInteractable focus, string prompt) => focusPrompt = focus != null ? prompt : null;

        void Update()
        {
            if (label == null || Time.time < nextRefreshAt)
                return;

            nextRefreshAt = Time.time + refreshEvery;
            label.text = CurrentPrompt();
        }

        string CurrentPrompt()
        {
            if (!string.IsNullOrEmpty(focusPrompt))
                return $"{Key("Interact")}  {focusPrompt}";

            if (scanner != null && scanner.Subject != null && scanner.IsUnlocked)
            {
                return scanner.IsScanning
                    ? $"scanning {Mathf.RoundToInt(scanner.Progress * 100f)}% , keep them in the reticle"
                    : $"hold {Key("Scan")}  scan this stranger";
            }

            if (movement == null)
                return string.Empty;

            if (movement.IsGrappling)
                return "release to keep the momentum";

            if (movement.IsWallRunning && movement.Unlocked(AbilityId.WallJump))
                return $"{Key("Jump")}  jump off the wall";

            if (movement.GrappleAnchorInRange)
                return $"hold {Key("Grapple")}  grapple that surface";

            if (movement.LedgeAhead)
                return "keep pushing forward to climb";

            if (movement.CanWallRunNow)
                return "hold forward along the wall";

            if (movement.WallBeside && !movement.IsGrounded && movement.Unlocked(AbilityId.WallRun)
                && movement.HorizontalSpeed < movement.WallRunEntrySpeed)
                return $"too slow to wall run, needs {movement.WallRunEntrySpeed:F0} m/s on entry";

            if (movement.CanSlideNow)
                return $"{Key("Crouch")}  slide";

            if (movement.CanSlamNow)
                return $"{Key("Crouch")}  ground-slam";

            return string.Empty;
        }

        string Key(string actionName)
        {
            string key = input != null ? input.KeyFor(actionName) : string.Empty;
            return string.IsNullOrEmpty(key) ? actionName : key;
        }
    }
}
