using UnityEngine;

namespace FPSParkour.Core
{
    public enum DamageType
    {
        Kinetic,   // Bullets, melee
        Energy,    // Lasers/plasma
        EMP,       // Shields/synthetics
        Explosive, // AoE
        Corrosive, // Damage over time chemical
    }

    public struct DamageInfo
    {
        public float amount;
        public DamageType type;
        public Vector3 point;
        public Vector3 direction;
        public GameObject source;   // Who fired it
        public bool isCritical;     // E.g. headshot

        public DamageInfo(float amount, DamageType type, Vector3 point, Vector3 direction, GameObject source)
        {
            this.amount = amount;
            this.type = type;
            this.point = point;
            this.direction = direction;
            this.source = source;
            isCritical = false;
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(in DamageInfo info);
    }
}
