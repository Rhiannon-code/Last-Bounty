using FPSParkour.Core;
using UnityEngine;
using FPSParkour.Inventory;

namespace FPSParkour.Combat
{
    public enum FireMode { SemiAuto, FullAuto, Burst }
    public enum WeaponClass { Hitscan, Projectile }

    [CreateAssetMenu(fileName = "Weapon_", menuName = "FPS Parkour/Weapon", order = 3)]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "New Weapon";
        public Sprite icon;
        public ItemDefinition item;

        [Header("Firing")]
        public FireMode fireMode = FireMode.FullAuto;
        public WeaponClass weaponClass = WeaponClass.Hitscan;
        public float fireRate = 8f;
        public int burstCount = 3;
        public int pellets = 1;

        [Header("Damage")]
        public float damage = 20f;
        public DamageType damageType = DamageType.Kinetic;
        public float range = 200f; // hitscan reach

        [Header("Ammo")]
        public int magSize = 30;
        public int maxReserve = 180;
        public float reloadTime = 2f;
        public bool autoReloadWhenEmpty = true;

        [Header("Accuracy & recoil")]
        public float hipSpread = 3f;
        public float aimSpread = 0.3f;
        public float recoilPitch = 1.2f;
        public float recoilYaw = 0.4f;

        [Header("Aim down sights")]
        public float aimFovOffset = -20f;
        public float aimLerpSpeed = 12f;

        [Header("Ballistics")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 90f;
        public float gravityScale = 0.6f;
        public float drag = 0.02f;
        public float inheritShooterVelocity = 0.3f;

        [Header("Presentation (optional)")]
        public GameObject viewModelPrefab;
        public GameObject muzzleFlashPrefab;
        public GameObject impactPrefab;
        public LayerMask hitMask = ~0;
    }
}
