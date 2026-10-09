using System;
using System.Collections.Generic;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [Serializable]
    public struct ReputationState
    {
        public string[] FactionIds;
        public int[] Standings;
    }

    [DisallowMultipleComponent]
    public class ReputationSystem : MonoBehaviour, ISaveable
    {
        [Serializable]
        public class Rivalry
        {
            public string FactionId;
            public string[] Rivals;
            [Range(0f, 1f)] public float Bleed = 0.5f;
        }

        [SerializeField] Rivalry[] rivalries;
        [SerializeField] string saveKey = "reputation";

        readonly Dictionary<string, int> standing = new Dictionary<string, int>();

        public event Action<string, int> StandingChanged;

        public string SaveKey => saveKey;

        public int StandingOf(string factionId) =>
            !string.IsNullOrEmpty(factionId) && standing.TryGetValue(factionId, out int value) ? value : 0;

        public void Modify(string factionId, int delta)
        {
            if (string.IsNullOrEmpty(factionId) || delta == 0)
                return;

            Set(factionId, StandingOf(factionId) + delta);

            Rivalry rivalry = FindRivalry(factionId);
            if (rivalry?.Rivals == null)
                return;

            int bleed = Mathf.RoundToInt(-delta * rivalry.Bleed);
            if (bleed == 0)
                return;

            foreach (string rival in rivalry.Rivals)
                Set(rival, StandingOf(rival) + bleed);
        }

        Rivalry FindRivalry(string factionId)
        {
            if (rivalries == null)
                return null;

            foreach (Rivalry rivalry in rivalries)
            {
                if (rivalry != null && rivalry.FactionId == factionId)
                    return rivalry;
            }

            return null;
        }

        void Set(string factionId, int value)
        {
            if (string.IsNullOrEmpty(factionId))
                return;

            int clamped = Mathf.Clamp(value, -100, 100);
            standing[factionId] = clamped;
            StandingChanged?.Invoke(factionId, clamped);
        }

        public string CaptureJson()
        {
            List<string> ids = new List<string>();
            List<int> values = new List<int>();

            foreach (KeyValuePair<string, int> pair in standing)
            {
                ids.Add(pair.Key);
                values.Add(pair.Value);
            }

            return JsonUtility.ToJson(new ReputationState { FactionIds = ids.ToArray(), Standings = values.ToArray() });
        }

        public void RestoreJson(string json)
        {
            standing.Clear();

            ReputationState state = JsonUtility.FromJson<ReputationState>(json);
            if (state.FactionIds == null || state.Standings == null)
                return;

            int count = Mathf.Min(state.FactionIds.Length, state.Standings.Length);
            for (int i = 0; i < count; i++)
                standing[state.FactionIds[i]] = state.Standings[i];
        }
    }
}
