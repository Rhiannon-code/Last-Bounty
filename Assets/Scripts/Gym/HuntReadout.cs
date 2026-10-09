using System.Text;
using FPSParkour.AI;
using FPSParkour.Combat;
using FPSParkour.Investigation;
using FPSParkour.Perks;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.Gym
{
    [DisallowMultipleComponent]
    public class HuntReadout : MonoBehaviour
    {
        [SerializeField] HuntDirector director;
        [SerializeField] TargetScanner scanner;
        [SerializeField] GadgetThrower gadget;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PerkTree perks;
        [SerializeField] Text label;
        [SerializeField, Min(0.02f)] float refreshEvery = 0.1f;

        readonly StringBuilder builder = new StringBuilder();

        float nextRefreshAt;

        void Update()
        {
            if (label == null || director == null || Time.time < nextRefreshAt)
                return;

            nextRefreshAt = Time.time + refreshEvery;

            builder.Clear();
            AppendHunt();
            AppendScan();
            AppendTools();

            label.text = builder.ToString();
        }

        void AppendHunt()
        {
            Dossier dossier = director.Dossier;
            builder.AppendLine($"phase    {director.Phase}");

            if (dossier != null)
                builder.AppendLine($"dossier  {dossier.CluesKnown}/{dossier.TotalMarks} marks  " +
                                   $"(accuse needs {dossier.MinimumMarksToAccuse})");

            if (director.Heat != null)
                builder.AppendLine($"heat     {Bar(director.Heat.Heat)} {director.Heat.Heat * 100f:F0}%");

            FleeingBountyBrain target = director.Target;
            if (target == null)
                return;

            builder.AppendLine();
            builder.AppendLine($"target   {target.State}");

            if (target.IsRunning)
            {
                builder.AppendLine($"gap      {target.DistanceToPursuer:F0} m");
                builder.AppendLine($"stamina  {Bar(target.StaminaFraction)}");
            }
        }

        void AppendScan()
        {
            if (scanner == null)
                return;

            builder.AppendLine();

            if (!scanner.IsUnlocked)
            {
                builder.AppendLine("scan     locked: needs Field Optics");
                return;
            }

            CrowdIdentity subject = scanner.Subject;

            if (subject == null)
            {
                builder.AppendLine("scan     hold V on a stranger");
                return;
            }

            if (!subject.WasScanned)
            {
                builder.AppendLine($"scan     {subject.DisplayName}  {Bar(scanner.Progress)}");
                return;
            }

            builder.AppendLine($"scan     {subject.DisplayName}: matches {subject.MatchedMarks}/{subject.KnownMarks}"
                               + (subject.LooksLikeTarget ? "  ** probable **" : string.Empty));
        }

        void AppendTools()
        {
            builder.AppendLine();

            if (gadget != null)
                builder.AppendLine(gadget.IsUnlocked
                    ? $"snare    {gadget.Charges}/{gadget.MaxCharges}   G throw"
                    : "snare    locked: needs Capture Rig");

            if (perks != null)
                builder.AppendLine($"perks    {perks.SkillPoints} points unspent   P open tree");

            if (interactor?.Focus != null)
                builder.AppendLine($"[E] {interactor.Focus.Prompt}");
        }

        static string Bar(float fraction)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(fraction) * 10f);
            return new string('#', filled) + new string('.', 10 - filled);
        }
    }
}
