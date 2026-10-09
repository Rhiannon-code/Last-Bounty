using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSParkour.Narrative
{
    [DisallowMultipleComponent]
    public class BarkPlayer : MonoBehaviour
    {
        [SerializeField] StoryFlags flags;
        [SerializeField, Min(0f)] float globalCooldown = 3f;

        readonly Dictionary<string, float> nextAllowed = new Dictionary<string, float>();
        readonly List<Bark> eligible = new List<Bark>();

        float nextGlobal;

        public event Action<Bark> BarkPlayed;

        public bool TryPlay(BarkSet set)
        {
            if (set == null || set.Barks == null || Time.time < nextGlobal)
                return false;

            if (nextAllowed.TryGetValue(set.Id, out float allowedAt) && Time.time < allowedAt)
                return false;

            eligible.Clear();
            int totalWeight = 0;

            foreach (Bark bark in set.Barks)
            {
                if (bark == null || string.IsNullOrEmpty(bark.Line) || !bark.Condition.IsMet(flags))
                    continue;

                eligible.Add(bark);
                totalWeight += Mathf.Max(1, bark.Weight);
            }

            if (eligible.Count == 0)
                return false;

            int pick = UnityEngine.Random.Range(0, totalWeight);
            foreach (Bark bark in eligible)
            {
                pick -= Mathf.Max(1, bark.Weight);
                if (pick >= 0)
                    continue;

                nextGlobal = Time.time + globalCooldown;
                nextAllowed[set.Id] = Time.time + set.CooldownSeconds;
                BarkPlayed?.Invoke(bark);
                return true;
            }

            return false;
        }
    }
}
