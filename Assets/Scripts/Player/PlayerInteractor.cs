using System;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Player
{
    [DisallowMultipleComponent]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform aimOrigin;
        [SerializeField] float range = 3.5f;
        [SerializeField] float radius = 0.4f;
        [SerializeField] LayerMask mask = ~0;

        public event Action<IInteractable, string> FocusChanged;
        public event Action<IInteractable> Interacted;

        public IInteractable Focus { get; private set; }

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (aimOrigin == null && Camera.main != null) aimOrigin = Camera.main.transform;
        }

        void Update()
        {
            if (aimOrigin == null)
                return;

            SetFocus(FindFocus());

            if (Focus != null && input != null && input.InteractPressed && Focus.CanInteract(gameObject))
            {
                Focus.Interact(gameObject);
                Interacted?.Invoke(Focus);
            }
        }

        IInteractable FindFocus()
        {
            if (!Physics.SphereCast(aimOrigin.position, radius, aimOrigin.forward, out RaycastHit hit,
                    range, mask, QueryTriggerInteraction.Collide))
                return null;

            IInteractable found = hit.collider.GetComponentInParent<IInteractable>();
            return found != null && found.CanInteract(gameObject) ? found : null;
        }

        void SetFocus(IInteractable next)
        {
            if (ReferenceEquals(next, Focus))
                return;

            Focus = next;
            FocusChanged?.Invoke(next, next != null ? next.Prompt : string.Empty);
        }
    }
}
