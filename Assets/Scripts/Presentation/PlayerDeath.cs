using FPSParkour.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.Presentation
{
    [DisallowMultipleComponent]
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] CharacterController controller;
        [SerializeField] Graphic fade;
        [SerializeField] Text label;
        [SerializeField] float respawnAfterSeconds = 2f;
        [SerializeField] Transform respawnAt;

        Vector3 spawnPoint;
        float respawnAtTime = -1f;

        public bool IsDead => respawnAtTime > 0f;

        void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (controller == null) controller = GetComponent<CharacterController>();

            spawnPoint = respawnAt != null ? respawnAt.position : transform.position;
            SetOverlay(false);
        }

        void OnEnable()
        {
            if (stats != null)
                stats.OnHealthChanged += OnHealthChanged;
        }

        void OnDisable()
        {
            if (stats != null)
                stats.OnHealthChanged -= OnHealthChanged;
        }

        void Update()
        {
            if (!IsDead || Time.time < respawnAtTime)
                return;

            Respawn();
        }

        void OnHealthChanged(float current, float max)
        {
            if (IsDead || current > 0f)
                return;

            respawnAtTime = Time.time + respawnAfterSeconds;
            SetOverlay(true);
        }

        void Respawn()
        {
            respawnAtTime = -1f;
            SetOverlay(false);

            bool wasEnabled = controller != null && controller.enabled;

            if (wasEnabled)
                controller.enabled = false;

            transform.position = spawnPoint;

            if (wasEnabled)
                controller.enabled = true;

            stats?.Heal(stats.MaxHealth);
        }

        void SetOverlay(bool visible)
        {
            if (fade != null)
            {
                Color colour = fade.color;
                colour.a = visible ? 0.75f : 0f;
                fade.color = colour;
                fade.enabled = visible;
            }

            if (label != null)
            {
                label.text = visible ? "DOWN" : string.Empty;
                label.enabled = visible;
            }
        }
    }
}
