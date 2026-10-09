using System.Collections.Generic;
using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.World
{
    [DisallowMultipleComponent]
    public class CrowdSpawner : MonoBehaviour
    {
        [SerializeField] Transform tracked;
        [SerializeField] GameObject[] pedestrianPrefabs;
        [SerializeField, Min(0)] int maxPedestrians = 24;
        [SerializeField, Min(1f)] float spawnRadius = 40f;
        [SerializeField, Min(1f)] float despawnRadius = 60f;
        [SerializeField, Min(0.1f)] float evaluateEverySeconds = 1f;
        [SerializeField] LayerMask groundMask = ~0;

        [Header("Ambient chatter")]
        [SerializeField] BarkPlayer barks;
        [SerializeField] BarkSet crowdBarks;
        [SerializeField, Min(0f)] float barkChancePerEvaluate = 0.25f;

        readonly List<GameObject> active = new List<GameObject>();

        float nextEvaluateAt;

        public int ActiveCount => active.Count;

        void Start()
        {
            if (tracked != null)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                tracked = player.transform;
        }

        void Update()
        {
            if (tracked == null || pedestrianPrefabs == null || pedestrianPrefabs.Length == 0)
                return;

            if (Time.time < nextEvaluateAt)
                return;

            nextEvaluateAt = Time.time + evaluateEverySeconds;

            Recycle();

            if (active.Count < maxPedestrians)
                TrySpawn();

            if (crowdBarks != null && Random.value < barkChancePerEvaluate)
                barks?.TryPlay(crowdBarks);
        }

        void Recycle()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(active[i].transform.position, tracked.position) <= despawnRadius)
                    continue;

                Destroy(active[i]);
                active.RemoveAt(i);
            }
        }

        void TrySpawn()
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.5f, spawnRadius);
            Vector3 candidate = tracked.position + new Vector3(offset.x, 25f, offset.y);

            if (!Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
                return;

            GameObject prefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];
            if (prefab == null)
                return;

            active.Add(Instantiate(prefab, hit.point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform));
        }
    }
}
