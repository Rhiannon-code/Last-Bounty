using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class ScreenCursor : MonoBehaviour
    {
        [SerializeField] GameObject[] screens;
        [SerializeField] FirstPersonLook look;

        bool free;

        void Start() => Apply(false);

        void Update()
        {
            bool wanted = AnyOpen();

            if (wanted != free)
                Apply(wanted);
        }

        bool AnyOpen()
        {
            if (screens == null)
                return false;

            foreach (GameObject screen in screens)
            {
                if (screen != null && screen.activeInHierarchy)
                    return true;
            }

            return false;
        }

        void Apply(bool wanted)
        {
            free = wanted;
            Cursor.lockState = wanted ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wanted;

            if (look != null)
                look.enabled = !wanted;
        }
    }
}
