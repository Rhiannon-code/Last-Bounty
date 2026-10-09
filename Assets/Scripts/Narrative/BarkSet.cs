using System;
using UnityEngine;

namespace FPSParkour.Narrative
{
    [Serializable]
    public class Bark
    {
        public string Speaker;
        [TextArea] public string Line;
        public FlagCondition Condition = new FlagCondition();
        [Min(1)] public int Weight = 1;
    }

    [CreateAssetMenu(fileName = "Barks_", menuName = "FPS Parkour/Bark Set", order = 11)]
    public class BarkSet : ScriptableObject
    {
        [SerializeField] string id = "barks.unnamed";
        [SerializeField] Bark[] barks;
        [SerializeField, Min(0f)] float cooldownSeconds = 12f;

        public string Id => id;
        public Bark[] Barks => barks;
        public float CooldownSeconds => cooldownSeconds;
    }
}
