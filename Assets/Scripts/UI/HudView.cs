using FPSParkour.Bounty;
using FPSParkour.Combat;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class HudView : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] WeaponController weapons;
        [SerializeField] CreditsWallet wallet;

        [Header("Widgets")]
        [SerializeField] Slider healthBar;
        [SerializeField] Text ammoLabel;
        [SerializeField] Text weaponLabel;
        [SerializeField] Text creditsLabel;
        [SerializeField] GameObject reloadIndicator;

        void OnEnable()
        {
            if (stats != null) stats.OnHealthChanged += OnHealthChanged;
            if (wallet != null) wallet.Changed += OnCreditsChanged;

            if (weapons != null)
            {
                weapons.OnAmmoChanged += OnAmmoChanged;
                weapons.OnWeaponChanged += OnWeaponChanged;
                weapons.OnReloadStarted += OnReloadStarted;
                weapons.OnReloadCompleted += OnReloadCompleted;
            }
        }

        void OnDisable()
        {
            if (stats != null) stats.OnHealthChanged -= OnHealthChanged;
            if (wallet != null) wallet.Changed -= OnCreditsChanged;

            if (weapons != null)
            {
                weapons.OnAmmoChanged -= OnAmmoChanged;
                weapons.OnWeaponChanged -= OnWeaponChanged;
                weapons.OnReloadStarted -= OnReloadStarted;
                weapons.OnReloadCompleted -= OnReloadCompleted;
            }
        }

        void OnHealthChanged(float current, float max)
        {
            if (healthBar != null)
                healthBar.value = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        void OnAmmoChanged(int magazine, int reserve)
        {
            if (ammoLabel != null)
                ammoLabel.text = $"{magazine} / {reserve}";
        }

        void OnWeaponChanged(WeaponInstance instance)
        {
            if (weaponLabel != null)
                weaponLabel.text = instance?.definition != null ? instance.definition.name : string.Empty;
        }

        void OnCreditsChanged(int credits)
        {
            if (creditsLabel != null)
                creditsLabel.text = credits.ToString();
        }

        void OnReloadStarted() => SetReloading(true);
        void OnReloadCompleted() => SetReloading(false);

        void SetReloading(bool value)
        {
            if (reloadIndicator != null)
                reloadIndicator.SetActive(value);
        }
    }
}
