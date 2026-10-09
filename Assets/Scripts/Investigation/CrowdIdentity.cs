using UnityEngine;

namespace FPSParkour.Investigation
{
    [DisallowMultipleComponent]
    public class CrowdIdentity : MonoBehaviour
    {
        [SerializeField] IdentityCatalog catalog;
        [SerializeField] IdentityMark[] marks;
        [SerializeField] bool rollFromCatalog = true;
        [SerializeField] bool rollSpecies = true;
        [SerializeField] string fixedSpecies;
        [SerializeField] Transform silhouette;
        [SerializeField] bool isBountyTarget;
        [SerializeField] string displayName = "local";
        [SerializeField] Renderer body;

        public bool IsBountyTarget => isBountyTarget;
        public string DisplayName => displayName;
        public IdentityMark[] Marks => marks;
        public bool WasScanned { get; private set; }
        public int MatchedMarks { get; private set; }
        public int KnownMarks { get; private set; }

        public bool LooksLikeTarget => WasScanned && KnownMarks > 0 && MatchedMarks == KnownMarks;

        void Awake()
        {
            if (rollFromCatalog && catalog != null)
            {
                marks = catalog.Roll();

                if (!rollSpecies && !string.IsNullOrEmpty(fixedSpecies))
                    Set(TraitSlot.Species, fixedSpecies);
            }

            Reshape();
            Recolour();
        }

        void Set(TraitSlot slot, string value)
        {
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i].Slot == slot)
                    marks[i].Value = value;
            }
        }

        void Reshape()
        {
            if (silhouette == null || catalog == null || !TryGetValue(TraitSlot.Build, out string build))
                return;

            silhouette.localScale = catalog.ScaleFor(build);
        }

        public bool TryGetValue(TraitSlot slot, out string value)
        {
            if (marks != null)
            {
                foreach (IdentityMark mark in marks)
                {
                    if (mark.Slot != slot)
                        continue;

                    value = mark.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void RecordScan(int matched, int known)
        {
            WasScanned = true;
            MatchedMarks = matched;
            KnownMarks = known;
        }

        void Recolour()
        {
            if (body == null || !TryGetValue(TraitSlot.Garment, out string garment))
                return;

            Random.State state = Random.state;
            Random.InitState(garment.GetHashCode());
            Color colour = Color.HSVToRGB(Random.value, 0.32f, Random.Range(0.55f, 0.85f));
            Random.state = state;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            block.SetColor("_BaseColor", colour);
            body.SetPropertyBlock(block);
        }
    }
}
