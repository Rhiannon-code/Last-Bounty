using System;
using System.Collections.Generic;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Narrative
{
    [Serializable]
    public struct StoryFlagsState
    {
        public string[] Set;
    }

    [DisallowMultipleComponent]
    public class StoryFlags : MonoBehaviour, ISaveable
    {
        [SerializeField] string saveKey = "story.flags";
        [SerializeField] string[] setAtStart;

        readonly HashSet<string> flags = new HashSet<string>();

        public event Action<string, bool> FlagChanged;

        public string SaveKey => saveKey;
        public IReadOnlyCollection<string> All => flags;

        void Awake()
        {
            if (setAtStart == null)
                return;

            foreach (string flag in setAtStart)
                Set(flag);
        }

        public bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public bool Set(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Add(flag))
                return false;

            FlagChanged?.Invoke(flag, true);
            return true;
        }

        public void SetAll(IEnumerable<string> toSet)
        {
            if (toSet == null)
                return;

            foreach (string flag in toSet)
                Set(flag);
        }

        public bool Clear(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flags.Remove(flag))
                return false;

            FlagChanged?.Invoke(flag, false);
            return true;
        }

        public int CountSet(IEnumerable<string> candidates)
        {
            if (candidates == null)
                return 0;

            int count = 0;
            foreach (string flag in candidates)
            {
                if (Has(flag))
                    count++;
            }

            return count;
        }

        public string CaptureJson()
        {
            string[] set = new string[flags.Count];
            flags.CopyTo(set);
            return JsonUtility.ToJson(new StoryFlagsState { Set = set });
        }

        public void RestoreJson(string json)
        {
            flags.Clear();

            StoryFlagsState state = JsonUtility.FromJson<StoryFlagsState>(json);
            if (state.Set == null)
                return;

            foreach (string flag in state.Set)
                flags.Add(flag);
        }
    }
}
