using System.Text;
using FPSParkour.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class JobTrackerView : MonoBehaviour
    {
        [SerializeField] JobRunner runner;

        [Header("Widgets")]
        [SerializeField] Text titleLabel;
        [SerializeField] Text objectivesLabel;
        [SerializeField] Text noticeLabel;

        [SerializeField, Min(1f)] float noticeSeconds = 6f;
        [SerializeField, Min(1f)] float briefingSeconds = 8f;

        [SerializeField] string doneColour = "#6E7A6E";
        [SerializeField] string currentColour = "#F0E6C8";
        [SerializeField] string laterColour = "#8A8A8A";

        readonly StringBuilder builder = new StringBuilder();

        float noticeUntil;
        float briefingUntil;

        void OnEnable()
        {
            if (runner == null)
                return;

            runner.JobBegun += OnJobBegun;
            runner.Notice += OnNotice;
            runner.JobCompleted += OnCompleted;
            runner.JobFailed += OnFailed;
        }

        void OnDisable()
        {
            if (runner == null)
                return;

            runner.JobBegun -= OnJobBegun;
            runner.Notice -= OnNotice;
            runner.JobCompleted -= OnCompleted;
            runner.JobFailed -= OnFailed;
        }

        void OnJobBegun(JobDefinition job)
        {
            briefingUntil = Time.time + briefingSeconds;

            if (titleLabel != null)
                titleLabel.text = job.Title;
        }

        void OnNotice(string message)
        {
            if (noticeLabel != null)
                noticeLabel.text = message;

            noticeUntil = Time.time + noticeSeconds;
        }

        void OnCompleted(JobDefinition job) => OnNotice($"{job.Title}, closed.");

        void OnFailed(JobDefinition job, FailureKind kind) => OnNotice($"{job.Title}, failed.");

        void Update()
        {
            if (noticeLabel != null && Time.time > noticeUntil && noticeLabel.text.Length > 0)
                noticeLabel.text = string.Empty;

            if (objectivesLabel == null || runner == null || runner.Active == null)
                return;

            if (Time.time < briefingUntil)
            {
                objectivesLabel.text = runner.Active.Briefing;
                return;
            }

            builder.Clear();

            JobObjective[] objectives = runner.Active.Objectives;

            for (int i = 0; i < objectives.Length; i++)
            {
                bool done = i < runner.ObjectiveIndex;
                bool current = i == runner.ObjectiveIndex;

                builder.Append("<color=").Append(done ? doneColour : current ? currentColour : laterColour).Append('>');
                builder.Append(done ? "  ✓ " : current ? "▸ " : "    ");
                builder.Append(objectives[i].Description);
                builder.Append("</color>").AppendLine();

                if (current && !string.IsNullOrEmpty(objectives[i].Hint))
                    builder.Append("<color=").Append(laterColour).Append(">     ")
                           .Append(objectives[i].Hint).Append("</color>").AppendLine();
            }

            objectivesLabel.text = builder.ToString();
        }
    }
}
