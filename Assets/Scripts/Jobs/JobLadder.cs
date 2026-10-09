using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.Jobs
{
    [DisallowMultipleComponent]
    public class JobLadder : MonoBehaviour
    {
        [SerializeField] JobRunner runner;
        [SerializeField] StoryFlags flags;
        [SerializeField] JobDefinition[] jobs;
        [SerializeField, Min(0f)] float gapSeconds = 4f;
        [SerializeField] bool beginOnStart = true;

        public int Index { get; private set; }
        public JobDefinition Next => Index < jobs.Length ? jobs[Index] : null;

        float handOverAt = -1f;

        void OnEnable()
        {
            if (runner == null)
                return;

            runner.JobCompleted += OnFinished;
            runner.JobFailed += OnFailed;
        }

        void OnDisable()
        {
            if (runner == null)
                return;

            runner.JobCompleted -= OnFinished;
            runner.JobFailed -= OnFailed;
        }

        void Start()
        {
            if (beginOnStart)
                StartLadder();
        }

        public void StartLadder()
        {
            Index = 0;
            BeginNextAvailable();
        }

        void Update()
        {
            if (handOverAt < 0f || Time.time < handOverAt)
                return;

            handOverAt = -1f;
            BeginNextAvailable();
        }

        void OnFinished(JobDefinition job)
        {
            Index++;
            handOverAt = Time.time + gapSeconds;
        }

        void OnFailed(JobDefinition job, FailureKind kind) => handOverAt = Time.time + gapSeconds;

        void BeginNextAvailable()
        {
            while (Index < jobs.Length)
            {
                JobDefinition job = jobs[Index];

                if (job != null && (job.Availability == null || job.Availability.IsMet(flags)))
                {
                    runner.Begin(job);
                    return;
                }

                Index++;
            }
        }
    }
}
