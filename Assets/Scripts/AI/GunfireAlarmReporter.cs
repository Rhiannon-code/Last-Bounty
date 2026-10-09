using FPSParkour.Combat;
using UnityEngine;

namespace FPSParkour.AI
{
    [DisallowMultipleComponent]
    public class GunfireAlarmReporter : MonoBehaviour
    {
        [SerializeField] WeaponController weapons;
        [SerializeField] float alarmRadius = 45f;
        [SerializeField] EnemySenses[] alsoNotify;

        void OnEnable()
        {
            if (weapons != null)
                weapons.OnFired += OnFired;
        }

        void OnDisable()
        {
            if (weapons != null)
                weapons.OnFired -= OnFired;
        }

        void OnFired(WeaponInstance instance)
        {
            CityAlarm.Instance?.Raise(transform.position, alarmRadius);

            if (alsoNotify == null)
                return;

            foreach (EnemySenses senses in alsoNotify)
            {
                if (senses != null && Vector3.Distance(senses.transform.position, transform.position) <= alarmRadius)
                    senses.ReportNoise(transform.position);
            }
        }
    }
}
