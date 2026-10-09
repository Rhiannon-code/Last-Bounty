using System.Collections.Generic;
using FPSParkour.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] DialogueRunner runner;
        [SerializeField] GameObject root;
        [SerializeField] Text speakerLabel;
        [SerializeField] Text lineLabel;
        [SerializeField] RectTransform choiceContainer;
        [SerializeField] Button choiceButtonPrefab;
        [SerializeField] Button continueButton;

        readonly List<Button> choiceButtons = new List<Button>();

        void OnEnable()
        {
            if (runner == null)
                return;

            runner.LineShown += OnLineShown;
            runner.ChoicesOffered += OnChoicesOffered;
            runner.Ended += OnEnded;

            continueButton?.onClick.AddListener(runner.Advance);
            SetVisible(false);
        }

        void OnDisable()
        {
            if (runner == null)
                return;

            runner.LineShown -= OnLineShown;
            runner.ChoicesOffered -= OnChoicesOffered;
            runner.Ended -= OnEnded;

            continueButton?.onClick.RemoveListener(runner.Advance);
        }

        void OnLineShown(DialogueNode node)
        {
            SetVisible(true);
            ClearChoices();

            if (speakerLabel != null) speakerLabel.text = node.Speaker;
            if (lineLabel != null) lineLabel.text = node.Line;
            if (continueButton != null) continueButton.gameObject.SetActive(!node.IsChoiceNode);
        }

        void OnChoicesOffered(IReadOnlyList<DialogueChoice> choices)
        {
            ClearChoices();

            if (choiceContainer == null || choiceButtonPrefab == null)
                return;

            for (int i = 0; i < choices.Count; i++)
            {
                Button button = Instantiate(choiceButtonPrefab, choiceContainer);

                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = choices[i].Text;

                int index = i;
                button.onClick.AddListener(() => runner.Choose(index));
                choiceButtons.Add(button);
            }
        }

        void OnEnded(DialogueGraph graph)
        {
            ClearChoices();
            SetVisible(false);
        }

        void ClearChoices()
        {
            foreach (Button button in choiceButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            choiceButtons.Clear();
        }

        void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }
    }
}
