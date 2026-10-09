using System;
using UnityEngine;

namespace FPSParkour.AI
{
    [DisallowMultipleComponent]
    public class CityAlarm : MonoBehaviour
    {
        public static CityAlarm Instance { get; private set; }

        [SerializeField] float defaultRadius = 35f;

        public event Action<Vector3, float> Raised;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"A second {nameof(CityAlarm)} exists on '{name}'. There must be exactly one.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Raise(Vector3 position) => Raise(position, defaultRadius);

        public void Raise(Vector3 position, float radius) => Raised?.Invoke(position, radius);
    }
}
