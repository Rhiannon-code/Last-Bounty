using System.Collections.Generic;
using System.IO;
using FPSParkour.Bounty;
using FPSParkour.Investigation;
using FPSParkour.Jobs;
using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class JobBuilder
    {
        const string BountyRoot = "Assets/_Bounties";
        const string JobRoot = "Assets/_Jobs";

        [MenuItem("FPS Parkour/Build Showcase Jobs")]
        public static void Build()
        {
            Dictionary<string, BountyContract> byName = LoadContracts();

            if (byName.Count == 0)
            {
                EditorUtility.DisplayDialog("No contracts",
                    $"Run 'FPS Parkour > Import Bounty Roster' first, it writes the contracts to {BountyRoot}.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Build showcase jobs",
                    $"DELETES and regenerates:\n\n{JobRoot}\n\nNothing else is touched. Commit first.",
                    "Build", "Cancel"))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Generate(byName);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Showcase job build failed: {e}");
                }
            };
        }

        static void Generate(Dictionary<string, BountyContract> byName)
        {
            if (AssetDatabase.IsValidFolder(JobRoot))
                AssetDatabase.DeleteAsset(JobRoot);

            Directory.CreateDirectory(JobRoot);
            AssetDatabase.Refresh();

            List<JobDefinition> ladder = new List<JobDefinition>();

            ladder.Add(Create(byName, "Teddy Vane", "job.01.vane", "Skim job: Teddy Vane",
                JobShape.StalkAndChase, FailureKind.None,
                "scanner, informants, heat, flee AI, snare, resolution",
                "Petty theft. The board wants him alive and nobody is in a hurry.\n" +
                "He is in a crowd and he does not know you are looking yet.",
                Marks("duat-born", "slight", "ledger cuff", "transit weave"),
                new[]
                {
                    Step(ObjectiveKind.ScanAnyone, "Scan anyone in the crowd",
                        "Hold the scan key on a stranger. It compares them against what the contract already told you."),
                    Step(ObjectiveKind.LearnClues, "Buy a second mark from a local", 2f,
                        "Most passers-by know nothing. Informants want money or pressure."),
                    Step(ObjectiveKind.IdentifyTarget, "Detain the man who matches",
                        "Scan before you accuse. A civilian can match every mark you know."),
                    Step(ObjectiveKind.ChaseBegins, "Keep up when he runs"),
                    Step(ObjectiveKind.DownTarget, "Put him down",
                        "The snare roots without hurting. Shooting works, and costs you half the fee."),
                    Step(ObjectiveKind.ResolveContract, "Close the contract"),
                }));

            ladder.Add(Create(byName, "Ilva Roon", "job.02.roon", "Quiet job: Ilva Roon",
                JobShape.Stakeout, FailureKind.HeatMaxed,
                "district heat as a fail state, scanning at range and from above",
                "She has people watching for exactly this. Identify her without the arc noticing.\n" +
                "Every scan and every question raises heat. Let it max and she is gone.",
                Marks("threl", "average", "clinic brace", "work drab"),
                new[]
                {
                    Step(ObjectiveKind.LearnClues, "Learn three of her marks", 3f,
                        "Scanning from a rooftop still works and nobody up there is watching you do it."),
                    Step(ObjectiveKind.IdentifyTarget, "Identify her"),
                    Step(ObjectiveKind.ResolveContract, "Close the contract"),
                }));

            ladder.Add(Create(byName, "Brann Oko", "job.03.oko", "Roof job: Brann Oko",
                JobShape.RooftopPursuit, FailureKind.TargetEscapes,
                "the roof network, grapple ladder, wall-run, the runner's drop links",
                "Somebody warned him. He is already on the roofs and already moving.\n" +
                "He can climb a fire escape and dive off an edge. He cannot do what you can do.",
                Marks("sivet", "heavy", "hauler arm", "sump-stained coat"),
                new[]
                {
                    Step(ObjectiveKind.ReachHeight, "Get onto the roof network", 18f,
                        "Fire escape, scaffold, or grapple a boom. The booms go all the way up."),
                    Step(ObjectiveKind.SnareTarget, "Snare him up there",
                        "He drops to the street when he is cornered. Going over the top is faster than following."),
                    Step(ObjectiveKind.ResolveContract, "Close the contract"),
                }));

            ladder.Add(Create(byName, "Hask", "job.04.hask", "Hard job: Hask",
                JobShape.SnatchUnderGuard, FailureKind.None,
                "weapons, Subduable's non-lethal floor, ground-slam, the resolution choice under fire",
                "He is not running anywhere. He is sitting in the middle of people who are paid to stop you.\n" +
                "The guards are not on the contract. He is.",
                Marks("korrath", "heavy", "spinal brace", "arc coat"),
                new[]
                {
                    Step(ObjectiveKind.DownHostiles, "Clear the guards", 3f,
                        "They go down rather than dying. So does he, that is the floor, not a difficulty setting."),
                    Step(ObjectiveKind.DownTarget, "Put Hask down"),
                    Step(ObjectiveKind.ResolveContract, "Choose, and mean it",
                        "Alive pays full. Killing pays half. That ordering never changes."),
                }));

            ladder.Add(Create(byName, "Eska Marrow", "job.05.marrow", "Long job: Eska Marrow",
                JobShape.StalkAndChase, FailureKind.WrongAccusation,
                "the whole loop at depth, one wrong accusation ends it",
                "Four marks, and the board gave you one. Get it wrong and she knows you are here.",
                Marks("threl", "tall", "burn-scar rig", "vendor apron"),
                new[]
                {
                    Step(ObjectiveKind.LearnClues, "Work out three more marks", 4f),
                    Step(ObjectiveKind.IdentifyTarget, "Accuse the right person",
                        "There is no second chance to be quiet about this one."),
                    Step(ObjectiveKind.ChaseBegins, "Run her down"),
                    Step(ObjectiveKind.DownTarget, "Put her down"),
                    Step(ObjectiveKind.ResolveContract, "Close the contract"),
                }));

            ladder.Add(Create(byName, "Vosk Kirra", "job.06.kirra", "Capstone: Vosk Kirra",
                JobShape.StalkAndChase, FailureKind.TargetEscapes,
                "every system in one contract",
                "Nine counts on the board. Considerably more than nine in fact.\n" +
                "He is right about one thing: nobody ever kills him. There is no money in it.",
                Marks("threl", "heavy", "sump rig", "arc coat"),
                new[]
                {
                    Step(ObjectiveKind.LearnClues, "Build the dossier", 3f),
                    Step(ObjectiveKind.IdentifyTarget, "Identify Kirra"),
                    Step(ObjectiveKind.ChaseBegins, "He runs. Everyone runs."),
                    Step(ObjectiveKind.ReachHeight, "Cut him off from above", 18f),
                    Step(ObjectiveKind.DownTarget, "Put him down"),
                    Step(ObjectiveKind.ResolveContract, "Close the contract"),
                }));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int built = 0;
            foreach (JobDefinition job in ladder)
            {
                if (job != null)
                    built++;
            }

            Debug.Log($"[Jobs] {built}/6 showcase jobs written to {JobRoot}. " +
                      "Any that are missing had no matching contract in the roster.");
        }

        static Dictionary<string, BountyContract> LoadContracts()
        {
            Dictionary<string, BountyContract> byName =
                new Dictionary<string, BountyContract>(System.StringComparer.OrdinalIgnoreCase);

            if (!AssetDatabase.IsValidFolder(BountyRoot))
                return byName;

            foreach (string guid in AssetDatabase.FindAssets("t:BountyContract", new[] { BountyRoot }))
            {
                BountyContract contract =
                    AssetDatabase.LoadAssetAtPath<BountyContract>(AssetDatabase.GUIDToAssetPath(guid));

                if (contract != null && !string.IsNullOrEmpty(contract.TargetName))
                    byName[contract.TargetName] = contract;
            }

            return byName;
        }

        static JobDefinition Create(Dictionary<string, BountyContract> byName, string targetName,
            string id, string title, JobShape shape, FailureKind failsOn, string showcases,
            string briefing, IdentityMark[] marks, JobObjective[] objectives)
        {
            if (!byName.TryGetValue(targetName, out BountyContract contract))
            {
                Debug.LogWarning($"[Jobs] No contract named '{targetName}' in {BountyRoot}: skipping {id}.");
                return null;
            }

            JobDefinition job = ScriptableObject.CreateInstance<JobDefinition>();
            AssetDatabase.CreateAsset(job, $"{JobRoot}/Job_{id.Replace('.', '_')}.asset");

            new AssetAuthoring(job)
                .Str("id", id).Str("title", title)
                .Enum("shape", (int)shape).Enum("failsOn", (int)failsOn)
                .Ref("contract", contract)
                .Str("briefing", briefing).Str("showcases", showcases)
                .Int("minimumMarksToAccuse", 2)
                .Enums("knownAtStart", (int)TraitSlot.Species)
                .Apply("targetMarks", property => WriteMarks(property, marks))
                .Apply("objectives", property => WriteObjectives(property, objectives))
                .Save();

            return job;
        }

        static IdentityMark[] Marks(string species, string build, string augment, string garment)
        {
            return new[]
            {
                new IdentityMark(TraitSlot.Species, species),
                new IdentityMark(TraitSlot.Build, build),
                new IdentityMark(TraitSlot.Augment, augment),
                new IdentityMark(TraitSlot.Garment, garment),
            };
        }

        static JobObjective Step(ObjectiveKind kind, string description, float amount = 0f, string hint = null)
        {
            return new JobObjective { Kind = kind, Description = description, Amount = amount, Hint = hint };
        }

        static JobObjective Step(ObjectiveKind kind, string description, string hint)
        {
            return Step(kind, description, 0f, hint);
        }

        static void WriteMarks(SerializedProperty property, IdentityMark[] marks)
        {
            property.arraySize = marks.Length;

            for (int i = 0; i < marks.Length; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Slot").enumValueIndex = (int)marks[i].Slot;
                element.FindPropertyRelative("Value").stringValue = marks[i].Value;
            }
        }

        static void WriteObjectives(SerializedProperty property, JobObjective[] objectives)
        {
            property.arraySize = objectives.Length;

            for (int i = 0; i < objectives.Length; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Kind").enumValueIndex = (int)objectives[i].Kind;
                element.FindPropertyRelative("Description").stringValue = objectives[i].Description;
                element.FindPropertyRelative("Amount").floatValue = objectives[i].Amount;
                element.FindPropertyRelative("Hint").stringValue = objectives[i].Hint ?? string.Empty;
            }
        }
    }
}
