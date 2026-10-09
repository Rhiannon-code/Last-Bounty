using FPSParkour.Combat;
using FPSParkour.Config;
using FPSParkour.Inventory;
using FPSParkour.Movement;
using FPSParkour.Perks;
using FPSParkour.Presentation;
using FPSParkour.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.EditorTools
{
    public class PlayerRig
    {
        public GameObject Root;
        public Transform CameraPivot;
        public Camera Camera;
        public PlayerStats Stats;
        public AbilityUnlocks Unlocks;
        public PerkTree Perks;
        public PlayerMovementController Movement;
        public WeaponController Weapons;
        public CharacterController Controller;
    }

    public static class PlayerRigBuilder
    {
        public static PlayerRig Build(MovementConfig config, WeaponDefinition[] loadout, Vector3 position)
        {
            GameObject root = new GameObject("Player") { tag = "Player" };
            root.transform.position = position;

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.4f;

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            cameraObject.transform.SetParent(pivot.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            Camera camera = cameraObject.GetComponent<Camera>();

            PlayerMotor motor = root.AddComponent<PlayerMotor>();
            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            PlayerStats stats = root.AddComponent<PlayerStats>();
            AbilityUnlocks unlocks = root.AddComponent<AbilityUnlocks>();
            PerkTree perks = root.AddComponent<PerkTree>();
            FirstPersonLook look = root.AddComponent<FirstPersonLook>();
            PlayerMovementController movement = root.AddComponent<PlayerMovementController>();
            InventorySystem inventory = root.AddComponent<InventorySystem>();
            InventoryInputBridge inventoryInput = root.AddComponent<InventoryInputBridge>();
            WeaponController weapons = root.AddComponent<WeaponController>();
            GroundSlamImpact slam = root.AddComponent<GroundSlamImpact>();

            int worldMask = ~LayerMask.GetMask("Ignore Raycast");

            new AssetAuthoring(motor).Mask("groundMask", worldMask).Save();

            new AssetAuthoring(look)
                .Ref("playerBody", root.transform)
                .Ref("cameraPivot", pivot.transform)
                .Ref("cam", camera)
                .Ref("input", input)
                .Save();

            new AssetAuthoring(movement)
                .Ref("motor", motor).Ref("input", input).Ref("config", config)
                .Ref("look", look).Ref("stats", stats).Ref("unlocks", unlocks)
                .Save();

            new AssetAuthoring(weapons)
                .Ref("input", input).Ref("look", look).Ref("stats", stats)
                .Ref("aimOrigin", camera != null ? camera.transform : pivot.transform)
                .Ref("weaponSocket", pivot.transform)
                .Refs("startingLoadout", loadout)
                .Save();

            new AssetAuthoring(slam).Ref("controller", movement).Mask("hitMask", worldMask).Save();
            new AssetAuthoring(inventoryInput).Ref("input", input).Ref("inventory", inventory).Save();

            SeedInventory(root, inventory);

            BuildViewmodel(root, camera, movement, weapons);

            return new PlayerRig
            {
                Root = root,
                CameraPivot = pivot.transform,
                Camera = camera,
                Stats = stats,
                Unlocks = unlocks,
                Perks = perks,
                Movement = movement,
                Weapons = weapons,
                Controller = controller
            };
        }

        static void SeedInventory(GameObject root, InventorySystem inventory)
        {
            (string asset, int count)[] bag =
            {
                ("Item_KineticAmmo", 48),
                ("Item_CellAmmo", 90),
                ("Item_SnarePod", 3),
                ("Item_Medkit", 2),
                ("Item_Scrap", 12),
            };

            StartingInventory starting = root.AddComponent<StartingInventory>();

            new AssetAuthoring(starting)
                .Ref("inventory", inventory)
                .Apply("contents", property =>
                {
                    property.arraySize = bag.Length;

                    for (int i = 0; i < bag.Length; i++)
                    {
                        SerializedProperty entry = property.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("item").objectReferenceValue =
                            AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                                $"{GymContentBuilder.ContentRoot}/{bag[i].asset}.asset");
                        entry.FindPropertyRelative("count").intValue = bag[i].count;
                    }
                })
                .Save();
        }

        static void BuildViewmodel(GameObject root, Camera camera, PlayerMovementController movement,
            WeaponController weapons)
        {
            if (camera == null || !CityArt.Available)
                return;

            GameObject holder = new GameObject("Viewmodel");
            holder.transform.SetParent(camera.transform, false);
            holder.transform.localPosition = new Vector3(0f, -0.06f, 0.12f);

            Animator animator = CityArt.Attach(holder, CityArt.ViewmodelArms(), CityArt.Viewmodel(), Vector3.zero);

            if (animator == null)
            {
                Object.DestroyImmediate(holder);
                return;
            }

            GameObject weapon = CityArt.ViewmodelWeapon();

            if (weapon != null)
            {
                Transform hand = FindBone(animator.transform, "hand_r");

                if (hand != null)
                    PrefabUtility.InstantiatePrefab(weapon, hand);
            }

            ViewmodelRig rig = holder.AddComponent<ViewmodelRig>();
            new AssetAuthoring(rig)
                .Ref("animator", animator).Ref("movement", movement).Ref("weapons", weapons)
                .Ref("swayPivot", holder.transform)
                .Save();
        }

        static Transform FindBone(Transform root, string name)
        {
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return bone;
            }

            return null;
        }

        public static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, Color? colour = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = centre;
            box.transform.localScale = size;

            if (colour.HasValue)
            {
                Renderer renderer = box.GetComponent<Renderer>();
                Material material = new Material(renderer.sharedMaterial);
                material.color = colour.Value;
                renderer.sharedMaterial = material;
            }

            return box;
        }

        public static Image Panel(Transform parent, string name, Color colour)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = colour;
            return panel.GetComponent<Image>();
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform canvas, string name, Vector2 anchoredPosition, Vector2 size, int fontSize = 14)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(canvas, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = root.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        static Font uiFont;
        static Font UiFont()
        {
            if (uiFont != null)
                return uiFont;

            try
            {
                uiFont = AssetDatabase.GetBuiltinExtraResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"Built in UI font unavailable, falling back to an OS font: {e.Message}");
            }

            if (uiFont == null)
                uiFont = Font.CreateDynamicFontFromOSFont("DejaVu Sans", 14);

            return uiFont;
        }

        public static Text Corner(Transform canvas, string name, Vector2 anchor, Vector2 position,
            Vector2 size, int fontSize = 14)
        {
            Text text = Label(canvas, name, position, size, fontSize);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            return text;
        }
    }
}
