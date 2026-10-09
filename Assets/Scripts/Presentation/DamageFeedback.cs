using FPSParkour.AI;
using FPSParkour.Combat;
using FPSParkour.Core;
using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.Presentation
{
    [DisallowMultipleComponent]
    public class DamageFeedback : MonoBehaviour
    {
        [Header("Outgoing")]
        [SerializeField] GameObject shooter;
        [SerializeField] Graphic hitMarker;
        [SerializeField] float markerSeconds = 0.12f;
        [SerializeField] Color hitColour = Color.white;
        [SerializeField] Color downedColour = new Color(0.4f, 1f, 0.6f);

        [Header("Incoming")]
        [SerializeField] PlayerStats stats;
        [SerializeField] Graphic damageFlash;
        [SerializeField] float flashSeconds = 0.35f;
        [SerializeField] Color flashColour = new Color(0.7f, 0.1f, 0.1f, 0.45f);

        float markerUntil;
        float flashUntil;
        float lastHealth;

        void Awake()
        {
            lastHealth = stats != null ? stats.Health : 0f;
            Show(hitMarker, 0f, hitColour);
            Show(damageFlash, 0f, flashColour);
        }

        void OnEnable()
        {
            if (stats != null)
                stats.OnHealthChanged += OnHealthChanged;

            Health.AnyDamaged += OnAnyDamaged;
        }

        void OnDisable()
        {
            if (stats != null)
                stats.OnHealthChanged -= OnHealthChanged;

            Health.AnyDamaged -= OnAnyDamaged;
        }

        void Update()
        {
            Fade(hitMarker, markerUntil, markerSeconds, hitColour);
            Fade(damageFlash, flashUntil, flashSeconds, flashColour);
        }

        void OnAnyDamaged(Health target, float dealt, DamageInfo info)
        {
            if (shooter == null || info.source != shooter || target == null)
                return;

            Subduable subduable = target.GetComponent<Subduable>();
            ReportHit(subduable != null && subduable.IsDowned);
        }

        public void ReportHit(bool downed)
        {
            markerUntil = Time.time + markerSeconds;
            Show(hitMarker, 1f, downed ? downedColour : hitColour);
        }

        void OnHealthChanged(float current, float max)
        {
            if (current < lastHealth)
            {
                flashUntil = Time.time + flashSeconds;
                Show(damageFlash, 1f, flashColour);
            }

            lastHealth = current;
        }

        void Fade(Graphic graphic, float until, float duration, Color colour)
        {
            if (graphic == null || duration <= 0f)
                return;

            float remaining = until - Time.time;
            Show(graphic, remaining <= 0f ? 0f : remaining / duration, colour);
        }

        static void Show(Graphic graphic, float alpha, Color colour)
        {
            if (graphic == null)
                return;

            colour.a *= Mathf.Clamp01(alpha);
            graphic.color = colour;
            graphic.enabled = alpha > 0.001f;
        }
    }
}
