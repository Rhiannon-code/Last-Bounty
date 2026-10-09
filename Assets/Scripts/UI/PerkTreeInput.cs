using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class PerkTreeInput : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PerkTreeView view;

        void Update()
        {
            if (input != null && view != null && input.PerkScreenToggled)
                view.Toggle();
        }
    }
}
