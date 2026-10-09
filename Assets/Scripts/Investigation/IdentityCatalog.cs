using UnityEngine;

namespace FPSParkour.Investigation
{
    [CreateAssetMenu(fileName = "IdentityCatalog", menuName = "FPS Parkour/Identity Catalog", order = 30)]
    public class IdentityCatalog : ScriptableObject
    {
        [SerializeField] string[] species = { "human", "kroga", "hasque", "synth" };
        [SerializeField] string[] builds = { "slight", "average", "heavy", "tall" };
        [SerializeField] Vector2[] buildScales =
        {
            new Vector2(0.92f, 0.96f), new Vector2(1f, 1f),
            new Vector2(1.12f, 0.98f), new Vector2(0.97f, 1.08f)
        };
        [SerializeField] string[] augments = { "none", "optic", "spinal brace", "hauler arm" };
        [SerializeField] string[] garments = { "work drab", "arc coat", "vendor apron", "transit weave" };

        public string[] Values(TraitSlot slot)
        {
            switch (slot)
            {
                case TraitSlot.Species: return species;
                case TraitSlot.Build: return builds;
                case TraitSlot.Augment: return augments;
                default: return garments;
            }
        }

        public Vector3 ScaleFor(string build)
        {
            if (builds != null && buildScales != null)
            {
                for (int i = 0; i < builds.Length && i < buildScales.Length; i++)
                {
                    if (builds[i] == build)
                        return new Vector3(buildScales[i].x, buildScales[i].y, buildScales[i].x);
                }
            }

            return Vector3.one;
        }

        public IdentityMark[] Roll()
        {
            IdentityMark[] marks = new IdentityMark[4];

            for (int i = 0; i < marks.Length; i++)
            {
                TraitSlot slot = (TraitSlot)i;
                string[] pool = Values(slot);
                marks[i] = new IdentityMark(slot, pool != null && pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : "unknown");
            }

            return marks;
        }

        public bool Contains(TraitSlot slot, string value)
        {
            string[] pool = Values(slot);

            if (pool == null)
                return false;

            foreach (string entry in pool)
            {
                if (entry == value)
                    return true;
            }

            return false;
        }
    }
}
