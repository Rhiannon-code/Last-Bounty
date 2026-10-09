using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSParkour.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction _move, _look, _jump, _sprint, _crouch, _dash, _grapple, _fire, _aim, _reload, _inventory, _scroll;
        private InputAction _interact, _gadget, _scan, _perks;
        private InputAction[] _quickSlots;
        private readonly Dictionary<string, InputAction> _byName = new();
        private readonly Dictionary<string, string> _keyText = new();

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }

        public bool JumpPressed { get; private set; }
        public bool JumpReleased { get; private set; }
        public bool DashPressed { get; private set; }
        public bool GrappleHeld { get; private set; }
        public bool GrapplePressed { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool AimHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public int WeaponScroll { get; private set; }
        public bool InventoryToggled { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool GadgetPressed { get; private set; }
        public bool ScanHeld { get; private set; }
        public bool PerkScreenToggled { get; private set; }
        public int QuickSlotPressed { get; private set; } = -1;

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

            _look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            _jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _crouch = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            _dash = new InputAction("Dash", InputActionType.Button, "<Keyboard>/leftAlt");
            _grapple = new InputAction("Grapple", InputActionType.Button, "<Keyboard>/q");
            _fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            _aim = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            _reload = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            _scroll = new InputAction("WeaponScroll", InputActionType.Value, "<Mouse>/scroll/y");
            _inventory = new InputAction("Inventory", InputActionType.Button, "<Keyboard>/tab");
            _interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            _gadget = new InputAction("Gadget", InputActionType.Button, "<Keyboard>/g");
            _scan = new InputAction("Scan", InputActionType.Button, "<Keyboard>/v");
            _perks = new InputAction("PerkScreen", InputActionType.Button, "<Keyboard>/p");

            _quickSlots = new InputAction[4];
            for (int i = 0; i < 4; i++)
                _quickSlots[i] = new InputAction($"Slot{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");

            foreach (var action in new[]
                     {
                         _move, _look, _jump, _sprint, _crouch, _dash, _grapple, _fire, _aim,
                         _reload, _inventory, _scroll, _interact, _gadget, _scan, _perks
                     })
                _byName[action.name] = action;
        }

        public string KeyFor(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return string.Empty;

            if (_keyText.TryGetValue(actionName, out string cached))
                return cached;

            if (!_byName.TryGetValue(actionName, out InputAction action))
                return string.Empty;

            cached = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            _keyText[actionName] = cached;
            return cached;
        }

        private void OnEnable()
        {
            _move.Enable(); _look.Enable(); _jump.Enable(); _sprint.Enable();
            _crouch.Enable(); _dash.Enable(); _grapple.Enable(); _fire.Enable();
            _aim.Enable(); _reload.Enable(); _scroll.Enable(); _inventory.Enable();
            _interact.Enable(); _gadget.Enable(); _scan.Enable(); _perks.Enable();
            foreach (var s in _quickSlots) s.Enable();
        }

        private void OnDisable()
        {
            _move.Disable(); _look.Disable(); _jump.Disable(); _sprint.Disable();
            _crouch.Disable(); _dash.Disable(); _grapple.Disable(); _fire.Disable();
            _aim.Disable(); _reload.Disable(); _scroll.Disable(); _inventory.Disable();
            _interact.Disable(); _gadget.Disable(); _scan.Disable(); _perks.Disable();
            foreach (var s in _quickSlots) s.Disable();
        }

        private void Update()
        {
            Move = _move.ReadValue<Vector2>();
            Look = _look.ReadValue<Vector2>();
            SprintHeld = _sprint.IsPressed();
            CrouchHeld = _crouch.IsPressed();

            JumpPressed = _jump.WasPressedThisFrame();
            JumpReleased = _jump.WasReleasedThisFrame();
            DashPressed = _dash.WasPressedThisFrame();
            GrappleHeld = _grapple.IsPressed();
            GrapplePressed = _grapple.WasPressedThisFrame();
            FireHeld = _fire.IsPressed();
            FirePressed = _fire.WasPressedThisFrame();
            AimHeld = _aim.IsPressed();
            ReloadPressed = _reload.WasPressedThisFrame();
            float scrollY = _scroll.ReadValue<float>();
            WeaponScroll = scrollY > 0.1f ? 1 : (scrollY < -0.1f ? -1 : 0);
            InventoryToggled = _inventory.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            GadgetPressed = _gadget.WasPressedThisFrame();
            ScanHeld = _scan.IsPressed();
            PerkScreenToggled = _perks.WasPressedThisFrame();

            QuickSlotPressed = -1;
            for (int i = 0; i < _quickSlots.Length; i++)
                if (_quickSlots[i].WasPressedThisFrame())
                    QuickSlotPressed = i + 1;
        }
    }
}
