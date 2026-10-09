using System;
using UnityEngine;

namespace FPSParkour.Narrative
{
    /// <summary>A [CHOICE] branch from SCRIPT_FORMAT.md.</summary>
    [Serializable]
    public class DialogueChoice
    {
        [TextArea] public string Text;
        public string NextNodeId;
        public FlagCondition Condition = new FlagCondition();
        public string[] SetFlags;
    }

    [Serializable]
    public class DialogueNode
    {
        public string Id;
        public string Speaker;
        [TextArea(2, 6)] public string Line;
        public FlagCondition Condition = new FlagCondition();
        public string[] SetFlags;
        public string NextNodeId;
        public DialogueChoice[] Choices;

        public bool IsChoiceNode => Choices != null && Choices.Length > 0;
    }

    [CreateAssetMenu(fileName = "Dialogue_", menuName = "FPS Parkour/Dialogue Graph", order = 10)]
    public class DialogueGraph : ScriptableObject
    {
        [SerializeField] string id = "dlg.unnamed";
        [SerializeField] string entryNodeId;
        [SerializeField] DialogueNode[] nodes;

        public string Id => id;
        public string EntryNodeId => entryNodeId;
        public DialogueNode[] Nodes => nodes;

        public DialogueNode Find(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || nodes == null)
                return null;

            foreach (DialogueNode node in nodes)
            {
                if (node != null && node.Id == nodeId)
                    return node;
            }

            return null;
        }

        public bool Validate(out string problem)
        {
            if (nodes == null || nodes.Length == 0)
            {
                problem = $"'{id}' has no nodes";
                return false;
            }

            if (Find(entryNodeId) == null)
            {
                problem = $"'{id}' entry node '{entryNodeId}' does not exist";
                return false;
            }

            foreach (DialogueNode node in nodes)
            {
                if (!string.IsNullOrEmpty(node.NextNodeId) && Find(node.NextNodeId) == null)
                {
                    problem = $"'{id}' node '{node.Id}' points at missing node '{node.NextNodeId}'";
                    return false;
                }

                if (node.Choices == null)
                    continue;

                foreach (DialogueChoice choice in node.Choices)
                {
                    if (!string.IsNullOrEmpty(choice.NextNodeId) && Find(choice.NextNodeId) == null)
                    {
                        problem = $"'{id}' choice in '{node.Id}' points at missing node '{choice.NextNodeId}'";
                        return false;
                    }
                }
            }

            problem = null;
            return true;
        }
    }
}
