using UnityEngine;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class GymStation : MonoBehaviour
    {
        [SerializeField] string stationName = "Station";
        [SerializeField, TextArea] string whatToTest;

        public string StationName => stationName;
        public string WhatToTest => whatToTest;

        public void Configure(string name, string testing)
        {
            stationName = name;
            whatToTest = testing;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
        }
    }
}
