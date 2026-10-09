using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Inventory
{
    [RequireComponent(typeof(InventorySystem))]
    public class InventoryInputBridge : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private InventorySystem inventory;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            inventory.OnOpenStateChanged += HandleOpenStateChanged;
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnOpenStateChanged -= HandleOpenStateChanged;
            if (inventory != null && inventory.IsOpen) HandleOpenStateChanged(false);
        }

        private void Update()
        {
            if (input == null) return;

            if (input.InventoryToggled)
                inventory.ToggleOpen();

            int slot = input.QuickSlotPressed;
            if (slot > 0)
                inventory.SelectQuickSlot(slot - 1); // Input is 1-based, slots are 0-based
        }

        private void HandleOpenStateChanged(bool open)
        {
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            Time.timeScale = open ? 0f : 1f; // Simple pause while browsing, remove for real time inventory
        }
    }
}
