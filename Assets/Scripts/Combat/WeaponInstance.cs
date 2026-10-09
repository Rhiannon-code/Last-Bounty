using System;

namespace FPSParkour.Combat
{
    [Serializable]
    public class WeaponInstance
    {
        public WeaponDefinition definition;
        public int ammoInMag;
        public int reserveAmmo;

        public WeaponInstance(WeaponDefinition definition)
        {
            this.definition = definition;
            ammoInMag = definition.magSize;
            reserveAmmo = definition.maxReserve;
        }

        public bool IsEmpty => ammoInMag <= 0;
        public bool HasReserve => reserveAmmo > 0;
    }
}
