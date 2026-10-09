namespace FPSParkour.Player
{
    public static class AbilityCatalog
    {
        public readonly struct Entry
        {
            public readonly AbilityId Id;
            public readonly string Name;
            public readonly string Action;
            public readonly string Manual;
            public readonly string Tip;
            public readonly bool Teach;

            public Entry(AbilityId id, string name, string action, string manual, string tip, bool teach = true)
            {
                Id = id;
                Name = name;
                Action = action;
                Manual = manual;
                Tip = tip;
                Teach = teach;
            }
            public string KeyText(PlayerInputReader input)
            {
                string key = input != null ? input.KeyFor(Action) : string.Empty;
                return string.IsNullOrEmpty(key) ? Manual : key;
            }
        }

        public static readonly Entry[] Traversal =
        {
            new Entry(AbilityId.Sprint, "Sprint", "Sprint", "hold",
                "Hold to run. Speed is the currency, the slide and the wall-run both refuse to start below a threshold."),
            new Entry(AbilityId.Jump, "Jump", "Jump", "",
                "Jump. Held longer goes higher.", teach: false),
            new Entry(AbilityId.Crouch, "Crouch", "Crouch", "",
                "Crouch. Standing up is blocked while there is something overhead.", teach: false),
            new Entry(AbilityId.Slide, "Slide", "Crouch", "crouch at speed",
                "Crouch while sprinting. A slide keeps the speed you entered with, watch the peak figure to see whether it actually paid."),
            new Entry(AbilityId.Mantle, "Mantle", "", "walk into a ledge",
                "No key: push forward into any ledge under about two metres and you pull yourself up."),
            new Entry(AbilityId.WallRun, "Wall-run", "", "airborne, alongside a wall",
                "Jump toward a wall, hold forward, keep it beside you. Too slow on entry and nothing happens at all."),
            new Entry(AbilityId.WallJump, "Wall-jump", "Jump", "",
                "Jump while wall-running to launch off the wall, and it refreshes your air jump."),
            new Entry(AbilityId.DoubleJump, "Double-jump", "Jump", "",
                "Jump again in mid-air. A wall-run gives it back to you."),
            new Entry(AbilityId.AirDash, "Air-dash", "Dash", "",
                "A flat horizontal burst in the direction you are holding. Charges recharge over time."),
            new Entry(AbilityId.Grapple, "Grapple", "Grapple", "hold",
                "Hold aimed at a surface in range to be pulled toward it. Release to keep the momentum."),
            new Entry(AbilityId.GroundSlam, "Ground-slam", "Crouch", "crouch in the air",
                "Crouch in mid-air to slam down. The impact carries an area hit, and you land moving forward."),
        };

        public static readonly Entry[] Hunting =
        {
            new Entry(AbilityId.Scanner, "Scan", "Scan", "hold",
                "Hold on a stranger to read their marks against the dossier. It breaks if they leave the reticle, and every scan raises heat."),
            new Entry(AbilityId.SnareLauncher, "Snare", "Gadget", "",
                "Lob a snare pod to root someone without hurting them. Three charges, damage is how a capture becomes a kill by accident."),
        };

        public static bool TryFind(AbilityId id, out Entry entry)
        {
            foreach (Entry candidate in Traversal)
            {
                if (candidate.Id != id) continue;
                entry = candidate;
                return true;
            }

            foreach (Entry candidate in Hunting)
            {
                if (candidate.Id != id) continue;
                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }
    }
}
