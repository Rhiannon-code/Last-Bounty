using System;
using UnityEngine;

namespace FPSParkour.Investigation
{
    public enum TraitSlot
    {
        Species,
        Build,
        Augment,
        Garment,
    }

    [Serializable]
    public struct IdentityMark
    {
        public TraitSlot Slot;
        public string Value;

        public IdentityMark(TraitSlot slot, string value)
        {
            Slot = slot;
            Value = value;
        }
    }
}
