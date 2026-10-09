using System.Collections.Generic;
using System.Text;
using FPSParkour.Perks;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class PerkTreeView : MonoBehaviour
    {
        [SerializeField] PerkTree tree;
        [SerializeField] PerkDefinition[] displayed;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform content;
        [SerializeField] Button rowPrefab;
        [SerializeField] Text pointsLabel;

        readonly List<Button> rows = new List<Button>();
        readonly StringBuilder builder = new StringBuilder();

        void OnEnable()
        {
            if (tree != null)
            {
                tree.OnPerkUnlocked += OnPerkUnlocked;
                tree.OnSkillPointsChanged += OnPointsChanged;
            }

            Rebuild();
        }

        void OnDisable()
        {
            if (tree == null)
                return;

            tree.OnPerkUnlocked -= OnPerkUnlocked;
            tree.OnSkillPointsChanged -= OnPointsChanged;
        }

        public void Toggle()
        {
            if (root == null)
                return;

            root.SetActive(!root.activeSelf);
            if (root.activeSelf)
                Rebuild();
        }

        void OnPerkUnlocked(PerkDefinition perk) => Rebuild();
        void OnPointsChanged(int points) => Rebuild();

        string Describe(PerkDefinition perk, bool unlocked)
        {
            builder.Clear();
            builder.Append(unlocked ? "[x] " : tree.CanUnlock(perk) ? "[ ] " : "[-] ");
            builder.Append(perk.displayName);
            builder.Append("   T").Append(perk.tier);
            builder.Append("  ").Append(perk.cost).Append(perk.cost == 1 ? "pt" : "pts");

            if (unlocked)
                return builder.ToString();

            if (perk.prerequisites == null)
                return builder.ToString();

            bool first = true;

            foreach (PerkDefinition prerequisite in perk.prerequisites)
            {
                if (prerequisite == null || tree.IsUnlocked(prerequisite))
                    continue;

                builder.Append(first ? "   needs " : ", ");
                builder.Append(prerequisite.displayName);
                first = false;
            }

            return builder.ToString();
        }

        void Rebuild()
        {
            foreach (Button row in rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            rows.Clear();

            if (tree == null || displayed == null || content == null || rowPrefab == null)
                return;

            if (pointsLabel != null)
                pointsLabel.text = $"Points: {tree.SkillPoints}";

            foreach (PerkDefinition perk in displayed)
            {
                if (perk == null)
                    continue;

                Button row = Instantiate(rowPrefab, content);
                bool unlocked = tree.IsUnlocked(perk);

                Text label = row.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = Describe(perk, unlocked);

                row.interactable = !unlocked && tree.CanUnlock(perk);

                PerkDefinition captured = perk;
                row.onClick.AddListener(() => tree.TryUnlock(captured));
                rows.Add(row);
            }
        }
    }
}
