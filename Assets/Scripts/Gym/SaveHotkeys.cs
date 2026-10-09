using FPSParkour.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class SaveHotkeys : MonoBehaviour
    {
        [SerializeField] SaveCoordinator coordinator;
        [SerializeField] string slot = "testbed";

        public string LastResult { get; private set; } = string.Empty;

        void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (coordinator == null || keyboard == null || !keyboard.leftCtrlKey.isPressed)
                return;

            if (keyboard.sKey.wasPressedThisFrame)
                LastResult = coordinator.Save(slot) ? $"saved '{slot}'" : "save FAILED";
            else if (keyboard.lKey.wasPressedThisFrame)
                LastResult = coordinator.Load(slot) ? $"loaded '{slot}'" : "no save to load";
            else
                return;

            Debug.Log($"[Save] {LastResult}");
        }
    }
}
