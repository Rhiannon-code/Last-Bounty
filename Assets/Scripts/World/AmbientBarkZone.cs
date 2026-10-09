using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class AmbientBarkZone : MonoBehaviour
    {
        [SerializeField] BarkPlayer barks;
        [SerializeField] BarkSet set;
        [SerializeField] string playerTag = "Player";
        [SerializeField, Min(0f)] float repeatEverySeconds = 20f;

        float nextAllowedAt;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other) => TryBark(other);

        void OnTriggerStay(Collider other)
        {
            if (repeatEverySeconds > 0f)
                TryBark(other);
        }

        void TryBark(Collider other)
        {
            if (!other.CompareTag(playerTag) || Time.time < nextAllowedAt)
                return;

            if (barks != null && barks.TryPlay(set))
                nextAllowedAt = Time.time + repeatEverySeconds;
        }
    }
}
