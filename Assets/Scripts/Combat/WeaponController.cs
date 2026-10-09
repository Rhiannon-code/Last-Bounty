using FPSParkour.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using FPSParkour.Player;

namespace FPSParkour.Combat
{
    public class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private Transform weaponSocket;

        [Header("Loadout")]
        [SerializeField] private List<WeaponDefinition> startingLoadout = new();

        private readonly List<WeaponInstance> _loadout = new();
        private int _index = -1;

        private float _fireCooldown;
        private bool _isReloading;
        private float _reloadTimer;
        private float _aimT;
        private bool _wasAiming;
        private int _burstRemaining;
        private GameObject _viewModel;

        public WeaponInstance Current => (_index >= 0 && _index < _loadout.Count) ? _loadout[_index] : null;
        public bool IsReloading => _isReloading;
        public float AimProgress => _aimT;
        public IReadOnlyList<WeaponInstance> Loadout => _loadout;
        public int CurrentIndex => _index;

        public event Action<WeaponInstance> OnWeaponChanged;
        public event Action<int, int> OnAmmoChanged;   // Mag, reserve
        public event Action<WeaponInstance> OnFired;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action<bool> OnAimChanged;

        private void Awake()
        {
            if (aimOrigin == null && look != null) aimOrigin = look.CameraTransform;
            foreach (var def in startingLoadout)
                if (def != null) _loadout.Add(new WeaponInstance(def));
            if (_loadout.Count > 0) EquipIndex(0);
        }

        private void Update()
        {
            if (input == null) return;

            HandleSwitching();
            if (Current == null) return;

            if (_fireCooldown > 0f) _fireCooldown -= Time.deltaTime;

            HandleReload(Time.deltaTime);
            HandleAim(Time.deltaTime);
            HandleFire();
        }

        private void HandleSwitching()
        {
            if (_loadout.Count == 0) return;

            int slot = input.QuickSlotPressed; // 1 based
            if (slot > 0 && slot <= _loadout.Count)
                EquipIndex(slot - 1);

            if (input.WeaponScroll != 0)
            {
                int next = (_index + input.WeaponScroll + _loadout.Count) % _loadout.Count;
                EquipIndex(next);
            }
        }

        private void EquipIndex(int i)
        {
            if (i < 0 || i >= _loadout.Count || i == _index) return;

            _index = i;
            _isReloading = false;
            _burstRemaining = 0;
            _fireCooldown = 0f;

            if (_viewModel != null) Destroy(_viewModel);
            var def = Current.definition;
            if (def.viewModelPrefab != null && weaponSocket != null)
                _viewModel = Instantiate(def.viewModelPrefab, weaponSocket);

            OnWeaponChanged?.Invoke(Current);
            RaiseAmmo();
        }

        private void HandleReload(float dt)
        {
            if (_isReloading)
            {
                _reloadTimer -= dt;
                if (_reloadTimer <= 0f) FinishReload();
                return;
            }

            if (input.ReloadPressed) TryStartReload();
        }

        private void TryStartReload()
        {
            var w = Current;
            if (_isReloading || w == null) return;
            if (w.ammoInMag >= EffectiveMag(w) || !w.HasReserve) return;

            _isReloading = true;
            _burstRemaining = 0;
            _reloadTimer = EffectiveReloadTime(w);
            OnReloadStarted?.Invoke();
        }

        private void FinishReload()
        {
            _isReloading = false;
            var w = Current;
            if (w == null) return;

            int needed = EffectiveMag(w) - w.ammoInMag;
            int take = Mathf.Min(needed, w.reserveAmmo);
            w.ammoInMag += take;
            w.reserveAmmo -= take;

            OnReloadCompleted?.Invoke();
            RaiseAmmo();
        }

        private void HandleAim(float dt)
        {
            var def = Current.definition;
            bool wantAim = input.AimHeld;
            _aimT = Mathf.MoveTowards(_aimT, wantAim ? 1f : 0f, def.aimLerpSpeed * dt);

            if (look != null)
                look.SetAimFovOffset(Mathf.Lerp(0f, def.aimFovOffset, _aimT));

            if (wantAim != _wasAiming)
            {
                _wasAiming = wantAim;
                OnAimChanged?.Invoke(wantAim);
            }
        }

        private void HandleFire()
        {
            if (_isReloading) return;
            var w = Current;
            var def = w.definition;

            if (w.IsEmpty)
            {
                if ((input.FirePressed || input.FireHeld) && def.autoReloadWhenEmpty)
                    TryStartReload();
                return;
            }

            bool trigger = def.fireMode switch
            {
                FireMode.SemiAuto => input.FirePressed,
                FireMode.FullAuto => input.FireHeld,
                FireMode.Burst => StartOrContinueBurst(def),
                _ => false,
            };

            if (trigger && _fireCooldown <= 0f)
                FireOnce(w, def);
        }

        private bool StartOrContinueBurst(WeaponDefinition def)
        {
            if (_burstRemaining <= 0 && input.FirePressed)
                _burstRemaining = def.burstCount;
            return _burstRemaining > 0;
        }

