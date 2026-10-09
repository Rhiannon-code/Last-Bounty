using System.Text;
using FPSParkour.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class WeaponBarView : MonoBehaviour
    {
        [SerializeField] WeaponController weapons;

        [Header("Widgets")]
        [SerializeField] Text label;

        [SerializeField] string selectedColour = "#FFD27F";
        [SerializeField] string carriedColour = "#9AA0A6";

        readonly StringBuilder builder = new StringBuilder();

        void OnEnable()
        {
            if (weapons == null)
                return;

            weapons.OnWeaponChanged += OnWeaponChanged;
            weapons.OnAmmoChanged += OnAmmoChanged;
            Rebuild();
        }

        void OnDisable()
        {
            if (weapons == null)
                return;

            weapons.OnWeaponChanged -= OnWeaponChanged;
            weapons.OnAmmoChanged -= OnAmmoChanged;
        }

        void OnWeaponChanged(WeaponInstance instance) => Rebuild();
        void OnAmmoChanged(int magazine, int reserve) => Rebuild();

        void Rebuild()
        {
            if (label == null || weapons == null)
                return;

            builder.Clear();

            for (int i = 0; i < weapons.Loadout.Count; i++)
            {
                WeaponInstance instance = weapons.Loadout[i];
                if (instance?.definition == null)
                    continue;

                bool selected = i == weapons.CurrentIndex;

                builder.Append("<color=").Append(selected ? selectedColour : carriedColour).Append('>');
                builder.Append(i + 1).Append("  ").Append(instance.definition.displayName);
                builder.Append("  ").Append(instance.ammoInMag).Append('/').Append(instance.reserveAmmo);
                builder.Append("</color>").AppendLine();
            }

            label.text = builder.ToString();
        }
    }
}
