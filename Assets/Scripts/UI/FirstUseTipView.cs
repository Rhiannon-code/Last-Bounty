using System.Collections.Generic;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class FirstUseTipView : MonoBehaviour
    {
        [SerializeField] AbilityAvailability availability;
        [SerializeField] PlayerInputReader input;

        [Header("Widgets")]
        [SerializeField] GameObject panel;
        [SerializeField] Text titleLabel;
        [SerializeField] Text bodyLabel;
        [SerializeField, Min(1f)] float secondsOnScreen = 6f;
        [SerializeField, Min(0.1f)] float secondsBetween = 0.6f;

        readonly HashSet<AbilityId> shown = new HashSet<AbilityId>();

        float nextChangeAt;

        void Start() => Hide();

        void Update()
        {
            if (Time.time < nextChangeAt)
                return;

            if (panel != null && panel.activeSelf)
            {
                Hide();
                return;
            }

            if (TryNext(AbilityCatalog.Traversal, out AbilityCatalog.Entry entry)
                || TryNext(AbilityCatalog.Hunting, out entry))
                Show(entry);
        }

        bool TryNext(AbilityCatalog.Entry[] entries, out AbilityCatalog.Entry found)
        {
            foreach (AbilityCatalog.Entry entry in entries)
            {
                if (!entry.Teach || shown.Contains(entry.Id) || availability == null || !availability.UsableNow(entry.Id))
                    continue;

                found = entry;
                return true;
            }

            found = default;
            return false;
        }

        void Show(AbilityCatalog.Entry entry)
        {
            shown.Add(entry.Id);

            if (titleLabel != null)
                titleLabel.text = $"{entry.Name}   [{entry.KeyText(input)}]";

            if (bodyLabel != null)
                bodyLabel.text = entry.Tip;

            if (panel != null)
                panel.SetActive(true);

            nextChangeAt = Time.time + secondsOnScreen;
        }

        void Hide()
        {
            if (panel != null)
                panel.SetActive(false);

            nextChangeAt = Time.time + secondsBetween;
        }
    }
}
