using System.Collections.Generic;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class GymController : MonoBehaviour
    {
        [SerializeField] AbilityUnlocks unlocks;
        [SerializeField] PlayerStats stats;
        [SerializeField] Transform player;
        [SerializeField] CharacterController controller;
        [SerializeField] GymStation[] stations;

        [Header("Toggled by F1..F4")]
        [SerializeField] AbilityId[] togglable =
        {
            AbilityId.DoubleJump, AbilityId.AirDash, AbilityId.Grapple, AbilityId.GroundSlam
        };

        [SerializeField] float fallResetHeight = -40f;

        Vector3 spawnPoint;
        int currentStation = -1;

        public IReadOnlyList<GymStation> Stations => stations;
        public GymStation CurrentStation => currentStation >= 0 && currentStation < stations.Length ? stations[currentStation] : null;

        void Awake()
        {
            if (player == null) player = transform;
            if (controller == null) controller = player.GetComponent<CharacterController>();
            spawnPoint = player.position;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            ReadAbilityToggles(keyboard);
            ReadStationKeys(keyboard);

            if (keyboard.rKey.wasPressedThisFrame)
                Respawn();

            if (player.position.y < fallResetHeight)
                Respawn();
        }

        void ReadAbilityToggles(Keyboard keyboard)
        {
            Key[] keys = { Key.F1, Key.F2, Key.F3, Key.F4 };

            for (int i = 0; i < keys.Length && i < togglable.Length; i++)
            {
                if (keyboard[keys[i]].wasPressedThisFrame)
                    Toggle(togglable[i]);
            }

            if (keyboard.f5Key.wasPressedThisFrame)
                GrantAll();

            if (keyboard.f6Key.wasPressedThisFrame)
                RevokeAll();
        }

        void ReadStationKeys(Keyboard keyboard)
        {
            if (stations == null)
                return;

            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

            for (int i = 0; i < digits.Length && i < stations.Length; i++)
            {
                if (keyboard[digits[i]].wasPressedThisFrame && keyboard.leftAltKey.isPressed)
                    GoToStation(i);
            }
        }

        public void Toggle(AbilityId ability)
        {
            if (unlocks == null)
                return;

            if (unlocks.Has(ability))
                unlocks.Revoke(ability);
            else
                unlocks.Grant(ability);
        }

        public void GrantAll()
        {
            if (unlocks == null)
                return;

            foreach (AbilityId ability in togglable)
                unlocks.Grant(ability);
        }

        public void RevokeAll()
        {
            if (unlocks == null)
                return;

            foreach (AbilityId ability in togglable)
                unlocks.Revoke(ability);
        }

        public void GoToStation(int index)
        {
            if (stations == null || index < 0 || index >= stations.Length || stations[index] == null)
                return;

            currentStation = index;
            Teleport(stations[index].transform.position + Vector3.up * 0.2f);
        }

        public void Respawn()
        {
            Teleport(CurrentStation != null ? CurrentStation.transform.position + Vector3.up * 0.2f : spawnPoint);
        }

        void Teleport(Vector3 position)
        {
            bool wasEnabled = controller != null && controller.enabled;

            if (wasEnabled)
                controller.enabled = false;

            player.position = position;

            if (wasEnabled)
                controller.enabled = true;
        }
    }
}
