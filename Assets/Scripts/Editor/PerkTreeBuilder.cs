using System.Collections.Generic;
using FPSParkour.Perks;
using FPSParkour.Player;
using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class PerkTreeBuilder
    {
        class Node
        {
            public string Asset;
            public string Display;
            public string Description;
            public PerkCategory Category;
            public int Tier;
            public int Cost;
            public string[] Requires;
            public PerkEffect[] Effects;
        }

        static readonly Node[] Nodes =
        {
            new Node
            {
                Asset = "Perk_GravWeave", Display = "Grav-Weave Musculature", Category = PerkCategory.Movement,
                Tier = 0, Cost = 1, Requires = new string[0],
                Description = "Woven counter-mass through the long bones. You get a second push against nothing.",
                Effects = new[] { Grant(AbilityId.DoubleJump), Stat(StatType.GravityMult, ModifierOp.PercentMult, 0.9f) }
            },
            new Node
            {
                Asset = "Perk_ReflexBooster", Display = "Reflex Booster", Category = PerkCategory.Movement,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_GravWeave" },
                Description = "Pre-fires the correction before you have decided to make it.",
                Effects = new[] { Grant(AbilityId.AirDash), Stat(StatType.DashCharges, ModifierOp.Flat, 1f) }
            },
            new Node
            {
                Asset = "Perk_OverclockedLegs", Display = "Overclocked Legs", Category = PerkCategory.Augment,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_GravWeave" },
                Description = "Runs the leg drivers past their rated duty cycle. They do not complain until later.",
                Effects = new[]
                {
                    Stat(StatType.WallRunDurationMult, ModifierOp.PercentAdd, 0.40f),
                    Stat(StatType.SprintSpeedMult, ModifierOp.PercentAdd, 0.15f)
                }
            },
            new Node
            {
                Asset = "Perk_NeuroGrapple", Display = "Neuro-Grapple", Category = PerkCategory.Movement,
                Tier = 2, Cost = 3, Requires = new[] { "Perk_ReflexBooster" },
                Description = "Line and winch, fired on intent rather than on a trigger.",
                Effects = new[] { Grant(AbilityId.Grapple) }
            },
            new Node
            {
                Asset = "Perk_KineticDampers", Display = "Kinetic Dampers", Category = PerkCategory.Movement,
                Tier = 2, Cost = 3, Requires = new[] { "Perk_OverclockedLegs" },
                Description = "Dumps a fall into the ground instead of into you. Everyone nearby shares it.",
                Effects = new[] { Grant(AbilityId.GroundSlam) }
            },
            new Node
            {
                Asset = "Perk_VectorRider", Display = "Vector Rider", Category = PerkCategory.Movement,
                Tier = 3, Cost = 5, Requires = new[] { "Perk_NeuroGrapple", "Perk_KineticDampers" },
                Description = "Nothing new. Everything you already had, further and for longer.",
                Effects = new[]
                {
                    Stat(StatType.DashCharges, ModifierOp.Flat, 1f),
                    Stat(StatType.GrappleRangeMult, ModifierOp.PercentAdd, 0.35f),
                    Stat(StatType.SlideDurationMult, ModifierOp.PercentAdd, 0.30f)
                }
            },

            new Node
            {
                Asset = "Perk_DermalPlating", Display = "Dermal Plating", Category = PerkCategory.Augment,
                Tier = 0, Cost = 1, Requires = new string[0],
                Description = "Subdermal lattice. Standard issue for anyone the guild expects to get shot.",
                Effects = new[]
                {
                    Stat(StatType.MaxHealth, ModifierOp.Flat, 50f),
                    Stat(StatType.FallDamageResist, ModifierOp.Flat, 0.5f)
                }
            },
            new Node
            {
                Asset = "Perk_OxygenDebt", Display = "Oxygen Debt", Category = PerkCategory.Augment,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_DermalPlating" },
                Description = "Borrows against tomorrow to finish today's chase.",
                Effects = new[] { Stat(StatType.MaxStamina, ModifierOp.PercentAdd, 0.40f) }
            },
            new Node
            {
                Asset = "Perk_ImpactWeave", Display = "Impact Weave", Category = PerkCategory.Augment,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_DermalPlating" },
                Description = "Spreads a landing across the whole frame. Roofs stop being a commitment.",
                Effects = new[] { Stat(StatType.FallDamageResist, ModifierOp.Flat, 0.35f) }
            },
            new Node
            {
                Asset = "Perk_SecondHeart", Display = "Second Heart", Category = PerkCategory.Augment,
                Tier = 2, Cost = 3, Requires = new[] { "Perk_OxygenDebt" },
                Description = "A second pump under the first. Illegal in four arcs and fitted in all of them.",
                Effects = new[]
                {
                    Stat(StatType.MaxHealth, ModifierOp.PercentAdd, 0.40f),
                    Stat(StatType.MaxStamina, ModifierOp.PercentAdd, 0.25f)
                }
            },
            new Node
            {
                Asset = "Perk_HollowBones", Display = "Hollow Bones", Category = PerkCategory.Augment,
                Tier = 3, Cost = 5, Requires = new[] { "Perk_ImpactWeave", "Perk_SecondHeart" },
                Description = "There is less of you than there was. You move like it.",
                Effects = new[]
                {
                    Stat(StatType.GravityMult, ModifierOp.PercentMult, 0.85f),
                    Stat(StatType.JumpHeightMult, ModifierOp.PercentAdd, 0.20f),
                    Stat(StatType.MoveSpeedMult, ModifierOp.PercentAdd, 0.10f)
                }
            },
            
            new Node
            {
                Asset = "Perk_SmartLinkOptics", Display = "Smart-Link Optics", Category = PerkCategory.Weapon,
                Tier = 0, Cost = 1, Requires = new string[0],
                Description = "Ties the sight to the wrist. The gun stops arguing with you.",
                Effects = new[]
                {
                    Weapon(WeaponStatType.SpreadMult, ModifierOp.PercentMult, 0.7f),
                    Weapon(WeaponStatType.FireRateMult, ModifierOp.PercentAdd, 0.10f)
                }
            },
            new Node
            {
                Asset = "Perk_ExtendedMags", Display = "Extended Mags", Category = PerkCategory.Weapon,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_SmartLinkOptics" },
                Description = "More of it before you have to think about it.",
                Effects = new[] { Weapon(WeaponStatType.MagSizeAdd, ModifierOp.Flat, 8f) }
            },
            new Node
            {
                Asset = "Perk_RecoilGovernor", Display = "Recoil Governor", Category = PerkCategory.Weapon,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_SmartLinkOptics" },
                Description = "Eats the climb and hands the weapon back level.",
                Effects = new[]
                {
                    Weapon(WeaponStatType.SpreadMult, ModifierOp.PercentMult, 0.75f),
                    Weapon(WeaponStatType.ReloadSpeedMult, ModifierOp.PercentAdd, 0.25f)
                }
            },
            new Node
            {
                Asset = "Perk_DeepReserve", Display = "Deep Reserve", Category = PerkCategory.Weapon,
                Tier = 2, Cost = 3, Requires = new[] { "Perk_ExtendedMags" },
                Description = "You are not going back to the arc for ammunition halfway through a chase.",
                Effects = new[]
                {
                    Weapon(WeaponStatType.ReserveAmmoMult, ModifierOp.PercentAdd, 0.60f),
                    Weapon(WeaponStatType.MagSizeAdd, ModifierOp.Flat, 4f)
                }
            },
            new Node
            {
                Asset = "Perk_MarksmanSync", Display = "Marksman Sync", Category = PerkCategory.Weapon,
                Tier = 3, Cost = 5, Requires = new[] { "Perk_RecoilGovernor", "Perk_DeepReserve" },
                Description = "Beats a target down faster. It still cannot kill one: that stays a decision.",
                Effects = new[]
                {
                    Weapon(WeaponStatType.DamageMult, ModifierOp.PercentAdd, 0.25f),
                    Weapon(WeaponStatType.FireRateMult, ModifierOp.PercentAdd, 0.15f)
                }
            },

            new Node
            {
                Asset = "Perk_FieldOptics", Display = "Field Optics", Category = PerkCategory.Utility,
                Tier = 0, Cost = 1, Requires = new string[0],
                Description = "Reads a stranger's marks off a crowd at range. Without it you are asking, not looking.",
                Effects = new[] { Grant(AbilityId.Scanner), Stat(StatType.ScanRangeMult, ModifierOp.PercentAdd, 0.25f) }
            },
            new Node
            {
                Asset = "Perk_CaptureRig", Display = "Capture Rig", Category = PerkCategory.Utility,
                Tier = 0, Cost = 1, Requires = new string[0],
                Description = "Launcher and pods. The only thing in the kit that stops someone without hurting them.",
                Effects = new[] { Grant(AbilityId.SnareLauncher), Stat(StatType.SnareCharges, ModifierOp.Flat, 1f) }
            },
            new Node
            {
                Asset = "Perk_PatternRecognition", Display = "Pattern Recognition", Category = PerkCategory.Utility,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_FieldOptics" },
                Description = "Resolves a read in half the time. Half the time is the difference in a moving crowd.",
                Effects = new[] { Stat(StatType.ScanSpeedMult, ModifierOp.PercentAdd, 0.60f) }
            },
            new Node
            {
                Asset = "Perk_GreyMan", Display = "Grey Man", Category = PerkCategory.Utility,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_FieldOptics" },
                Description = "Nobody remembers being looked at. The district stays calm around you.",
                Effects = new[] { Stat(StatType.HeatGainMult, ModifierOp.PercentMult, 0.6f) }
            },
            new Node
            {
                Asset = "Perk_SnareBandolier", Display = "Snare Bandolier", Category = PerkCategory.Utility,
                Tier = 1, Cost = 2, Requires = new[] { "Perk_CaptureRig" },
                Description = "Two more pods and a longer hold. Missing stops ending the chase.",
                Effects = new[]
                {
                    Stat(StatType.SnareCharges, ModifierOp.Flat, 2f),
                    Stat(StatType.SnareDurationMult, ModifierOp.PercentAdd, 0.30f)
                }
            },
            new Node
            {
                Asset = "Perk_ColdRead", Display = "Cold Read", Category = PerkCategory.Utility,
                Tier = 2, Cost = 3, Requires = new[] { "Perk_PatternRecognition", "Perk_GreyMan" },
                Description = "Work a whole square without anyone deciding you are working it.",
                Effects = new[]
                {
                    Stat(StatType.HeatGainMult, ModifierOp.PercentMult, 0.55f),
                    Stat(StatType.ScanRangeMult, ModifierOp.PercentAdd, 0.40f)
                }
            },
            new Node
            {
                Asset = "Perk_TheLongGame", Display = "The Long Game", Category = PerkCategory.Utility,
                Tier = 3, Cost = 5, Requires = new[] { "Perk_ColdRead", "Perk_SnareBandolier" },
                Description = "You stopped needing the chase.",
                Effects = new[]
                {
                    Stat(StatType.SnareCharges, ModifierOp.Flat, 2f),
                    Stat(StatType.SnareDurationMult, ModifierOp.PercentAdd, 0.40f),
                    Stat(StatType.HeatGainMult, ModifierOp.PercentMult, 0.7f)
                }
            },
        };

        public static int TotalCost
        {
            get
            {
                int total = 0;

                foreach (Node node in Nodes)
                    total += node.Cost;

                return total;
            }
        }

        public static readonly string[] StartingAssets =
        {
            "Perk_GravWeave", "Perk_FieldOptics", "Perk_CaptureRig"
        };

        public static PerkDefinition[] Build(string contentRoot)
        {
            Dictionary<string, PerkDefinition> byAsset = new Dictionary<string, PerkDefinition>();
            PerkDefinition[] ordered = new PerkDefinition[Nodes.Length];

            for (int i = 0; i < Nodes.Length; i++)
            {
                Node node = Nodes[i];
                PerkDefinition perk = ScriptableObject.CreateInstance<PerkDefinition>();

                perk.displayName = node.Display;
                perk.description = node.Description;
                perk.category = node.Category;
                perk.tier = node.Tier;
                perk.cost = node.Cost;
                perk.effects = node.Effects;

                AssetDatabase.CreateAsset(perk, $"{contentRoot}/{node.Asset}.asset");
                byAsset[node.Asset] = perk;
                ordered[i] = perk;
            }

            foreach (Node node in Nodes)
            {
                PerkDefinition perk = byAsset[node.Asset];
                perk.prerequisites = new PerkDefinition[node.Requires.Length];

                for (int i = 0; i < node.Requires.Length; i++)
                {
                    if (!byAsset.TryGetValue(node.Requires[i], out PerkDefinition prerequisite))
                        Debug.LogError($"Perk '{node.Asset}' requires '{node.Requires[i]}', which is not in the tree.");
                    else
                        perk.prerequisites[i] = prerequisite;
                }

                EditorUtility.SetDirty(perk);
            }

            return ordered;
        }

        public static PerkDefinition[] All(string contentRoot)
        {
            PerkDefinition[] all = new PerkDefinition[Nodes.Length];

            for (int i = 0; i < Nodes.Length; i++)
                all[i] = AssetDatabase.LoadAssetAtPath<PerkDefinition>($"{contentRoot}/{Nodes[i].Asset}.asset");

            return all;
        }

        public static PerkDefinition[] Starting(string contentRoot)
        {
            PerkDefinition[] starting = new PerkDefinition[StartingAssets.Length];

            for (int i = 0; i < StartingAssets.Length; i++)
                starting[i] = AssetDatabase.LoadAssetAtPath<PerkDefinition>($"{contentRoot}/{StartingAssets[i]}.asset");

            return starting;
        }

        static PerkEffect Grant(AbilityId ability) =>
            new PerkEffect { kind = PerkEffectKind.GrantAbility, ability = ability };

        static PerkEffect Stat(StatType stat, ModifierOp op, float value) =>
            new PerkEffect { kind = PerkEffectKind.PlayerStat, playerStat = stat, op = op, value = value };

        static PerkEffect Weapon(WeaponStatType stat, ModifierOp op, float value) =>
            new PerkEffect { kind = PerkEffectKind.WeaponStat, weaponStat = stat, op = op, value = value };
    }
}
