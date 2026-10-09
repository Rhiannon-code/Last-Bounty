using System;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [Serializable]
    public struct CreditsState
    {
        public int Credits;
        public int LifetimeEarned;
    }

    [DisallowMultipleComponent]
    public class CreditsWallet : MonoBehaviour, ISaveable
    {
        [SerializeField, Min(0)] int credits;
        [SerializeField] string saveKey = "credits";

        int lifetimeEarned;

        public event Action<int> Changed;

        public int Credits => credits;
        public int LifetimeEarned => lifetimeEarned;
        public string SaveKey => saveKey;

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            credits += amount;
            lifetimeEarned += amount;
            Changed?.Invoke(credits);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;

            if (credits < amount)
                return false;

            credits -= amount;
            Changed?.Invoke(credits);
            return true;
        }

        public string CaptureJson() =>
            JsonUtility.ToJson(new CreditsState { Credits = credits, LifetimeEarned = lifetimeEarned });

        public void RestoreJson(string json)
        {
            CreditsState state = JsonUtility.FromJson<CreditsState>(json);
            credits = state.Credits;
            lifetimeEarned = state.LifetimeEarned;
            Changed?.Invoke(credits);
        }
    }
}
