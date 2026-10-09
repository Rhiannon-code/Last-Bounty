using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Combat
{
    public class Hitbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health target;
        [SerializeField] private float damageMultiplier = 2f;

        private void Reset() => target = GetComponentInParent<Health>();

        public void ApplyDamage(in DamageInfo info)
        {
            if (target == null) return;
            DamageInfo scaled = info;
            scaled.amount = info.amount * damageMultiplier;
            scaled.isCritical = damageMultiplier > 1f;
            target.ApplyDamage(in scaled);
        }
    }
}
