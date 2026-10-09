using System;
using System.Collections.Generic;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSParkour.World
{
    [DisallowMultipleComponent]
    public class DistrictStreamer : MonoBehaviour
    {
        [SerializeField] Transform tracked;
        [SerializeField] DistrictDefinition[] districts;
        [SerializeField] StoryFlags flags;
        [SerializeField, Min(0.1f)] float evaluateEverySeconds = 0.5f;

        readonly Dictionary<string, AsyncOperation> loading = new Dictionary<string, AsyncOperation>();
        readonly HashSet<string> loaded = new HashSet<string>();

        float nextEvaluateAt;

        public event Action<DistrictDefinition> DistrictLoaded;
        public event Action<DistrictDefinition> DistrictUnloaded;

        public IReadOnlyCollection<string> Loaded => loaded;

        void Start()
        {
            if (tracked == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    tracked = player.transform;
            }
        }

        void Update()
        {
            if (tracked == null || districts == null || Time.time < nextEvaluateAt)
                return;

            nextEvaluateAt = Time.time + evaluateEverySeconds;
            Vector3 position = tracked.position;

            foreach (DistrictDefinition district in districts)
            {
                if (district == null || string.IsNullOrEmpty(district.SceneName))
                    continue;

                float distance = Vector3.Distance(position, district.Centre);
                bool isLoaded = loaded.Contains(district.SceneName);
                bool allowed = district.Availability.IsMet(flags);

                if (!isLoaded && allowed && distance <= district.LoadRadius)
                    Load(district);
                else if (isLoaded && (!allowed || distance > district.UnloadRadius))
                    Unload(district);
            }
        }

        void Load(DistrictDefinition district)
        {
            if (loading.ContainsKey(district.SceneName))
                return;

            AsyncOperation operation = SceneManager.LoadSceneAsync(district.SceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"District scene '{district.SceneName}' is not in Build Settings.", this);
                return;
            }

            loading[district.SceneName] = operation;
            operation.completed += _ =>
            {
                loading.Remove(district.SceneName);
                loaded.Add(district.SceneName);
                DistrictLoaded?.Invoke(district);
            };
        }

        void Unload(DistrictDefinition district)
        {
            if (loading.ContainsKey(district.SceneName))
                return;

            loaded.Remove(district.SceneName);
            AsyncOperation operation = SceneManager.UnloadSceneAsync(district.SceneName);

            if (operation != null)
                operation.completed += _ => DistrictUnloaded?.Invoke(district);
        }
    }
}
