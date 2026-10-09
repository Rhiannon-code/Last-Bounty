using System.IO;
using FPSParkour.Combat;
using FPSParkour.Config;
using FPSParkour.Core;
using FPSParkour.Gym;
using FPSParkour.Inventory;
using FPSParkour.Movement;
using FPSParkour.Perks;
using FPSParkour.Player;
using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class GymContentBuilder
    {
        public const string ContentRoot = "Assets/_Gym/Content";
        const string PrefabRoot = "Assets/_Gym/Prefabs";

        [MenuItem("FPS Parkour/Build Gym Content")]
        public static void Build()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Build gym content",
                $"This DELETES and regenerates:\n\n{ContentRoot}\n{PrefabRoot}\n\n" +
                "Anything hand-edited in those two folders is lost. Nothing outside them is touched.\n\n" +
                "Commit first if you have unsaved work.",
                "Build", "Cancel");

            if (!proceed)
                return;

            // Rule 14: never do the work inside the dialog/layout callback.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Generate();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Gym content build failed: {e}");
                }
            };
        }

        static void Generate()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                Reset(ContentRoot);
                Reset(PrefabRoot);

                MovementConfig config = Create<MovementConfig>("MovementConfig_Gym");
                PerkTreeBuilder.Build(ContentRoot);

                GameObject bolt = BuildProjectilePrefab("Projectile_Bolt", new Color(0.4f, 0.9f, 1f));
                BuildProjectilePrefab("Projectile_EnemyBolt", new Color(1f, 0.45f, 0.25f));
                BuildWeapons(bolt);
                BuildItems();
                BuildTargetPrefab();

                EditorUtility.SetDirty(config);
                SetExecutionOrder();

                Debug.Log($"Gym content built under {ContentRoot} and {PrefabRoot}. " +
                          "Now run 'FPS Parkour > Build Mechanics Gym Scene'.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        static void SetExecutionOrder()
        {
            SetOrder<PlayerInputReader>(-100);
            SetOrder<PlayerMovementController>(-50);
        }

        static void SetOrder<T>(int order) where T : MonoBehaviour
        {
            MonoScript script = MonoScript.FromMonoBehaviour(
                new GameObject("~tmp", typeof(T)).GetComponent<T>());

            Object.DestroyImmediate(script.GetClass() != null ? GameObject.Find("~tmp") : null);

            if (script != null && MonoImporter.GetExecutionOrder(script) != order)
                MonoImporter.SetExecutionOrder(script, order);
        }

        static void BuildWeapons(GameObject boltPrefab)
        {
            WeaponDefinition sidearm = Create<WeaponDefinition>("Weapon_Sidearm");
            sidearm.displayName = "Sidearm";
            sidearm.fireMode = FireMode.SemiAuto;
            sidearm.weaponClass = WeaponClass.Hitscan;
            sidearm.fireRate = 5f;
            sidearm.damage = 26f;
            sidearm.damageType = DamageType.Kinetic;
            sidearm.magSize = 12;
            sidearm.maxReserve = 96;
            sidearm.reloadTime = 1.4f;
            sidearm.hipSpread = 1.6f;
            sidearm.aimSpread = 0.15f;
            sidearm.weaponClass = WeaponClass.Projectile;
            sidearm.projectilePrefab = boltPrefab;
            sidearm.projectileSpeed = 110f;
            sidearm.gravityScale = 0.5f;
            sidearm.drag = 0.01f;
            sidearm.inheritShooterVelocity = 0.3f;
            sidearm.range = 160f;
            EditorUtility.SetDirty(sidearm);

            WeaponDefinition rifle = Create<WeaponDefinition>("Weapon_Rifle");
            rifle.displayName = "Pulse Rifle";
            rifle.fireMode = FireMode.FullAuto;
            rifle.weaponClass = WeaponClass.Hitscan;
            rifle.fireRate = 10f;
            rifle.damage = 12f;
            rifle.damageType = DamageType.Energy;
            rifle.magSize = 30;
            rifle.maxReserve = 240;
            rifle.reloadTime = 2f;
            rifle.hipSpread = 3.2f;
            rifle.aimSpread = 0.4f;
            rifle.weaponClass = WeaponClass.Projectile;
            rifle.projectilePrefab = boltPrefab;
            rifle.projectileSpeed = 150f;
            rifle.gravityScale = 0.15f;
            rifle.drag = 0.005f;
            rifle.inheritShooterVelocity = 0.3f;
            rifle.range = 220f;
            EditorUtility.SetDirty(rifle);

            WeaponDefinition thrower = Create<WeaponDefinition>("Weapon_BoltThrower");
            thrower.displayName = "Bolt Thrower";
            thrower.fireMode = FireMode.SemiAuto;
            thrower.weaponClass = WeaponClass.Projectile;
            thrower.fireRate = 2f;
            thrower.damage = 55f;
            thrower.damageType = DamageType.Energy;
            thrower.magSize = 6;
            thrower.maxReserve = 48;
            thrower.reloadTime = 2.4f;
            thrower.projectilePrefab = boltPrefab;
            thrower.projectileSpeed = 45f;
            thrower.gravityScale = 1f;
            thrower.drag = 0.03f;
            thrower.inheritShooterVelocity = 0.4f;
            thrower.range = 120f;
            EditorUtility.SetDirty(thrower);
        }

        static GameObject BuildProjectilePrefab(string name, Color colour)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = name;
            root.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(root.GetComponent<Collider>());

            Renderer renderer = root.GetComponent<Renderer>();
            Material material = new Material(renderer.sharedMaterial) { color = colour };
            renderer.sharedMaterial = material;

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.09f;
            trail.endWidth = 0f;
            trail.sharedMaterial = material;
            trail.numCapVertices = 2;

            Projectile projectile = root.AddComponent<Projectile>();
            new AssetAuthoring(projectile)
                .Float("radius", 0.05f)
                .Float("impactImpulse", 6f)
                .Float("maxRicochetAngle", 22f)
                .Int("maxRicochets", 2)
                .Save();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{name}.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        static void BuildItems()
        {
            ItemDefinition sidearm = Item("Item_Sidearm", "Sidearm", ItemCategory.Weapon, 2, 1, 1,
                "Semi-automatic. Fast to bring up, light drop.", quickSlottable: true);
            ItemDefinition rifle = Item("Item_PulseRifle", "Pulse Rifle", ItemCategory.Weapon, 3, 1, 1,
                "Full-auto. Flattest trajectory you carry.", quickSlottable: true);
            ItemDefinition bolt = Item("Item_BoltThrower", "Bolt Thrower", ItemCategory.Weapon, 3, 2, 1,
                "Heavy arcing bolt. Aim high past twenty metres.", quickSlottable: true);

            Item("Item_SnarePod", "Snare Pod", ItemCategory.Gadget, 1, 1, 3,
                "Roots a target without hurting them. A capture, not a kill.", quickSlottable: true);

            Item("Item_KineticAmmo", "Kinetic Rounds", ItemCategory.Ammo, 1, 1, 60,
                "Sidearm and bolt thrower reserve.");
            Item("Item_CellAmmo", "Charge Cells", ItemCategory.Ammo, 1, 1, 120,
                "Pulse rifle reserve.");

            Item("Item_Medkit", "Trauma Kit", ItemCategory.Consumable, 2, 1, 3,
                "Field repair. Slow to apply, not a firefight answer.");
            Item("Item_Scrap", "Salvage", ItemCategory.Resource, 1, 1, 20,
                "Sells by weight at any broker.");
            Item("Item_DataSpike", "Data Spike", ItemCategory.QuestItem, 1, 1, 1,
                "Contract evidence. Hand it in intact.");

            Link("Weapon_Sidearm", sidearm);
            Link("Weapon_Rifle", rifle);
            Link("Weapon_BoltThrower", bolt);
        }

        static ItemDefinition Item(string assetName, string displayName, ItemCategory category,
            int width, int height, int maxStack, string description, bool quickSlottable = false)
        {
            ItemDefinition item = Create<ItemDefinition>(assetName);
            item.id = assetName.Replace("Item_", string.Empty).ToLowerInvariant();
            item.displayName = displayName;
            item.description = description;
            item.category = category;
            item.gridWidth = width;
            item.gridHeight = height;
            item.maxStack = maxStack;
            item.quickSlottable = quickSlottable;
            EditorUtility.SetDirty(item);
            return item;
        }

        static void Link(string weaponAsset, ItemDefinition item)
        {
            WeaponDefinition weapon =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{ContentRoot}/{weaponAsset}.asset");

            if (weapon == null)
                return;

            weapon.item = item;
            EditorUtility.SetDirty(weapon);
        }

        static GameObject BuildTargetPrefab()
        {
            GameObject root = new GameObject("Target_Dummy");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = Vector3.up * 2.05f;
            head.transform.localScale = Vector3.one * 0.45f;

            Health health = root.AddComponent<Health>();
            RangeTarget target = root.AddComponent<RangeTarget>();
            Hitbox hitbox = head.AddComponent<Hitbox>();

            new AssetAuthoring(hitbox).Ref("target", health).Float("damageMultiplier", 2.5f).Save();
            new AssetAuthoring(target)
                .Ref("health", health)
                .Ref("tint", body.GetComponent<Renderer>())
                .Float("respawnAfter", 2f)
                .Save();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/Target_Dummy.prefab");
            Object.DestroyImmediate(root);
            return saved;
        }

        static T Create<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, $"{ContentRoot}/{assetName}.asset");
            return asset;
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
