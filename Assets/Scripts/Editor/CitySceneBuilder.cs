using System.Collections.Generic;
using System.IO;
using FPSParkour.AI;
using FPSParkour.Bounty;
using FPSParkour.Combat;
using FPSParkour.Config;
using FPSParkour.Gym;
using FPSParkour.Investigation;
using FPSParkour.Jobs;
using FPSParkour.World;
using FPSParkour.Narrative;
using FPSParkour.Perks;
using FPSParkour.Presentation;
using FPSParkour.Save;
using FPSParkour.Player;
using FPSParkour.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace FPSParkour.EditorTools
{
    public static class CitySceneBuilder
    {
        const string GymContent = "Assets/_Gym/Content";
        const string GymPrefabs = "Assets/_Gym/Prefabs";
        const string ScenePath = "Assets/_City/Scenes/CitySequence.unity";

        [MenuItem("FPS Parkour/Build City District Scene")]
        public static void Build()
        {
            MovementConfig config = AssetDatabase.LoadAssetAtPath<MovementConfig>($"{GymContent}/MovementConfig_Gym.asset");
            if (config == null)
            {
                EditorUtility.DisplayDialog("Missing content",
                    "Run 'FPS Parkour > Build Gym Content' first, the district shares its config and weapons.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Build city district scene",
                    $"DELETES and regenerates:\n\n{ScenePath}\n{CityCast.ContentRoot}\n{CityCast.PrefabRoot}\n\n" +
                    "The open scene will be replaced. Commit first.\n\n" +
                    "A NavMesh is baked over the whole 520 m sector as part of the build, expect this to " +
                    "take a minute or two, and the editor to be unresponsive while it runs.",
                    "Build", "Cancel"))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Generate(config);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"City district build failed: {e}");
                }
            };
        }

        static void Generate(MovementConfig config)
        {
            Reset(CityCast.ContentRoot);
            Reset(CityCast.PrefabRoot);

            IdentityCatalog catalog = CityCast.BuildCatalog();
            BountyContract contract = CityCast.BuildContract();
            BountyContract[] roster = LoadRoster(contract);
            DialogueGraph[] graphs = CityCast.BuildInformantGraphs();
            GameObject[] pedestrianPrefabs = CityCast.BuildAmbientPrefabs(catalog);
            SnarePod podPrefab = CityCast.BuildSnarePodPrefab();

            GameObject enemyBolt = AssetDatabase.LoadAssetAtPath<GameObject>($"{GymPrefabs}/Projectile_EnemyBolt.prefab");
            GameObject hostilePrefab = CityCast.BuildHostilePrefab(enemyBolt);

            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject districtRoot = new GameObject("District");
            List<Transform> fleeNodes = new List<Transform>();
            CityGeometry.Build(districtRoot.transform, fleeNodes);

            Bake(districtRoot);

            CityGeometry.BuildLinks(districtRoot.transform);

            CityWiring wiring = BuildSystems(catalog, contract, roster, fleeNodes.ToArray());
            PlayerRig rig = BuildPlayer(config, podPrefab, wiring);

            GameObject cast = new GameObject("Cast");
            CityCast.PopulateLocals(wiring, graphs, cast.transform);
            CityCast.PopulateHostiles(hostilePrefab, cast.transform);

            BountyBoard board = BuildBoard(wiring, districtRoot.transform);
            FleeingBountyBrain target = SpawnTarget(wiring, cast.transform);
            new AssetAuthoring(wiring.Director).Ref("target", target).Save();

            AttachCrowd(wiring, pedestrianPrefabs, rig);

            ContractProgression progression = wiring.Registry.gameObject.AddComponent<ContractProgression>();
            new AssetAuthoring(progression)
                .Ref("registry", wiring.Registry).Ref("tree", rig.Perks)
                .Save();

            BuildHud(rig, wiring, board);

            GameObject light = GameObject.Find("Directional Light");
            if (light != null)
                light.transform.rotation = Quaternion.Euler(38f, 22f, 0f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"City district built at {ScenePath}. NavMesh baked over {CityLayout.SectorHalf * 2f} m. " +
                      $"{CityLayout.HubCount} hubs, {CityLayout.Grid * CityLayout.Grid - CityLayout.HubCount} blocks. " +
                      "Press Play: hold V to scan a stranger, E to ask or detain, G to throw a snare.");
        }

        static void Bake(GameObject root)
        {
            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.25f;
            surface.overrideTileSize = true;
            surface.tileSize = 256;
            surface.minRegionArea = 4f;
            surface.BuildNavMesh();
        }

        static CityWiring BuildSystems(IdentityCatalog catalog, BountyContract contract,
            BountyContract[] roster, Transform[] fleeNodes)
        {
            GameObject systems = new GameObject("Systems");

            systems.AddComponent<CityAlarm>();
            StoryFlags flags = systems.AddComponent<StoryFlags>();
            CreditsWallet wallet = systems.AddComponent<CreditsWallet>();
            ReputationSystem reputation = systems.AddComponent<ReputationSystem>();
            BountyRegistry registry = systems.AddComponent<BountyRegistry>();
            DialogueRunner runner = systems.AddComponent<DialogueRunner>();
            BarkPlayer barks = systems.AddComponent<BarkPlayer>();
            Dossier dossier = systems.AddComponent<Dossier>();
            DistrictHeat heat = systems.AddComponent<DistrictHeat>();
            HuntDirector director = systems.AddComponent<HuntDirector>();

            new AssetAuthoring(wallet).Int("credits", 900).Save();
            new AssetAuthoring(runner).Ref("flags", flags).Save();
            new AssetAuthoring(barks).Ref("flags", flags).Save();

            new AssetAuthoring(registry)
                .Refs("roster", roster).Ref("flags", flags)
                .Ref("wallet", wallet).Ref("reputation", reputation)
                .Save();

            new AssetAuthoring(dossier)
                .Ref("contract", contract).Ref("catalog", catalog)
                .Int("minimumMarksToAccuse", 2)
                .Enums("knownAtStart", (int)TraitSlot.Species)
                .Apply("targetMarks", property =>
                {
                    IdentityMark[] marks = CityCast.Target;
                    property.arraySize = marks.Length;

                    for (int i = 0; i < marks.Length; i++)
                    {
                        SerializedProperty element = property.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("Slot").enumValueIndex = (int)marks[i].Slot;
                        element.FindPropertyRelative("Value").stringValue = marks[i].Value;
                    }
                })
                .Save();

            new AssetAuthoring(director)
                .Ref("dossier", dossier).Ref("heat", heat).Ref("registry", registry)
                .Save();

            JobRunner jobs = systems.AddComponent<JobRunner>();
            new AssetAuthoring(jobs)
                .Ref("director", director).Ref("registry", registry).Ref("flags", flags)
                .Save();

            JobLadder ladder = systems.AddComponent<JobLadder>();
            new AssetAuthoring(ladder)
                .Ref("runner", jobs).Ref("flags", flags)
                .Refs("jobs", LoadLadder())
                .Save();

            SaveCoordinator saves = systems.AddComponent<SaveCoordinator>();
            SaveHotkeys hotkeys = systems.AddComponent<SaveHotkeys>();
            new AssetAuthoring(hotkeys).Ref("coordinator", saves).Save();

            CombatTuner tuner = systems.AddComponent<CombatTuner>();
            new AssetAuthoring(tuner)
                .Float("leadAccuracy", 0.8f).Float("projectileSpeed", 70f)
                .Float("shotsPerSecond", 1.8f).Float("spreadDegrees", 2.5f)
                .Save();

            return new CityWiring
            {
                Catalog = catalog,
                Contract = contract,
                Dossier = dossier,
                Heat = heat,
                Director = director,
                Runner = runner,
                Flags = flags,
                Wallet = wallet,
                Registry = registry,
                Barks = barks,
                FleeNodes = fleeNodes,
                Jobs = jobs,
            };
        }

        static PlayerRig BuildPlayer(MovementConfig config, SnarePod podPrefab, CityWiring wiring)
        {
            WeaponDefinition[] loadout =
            {
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{GymContent}/Weapon_Sidearm.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{GymContent}/Weapon_Rifle.asset")
            };

            PlayerRig rig = PlayerRigBuilder.Build(config, loadout,
                CityLayout.HubCentre(0) + new Vector3(0f, 1.2f, -70f));

            GunfireAlarmReporter reporter = rig.Root.AddComponent<GunfireAlarmReporter>();
            new AssetAuthoring(reporter).Ref("weapons", rig.Weapons).Float("alarmRadius", 55f).Save();

            PlayerInteractor interactor = rig.Root.AddComponent<PlayerInteractor>();
            new AssetAuthoring(interactor).Ref("aimOrigin", rig.Camera.transform).Save();

            TargetScanner scanner = rig.Root.AddComponent<TargetScanner>();
            new AssetAuthoring(scanner)
                .Ref("aimOrigin", rig.Camera.transform)
                .Ref("dossier", wiring.Dossier).Ref("heat", wiring.Heat)
                .Save();

            GadgetThrower gadget = rig.Root.AddComponent<GadgetThrower>();
            new AssetAuthoring(gadget)
                .Ref("aimOrigin", rig.Camera.transform).Ref("podPrefab", podPrefab)
                .Int("maxCharges", 3)
                .Save();

            new AssetAuthoring(rig.Perks)
                .Refs("startingPerks", PerkTreeBuilder.Starting(GymContent))
                .Int("skillPoints", 12)
                .Save();

            new AssetAuthoring(rig.Unlocks)
                .Enums("baseAbilities", AllAbilities())
                .Save();

            GymController gym = rig.Root.AddComponent<GymController>();
            new AssetAuthoring(gym)
                .Ref("unlocks", rig.Unlocks).Ref("stats", rig.Stats)
                .Ref("player", rig.Root.transform).Ref("controller", rig.Controller)
                .Enums("togglable",
                    (int)AbilityId.DoubleJump, (int)AbilityId.AirDash, (int)AbilityId.Grapple,
                    (int)AbilityId.GroundSlam, (int)AbilityId.Scanner, (int)AbilityId.SnareLauncher)
                .Save();

            new AssetAuthoring(wiring.Jobs)
                .Ref("scanner", scanner).Ref("gadget", gadget).Ref("player", rig.Root.transform)
                .Save();

            AbilityAvailability availability = rig.Root.AddComponent<AbilityAvailability>();
            new AssetAuthoring(availability)
                .Ref("unlocks", rig.Unlocks).Ref("movement", rig.Movement)
                .Ref("gadget", gadget).Ref("scanner", scanner)
                .Save();

            return rig;
        }

        static int[] AllAbilities()
        {
            System.Array values = System.Enum.GetValues(typeof(AbilityId));
            int[] indices = new int[values.Length];

            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;

            return indices;
        }

        static FleeingBountyBrain SpawnTarget(CityWiring wiring, Transform parent)
        {
            Vector3 wanted = CityLayout.HubCentre(CityLayout.HubCount - 1) + new Vector3(18f, 0f, 26f);
            Vector3 at = NavMesh.SamplePosition(wanted, out NavMeshHit hit, 18f, NavMesh.AllAreas) ? hit.position : wanted;

            FleeingBountyBrain brain = CityCast.BuildTarget(wiring, at);
            brain.transform.SetParent(parent, true);
            return brain;
        }

        static void AttachCrowd(CityWiring wiring, GameObject[] pedestrianPrefabs, PlayerRig rig)
        {
            GameObject crowd = new GameObject("Crowd");
            CrowdSpawner spawner = crowd.AddComponent<CrowdSpawner>();

            new AssetAuthoring(spawner)
                .Ref("tracked", rig.Root.transform)
                .Refs("pedestrianPrefabs", pedestrianPrefabs)
                .Int("maxPedestrians", 30)
                .Float("spawnRadius", 48f)
                .Float("despawnRadius", 74f)
                .Save();
        }

        static void BuildHud(PlayerRig rig, CityWiring wiring, BountyBoard board)
        {
            GameObject canvasObject = new GameObject("CityHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            HudBuilder.EnsureEventSystem();

            Text movementLabel = PlayerRigBuilder.Label(canvasObject.transform, "Movement", new Vector2(16f, -16f), new Vector2(560f, 260f));
            MovementReadout movement = canvasObject.AddComponent<MovementReadout>();
            new AssetAuthoring(movement)
                .Ref("controller", rig.Movement).Ref("unlocks", rig.Unlocks).Ref("label", movementLabel)
                .Save();

            Text huntLabel = PlayerRigBuilder.Label(canvasObject.transform, "Hunt", new Vector2(16f, -292f), new Vector2(560f, 300f));
            HuntReadout hunt = canvasObject.AddComponent<HuntReadout>();
            new AssetAuthoring(hunt)
                .Ref("director", wiring.Director)
                .Ref("scanner", rig.Root.GetComponent<TargetScanner>())
                .Ref("gadget", rig.Root.GetComponent<GadgetThrower>())
                .Ref("interactor", rig.Root.GetComponent<PlayerInteractor>())
                .Ref("perks", rig.Perks)
                .Ref("label", huntLabel)
                .Save();

            GameObject dialogue = BuildDialogueUi(canvasObject, rig, wiring);
            GameObject perks = BuildPerkScreen(canvasObject, rig);
            GameObject inventory = BuildPlayerHud(canvasObject, rig, wiring);
            GameObject resolution = BuildResolutionPrompt(canvasObject, rig, wiring);
            GameObject boardScreen = BuildBoardScreen(canvasObject, rig, board);

            BuildCoaching(canvasObject, rig, wiring);

            ScreenCursor cursor = canvasObject.AddComponent<ScreenCursor>();
            new AssetAuthoring(cursor)
                .Refs("screens", dialogue, perks, resolution, boardScreen, inventory)
                .Ref("look", rig.Root.GetComponent<FirstPersonLook>())
                .Save();
        }

        static void BuildCoaching(GameObject canvas, PlayerRig rig, CityWiring wiring)
        {
            PlayerInputReader input = rig.Root.GetComponent<PlayerInputReader>();
            AbilityAvailability availability = rig.Root.GetComponent<AbilityAvailability>();

            Text traversal = PlayerRigBuilder.Corner(canvas.transform, "AbilityBar_Traversal", new Vector2(1f, 1f),
                new Vector2(-16f, -16f), new Vector2(300f, 210f), 14);
            Text hunting = PlayerRigBuilder.Corner(canvas.transform, "AbilityBar_Hunting", new Vector2(1f, 1f),
                new Vector2(-16f, -238f), new Vector2(300f, 56f), 14);
            traversal.supportRichText = true;
            hunting.supportRichText = true;

            AbilityBarView bar = canvas.AddComponent<AbilityBarView>();
            new AssetAuthoring(bar)
                .Ref("availability", availability).Ref("input", input)
                .Ref("traversalLabel", traversal).Ref("huntingLabel", hunting)
                .Save();

            Text prompt = PlayerRigBuilder.Corner(canvas.transform, "ContextPrompt", new Vector2(0.5f, 0f),
                new Vector2(0f, 128f), new Vector2(720f, 28f), 18);
            prompt.alignment = TextAnchor.LowerCenter;

            ContextPromptView prompts = canvas.AddComponent<ContextPromptView>();
            new AssetAuthoring(prompts)
                .Ref("interactor", rig.Root.GetComponent<PlayerInteractor>())
                .Ref("input", input).Ref("movement", rig.Movement)
                .Ref("scanner", rig.Root.GetComponent<TargetScanner>())
                .Ref("label", prompt)
                .Save();

            GameObject tip = new GameObject("FirstUseTip", typeof(RectTransform), typeof(Image));
            tip.transform.SetParent(canvas.transform, false);
            RectTransform tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0f);
            tipRect.anchorMax = new Vector2(0.5f, 0f);
            tipRect.pivot = new Vector2(0.5f, 0f);
            tipRect.anchoredPosition = new Vector2(0f, 172f);
            tipRect.sizeDelta = new Vector2(620f, 96f);
            tip.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.82f);

            Text tipTitle = PlayerRigBuilder.Label(tip.transform, "Title", new Vector2(16f, -12f), new Vector2(580f, 24f), 18);
            Text tipBody = PlayerRigBuilder.Label(tip.transform, "Body", new Vector2(16f, -42f), new Vector2(588f, 48f), 14);

            FirstUseTipView tips = canvas.AddComponent<FirstUseTipView>();
            new AssetAuthoring(tips)
                .Ref("availability", availability).Ref("input", input)
                .Ref("panel", tip).Ref("titleLabel", tipTitle).Ref("bodyLabel", tipBody)
                .Save();

            Text jobTitle = PlayerRigBuilder.Corner(canvas.transform, "JobTitle", new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(820f, 26f), 19);
            jobTitle.alignment = TextAnchor.UpperCenter;

            Text jobSteps = PlayerRigBuilder.Corner(canvas.transform, "JobObjectives", new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(820f, 170f), 15);
            jobSteps.alignment = TextAnchor.UpperCenter;
            jobSteps.supportRichText = true;

            Text jobNotice = PlayerRigBuilder.Corner(canvas.transform, "JobNotice", new Vector2(0.5f, 0f),
                new Vector2(0f, 200f), new Vector2(760f, 56f), 17);
            jobNotice.alignment = TextAnchor.LowerCenter;

            JobTrackerView tracker = canvas.AddComponent<JobTrackerView>();
            new AssetAuthoring(tracker)
                .Ref("runner", wiring.Jobs)
                .Ref("titleLabel", jobTitle).Ref("objectivesLabel", jobSteps).Ref("noticeLabel", jobNotice)
                .Save();
        }


        static GameObject BuildDialogueUi(GameObject canvas, PlayerRig rig, CityWiring wiring)
        {
            GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(880f, 260f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            Text speaker = PlayerRigBuilder.Label(panel.transform, "Speaker", new Vector2(20f, -16f), new Vector2(400f, 26f), 18);
            Text line = PlayerRigBuilder.Label(panel.transform, "Line", new Vector2(20f, -48f), new Vector2(840f, 70f), 16);

            GameObject choices = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choices.transform.SetParent(panel.transform, false);

            RectTransform choicesRect = choices.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0f, 1f);
            choicesRect.anchorMax = new Vector2(0f, 1f);
            choicesRect.pivot = new Vector2(0f, 1f);
            choicesRect.anchoredPosition = new Vector2(20f, -124f);
            choicesRect.sizeDelta = new Vector2(840f, 120f);

            VerticalLayoutGroup layout = choices.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            Button buttonPrefab = BuildChoiceButton(canvas.transform, "ChoiceButton");
            Button continueButton = BuildButton(panel.transform, "Continue", "[ continue ]", new Vector2(20f, -212f));

            DialogueView view = canvas.AddComponent<DialogueView>();
            new AssetAuthoring(view)
                .Ref("runner", wiring.Runner).Ref("root", panel)
                .Ref("speakerLabel", speaker).Ref("lineLabel", line)
                .Ref("choiceContainer", choicesRect).Ref("choiceButtonPrefab", buttonPrefab)
                .Ref("continueButton", continueButton)
                .Save();

            panel.SetActive(false);
            return panel;
        }

        static Button BuildChoiceButton(Transform canvas, string name)
        {
            Button button = BuildButton(canvas, name, "choice", Vector2.zero);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(button.gameObject, $"{CityCast.PrefabRoot}/{name}.prefab");
            Object.DestroyImmediate(button.gameObject);
            return saved.GetComponent<Button>();
        }

        static GameObject BuildPerkScreen(GameObject canvas, PlayerRig rig)
        {
            GameObject panel = new GameObject("PerkPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 900f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

            Text points = PlayerRigBuilder.Label(panel.transform, "Points", new Vector2(20f, -14f), new Vector2(500f, 26f), 18);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(panel.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(20f, -48f);
            contentRect.sizeDelta = new Vector2(720f, 830f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            Button rowPrefab = BuildChoiceButton(canvas.transform, "PerkRow");

            PerkTreeView view = canvas.AddComponent<PerkTreeView>();
            new AssetAuthoring(view)
                .Ref("tree", rig.Perks).Ref("root", panel)
                .Ref("content", contentRect).Ref("rowPrefab", rowPrefab).Ref("pointsLabel", points)
                .Refs("displayed", PerkTreeBuilder.All(GymContent))
                .Save();

            PerkTreeInput screen = canvas.AddComponent<PerkTreeInput>();
            new AssetAuthoring(screen)
                .Ref("input", rig.Root.GetComponent<PlayerInputReader>())
                .Ref("view", view)
                .Save();

            new AssetAuthoring(rig.Perks).Refs("allPerks", PerkTreeBuilder.All(GymContent)).Save();

            panel.SetActive(false);
            return panel;
        }

        static Button BuildButton(Transform parent, string name, string caption, Vector2 anchoredPosition)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(840f, 30f);

            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            Text label = PlayerRigBuilder.Label(root.transform, "Label", new Vector2(10f, -6f), new Vector2(820f, 22f), 15);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = caption;

            return root.GetComponent<Button>();
        }

        static JobDefinition[] LoadLadder()
        {
            List<JobDefinition> ladder = new List<JobDefinition>();

            if (AssetDatabase.IsValidFolder("Assets/_Jobs"))
            {
                string[] guids = AssetDatabase.FindAssets("t:JobDefinition", new[] { "Assets/_Jobs" });
                System.Array.Sort(guids, (a, b) =>
                    string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

                foreach (string guid in guids)
                {
                    JobDefinition job = AssetDatabase.LoadAssetAtPath<JobDefinition>(AssetDatabase.GUIDToAssetPath(guid));

                    if (job != null)
                        ladder.Add(job);
                }
            }

            if (ladder.Count == 0)
                Debug.LogWarning("[City] no jobs in Assets/_Jobs, run 'FPS Parkour > Build Showcase Jobs'.");

            return ladder.ToArray();
        }

        static BountyContract[] LoadRoster(BountyContract fallback)
        {
            const string imported = "Assets/_Bounties";

            if (!AssetDatabase.IsValidFolder(imported))
                return new[] { fallback };

            string[] guids = AssetDatabase.FindAssets("t:BountyContract", new[] { imported });

            if (guids.Length == 0)
                return new[] { fallback };

            List<BountyContract> roster = new List<BountyContract> { fallback };

            foreach (string guid in guids)
                roster.Add(AssetDatabase.LoadAssetAtPath<BountyContract>(AssetDatabase.GUIDToAssetPath(guid)));

            Debug.Log($"[City] roster: {roster.Count} contracts ({guids.Length} imported).");
            return roster.ToArray();
        }

        static BountyBoard BuildBoard(CityWiring wiring, Transform parent)
        {
            GameObject board = PlayerRigBuilder.Box(parent, "ContractBoard",
                CityLayout.HubCentre(0) + new Vector3(0f, 2f, -52f),
                new Vector3(6f, 4f, 0.6f), new Color(0.24f, 0.3f, 0.36f));

            BountyBoard component = board.AddComponent<BountyBoard>();
            new AssetAuthoring(component)
                .Ref("registry", wiring.Registry).Str("boardName", "Contract board")
                .Save();

            return component;
        }

        static GameObject BuildPlayerHud(GameObject canvas, PlayerRig rig, CityWiring wiring)
        {
            GameObject inventory = HudBuilder.Build(canvas, rig, wiring.Wallet);

            Image flash = PlayerRigBuilder.Panel(canvas.transform, "DamageFlash", new Color(0.7f, 0.1f, 0.1f, 0f));
            PlayerRigBuilder.Stretch(flash.rectTransform);
            flash.raycastTarget = false;
            flash.transform.SetAsFirstSibling();

            Image marker = PlayerRigBuilder.Panel(canvas.transform, "HitMarker", new Color(1f, 1f, 1f, 0f));
            marker.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            marker.rectTransform.sizeDelta = new Vector2(14f, 14f);
            marker.raycastTarget = false;

            DamageFeedback feedback = canvas.AddComponent<DamageFeedback>();
            new AssetAuthoring(feedback)
                .Ref("shooter", rig.Root).Ref("stats", rig.Stats)
                .Ref("hitMarker", marker).Ref("damageFlash", flash)
                .Save();

            Image deathFade = PlayerRigBuilder.Panel(canvas.transform, "DeathFade", new Color(0f, 0f, 0f, 0f));
            PlayerRigBuilder.Stretch(deathFade.rectTransform);
            deathFade.raycastTarget = false;

            Text downLabel = PlayerRigBuilder.Label(canvas.transform, "Down", new Vector2(0f, 0f), new Vector2(400f, 40f), 28);
            downLabel.alignment = TextAnchor.MiddleCenter;

            PlayerDeath death = rig.Root.AddComponent<PlayerDeath>();
            new AssetAuthoring(death)
                .Ref("stats", rig.Stats).Ref("controller", rig.Controller)
                .Ref("fade", deathFade).Ref("label", downLabel)
                .Save();

            Text subtitles = PlayerRigBuilder.Label(canvas.transform, "Subtitles", new Vector2(300f, -640f), new Vector2(660f, 90f), 15);
            BarkSubtitleView barkView = canvas.AddComponent<BarkSubtitleView>();
            new AssetAuthoring(barkView)
                .Ref("player", wiring.Barks).Ref("label", subtitles)
                .Save();

            return inventory;
        }

        static GameObject BuildResolutionPrompt(GameObject canvas, PlayerRig rig, CityWiring wiring)
        {
            GameObject panel = Screen(canvas.transform, "ResolutionPanel", new Vector2(560f, 260f), new Vector2(0f, 120f));

            Text target = PlayerRigBuilder.Label(panel.transform, "Target", new Vector2(20f, -16f), new Vector2(520f, 26f), 20);
            Text charges = PlayerRigBuilder.Label(panel.transform, "Charges", new Vector2(20f, -48f), new Vector2(520f, 80f), 15);

            Button capture = BuildButton(panel.transform, "Capture", "Capture", new Vector2(20f, -140f));
            Button kill = BuildButton(panel.transform, "Kill", "Kill", new Vector2(20f, -178f));
            Button release = BuildButton(panel.transform, "Release", "Let them go", new Vector2(20f, -216f));

            ContractResolutionView view = canvas.AddComponent<ContractResolutionView>();
            new AssetAuthoring(view)
                .Ref("registry", wiring.Registry).Ref("root", panel)
                .Ref("targetLabel", target).Ref("chargesLabel", charges)
                .Ref("captureButton", capture).Ref("killButton", kill).Ref("releaseButton", release)
                .Save();

            panel.SetActive(false);
            return panel;
        }

        static GameObject BuildBoardScreen(GameObject canvas, PlayerRig rig, BountyBoard board)
        {
            GameObject panel = Screen(canvas.transform, "BoardPanel", new Vector2(820f, 620f), Vector2.zero);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(panel.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(16f, -16f);
            contentRect.sizeDelta = new Vector2(400f, 580f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            Text dossier = PlayerRigBuilder.Label(panel.transform, "Dossier", new Vector2(432f, -16f), new Vector2(372f, 580f), 14);

            BountyBoardView view = canvas.AddComponent<BountyBoardView>();
            new AssetAuthoring(view)
                .Ref("board", board).Ref("root", panel)
                .Ref("content", contentRect).Ref("rowPrefab", BuildChoiceButton(canvas.transform, "BoardRow"))
                .Ref("dossierLabel", dossier)
                .Save();

            panel.SetActive(false);
            return panel;
        }

        static GameObject Screen(Transform canvas, string name, Vector2 size, Vector2 offset)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            return panel;
        }

        static void Reset(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);

            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
    }
}
