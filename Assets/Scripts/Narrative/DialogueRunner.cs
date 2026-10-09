using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSParkour.Narrative
{
    [DisallowMultipleComponent]
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] StoryFlags flags;
        [SerializeField] int maxSkipsPerAdvance = 64;

        readonly List<DialogueChoice> available = new List<DialogueChoice>();

        DialogueGraph graph;

        public bool IsRunning { get; private set; }
        public DialogueNode Current { get; private set; }
        public IReadOnlyList<DialogueChoice> AvailableChoices => available;

        public event Action<DialogueNode> LineShown;
        public event Action<IReadOnlyList<DialogueChoice>> ChoicesOffered;
        public event Action<DialogueGraph> Ended;

        public bool Begin(DialogueGraph toRun)
        {
            if (toRun == null || IsRunning)
                return false;

            if (!toRun.Validate(out string problem))
            {
                Debug.LogError($"Dialogue rejected: {problem}", this);
                return false;
            }

            graph = toRun;
            IsRunning = true;
            EnterNode(graph.EntryNodeId);
            return true;
        }

        public void Advance()
        {
            if (!IsRunning || Current == null || Current.IsChoiceNode)
                return;

            EnterNode(Current.NextNodeId);
        }

        public void Choose(int index)
        {
            if (!IsRunning || Current == null || !Current.IsChoiceNode)
                return;

            if (index < 0 || index >= available.Count)
                return;

            DialogueChoice choice = available[index];
            flags?.SetAll(choice.SetFlags);
            EnterNode(choice.NextNodeId);
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            DialogueGraph finished = graph;
            IsRunning = false;
            Current = null;
            graph = null;
            available.Clear();
            Ended?.Invoke(finished);
        }

        void EnterNode(string nodeId)
        {
            for (int guard = 0; guard < maxSkipsPerAdvance; guard++)
            {
                DialogueNode node = graph != null ? graph.Find(nodeId) : null;

                if (node == null)
                {
                    Stop();
                    return;
                }

                if (!node.Condition.IsMet(flags))
                {
                    nodeId = node.NextNodeId;
                    continue;
                }

                Current = node;
                flags?.SetAll(node.SetFlags);

                if (node.IsChoiceNode)
                {
                    BuildChoices(node);

                    if (available.Count == 0)
                    {
                        nodeId = node.NextNodeId;
                        continue;
                    }

                    LineShown?.Invoke(node);
                    ChoicesOffered?.Invoke(available);
                    return;
                }

                LineShown?.Invoke(node);
                return;
            }

            Debug.LogError($"Dialogue '{graph?.Id}' skipped {maxSkipsPerAdvance} nodes without landing; check for a condition loop.", this);
            Stop();
        }

        void BuildChoices(DialogueNode node)
        {
            available.Clear();

            foreach (DialogueChoice choice in node.Choices)
            {
                if (choice != null && choice.Condition.IsMet(flags))
                    available.Add(choice);
            }
        }
    }
}
