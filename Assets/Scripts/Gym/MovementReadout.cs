using System.Text;
using FPSParkour.Movement;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class MovementReadout : MonoBehaviour
    {
        [SerializeField] PlayerMovementController controller;
        [SerializeField] AbilityUnlocks unlocks;
        [SerializeField] GymController gym;
        [SerializeField] CombatTuner tuner;
        [SerializeField] Text label;
        [SerializeField, Min(0.02f)] float refreshEvery = 0.05f;

        readonly StringBuilder builder = new StringBuilder();

        float nextRefreshAt;
        float peakSpeed;

        void Update()
        {
            if (controller == null || label == null || Time.time < nextRefreshAt)
                return;

            nextRefreshAt = Time.time + refreshEvery;

            Vector3 velocity = controller.Velocity;
            float horizontal = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            peakSpeed = Mathf.Max(peakSpeed, horizontal);

            builder.Clear();
            builder.AppendLine($"state    {controller.State}");
            builder.AppendLine($"speed    {horizontal:F2} m/s   (peak {peakSpeed:F2})");
            builder.AppendLine($"vertical {velocity.y:F2} m/s");

            if (gym?.CurrentStation != null)
            {
                builder.AppendLine();
                builder.AppendLine($"station  {gym.CurrentStation.StationName}");
                builder.AppendLine(gym.CurrentStation.WhatToTest);
            }

            if (unlocks != null)
            {
                builder.AppendLine();
                builder.AppendLine($"F1 double-jump {Mark(AbilityId.DoubleJump)}   F2 air-dash {Mark(AbilityId.AirDash)}");
                builder.AppendLine($"F3 grapple     {Mark(AbilityId.Grapple)}   F4 slam     {Mark(AbilityId.GroundSlam)}");
                builder.AppendLine("F5 all on   F6 all off   R respawn   Alt+1..9 station");
            }

            if (tuner != null)
            {
                builder.AppendLine();
                tuner.Describe(builder);
            }

            label.text = builder.ToString();
        }

        public void ResetPeak() => peakSpeed = 0f;

        string Mark(AbilityId ability) => unlocks.Has(ability) ? "[on] " : "[off]";
    }
}
