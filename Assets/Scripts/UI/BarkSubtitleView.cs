using System.Collections.Generic;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class BarkSubtitleView : MonoBehaviour
    {
        [SerializeField] BarkPlayer player;
        [SerializeField] Text label;
        [SerializeField, Min(1)] int maxLines = 4;
        [SerializeField] float holdSeconds = 5f;

        readonly Queue<string> lines = new Queue<string>();

        float clearAt;

        void OnEnable()
        {
            if (player != null)
                player.BarkPlayed += OnBark;
        }

        void OnDisable()
        {
            if (player != null)
                player.BarkPlayed -= OnBark;
        }

        void Update()
        {
            if (lines.Count == 0 || Time.time < clearAt)
                return;

            lines.Dequeue();
            clearAt = Time.time + holdSeconds;
            Redraw();
        }

        void OnBark(Bark bark)
        {
            if (bark == null)
                return;

            lines.Enqueue(string.IsNullOrEmpty(bark.Speaker) ? bark.Line : $"{bark.Speaker}: {bark.Line}");

            while (lines.Count > maxLines)
                lines.Dequeue();

            clearAt = Time.time + holdSeconds;
            Redraw();
        }

        void Redraw()
        {
            if (label != null)
                label.text = string.Join("\n", lines);
        }
    }
}