        private void FireOnce(WeaponInstance w, WeaponDefinition def)
        {
            _fireCooldown = 1f / Mathf.Max(0.01f, EffectiveFireRate(def));
            if (def.fireMode == FireMode.Burst) _burstRemaining--;

            Transform origin = aimOrigin != null ? aimOrigin : transform;
            float spread = Mathf.Lerp(def.hipSpread, def.aimSpread, _aimT) * SpreadFactor();
            float damage = EffectiveDamage(def);

            for (int p = 0; p < Mathf.Max(1, def.pellets); p++)
            {
                Vector3 dir = ApplySpread(origin, spread);

                if (def.projectilePrefab != null)
                    FireProjectile(origin.position, dir, def, damage);
                else
                    FireHitscan(origin.position, dir, def, damage);
            }

            w.ammoInMag = Mathf.Max(0, w.ammoInMag - 1);

            if (look != null)
                look.AddRecoilKick(def.recoilPitch, UnityEngine.Random.Range(-def.recoilYaw, def.recoilYaw));

            if (def.muzzleFlashPrefab != null)
            {
                Transform muzzle = weaponSocket != null ? weaponSocket : origin;
                var fx = Instantiate(def.muzzleFlashPrefab, muzzle.position, muzzle.rotation);
                Destroy(fx, 2f);
            }

            OnFired?.Invoke(w);
            RaiseAmmo();
        }

        private void FireHitscan(Vector3 pos, Vector3 dir, WeaponDefinition def, float damage)
        {
            if (!Physics.Raycast(pos, dir, out RaycastHit hit, def.range, def.hitMask,
                    QueryTriggerInteraction.Ignore))
                return;

            var info = new DamageInfo(damage, def.damageType, hit.point, dir, gameObject);
            IDamageable dmg = hit.collider.GetComponentInParent<IDamageable>();
            dmg?.ApplyDamage(in info);

            if (def.impactPrefab != null)
            {
                var fx = Instantiate(def.impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 5f);
            }
        }

        private void FireProjectile(Vector3 pos, Vector3 dir, WeaponDefinition def, float damage)
        {
            if (def.projectilePrefab == null) return;

            Vector3 muzzle = pos + dir * 0.5f;
            var go = Instantiate(def.projectilePrefab, muzzle, Quaternion.LookRotation(dir));

            if (!go.TryGetComponent(out Projectile proj)) return;

            proj.Launch(new BallisticShot
            {
                Origin = muzzle,
                Velocity = dir * def.projectileSpeed + ShooterVelocity * def.inheritShooterVelocity,
                Damage = damage,
                Range = def.range,
                Type = def.damageType,
                Source = gameObject,
                Mask = def.hitMask,
                ImpactPrefab = def.impactPrefab,
                GravityScale = def.gravityScale,
                Drag = def.drag
            });
        }

        private CharacterController _shooterBody;
        private bool _shooterBodyResolved;

        private Vector3 ShooterVelocity
        {
            get
            {
                if (!_shooterBodyResolved)
                {
                    _shooterBody = GetComponent<CharacterController>();
                    _shooterBodyResolved = true;
                }

                return _shooterBody != null ? _shooterBody.velocity : Vector3.zero;
            }
        }

        private static Vector3 ApplySpread(Transform origin, float angleDeg)
        {
            if (angleDeg <= 0f) return origin.forward;
            Vector3 dir = origin.forward;
            dir = Quaternion.AngleAxis(UnityEngine.Random.Range(-angleDeg, angleDeg), origin.up) * dir;
            dir = Quaternion.AngleAxis(UnityEngine.Random.Range(-angleDeg, angleDeg), origin.right) * dir;
            return dir;
        }

        public void AddWeapon(WeaponDefinition def)
        {
            if (def == null) return;
            var existing = _loadout.Find(w => w.definition == def);
            if (existing != null)
            {
                existing.reserveAmmo = Mathf.Min(existing.reserveAmmo + def.magSize, def.maxReserve);
            }
            else
            {
                _loadout.Add(new WeaponInstance(def));
                if (_loadout.Count == 1) EquipIndex(0);
            }
            RaiseAmmo();
        }

        public void AddAmmo(WeaponDefinition def, int amount)
        {
            var w = _loadout.Find(x => x.definition == def);
            if (w == null) return;
            w.reserveAmmo = Mathf.Min(w.reserveAmmo + amount, def.maxReserve);
            RaiseAmmo();
        }

        private float Weapon(WeaponStatType t, float baseValue) =>
            stats != null ? stats.ModifyWeapon(t, baseValue) : baseValue;

        private float EffectiveDamage(WeaponDefinition def) => Weapon(WeaponStatType.DamageMult, def.damage);
        private float EffectiveFireRate(WeaponDefinition def) => Weapon(WeaponStatType.FireRateMult, def.fireRate);
        private int EffectiveMag(WeaponInstance w) => Mathf.RoundToInt(Weapon(WeaponStatType.MagSizeAdd, w.definition.magSize));
        private float EffectiveReloadTime(WeaponInstance w) =>
            w.definition.reloadTime / Mathf.Max(0.01f, Weapon(WeaponStatType.ReloadSpeedMult, 1f));
        private float SpreadFactor() => Weapon(WeaponStatType.SpreadMult, 1f);

        private void RaiseAmmo()
        {
            var w = Current;
            if (w != null) OnAmmoChanged?.Invoke(w.ammoInMag, w.reserveAmmo);
        }
    }
}
