using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class CityArt
    {
        const string ArtRoot = "Assets/_Art";
        const string Imported = "Assets/FabImported";
        const string ViewmodelPack = "FPS_Automatic_Rifle_01_Animations";
        const string CharacterPack = "Cyber_Characters_Pack";

        public static readonly string[] CivilianMeshes =
        {
            "SK_CyberHucker", "SK_CyberPrisoner_Full", "SK_CyberHunter", "SK_CyberSoldier_Full_2"
        };

        public const string GuardMesh = "SK_CyberSoldier_Full_1";

        public static bool Available => AssetDatabase.IsValidFolder(ArtRoot)
                                        && AssetDatabase.IsValidFolder(Imported);

        public static AnimatorController Viewmodel() => Load<AnimatorController>($"{ArtRoot}/Viewmodel.controller");

        public static AnimatorController Character() => Load<AnimatorController>($"{ArtRoot}/Character.controller");

        public static GameObject ViewmodelArms() => FindModel(ViewmodelPack, "SK_Hands_04");

        public static GameObject ViewmodelWeapon() => FindModel(ViewmodelPack, "Automatic_Rifle");

        public static GameObject CharacterMesh(string meshName) => FindModel(CharacterPack, meshName);

        static T Load<T>(string path) where T : Object =>
            AssetDatabase.IsValidFolder(ArtRoot) ? AssetDatabase.LoadAssetAtPath<T>(path) : null;

        static GameObject FindModel(string pack, string contains)
        {
            string folder = $"{Imported}/{pack}";

            if (!AssetDatabase.IsValidFolder(folder))
                return null;

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }

        public static Animator Attach(GameObject root, GameObject model, AnimatorController controller,
            Vector3 localPosition, float scale = 1f)
        {
            if (model == null)
                return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Visual";
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scale;

            Animator animator = instance.GetComponent<Animator>();

            if (animator == null)
                animator = instance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            return animator;
        }

        public static void HideCapsule(Renderer capsule)
        {
            if (capsule != null)
                capsule.enabled = false;
        }
    }
}
