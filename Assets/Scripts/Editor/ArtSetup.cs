using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class ArtSetup
    {
        const string Source = "/mnt/Bulk_Share/Resources/Unity Conversions";
        const string Imported = "Assets/FabImported";
        const string ArtRoot = "Assets/_Art";
        const string ViewmodelPack = "FPS_Automatic_Rifle_01_Animations";
        const string CharacterPack = "Cyber_Characters_Pack";
        const string LocomotionPack = "Sci-Fi_Characters_Pack_Vol.2";

        static readonly string[] Packs = { ViewmodelPack, CharacterPack, LocomotionPack };

        [MenuItem("FPS Parkour/Art/1: Import Character Art")]
        public static void Import()
        {
            List<string> missing = Packs.Where(p => !Directory.Exists(Path.Combine(Source, p))).ToList();

            if (missing.Count > 0)
            {
                EditorUtility.DisplayDialog("Packs not found",
                    $"Not under {Source}:\n\n  {string.Join("\n  ", missing)}\n\n" +
                    "Is the Bulk_Share mount up?", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Import character art",
                    $"Imports three packs into {Imported}/ , roughly 1.7 GB of meshes and textures.\n\n" +
                    $"  {string.Join("\n  ", Packs)}\n\n" +
                    "Additive: nothing outside that folder is touched. Unity will be busy for several " +
                    "minutes and the first texture import pass is the slow part.\n\n" +
                    "Run step 2 afterwards to make the rigs usable.",
                    "Import", "Cancel"))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    foreach (string pack in Packs)
                    {
                        Debug.Log($"[Art] importing {pack}…");
                        FabImport.FabImporter.Run(Path.Combine(Source, pack));
                    }

                    Debug.Log("[Art] import done. Now run 'FPS Parkour > Art > 2: Build Rigs And Controllers'.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Art import failed: {e}");
                }
            };
        }

        [MenuItem("FPS Parkour/Art/2: Build Rigs And Controllers")]
        public static void BuildRigs()
        {
            if (!AssetDatabase.IsValidFolder(Imported))
            {
                EditorUtility.DisplayDialog("Nothing imported",
                    $"{Imported} does not exist. Run step 1 first.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Build rigs and controllers",
                    $"Reimports every rigged FBX under {Imported} as Humanoid, then DELETES and " +
                    $"regenerates {ArtRoot}.\n\n" +
                    "Reimporting is the slow part and cannot be undone in bulk, but nothing here is " +
                    "hand-authored yet, so there is nothing to lose.",
                    "Build", "Cancel"))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Generate();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Rig build failed: {e}");
                }
            };
        }

        static void Generate()
        {
            Reset(ArtRoot);

            int promoted = PromoteToHumanoid();
            Debug.Log($"[Art] {promoted} rigs promoted to Humanoid.");

            AnimatorController viewmodel = BuildViewmodelController();
            AnimatorController character = BuildCharacterController();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Art] built {AssetDatabase.GetAssetPath(viewmodel)} and " +
                      $"{AssetDatabase.GetAssetPath(character)}.\n" +
                      "Rebuild the district scene to pick them up, the builders attach art when it " +
                      "exists and fall back to capsules when it does not.");
        }

        static int PromoteToHumanoid()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Imported });
            int changed = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (EditorUtility.DisplayCancelableProgressBar("Promoting rigs to Humanoid",
                        Path.GetFileName(path), (float)i / guids.Length))
                    break;

                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                    continue;

                if (importer.animationType == ModelImporterAnimationType.Human)
                    continue;

                if (!HasSkeleton(path))
                    continue;

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                changed++;
            }

            EditorUtility.ClearProgressBar();
            return changed;
        }

        static bool HasSkeleton(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is SkinnedMeshRenderer || asset is AnimationClip)
                    return true;
            }

            return false;
        }

        static AnimatorController BuildViewmodelController()
        {
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath($"{ArtRoot}/Viewmodel.controller");

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = machine.AddState("Locomotion");
            locomotion.motion = BuildLocomotionBlend(controller, ViewmodelPack,
                "Idle_01", "Walk_01", "Run_01", 0f, 2.4f, 6.5f);
            machine.defaultState = locomotion;

            AnimatorState airborne = machine.AddState("Airborne");
            airborne.motion = FindClip(ViewmodelPack, "Jump_loop") ?? FindClip(ViewmodelPack, "Jump");

            AnimatorStateTransition toAir = locomotion.AddTransition(airborne);
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            toAir.duration = 0.1f;
            toAir.hasExitTime = false;

            AnimatorStateTransition toGround = airborne.AddTransition(locomotion);
            toGround.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            toGround.duration = 0.12f;
            toGround.hasExitTime = false;

            AddOneShotLayer(controller, "Fire", FindClip(ViewmodelPack, "Shot"), "Fire");
            AddOneShotLayer(controller, "Reload", FindClip(ViewmodelPack, "Reload"), "Reload");

            return controller;
        }

        static AnimatorController BuildCharacterController()
        {
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath($"{ArtRoot}/Character.controller");

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Downed", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = machine.AddState("Locomotion");

            locomotion.motion = BuildLocomotionBlend(controller, LocomotionPack,
                "Idle_01", "Walk", "Run", 0f, 2.2f, 7.4f);
            machine.defaultState = locomotion;

            AnimatorState downed = machine.AddState("Downed");
            downed.motion = FindClip(LocomotionPack, "Dead_01");

            AnimatorStateTransition toDowned = locomotion.AddTransition(downed);
            toDowned.AddCondition(AnimatorConditionMode.If, 0f, "Downed");
            toDowned.duration = 0.15f;
            toDowned.hasExitTime = false;

            AnimatorStateTransition toUp = downed.AddTransition(locomotion);
            toUp.AddCondition(AnimatorConditionMode.IfNot, 0f, "Downed");
            toUp.duration = 0.2f;
            toUp.hasExitTime = false;

            AddOneShotLayer(controller, "Hit", FindClip(LocomotionPack, "Get_Hit"), "Hit");

            return controller;
        }

        static BlendTree BuildLocomotionBlend(AnimatorController controller, string pack,
            string idle, string walk, string run, float idleAt, float walkAt, float runAt)
        {
            BlendTree tree = new BlendTree { name = "Locomotion", blendParameter = "Speed" };
            AssetDatabase.AddObjectToAsset(tree, controller);

            AddMotion(tree, FindClip(pack, idle), idleAt);
            AddMotion(tree, FindClip(pack, walk), walkAt);
            AddMotion(tree, FindClip(pack, run), runAt);

            return tree;
        }

        static void AddMotion(BlendTree tree, AnimationClip clip, float threshold)
        {
            if (clip != null)
                tree.AddChild(clip, threshold);
            else
                Debug.LogWarning($"[Art] no clip for blend threshold {threshold}, that speed will hold the previous pose.");
        }

        static void AddOneShotLayer(AnimatorController controller, string name, AnimationClip clip, string trigger)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[Art] no clip found for the '{name}' layer; skipping it.");
                return;
            }

            controller.AddLayer(name);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer layer = layers[layers.Length - 1];
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine machine = layer.stateMachine;
            AnimatorState empty = machine.AddState("Empty");
            AnimatorState play = machine.AddState(name);
            play.motion = clip;
            machine.defaultState = empty;

            AnimatorStateTransition fire = empty.AddTransition(play);
            fire.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            fire.duration = 0.02f;
            fire.hasExitTime = false;

            AnimatorStateTransition back = play.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.08f;
        }

        static AnimationClip FindClip(string pack, string contains)
        {
            string folder = $"{Imported}/{pack}";

            if (!AssetDatabase.IsValidFolder(folder))
                return null;

            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip != null && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
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
