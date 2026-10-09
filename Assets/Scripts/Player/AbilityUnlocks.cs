using System;
using System.Collections.Generic;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Player
{
    [Serializable]
    public struct AbilityUnlocksState
    {
        public int[] Unlocked;
    }

    public class AbilityUnlocks : MonoBehaviour, ISaveable
    {
        [SerializeField] private string saveKey = "abilities";
        [SerializeField]
        private List<AbilityId> baseAbilities = new()
        {
            AbilityId.Sprint,
            AbilityId.Jump,
            AbilityId.Crouch,
            AbilityId.Slide,
            AbilityId.Mantle,
            AbilityId.WallRun,
            AbilityId.WallJump,
        };

        private readonly HashSet<AbilityId> _unlocked = new();
        public event Action<AbilityId, bool> OnAbilityChanged;

        private void Awake()
        {
            foreach (var id in baseAbilities)
                _unlocked.Add(id);
        }

        public string SaveKey => saveKey;

        public bool Has(AbilityId id) => _unlocked.Contains(id);

        public void Grant(AbilityId id)
        {
            if (_unlocked.Add(id))
                OnAbilityChanged?.Invoke(id, true);
        }

        public void Revoke(AbilityId id)
        {
            if (_unlocked.Remove(id))
                OnAbilityChanged?.Invoke(id, false);
        }

        public IReadOnlyCollection<AbilityId> All => _unlocked;

        public string CaptureJson()
        {
            var ids = new int[_unlocked.Count];
            int i = 0;

            foreach (var id in _unlocked)
                ids[i++] = (int)id;

            return JsonUtility.ToJson(new AbilityUnlocksState { Unlocked = ids });
        }

        public void RestoreJson(string json)
        {
            var state = JsonUtility.FromJson<AbilityUnlocksState>(json);
            _unlocked.Clear();

            if (state.Unlocked == null)
                return;

            foreach (int id in state.Unlocked)
                _unlocked.Add((AbilityId)id);
        }
    }
}
