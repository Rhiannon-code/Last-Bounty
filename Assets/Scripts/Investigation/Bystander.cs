using FPSParkour.Bounty;
using FPSParkour.Core;
using FPSParkour.Narrative;
using UnityEngine;

namespace FPSParkour.Investigation
{
    [DisallowMultipleComponent]
    public class Bystander : MonoBehaviour, IInteractable
    {
        [SerializeField] CrowdIdentity identity;
        [SerializeField] Dossier dossier;
        [SerializeField] DistrictHeat heat;
        [SerializeField] HuntDirector director;

        [Header("Asking")]
        [SerializeField] DialogueGraph graph;
        [SerializeField] DialogueRunner runner;
        [SerializeField] StoryFlags flags;
        [SerializeField] FlagCondition grantCondition = new FlagCondition();
        [SerializeField] bool revealsSpecificSlot;
        [SerializeField] TraitSlot revealsSlot;

        [Header("Price")]
        [SerializeField] CreditsWallet wallet;
        [SerializeField, Min(0)] int price;

        [Header("Cost in attention")]
        [SerializeField] float heatPerAsk = 0.03f;
        [SerializeField] FlagCondition leanedOnCondition = new FlagCondition();
        [SerializeField] float heatIfLeanedOn = 0.18f;
        [SerializeField, Range(0f, 1f)] float chanceAPasserByKnowsSomething = 0.35f;

        bool exhausted;
        bool waitingOnDialogue;

        public bool IsInformant => graph != null;

        public string Prompt
        {
            get
            {
                if (identity != null && identity.LooksLikeTarget)
                    return $"Detain: {identity.DisplayName} ({identity.MatchedMarks}/{identity.KnownMarks} marks)";

                if (exhausted)
                    return string.Empty;

                if (price > 0)
                    return $"Pay {price}cr for a lead";

                return dossier != null ? $"Ask about {dossier.TargetName}" : "Ask around";
            }
        }

        public bool CanInteract(GameObject actor)
        {
            if (identity != null && identity.LooksLikeTarget)
                return director != null && dossier != null && dossier.CanAccuse;

            return !exhausted && !waitingOnDialogue;
        }

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor))
                return;

            if (identity != null && identity.LooksLikeTarget)
            {
                director.Accuse(identity);
                return;
            }

            Ask();
        }

        void Ask()
        {
            heat?.Add(heatPerAsk);

            if (price > 0)
            {
                if (wallet == null || !wallet.TrySpend(price))
                    return;
            }

            if (graph != null && runner != null && runner.Begin(graph))
            {
                waitingOnDialogue = true;
                runner.Ended += OnDialogueEnded;
                return;
            }

            if (Random.value <= chanceAPasserByKnowsSomething)
                GrantClue();

            exhausted = true;
        }

        void OnDialogueEnded(DialogueGraph finished)
        {
            runner.Ended -= OnDialogueEnded;
            waitingOnDialogue = false;
            exhausted = true;

            if (grantCondition.IsMet(flags))
                GrantClue();

            if (leanedOnCondition.RequireAll != null && leanedOnCondition.RequireAll.Length > 0
                && leanedOnCondition.IsMet(flags))
                heat?.Add(heatIfLeanedOn);
        }

        void GrantClue()
        {
            if (dossier == null)
                return;

            if (revealsSpecificSlot)
                dossier.LearnClue(revealsSlot);
            else if (dossier.TryNextUnknownSlot(out TraitSlot slot))
                dossier.LearnClue(slot);
        }
    }
}
