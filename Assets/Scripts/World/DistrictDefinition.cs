using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.World
{
    [CreateAssetMenu(fileName = "District_", menuName = "FPS Parkour/District", order = 30)]
    public class DistrictDefinition : ScriptableObject
    {
        [SerializeField] string id = "district.unnamed";
        [SerializeField] string displayName = "Unnamed";
        [SerializeField] string sceneName;
        [SerializeField] Vector3 centre;
        [SerializeField, Min(1f)] float loadRadius = 250f;
        [SerializeField, Min(1f)] float unloadRadius = 320f;
        [SerializeField] FlagCondition availability = new FlagCondition();

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public Vector3 Centre => centre;
        public float LoadRadius => loadRadius;
        public float UnloadRadius => Mathf.Max(unloadRadius, loadRadius + 25f);
        public FlagCondition Availability => availability;
    }
}
