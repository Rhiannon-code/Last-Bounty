using System.Text;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class AbilityBarView : MonoBehaviour
    {
        [SerializeField] AbilityAvailability availability;
        [SerializeField] PlayerInputReader input;

        [Header("Widgets")]
        [SerializeField] Text traversalLabel;
        [SerializeField] Text huntingLabel;

        [SerializeField, Min(0.05f)] float refreshEvery = 0.15f;

        [Tooltip("Owned and usable right now.")]
        [SerializeField] string readyColour = "#8CE8A0";
        [SerializeField] string spentColour = "#C8C8C8";
        [SerializeField] string lockedColour = "#5A5A5A";

        readonly StringBuilder builder = new StringBuilder();

        float nextRefreshAt;

        void Update()
        {
            if (Time.time < nextRefreshAt)
                return;

            nextRefreshAt = Time.time + refreshEvery;

            Write(traversalLabel, AbilityCatalog.Traversal);
            Write(huntingLabel, AbilityCatalog.Hunting);
        }

        void Write(Text label, AbilityCatalog.Entry[] entries)
        {
            if (label == null)
                return;

            builder.Clear();

            foreach (AbilityCatalog.Entry entry in entries)
            {
                bool owned = availability == null || availability.Owns(entry.Id);
                bool usable = availability != null && availability.UsableNow(entry.Id);

                builder.Append("<color=").Append(!owned ? lockedColour : usable ? readyColour : spentColour).Append('>');
                builder.Append(entry.KeyText(input).PadRight(10)).Append(' ').Append(entry.Name);

                string state = availability != null ? availability.StateText(entry.Id) : string.Empty;
                if (!string.IsNullOrEmpty(state))
                    builder.Append("  ").Append(state);

                if (!owned)
                    builder.Append(" : locked");

                builder.Append("</color>").AppendLine();
            }

            label.text = builder.ToString();
        }
    }
}
