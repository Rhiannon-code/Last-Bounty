using System.Collections.Generic;
using FPSParkour.AI;
using FPSParkour.Bounty;
using FPSParkour.Combat;
using FPSParkour.Investigation;
using FPSParkour.Jobs;
using FPSParkour.Narrative;
using FPSParkour.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace FPSParkour.EditorTools
{
    public class CityWiring
    {
        public IdentityCatalog Catalog;
        public BountyContract Contract;
        public Dossier Dossier;
        public DistrictHeat Heat;
        public HuntDirector Director;
        public DialogueRunner Runner;
        public StoryFlags Flags;
        public CreditsWallet Wallet;
        public BountyRegistry Registry;
        public BarkPlayer Barks;
        public Transform[] FleeNodes;
        public JobRunner Jobs;
    }

    public static class CityCast
    {
        public const string ContentRoot = "Assets/_City/Content";
        public const string PrefabRoot = "Assets/_City/Prefabs";

        static readonly IdentityMark[] TargetMarks =
        {
            new IdentityMark(TraitSlot.Species, "threl"),
            new IdentityMark(TraitSlot.Build, "tall"),
            new IdentityMark(TraitSlot.Augment, "burn-scar rig"),
            new IdentityMark(TraitSlot.Garment, "arc coat"),
        };

        public static IdentityCatalog BuildCatalog()
        {
            IdentityCatalog catalog = ScriptableObject.CreateInstance<IdentityCatalog>();
            AssetDatabase.CreateAsset(catalog, $"{ContentRoot}/IdentityCatalog_District.asset");

            new AssetAuthoring(catalog)
                .Apply("species", property =>
                {
                    string[] values = { "threl", "sivet", "korrath", "duat-born" };
                    property.arraySize = values.Length;

                    for (int i = 0; i < values.Length; i++)
                        property.GetArrayElementAtIndex(i).stringValue = values[i];
                })
                .Save();

            return catalog;
        }

        public static BountyContract BuildContract()
        {
            BountyContract contract = ScriptableObject.CreateInstance<BountyContract>();

            new AssetAuthoring(contract)
                .Str("id", "bounty.testbed.runner")
                .Str("targetName", "Vesk Ollo")
                .Str("alias", "the Courier")
                .Str("species", "threl")
                .Str("issuingFactionId", "guild")
                .Str("charges", "Moving contraband beacons between arcs. Four counts filed, none contested.")
                .Str("whoTheyActuallyAre", "A courier who kept moving them after being told what was in them.")
                .Int("payoutAlive", 1400)
                .Save();

            AssetDatabase.CreateAsset(contract, $"{ContentRoot}/Bounty_TestbedRunner.asset");
            return contract;
        }

        public static DialogueGraph[] BuildInformantGraphs()
        {
            return new[]
            {
                Graph("Dialogue_Fence", "dlg.informant.fence", "Odder",
                    "You want the tall one. Everyone wants the tall one this week.",
                    "Two hundred and I'll tell you what he's wearing.",
                    "clue.fence", "He wears the coat. Arc coat, guild cut, too good for him."),

                Graph("Dialogue_Runner", "dlg.informant.runner", "Sil",
                    "I run parcels. I don't run mouths.",
                    "You'll run one today.",
                    "clue.runner", "Right arm. It's not an arm. Burn-scar rig, whole thing rebuilt."),

                Graph("Dialogue_Vendor", "dlg.informant.vendor", "Mother Tace",
                    "He buys here. Tuesdays, mostly, and he never haggles.",
                    "What does he look like buying?",
                    "clue.vendor", "Head and a half over me and half as heavy. You'll know."),
            };
        }

        static DialogueGraph Graph(string file, string id, string speaker, string opening, string push,
            string clueFlag, string payoff)
        {
            DialogueGraph graph = ScriptableObject.CreateInstance<DialogueGraph>();

            DialogueNode open = new DialogueNode
            {
                Id = "open",
                Speaker = speaker,
                Line = opening,
                Choices = new[]
                {
                    new DialogueChoice { Text = push, NextNodeId = "tell", SetFlags = new[] { clueFlag } },
                    new DialogueChoice
                    {
                        Text = "Say it or I make it a problem.",
                        NextNodeId = "tell",
                        SetFlags = new[] { clueFlag, $"{clueFlag}.leaned" }
                    },
                    new DialogueChoice { Text = "Forget it.", NextNodeId = "off" },
                }
            };

            DialogueNode tell = new DialogueNode { Id = "tell", Speaker = speaker, Line = payoff };
            DialogueNode off = new DialogueNode { Id = "off", Speaker = speaker, Line = "Thought not." };

            new AssetAuthoring(graph)
                .Str("id", id)
                .Str("entryNodeId", "open")
                .Apply("nodes", property =>
                {
                    property.arraySize = 3;
                    WriteNode(property.GetArrayElementAtIndex(0), open);
                    WriteNode(property.GetArrayElementAtIndex(1), tell);
                    WriteNode(property.GetArrayElementAtIndex(2), off);
                })
                .Save();

            AssetDatabase.CreateAsset(graph, $"{ContentRoot}/{file}.asset");
            return graph;
        }

        static void WriteNode(SerializedProperty property, DialogueNode node)
        {
            property.FindPropertyRelative("Id").stringValue = node.Id;
            property.FindPropertyRelative("Speaker").stringValue = node.Speaker;
            property.FindPropertyRelative("Line").stringValue = node.Line;
            property.FindPropertyRelative("NextNodeId").stringValue = node.NextNodeId ?? string.Empty;

            SerializedProperty choices = property.FindPropertyRelative("Choices");
            choices.arraySize = node.Choices != null ? node.Choices.Length : 0;

            for (int i = 0; i < choices.arraySize; i++)
            {
                DialogueChoice choice = node.Choices[i];
                SerializedProperty element = choices.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Text").stringValue = choice.Text;
                element.FindPropertyRelative("NextNodeId").stringValue = choice.NextNodeId ?? string.Empty;
                WriteStrings(element.FindPropertyRelative("SetFlags"), choice.SetFlags);
            }
        }

        static void WriteStrings(SerializedProperty property, string[] values)
        {
            property.arraySize = values != null ? values.Length : 0;

            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        public static GameObject[] BuildAmbientPrefabs(IdentityCatalog catalog)
        {
            if (!CityArt.Available)
                return new[] { BuildAmbientPrefab(catalog, "Pedestrian", null) };

            string[] species = catalog.Values(TraitSlot.Species);
            GameObject[] prefabs = new GameObject[CityArt.CivilianMeshes.Length];

            for (int i = 0; i < prefabs.Length; i++)
            {
                string name = i < species.Length ? species[i] : null;
                prefabs[i] = BuildAmbientPrefab(catalog, $"Pedestrian_{i}", CityArt.CivilianMeshes[i], name);
            }

            return prefabs;
        }

        static GameObject BuildAmbientPrefab(IdentityCatalog catalog, string name, string meshName,
            string species = null)
        {
            GameObject root = new GameObject(name);
            Renderer renderer = Body(root, new Color(0.6f, 0.65f, 0.7f), keepCollider: true);

            Agent(root, 2.2f, 240f, 10f);
            root.AddComponent<CrowdPedestrian>();

            Animator animator = Dress(root, meshName, renderer);

            CrowdIdentity identity = root.AddComponent<CrowdIdentity>();
            new AssetAuthoring(identity)
                .Ref("catalog", catalog).Bool("rollFromCatalog", true)
                .Bool("rollSpecies", species == null)
                .Str("fixedSpecies", species ?? string.Empty)
                .Ref("silhouette", animator != null ? animator.transform : null)
                .Ref("body", animator == null ? renderer : null)
                .Save();

            return Save(root, name);
        }

        static Animator Dress(GameObject root, string meshName, Renderer capsule)
        {
            if (meshName == null || !CityArt.Available)
                return null;

            Animator animator = CityArt.Attach(root, CityArt.CharacterMesh(meshName),
                CityArt.Character(), Vector3.zero);

            if (animator == null)
                return null;

            CityArt.HideCapsule(capsule);

            CharacterAnimator driver = root.AddComponent<CharacterAnimator>();
            new AssetAuthoring(driver).Ref("animator", animator).Save();

            return animator;
        }

        public static SnarePod BuildSnarePodPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "SnarePod";
            root.transform.localScale = Vector3.one * 0.3f;

            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = new Color(0.3f, 0.9f, 0.6f) };

            root.AddComponent<Rigidbody>();
            SnarePod pod = root.AddComponent<SnarePod>();

            GameObject saved = Save(root, "SnarePod");
            return saved.GetComponent<SnarePod>();
        }

        public static GameObject BuildHostilePrefab(GameObject enemyBolt)
        {
            GameObject root = new GameObject("Hostile_Grunt");
            Renderer capsule = Body(root, new Color(0.7f, 0.25f, 0.2f), keepCollider: true);
            Dress(root, CityArt.GuardMesh, capsule);

            GameObject eyes = new GameObject("Eyes");
            eyes.transform.SetParent(root.transform, false);
            eyes.transform.localPosition = new Vector3(0f, 1.7f, 0.3f);

            NavMeshAgent agent = Agent(root, 4.2f, 320f, 14f);
            agent.stoppingDistance = 1f;

            Health health = root.AddComponent<Health>();
            EnemySenses senses = root.AddComponent<EnemySenses>();
            EnemyWeapon weapon = root.AddComponent<EnemyWeapon>();
            EnemyBrain brain = root.AddComponent<EnemyBrain>();

            new AssetAuthoring(senses).Ref("eyes", eyes.transform).Float("sightRange", 45f).Save();

            new AssetAuthoring(weapon)
                .Ref("muzzle", eyes.transform).Ref("projectilePrefab", enemyBolt)
                .Float("damage", 7f).Float("shotsPerSecond", 1.8f).Float("projectileSpeed", 70f)
                .Float("gravityScale", 0.5f).Float("leadAccuracy", 0.8f)
                .Save();

            new AssetAuthoring(brain)
                .Ref("agent", agent).Ref("senses", senses).Ref("weapon", weapon).Ref("health", health)
                .Float("preferredRange", 14f).Float("tooCloseRange", 6f)
                .Save();

            return Save(root, "Hostile_Grunt");
        }

        public static FleeingBountyBrain BuildTarget(CityWiring wiring, Vector3 at)
        {
            GameObject root = new GameObject("BountyTarget_VeskOllo");
            root.transform.position = at;

            Renderer renderer = Body(root, new Color(0.6f, 0.65f, 0.7f), keepCollider: true);
            Animator dressed = Dress(root, SpeciesMesh(wiring.Catalog, "threl"), renderer);

            GameObject eyes = new GameObject("Eyes");
            eyes.transform.SetParent(root.transform, false);
            eyes.transform.localPosition = new Vector3(0f, 1.7f, 0.3f);

            Agent(root, 7.4f, 400f, 18f);

            Health health = root.AddComponent<Health>();
            EnemySenses senses = root.AddComponent<EnemySenses>();
            Subduable subduable = root.AddComponent<Subduable>();
            CrowdPedestrian blending = root.AddComponent<CrowdPedestrian>();
            FleeingBountyBrain brain = root.AddComponent<FleeingBountyBrain>();
            BountyTarget bounty = root.AddComponent<BountyTarget>();

            CrowdIdentity identity = root.AddComponent<CrowdIdentity>();
            new AssetAuthoring(identity)
                .Bool("rollFromCatalog", false)
                .Bool("isBountyTarget", true)
                .Str("displayName", "tall threl")
                .Ref("silhouette", dressed != null ? dressed.transform : null)
                .Ref("body", dressed == null ? renderer : null)
                .Apply("marks", property => WriteMarks(property, TargetMarks))
                .Save();
            new AssetAuthoring(senses)
                .Ref("eyes", eyes.transform).Float("sightRange", 70f).Float("loseTargetAfter", 5f)
                .Save();
            new AssetAuthoring(subduable).Ref("health", health).Save();
            new AssetAuthoring(blending).Float("wanderRadius", 30f).Save();
            new AssetAuthoring(brain)
                .Ref("health", health).Ref("subduable", subduable).Ref("senses", senses)
                .Refs("fleeNodes", wiring.FleeNodes)
                .Refs("disableWhenFleeing", blending)
                .Float("escapeDistance", CityLayout.SectorHalf * 2f * 0.21f)
                .Save();

            new AssetAuthoring(bounty)
                .Ref("contract", wiring.Contract).Ref("registry", wiring.Registry)
                .Ref("subduable", subduable).Ref("health", health)
                .Save();

            return brain;
        }

        public static void PopulateLocals(CityWiring wiring, DialogueGraph[] graphs, Transform parent)
        {
            int[] prices = { 200, 0, 120 };
            const int PerHub = 9;

            for (int hub = 0; hub < CityLayout.HubCount; hub++)
            {
                Vector3 hubCentre = CityLayout.HubCentre(hub);

                for (int slot = 0; slot < PerHub; slot++)
                    BuildLocal(wiring, graphs, parent, prices, hubCentre, hub, slot, PerHub);
            }
        }

        static void BuildLocal(CityWiring wiring, DialogueGraph[] graphs, Transform parent,
            int[] prices, Vector3 hubCentre, int hub, int slot, int perHub)
        {
            {
                int i = hub * perHub + slot;
                float angle = slot * Mathf.PI * 2f / perHub;
                Vector3 at = hubCentre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(20f, 46f);

                if (!NavMesh.SamplePosition(at, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                    return;

                bool isInformant = slot == 0 && hub < graphs.Length;
                GameObject local = new GameObject(isInformant ? $"Informant_{hub}" : $"Local_{i}");
                local.transform.SetParent(parent, false);
                local.transform.position = hit.position;

                Renderer renderer = Body(local, new Color(0.6f, 0.65f, 0.7f), keepCollider: true);
                Agent(local, 1.9f, 240f, 10f);
                local.AddComponent<CrowdPedestrian>();

                CrowdIdentity identity = local.AddComponent<CrowdIdentity>();
                new AssetAuthoring(identity)
                    .Ref("catalog", wiring.Catalog).Bool("rollFromCatalog", true).Ref("body", renderer)
                    .Str("displayName", isInformant ? "informant" : "local")
                    .Save();

                Bystander bystander = local.AddComponent<Bystander>();
                AssetAuthoring authoring = new AssetAuthoring(bystander)
                    .Ref("identity", identity).Ref("dossier", wiring.Dossier)
                    .Ref("heat", wiring.Heat).Ref("director", wiring.Director)
                    .Ref("flags", wiring.Flags).Ref("wallet", wiring.Wallet);

                if (isInformant)
                {
                    string clueFlag = ClueFlag(hub);
                    authoring
                        .Ref("graph", graphs[hub]).Ref("runner", wiring.Runner)
                        .Int("price", prices[hub])
                        .Apply("grantCondition.RequireAll", property => WriteFlag(property, clueFlag))
                        .Apply("leanedOnCondition.RequireAll", property => WriteFlag(property, $"{clueFlag}.leaned"));
                }

                authoring.Save();
            }
        }

        static string ClueFlag(int index)
        {
            switch (index)
            {
                case 0: return "clue.fence";
                case 1: return "clue.runner";
                default: return "clue.vendor";
            }
        }

        public static void PopulateHostiles(GameObject prefab, Transform parent)
        {
            List<Vector3> posts = new List<Vector3>();

            for (int i = 0; i < CityLayout.StreetCentres.Length; i++)
            {
                for (int j = 0; j < CityLayout.StreetCentres.Length; j++)
                {
                    if ((i + j) % 2 != 0)
                        continue;

                    posts.Add(new Vector3(CityLayout.StreetCentres[i], 0f, CityLayout.StreetCentres[j]));
                }
            }

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                    posts.Add(new Vector3(sx * CityLayout.RingCentre, 0f, sz * CityLayout.RingCentre));
            }

            foreach (Vector3 post in posts)
            {
                if (!NavMesh.SamplePosition(post, out NavMeshHit hit, 14f, NavMesh.AllAreas))
                    continue;

                GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                spawned.transform.position = hit.position;
            }
        }

        static string SpeciesMesh(IdentityCatalog catalog, string species)
        {
            string[] values = catalog.Values(TraitSlot.Species);

            for (int i = 0; i < values.Length && i < CityArt.CivilianMeshes.Length; i++)
            {
                if (values[i] == species)
                    return CityArt.CivilianMeshes[i];
            }

            return CityArt.CivilianMeshes[0];
        }

        static Renderer Body(GameObject root, Color colour, bool keepCollider)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;

            if (!keepCollider)
                Object.DestroyImmediate(body.GetComponent<Collider>());

            Renderer renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = colour };
            return renderer;
        }

        static NavMeshAgent Agent(GameObject root, float speed, float angularSpeed, float acceleration)
        {
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = speed;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = acceleration;
            agent.radius = 0.38f;
            agent.height = 2f;
            return agent;
        }

        static void WriteMarks(SerializedProperty property, IReadOnlyList<IdentityMark> marks)
        {
            property.arraySize = marks.Count;

            for (int i = 0; i < marks.Count; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Slot").enumValueIndex = (int)marks[i].Slot;
                element.FindPropertyRelative("Value").stringValue = marks[i].Value;
            }
        }

        static void WriteFlag(SerializedProperty property, string flag)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).stringValue = flag;
        }

        public static IdentityMark[] Target => TargetMarks;

        static GameObject Save(GameObject root, string name)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{name}.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }
    }
}
