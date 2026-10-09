using System;
using FPSParkour.AI;
using FPSParkour.Core;
using FPSParkour.Player;
using UnityEngine;

namespace FPSParkour.Investigation
{
    [Serializable]
    public struct DistrictHeatState
    {
        public float Heat;
    }

    [DisallowMultipleComponent]
    public class DistrictHeat : MonoBehaviour, ISaveable
    {
        [SerializeField] string saveKey = "heat";
        [SerializeField] PlayerStats stats;
        [SerializeField, Range(0f, 1f)] float heat;
        [SerializeField] float decayPerSecond = 0.012f;
        [SerializeField] float alarmContribution = 0.25f;

        bool maxedFired;

        public event Action<float> Changed;
        public event Action Maxed;

        public float Heat => heat;
        public bool IsMaxed => heat >= 1f;
        public string SaveKey => saveKey;

        public string CaptureJson() => JsonUtility.ToJson(new DistrictHeatState { Heat = heat });

        public void RestoreJson(string json)
        {
            heat = Mathf.Clamp01(JsonUtility.FromJson<DistrictHeatState>(json).Heat);

            maxedFired = IsMaxed;
            Changed?.Invoke(heat);
        }

        void OnEnable()
        {
            if (CityAlarm.Instance != null)
                CityAlarm.Instance.Raised += OnAlarm;
        }

        void OnDisable()
        {
            if (CityAlarm.Instance != null)
                CityAlarm.Instance.Raised -= OnAlarm;
        }

        void Update()
        {
            if (heat > 0f && !maxedFired)
                Add(-decayPerSecond * Time.deltaTime);
        }

        public void Add(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
                return;

            if (amount > 0f && stats != null)
                amount *= stats.Mult(StatType.HeatGainMult);

            heat = Mathf.Clamp01(heat + amount);
            Changed?.Invoke(heat);

            if (!IsMaxed || maxedFired)
                return;

            maxedFired = true;
            Maxed?.Invoke();
        }

        void OnAlarm(Vector3 position, float radius) => Add(alarmContribution);
    }
}
