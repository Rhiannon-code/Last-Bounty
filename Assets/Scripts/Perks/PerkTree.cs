using System;
using System.Collections.Generic;
using UnityEngine;
using FPSParkour.Core;
using FPSParkour.Player;

namespace FPSParkour.Perks
{
    [Serializable]
    public struct PerkTreeState
    {
        public int Points;
        public string[] Unlocked;
    }

    [RequireComponent(typeof(AbilityUnlocks))]
    public class PerkTree : MonoBehaviour, ISaveable
    {
        [SerializeField] private AbilityUnlocks unlocks;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private List<PerkDefinition> startingPerks = new();
        [SerializeField] private int skillPoints;
        [SerializeField] private List<PerkDefinition> allPerks = new();
        [SerializeField] private string saveKey = "perks";

        private readonly HashSet<PerkDefinition> _unlocked = new();

        public int SkillPoints => skillPoints;
        public IReadOnlyCollection<PerkDefinition> Unlocked => _unlocked;

        public event Action<PerkDefinition> OnPerkUnlocked;
        public event Action<int> OnSkillPointsChanged;

        private void Awake()
        {
            if (unlocks == null) unlocks = GetComponent<AbilityUnlocks>();
            if (stats == null) stats = GetComponent<PlayerStats>();

            foreach (var perk in startingPerks)
                if (perk != null)
                    ForceUnlock(perk);
        }

        public string SaveKey => saveKey;

        public bool IsUnlocked(PerkDefinition perk) => perk != null && _unlocked.Contains(perk);

        public bool CanUnlock(PerkDefinition perk)
        {
            if (perk == null || _unlocked.Contains(perk)) return false;
            if (skillPoints < perk.cost) return false;

            if (perk.prerequisites != null)
                foreach (var pre in perk.prerequisites)
                    if (pre != null && !_unlocked.Contains(pre))
                        return false;

            return true;
        }

        public bool TryUnlock(PerkDefinition perk)
        {
            if (!CanUnlock(perk)) return false;

            SetSkillPoints(skillPoints - perk.cost);
            ForceUnlock(perk);
            return true;
        }

        public void ForceUnlock(PerkDefinition perk)
        {
            if (perk == null || !_unlocked.Add(perk)) return;
            ApplyEffects(perk);
            OnPerkUnlocked?.Invoke(perk);
        }

        public void AwardSkillPoints(int amount) => SetSkillPoints(skillPoints + Mathf.Max(0, amount));

        private void SetSkillPoints(int value)
        {
            skillPoints = Mathf.Max(0, value);
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        public string CaptureJson()
        {
            var names = new List<string>(_unlocked.Count);
            foreach (var perk in _unlocked)
                names.Add(perk.name);

            return JsonUtility.ToJson(new PerkTreeState { Points = skillPoints, Unlocked = names.ToArray() });
        }

        public void RestoreJson(string json)
        {
            var state = JsonUtility.FromJson<PerkTreeState>(json);

            foreach (var perk in _unlocked)
                stats?.RemoveSource(perk);

            _unlocked.Clear();
            SetSkillPoints(state.Points);

            if (state.Unlocked == null)
                return;

            foreach (string name in state.Unlocked)
            {
                var perk = allPerks.Find(p => p != null && p.name == name);

                if (perk == null)
                    Debug.LogWarning($"Save names perk '{name}', which is not in this tree's node list.", this);
                else
                    ForceUnlock(perk);
            }
        }

        private void ApplyEffects(PerkDefinition perk)
        {
            if (perk.effects == null) return;

            foreach (var effect in perk.effects)
            {
                switch (effect.kind)
                {
                    case PerkEffectKind.GrantAbility:
                        unlocks.Grant(effect.ability);
                        break;

                    case PerkEffectKind.PlayerStat:
                        stats.AddModifier(effect.playerStat,
                            new StatModifier(effect.op, effect.value), perk);
                        break;

                    case PerkEffectKind.WeaponStat:
                        stats.AddWeaponModifier(effect.weaponStat,
                            new StatModifier(effect.op, effect.value), perk);
                        break;
                }
            }
        }
    }
}
