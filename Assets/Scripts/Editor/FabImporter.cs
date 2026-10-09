using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FabImport
{
    public class MeshSlot
    {
        public int slot;
        public string name;      // Slot name, matches the name inside the FBX
        public string material;  // UE asset path, or null if unassigned
    }

    [Serializable] public class MeshEntry
    {
        public string asset; public string file; public string type;
        [NonSerialized] public List<MeshSlot> materials = new List<MeshSlot>();
    }

    [Serializable] public class TextureEntry
    {
        public string asset; public string file;
        public bool srgb = true; public string compression;
        public bool flip_green;   // Texture is a normal map
        public bool linear;       // Data texture, never treat as sRGB
    }

    [Serializable] public class MaterialEntry
    {
        public string asset; public string parent;

        [NonSerialized] public Dictionary<string, string> textures =
            new Dictionary<string, string>();
        [NonSerialized] public Dictionary<string, float> scalars =
            new Dictionary<string, float>();
        [NonSerialized] public Dictionary<string, Color> vectors =
            new Dictionary<string, Color>();
    }

    public class ExportManifest
    {
        public string root;
        public List<MeshEntry> meshes = new List<MeshEntry>();
        public List<TextureEntry> textures = new List<TextureEntry>();
        public List<MaterialEntry> materials = new List<MaterialEntry>();
    }

    public enum Pipeline { BuiltIn, URP, HDRP }

    public static class Target
    {
        public static Pipeline Detect()
        {
            var rp = GraphicsSettings.defaultRenderPipeline;
            if (rp == null) return Pipeline.BuiltIn;
            var n = rp.GetType().Name;
            if (n.Contains("Universal")) return Pipeline.URP;
            if (n.Contains("HDRenderPipeline")) return Pipeline.HDRP;
            return Pipeline.BuiltIn;
        }

        public static string ShaderName(Pipeline p)
        {
            switch (p)
            {
                case Pipeline.URP:  return "Universal Render Pipeline/Lit";
                case Pipeline.HDRP: return "HDRP/Lit";
                default:            return "Standard";
            }
        }

        // Logical slot -> shader property, per pipeline.
        public static string Prop(Pipeline p, string slot)
        {
            switch (slot)
            {
                case "base":
                    return p == Pipeline.HDRP ? "_BaseColorMap"
                         : p == Pipeline.URP  ? "_BaseMap" : "_MainTex";
                case "baseColor":
                    return p == Pipeline.BuiltIn ? "_Color" : "_BaseColor";
                case "normal":
                    return p == Pipeline.HDRP ? "_NormalMap" : "_BumpMap";
                case "mask":
                    return p == Pipeline.HDRP ? "_MaskMap" : "_MetallicGlossMap";
                case "occlusion":
                    return p == Pipeline.HDRP ? null : "_OcclusionMap";
                case "emissive":
                    return p == Pipeline.HDRP ? "_EmissiveColorMap"
                                              : "_EmissionMap";
                case "metallic":  return "_Metallic";
                case "smoothness":
                    return p == Pipeline.BuiltIn ? "_Glossiness" : "_Smoothness";
            }
            return null;
        }
    }

    public static class Guess
    {
        static bool Has(string s, params string[] keys)
        {
            s = s.ToLowerInvariant();
            return keys.Any(k => s.Contains(k));
        }

        public static string Slot(string name)
        {
            if (Has(name, "normal", "_n_", "bump")) return "normal";
            if (Has(name, "orm", "arm", "mask", "packed", "rma", "mra"))
                return "mask";
            if (Has(name, "emiss", "emit")) return "emissive";
            if (Has(name, "occlusion", "ambientoc", "_ao")) return "occlusion";
            if (Has(name, "rough", "smooth")) return "smoothness";
            if (Has(name, "metal")) return "metallic";
            if (Has(name, "basecolor", "albedo", "diffuse", "_bc", "_d_",
                    "color", "base"))
                return "base";
            return null;
        }

        public static string SlotFromTextureName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            string leaf = assetPath.Split('/').Last().Split('.').Last();
            string up = leaf.ToUpperInvariant();

            string[][] rules =
            {
                new[] { "normal",     "_BN", "_NRM", "_NORM", "_N" },
                new[] { "mask",       "_MRA", "_ORM", "_RMA", "_ARM", "_MASK" },
                new[] { "occlusion",  "_AO" },
                new[] { "emissive",   "_EM", "_EMIT", "_E" },
                new[] { "metallic",   "_MET", "_M" },
                new[] { "smoothness", "_ROUGH", "_RGH", "_R" },
                new[] { "base",       "_BC", "_ALB", "_DIFF", "_D", "_C", "_A" },
            };
            foreach (var rule in rules)
                for (int i = 1; i < rule.Length; i++)
                    if (up.EndsWith(rule[i], StringComparison.Ordinal))
                        return rule[0];
            return null;
        }
    }

    public static class Json
    {
        public static object Parse(string s)
        {
            int i = 0; var v = ParseValue(s, ref i); return v;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object ParseValue(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return ParseObj(s, ref i);
            if (c == '[') return ParseArr(s, ref i);
            if (c == '"') return ParseStr(s, ref i);
            if (s.Length - i >= 4 && s.Substring(i, 4) == "true")
            { i += 4; return true; }
            if (s.Length - i >= 5 && s.Substring(i, 5) == "false")
            { i += 5; return false; }
            if (s.Length - i >= 4 && s.Substring(i, 4) == "null")
            { i += 4; return null; }
            int st = i;
            while (i < s.Length && "-+.eE0123456789".IndexOf(s[i]) >= 0) i++;
            double d;
            double.TryParse(s.Substring(st, i - st),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out d);
            return d;
        }

        static Dictionary<string, object> ParseObj(string s, ref int i)
        {
            var o = new Dictionary<string, object>(); i++; // {
            while (true)
            {
                Ws(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == '}') { i++; break; }
                if (s[i] == ',') { i++; continue; }
                string k = ParseStr(s, ref i);
                Ws(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                o[k] = ParseValue(s, ref i);
            }
            return o;
        }

        static List<object> ParseArr(string s, ref int i)
        {
            var a = new List<object>(); i++; // [
            while (true)
            {
                Ws(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == ']') { i++; break; }
                if (s[i] == ',') { i++; continue; }
                a.Add(ParseValue(s, ref i));
            }
            return a;
        }

        static string ParseStr(string s, ref int i)
        {
            var sb = new System.Text.StringBuilder(); i++; // "
            while (i < s.Length && s[i] != '"')
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'u':
                            sb.Append((char)Convert.ToInt32(
                                s.Substring(i + 1, 4), 16));
                            i += 4; break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else sb.Append(s[i]);
                i++;
            }
            i++; 
            return sb.ToString();
        }
    }

    public static class FabImporter
    {
        const string DestRoot = "Assets/FabImported";

        [MenuItem("Tools/Fab Import/Import Converted Packs...")]
        public static void ImportInteractive()
        {
            string src = EditorUtility.OpenFolderPanel(
                "Select exported packs folder", "", "");
            if (string.IsNullOrEmpty(src)) return;
            Run(src);
        }

        public static void RunBatch()
        {
            string src = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-fabSource") src = args[i + 1];
            if (string.IsNullOrEmpty(src))
            {
                Debug.LogError("[Fab] -fabSource <dir> is required");
                EditorApplication.Exit(2);
                return;
            }
            int failed = Run(src);
            EditorApplication.Exit(failed > 0 ? 1 : 0);
        }

        public static int Run(string sourceRoot)
        {
            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogError($"[Fab] no such directory: {sourceRoot}");
                return 1;
            }

            var pipeline = Target.Detect();
            Debug.Log($"[Fab] render pipeline: {pipeline}");

            var manifests = Directory.GetFiles(sourceRoot, "materials.json",
                                               SearchOption.AllDirectories);
            if (manifests.Length == 0)
            {
                Debug.LogError($"[Fab] no materials.json found under {sourceRoot}");
                return 1;
            }
            Debug.Log($"[Fab] {manifests.Length} exported root(s) to import");

            int failed = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var mf in manifests)
                {
                    try { CopyStage(mf, sourceRoot); }
                    catch (Exception e)
                    {
                        failed++;
                        Debug.LogError($"[Fab] copy failed for {mf}: {e.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            foreach (var mf in manifests)
            {
                try { ConfigureAndBuild(mf, sourceRoot, pipeline); }
                catch (Exception e)
                {
                    failed++;
                    Debug.LogError($"[Fab] import failed for {mf}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fab] done. {manifests.Length - failed} ok, {failed} failed.");
            return failed;
        }

        static string RelKey(string manifestPath, string sourceRoot)
        {
            var dir = Path.GetDirectoryName(manifestPath);
            var rel = dir.Substring(sourceRoot.TrimEnd('/').Length)
                         .Trim('/', '\\');
            return string.IsNullOrEmpty(rel) ? "Pack" : rel;
        }

        static void CopyStage(string manifestPath, string sourceRoot)
        {
            string srcDir = Path.GetDirectoryName(manifestPath);
            string rel = RelKey(manifestPath, sourceRoot);
            string dst = Path.Combine(DestRoot, rel);
            Directory.CreateDirectory(dst);

            foreach (var sub in new[] { "Meshes", "Textures" })
            {
                string s = Path.Combine(srcDir, sub);
                if (!Directory.Exists(s)) continue;
                foreach (var f in Directory.GetFiles(s, "*",
                                                     SearchOption.AllDirectories))
                {
                    string r = f.Substring(srcDir.Length).Trim('/', '\\');
                    string d = Path.Combine(dst, r);
                    Directory.CreateDirectory(Path.GetDirectoryName(d));
                    if (File.Exists(d) &&
                        new FileInfo(d).Length == new FileInfo(f).Length)
                        continue;
                    File.Copy(f, d, true);
                }
            }
        }

        static ExportManifest ReadManifest(string path)
        {
            var raw = Json.Parse(File.ReadAllText(path))
                      as Dictionary<string, object>;
            var m = new ExportManifest();
            if (raw == null) return m;
            object v;
            if (raw.TryGetValue("root", out v)) m.root = v as string;

            if (raw.TryGetValue("meshes", out v) && v is List<object>)
                foreach (Dictionary<string, object> e in
                         ((List<object>)v).OfType<Dictionary<string, object>>())
                {
                    var me = new MeshEntry();
                    object t;
                    if (e.TryGetValue("asset", out t)) me.asset = t as string;
                    if (e.TryGetValue("file", out t)) me.file = t as string;
                    if (e.TryGetValue("type", out t)) me.type = t as string;
                    if (e.TryGetValue("materials", out t) && t is List<object>)
                        foreach (var so in ((List<object>)t)
                                 .OfType<Dictionary<string, object>>())
                        {
                            var ms = new MeshSlot();
                            object x;
                            if (so.TryGetValue("slot", out x) && x is double)
                                ms.slot = (int)(double)x;
                            if (so.TryGetValue("name", out x))
                                ms.name = x as string;
                            if (so.TryGetValue("material", out x))
                                ms.material = x as string;
                            me.materials.Add(ms);
                        }
                    m.meshes.Add(me);
                }

            if (raw.TryGetValue("textures", out v) && v is List<object>)
                foreach (var e in ((List<object>)v)
                         .OfType<Dictionary<string, object>>())
                {
                    var te = new TextureEntry();
                    object t;
                    if (e.TryGetValue("asset", out t)) te.asset = t as string;
                    if (e.TryGetValue("file", out t)) te.file = t as string;
                    if (e.TryGetValue("srgb", out t) && t is bool)
                        te.srgb = (bool)t;
                    if (e.TryGetValue("compression", out t))
                        te.compression = t as string;
                    if (e.TryGetValue("flip_green", out t) && t is bool)
                        te.flip_green = (bool)t;
                    if (e.TryGetValue("linear", out t) && t is bool)
                        te.linear = (bool)t;
                    m.textures.Add(te);
                }

            if (raw.TryGetValue("materials", out v) && v is List<object>)
                foreach (var e in ((List<object>)v)
                         .OfType<Dictionary<string, object>>())
                {
                    var ma = new MaterialEntry();
                    object t;
                    if (e.TryGetValue("asset", out t)) ma.asset = t as string;
                    if (e.TryGetValue("parent", out t)) ma.parent = t as string;
                    if (e.TryGetValue("textures", out t) &&
                        t is Dictionary<string, object>)
                        foreach (var kv in (Dictionary<string, object>)t)
                            ma.textures[kv.Key] = kv.Value as string;
                    if (e.TryGetValue("scalars", out t) &&
                        t is Dictionary<string, object>)
                        foreach (var kv in (Dictionary<string, object>)t)
                            if (kv.Value is double)
                                ma.scalars[kv.Key] = (float)(double)kv.Value;
                    if (e.TryGetValue("vectors", out t) &&
                        t is Dictionary<string, object>)
                        foreach (var kv in (Dictionary<string, object>)t)
                        {
                            var arr = kv.Value as List<object>;
                            if (arr == null || arr.Count < 4) continue;
                            ma.vectors[kv.Key] = new Color(
                                (float)Convert.ToDouble(arr[0]),
                                (float)Convert.ToDouble(arr[1]),
                                (float)Convert.ToDouble(arr[2]),
                                (float)Convert.ToDouble(arr[3]));
                        }
                    m.materials.Add(ma);
                }
            return m;
        }

        static void ConfigureAndBuild(string manifestPath, string sourceRoot,
                                      Pipeline pipeline)
        {
            var man = ReadManifest(manifestPath);
            string rel = RelKey(manifestPath, sourceRoot);
            string dst = Path.Combine(DestRoot, rel);

            var texByAsset = new Dictionary<string, string>();
            foreach (var t in man.textures)
            {
                if (string.IsNullOrEmpty(t.file)) continue;
                string p = Path.Combine(dst, t.file).Replace('\\', '/');
                texByAsset[t.asset ?? ""] = p;
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool changed = false;
                bool wantNormal = t.flip_green;
                var wantType = wantNormal ? TextureImporterType.NormalMap
                                          : TextureImporterType.Default;
                if (ti.textureType != wantType)
                { ti.textureType = wantType; changed = true; }
                bool wantSrgb = t.srgb && !t.linear;
                if (!wantNormal && ti.sRGBTexture != wantSrgb)
                { ti.sRGBTexture = wantSrgb; changed = true; }
                if (changed)
                {
                    ti.SaveAndReimport();
                }
            }

            string matDir = Path.Combine(dst, "Materials").Replace('\\', '/');
            Directory.CreateDirectory(matDir);
            var shader = Shader.Find(Target.ShaderName(pipeline));
            if (shader == null)
            {
                Debug.LogError($"[Fab] shader not found: " +
                               $"{Target.ShaderName(pipeline)}");
                return;
            }

            var matByAsset = new Dictionary<string, Material>();
            foreach (var me in man.materials)
            {
                if (string.IsNullOrEmpty(me.asset)) continue;
                string name = me.asset.Split('.').Last().Split('/').Last();
                string mp = $"{matDir}/{Sanitize(name)}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, mp);
                }
                else if (mat.shader != shader) mat.shader = shader;

                foreach (var kv in me.textures)
                {
                    if (string.IsNullOrEmpty(kv.Value)) continue;
                    string slot = Guess.Slot(kv.Key)
                                  ?? Guess.SlotFromTextureName(kv.Value);
                    if (slot == null) continue;
                    string prop = Target.Prop(pipeline, slot);
                    if (prop == null || !mat.HasProperty(prop)) continue;
                    string texPath;
                    if (!texByAsset.TryGetValue(kv.Value, out texPath)) continue;
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (tex != null) mat.SetTexture(prop, tex);
                    if (slot == "normal") mat.EnableKeyword("_NORMALMAP");
                    if (slot == "emissive") mat.EnableKeyword("_EMISSION");
                }

                foreach (var kv in me.scalars)
                {
                    string slot = Guess.Slot(kv.Key);
                    if (slot != "metallic" && slot != "smoothness") continue;
                    string prop = Target.Prop(pipeline, slot);
                    if (prop == null || !mat.HasProperty(prop)) continue;
                    float val = kv.Value;
                    if (slot == "smoothness" &&
                        kv.Key.ToLowerInvariant().Contains("rough"))
                        val = 1f - val;
                    mat.SetFloat(prop, Mathf.Clamp01(val));
                }

                foreach (var kv in me.vectors)
                {
                    if (Guess.Slot(kv.Key) != "base") continue;
                    string prop = Target.Prop(pipeline, "baseColor");
                    if (prop != null && mat.HasProperty(prop))
                        mat.SetColor(prop, kv.Value);
                    break;
                }

                EditorUtility.SetDirty(mat);
                matByAsset[me.asset] = mat;
            }

            foreach (var me in man.meshes)
            {
                if (string.IsNullOrEmpty(me.file)) continue;
                string p = Path.Combine(dst, me.file).Replace('\\', '/');
                var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                if (mi == null) continue;
                bool dirty = false;

                if (me.type == "SkeletalMesh" || me.type == "AnimSequence")
                {
                    if (mi.animationType != ModelImporterAnimationType.Generic)
                    { mi.animationType = ModelImporterAnimationType.Generic; dirty = true; }
                    if (mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                    { mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true; }
                }
                if (me.type == "AnimSequence" && !mi.importAnimation)
                { mi.importAnimation = true; dirty = true; }

                var map = new List<AssetImporter.SourceAssetIdentifier>();
                var targets = new List<UnityEngine.Object>();
                foreach (var ms in me.materials)
                {
                    if (ms == null || string.IsNullOrEmpty(ms.name)) continue;

                    Material found = null;
                    if (!string.IsNullOrEmpty(ms.material))
                        matByAsset.TryGetValue(ms.material, out found);
                    if (found == null) continue;
                    map.Add(new AssetImporter.SourceAssetIdentifier(
                        typeof(Material), ms.name));
                    targets.Add(found);
                }
                if (map.Count > 0)
                {
                    mi.materialImportMode =
                        ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    mi.materialLocation = ModelImporterMaterialLocation.External;
                    for (int i = 0; i < map.Count; i++)
                        mi.AddRemap(map[i], targets[i]);
                    dirty = true;
                }

                if (dirty) mi.SaveAndReimport();
            }

            Debug.Log($"[Fab] {rel}: {man.meshes.Count} meshes, " +
                      $"{man.textures.Count} textures, " +
                      $"{man.materials.Count} materials");
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }
    }
}
