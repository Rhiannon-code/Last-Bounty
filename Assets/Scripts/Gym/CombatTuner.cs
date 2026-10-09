using System.Collections.Generic;
using System.Text;
using FPSParkour.AI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class CombatTuner : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float leadAccuracy = 0.8f;
        [SerializeField] float projectileSpeed = 70f;
        [SerializeField] float shotsPerSecond = 1.8f;
        [SerializeField] float spreadDegrees = 2.5f;

        [Header("Steps")]
        [SerializeField] float leadStep = 0.1f;
        [SerializeField] float speedStep = 10f;
        [SerializeField] float rateStep = 0.2f;
        [SerializeField] float spreadStep = 0.5f;
        [SerializeField, Min(0.1f)] float rescanEverySeconds = 2f;

        readonly List<EnemyWeapon> weapons = new List<EnemyWeapon>();

        float nextRescanAt;

        public float LeadAccuracy => leadAccuracy;
        public float ProjectileSpeed => projectileSpeed;
        public float ShotsPerSecond => shotsPerSecond;
        public float SpreadDegrees => spreadDegrees;

        void Start() => Rescan();

        void Update()
        {
            if (Time.time >= nextRescanAt)
                Rescan();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool changed = false;

            changed |= Step(keyboard.f7Key, keyboard.f8Key, ref leadAccuracy, leadStep, 0f, 1f);
            changed |= Step(keyboard.f9Key, keyboard.f10Key, ref projectileSpeed, speedStep, 10f, 400f);
            changed |= Step(keyboard.f11Key, keyboard.f12Key, ref shotsPerSecond, rateStep, 0.1f, 20f);
            changed |= Step(keyboard.leftBracketKey, keyboard.rightBracketKey, ref spreadDegrees, spreadStep, 0f, 25f);

            if (changed)
                Apply();
        }

        static bool Step(KeyControl down, KeyControl up, ref float value, float step, float min, float max)
        {
            if (down != null && down.wasPressedThisFrame)
            {
                value = Mathf.Clamp(value - step, min, max);
                return true;
            }

            if (up != null && up.wasPressedThisFrame)
            {
                value = Mathf.Clamp(value + step, min, max);
                return true;
            }

            return false;
        }

        public void Rescan()
        {
            nextRescanAt = Time.time + rescanEverySeconds;

            weapons.Clear();
            weapons.AddRange(FindObjectsByType<EnemyWeapon>(FindObjectsSortMode.None));
            Apply();
        }

        public void Apply()
        {
            foreach (EnemyWeapon weapon in weapons)
            {
                if (weapon == null)
                    continue;

                weapon.LeadAccuracy = leadAccuracy;
                weapon.ProjectileSpeed = projectileSpeed;
                weapon.ShotsPerSecond = shotsPerSecond;
                weapon.SpreadDegrees = spreadDegrees;
            }
        }

        public void Describe(StringBuilder into)
        {
            into.AppendLine($"enemies  {weapons.Count} tracked");
            into.AppendLine($"F7/F8   lead {leadAccuracy:F2}      F9/F10 speed {projectileSpeed:F0} m/s");
            into.AppendLine($"F11/F12 rate {shotsPerSecond:F1}/s   [ / ]  spread {spreadDegrees:F1}°");
        }
    }
}
