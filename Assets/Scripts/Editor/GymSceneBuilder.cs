using System.Collections.Generic;
using FPSParkour.Combat;
using FPSParkour.Config;
using FPSParkour.Gym;
using FPSParkour.Player;
using FPSParkour.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.EditorTools
{
    public static class GymSceneBuilder
    {
        const string ContentRoot = "Assets/_Gym/Content";
        const string PrefabRoot = "Assets/_Gym/Prefabs";
        const string ScenePath = "Assets/_Gym/Scenes/MechanicsGym.unity";

        const float Spacing = 60f;

        [MenuItem("FPS Parkour/Build Mechanics Gym Scene")]
        public static void Build()
        {
            MovementConfig config = AssetDatabase.LoadAssetAtPath<MovementConfig>($"{ContentRoot}/MovementConfig_Gym.asset");
            if (config == null)
            {
                EditorUtility.DisplayDialog("Missing content", "Run 'FPS Parkour > Build Gym Content' first.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Build gym scene",
                    $"Creates and overwrites:\n\n{ScenePath}\n\nThe open scene will be replaced. Save it first.",
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
                    Debug.LogError($"Gym scene build failed: {e}");
                }
            };
        }

        static void Generate(MovementConfig config)
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject courseRoot = new GameObject("Course");
            List<GymStation> stations = new List<GymStation>();

            float x = 0f;
            stations.Add(BuildSprintSlide(courseRoot.transform, ref x));
            stations.Add(BuildMantle(courseRoot.transform, ref x));
            stations.Add(BuildWallRun(courseRoot.transform, ref x));
            stations.Add(BuildWallJump(courseRoot.transform, ref x));
            stations.Add(BuildGap(courseRoot.transform, ref x, "Double-jump", 7f,
                "Gap is too wide for one jump. F1 toggles double jump, try it off, then on."));
            stations.Add(BuildGap(courseRoot.transform, ref x, "Air dash", 13f,
                "Too wide even for a double jump. F2 toggles air dash."));
            stations.Add(BuildGrapple(courseRoot.transform, ref x));
            stations.Add(BuildGroundSlam(courseRoot.transform, ref x));
            stations.Add(BuildRange(courseRoot.transform, ref x));

            WeaponDefinition[] loadout =
            {
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{ContentRoot}/Weapon_Sidearm.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{ContentRoot}/Weapon_Rifle.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{ContentRoot}/Weapon_BoltThrower.asset")
            };

            PlayerRig rig = PlayerRigBuilder.Build(config, loadout, new Vector3(0f, 1.2f, 0f));

            GymController gym = rig.Root.AddComponent<GymController>();
            new AssetAuthoring(gym)
                .Ref("unlocks", rig.Unlocks).Ref("stats", rig.Stats)
                .Ref("player", rig.Root.transform).Ref("controller", rig.Controller)
                .Refs("stations", stations.ToArray())
                .Save();

            BuildReadout(rig, gym);

            GameObject light = GameObject.Find("Directional Light");
            if (light != null)
                light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"Mechanics gym built at {ScenePath}. Press Play. " +
                      "F1-F4 toggle abilities, F5 all on, F6 all off, R respawn, Alt+1..9 station.");
        }

        static GymStation Station(Transform parent, Vector3 position, string name, string testing)
        {
            GameObject marker = new GameObject($"Station_{name}");
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;

            GymStation station = marker.AddComponent<GymStation>();
            station.Configure(name, testing);
            return station;
        }

        static GymStation BuildSprintSlide(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "Runway", new Vector3(x + 20f, -0.5f, 0f), new Vector3(50f, 1f, 10f));
            PlayerRigBuilder.Box(parent, "SlideTunnelRoof", new Vector3(x + 34f, 1.35f, 0f), new Vector3(8f, 0.4f, 10f), Color.yellow);

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Sprint & Slide",
                "Sprint the runway, slide the low tunnel. Watch peak speed: a slide should keep it, not kill it.");
            x += Spacing;
            return station;
        }

        static GymStation BuildMantle(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "MantleFloor", new Vector3(x + 12f, -0.5f, 0f), new Vector3(34f, 1f, 10f));

            float[] heights = { 0.6f, 1.1f, 1.6f, 2.1f };
            for (int i = 0; i < heights.Length; i++)
            {
                PlayerRigBuilder.Box(parent, $"Ledge_{heights[i]:0.0}m",
                    new Vector3(x + 6f + i * 6f, heights[i] * 0.5f, 0f),
                    new Vector3(3f, heights[i], 8f), Color.cyan);
            }

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Mantle",
                "Ledges at 0.6 / 1.1 / 1.6 / 2.1 m. Find where mantle stops reaching and whether that feels right.");
            x += Spacing;
            return station;
        }

        static GymStation BuildWallRun(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "WallRunApproach", new Vector3(x + 4f, -0.5f, 0f), new Vector3(14f, 1f, 10f));
            PlayerRigBuilder.Box(parent, "WallRunWall", new Vector3(x + 24f, 4f, 4f), new Vector3(28f, 8f, 1f), Color.magenta);
            PlayerRigBuilder.Box(parent, "WallRunLanding", new Vector3(x + 42f, -0.5f, 0f), new Vector3(10f, 1f, 10f));

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Wall-run",
                "Approach with speed and run the magenta wall. Tests entry speed threshold and duration.");
            x += Spacing;
            return station;
        }

        static GymStation BuildWallJump(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "ShaftFloor", new Vector3(x + 6f, -0.5f, 0f), new Vector3(14f, 1f, 10f));
            PlayerRigBuilder.Box(parent, "ShaftWallA", new Vector3(x + 6f, 8f, 2.2f), new Vector3(10f, 16f, 1f), Color.magenta);
            PlayerRigBuilder.Box(parent, "ShaftWallB", new Vector3(x + 6f, 8f, -2.2f), new Vector3(10f, 16f, 1f), Color.magenta);
            PlayerRigBuilder.Box(parent, "ShaftTopLedge", new Vector3(x + 6f, 14f, 5f), new Vector3(10f, 1f, 5f), Color.cyan);

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Wall-jump",
                "Alternate walls up the shaft to the top ledge. Tests wall-jump arc and the wall-run cooldown.");
            x += Spacing;
            return station;
        }

        static GymStation BuildGap(Transform parent, ref float x, string name, float gap, string testing)
        {
            PlayerRigBuilder.Box(parent, $"{name}_Take", new Vector3(x + 5f, -0.5f, 0f), new Vector3(16f, 1f, 10f));
            PlayerRigBuilder.Box(parent, $"{name}_Land", new Vector3(x + 13f + gap, -0.5f, 0f), new Vector3(16f, 1f, 10f));
            PlayerRigBuilder.Box(parent, $"{name}_Marker", new Vector3(x + 13f + gap * 0.5f, -6f, 0f), new Vector3(gap, 0.2f, 10f), Color.red);

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), name, testing);
            x += Spacing;
            return station;
        }

        static GymStation BuildGrapple(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "GrappleTake", new Vector3(x + 5f, -0.5f, 0f), new Vector3(16f, 1f, 10f));
            PlayerRigBuilder.Box(parent, "GrappleAnchor", new Vector3(x + 22f, 12f, 0f), new Vector3(6f, 1f, 6f), Color.green);
            PlayerRigBuilder.Box(parent, "GrappleLand", new Vector3(x + 40f, -0.5f, 0f), new Vector3(16f, 1f, 10f));

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Grapple",
                "Chasm with a green anchor overhead. F3 toggles grapple. Tests range, reel speed and exit momentum.");
            x += Spacing;
            return station;
        }

        static GymStation BuildGroundSlam(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "SlamFloor", new Vector3(x + 10f, -0.5f, 0f), new Vector3(30f, 1f, 16f));
            PlayerRigBuilder.Box(parent, "SlamTower", new Vector3(x, 7f, 0f), new Vector3(4f, 14f, 4f));
            PlayerRigBuilder.Box(parent, "SlamPlatform", new Vector3(x + 4f, 14f, 0f), new Vector3(8f, 0.5f, 8f), Color.cyan);

            GameObject dummy = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Target_Dummy.prefab");
            if (dummy != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(dummy, parent);
                    spawned.transform.position = new Vector3(x + 8f + (i % 2) * 3f, 0f, -2f + (i / 2) * 4f);
                }
            }

            GymStation station = Station(parent, new Vector3(x + 4f, 15f, 0f), "Ground-slam",
                "Slam from the platform onto the dummies below. F4 toggles slam. Tests AoE radius and falloff.");
            x += Spacing;
            return station;
        }

        static GymStation BuildRange(Transform parent, ref float x)
        {
            PlayerRigBuilder.Box(parent, "RangeFloor", new Vector3(x + 30f, -0.5f, 0f), new Vector3(80f, 1f, 24f));

            GameObject dummy = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Target_Dummy.prefab");
            if (dummy != null)
            {
                float[] distances = { 10f, 25f, 45f, 65f, 110f };
                for (int i = 0; i < distances.Length; i++)
                {
                    for (int lane = -1; lane <= 1; lane++)
                    {
                        GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(dummy, parent);
                        spawned.transform.position = new Vector3(x + distances[i], lane == 0 ? 0f : 2.5f, lane * 6f);
                    }
                }
            }

            GymStation station = Station(parent, new Vector3(x, 1f, 0f), "Shooting range",
                "Targets at 10/25/45/65/110 m, some elevated. Heads take 2.5x. Rounds TRAVEL and DROP: "
                + "the 110 m plate is there to make flight time and holdover obvious. Compare the flat "
                + "rifle bolt against the arcing bolt thrower.");
            x += Spacing;
            return station;
        }

        static void BuildReadout(PlayerRig rig, GymController gym)
        {
            GameObject canvasObject = new GameObject("GymHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Text label = PlayerRigBuilder.Label(canvasObject.transform, "Readout", new Vector2(16f, -16f), new Vector2(620f, 220f));

            MovementReadout readout = canvasObject.AddComponent<MovementReadout>();
            new AssetAuthoring(readout)
                .Ref("controller", rig.Movement)
                .Ref("unlocks", rig.Unlocks)
                .Ref("gym", gym)
                .Ref("label", label)
                .Save();

            BuildCoaching(canvasObject, rig);
            HudBuilder.Build(canvasObject, rig);
        }

        static void BuildCoaching(GameObject canvas, PlayerRig rig)
        {
            PlayerInputReader input = rig.Root.GetComponent<PlayerInputReader>();

            AbilityAvailability availability = rig.Root.AddComponent<AbilityAvailability>();
            new AssetAuthoring(availability)
                .Ref("unlocks", rig.Unlocks).Ref("movement", rig.Movement)
                .Save();

            Text traversal = PlayerRigBuilder.Corner(canvas.transform, "AbilityBar_Traversal",
                new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(300f, 210f), 14);
            traversal.supportRichText = true;

            AbilityBarView bar = canvas.AddComponent<AbilityBarView>();
            new AssetAuthoring(bar)
                .Ref("availability", availability).Ref("input", input)
                .Ref("traversalLabel", traversal)
                .Save();

            Text prompt = PlayerRigBuilder.Corner(canvas.transform, "ContextPrompt",
                new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(720f, 28f), 18);
            prompt.alignment = TextAnchor.LowerCenter;

            ContextPromptView prompts = canvas.AddComponent<ContextPromptView>();
            new AssetAuthoring(prompts)
                .Ref("input", input).Ref("movement", rig.Movement).Ref("label", prompt)
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
        }
    }
}
