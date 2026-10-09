using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FPSParkour.Bounty;
using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public static class RosterImporter
    {
        const string RosterPath = "/home/rhiannon/Local Docs/Prey 2 Project/story/BOUNTY_ROSTER.md";
        const string ContentRoot = "Assets/_Bounties";

        const string SectionStart = "## 4. The roster";
        const string SectionEnd = "## 5. Gabriela";

        static readonly Regex Header = new Regex(@"^\*\*(.+?)\*\*\s*[—-]\s*(.+)$", RegexOptions.Compiled);
        static readonly Regex Section = new Regex(@"^### (.+)$", RegexOptions.Compiled);
        static readonly Regex Fee = new Regex(@"([\d][\d,]*)\s*$", RegexOptions.Compiled);
        static readonly Regex Field = new Regex(@"^\*(Board|True|Lost|Weight[^:]*):\*\s*(.*)$", RegexOptions.Compiled);

        public class Entry
        {
            public string Name;
            public string Species;
            public string Category;
            public int Fee;
            public readonly Dictionary<string, StringBuilder> Fields = new Dictionary<string, StringBuilder>();

            public string Field(string key) =>
                Fields.TryGetValue(key, out StringBuilder value) ? value.ToString().Trim() : string.Empty;
        }

        [MenuItem("FPS Parkour/Import Bounty Roster")]
        public static void Import()
        {
            if (!File.Exists(RosterPath))
            {
                EditorUtility.DisplayDialog("Roster not found", $"No file at:\n\n{RosterPath}", "OK");
                return;
            }

            List<Entry> entries = Parse(File.ReadAllText(RosterPath));

            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing parsed",
                    "The roster was found but no contracts matched. The document's format has probably " +
                    "changed: check the '**NAME** - Species, category, fee' header lines.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Import bounty roster",
                    $"Parsed {entries.Count} contracts from BOUNTY_ROSTER.md.\n\n" +
                    $"DELETES and regenerates {ContentRoot}.\n\n" +
                    "Any hand edits to contract assets in that folder are lost. The Markdown is the " +
                    "source of truth, edit it there and re-run this.",
                    "Import", "Cancel"))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Generate(entries);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Roster import failed: {e}");
                }
            };
        }

        public static List<Entry> Parse(string document)
        {
            List<Entry> entries = new List<Entry>();

            int start = document.IndexOf(SectionStart, System.StringComparison.Ordinal);
            int end = document.IndexOf(SectionEnd, System.StringComparison.Ordinal);

            if (start < 0)
                return entries;

            if (end < start)
                end = document.Length;

            string[] lines = document.Substring(start, end - start).Split('\n');
            string category = string.Empty;
            Entry current = null;
            string field = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');

                Match section = Section.Match(line);
                if (section.Success)
                {
                    category = Clean(section.Groups[1].Value.Split(':')[0]);
                    current = null;
                    continue;
                }

                Match header = Header.Match(line);
                if (header.Success)
                {
                    current = BuildEntry(header, category, lines, i);
                    field = null;

                    if (current != null)
                        entries.Add(current);

                    continue;
                }

                if (current == null)
                    continue;

                Match tagged = Field.Match(line);
                if (tagged.Success)
                {
                    field = tagged.Groups[1].Value.StartsWith("Weight") ? "Weight" : tagged.Groups[1].Value;

                    if (!current.Fields.ContainsKey(field))
                        current.Fields[field] = new StringBuilder();

                    current.Fields[field].Append(' ').Append(Clean(tagged.Groups[2].Value));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    field = null;
                else if (field != null)
                    current.Fields[field].Append(' ').Append(Clean(line));
            }

            return entries;
        }

        static Entry BuildEntry(Match header, string category, string[] lines, int index)
        {
            bool isContract = false;

            for (int i = index + 1; i < Mathf.Min(index + 4, lines.Length); i++)
            {
                if (lines[i].StartsWith("*Board:*"))
                {
                    isContract = true;
                    break;
                }
            }

            if (!isContract)
                return null;

            string rest = header.Groups[2].Value.Split('·')[0].Trim();
            Match fee = Fee.Match(rest);

            if (!fee.Success)
                return null;

            string body = rest.Substring(0, fee.Index).TrimEnd().TrimEnd(',');

            return new Entry
            {
                Name = Clean(header.Groups[1].Value),
                Species = Clean(body.Split(',')[0]),
                Category = category,
                Fee = int.Parse(fee.Groups[1].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture),
            };
        }

        static void Generate(List<Entry> entries)
        {
            if (AssetDatabase.IsValidFolder(ContentRoot))
                AssetDatabase.DeleteAsset(ContentRoot);

            Directory.CreateDirectory(ContentRoot);
            AssetDatabase.Refresh();

            List<BountyContract> created = new List<BountyContract>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (Entry entry in entries)
                    created.Add(Create(entry));
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Roster] {created.Count} contracts written to {ContentRoot}. " +
                      "Assign them to the district's BountyRegistry roster, or rebuild the scene.");
        }

        static BountyContract Create(Entry entry)
        {
            BountyContract contract = ScriptableObject.CreateInstance<BountyContract>();
            string id = Slug(entry.Name);

            new AssetAuthoring(contract)
                .Str("id", $"bounty.{id}")
                .Str("targetName", entry.Name)
                .Str("species", entry.Species)
                .Str("alias", entry.Category)
                .Str("issuingFactionId", "guild")
                .Str("charges", entry.Field("Board"))
                .Str("whoTheyActuallyAre", Dossier(entry))
                .Int("payoutAlive", entry.Fee)
                .Save();

            AssetDatabase.CreateAsset(contract, $"{ContentRoot}/Bounty_{ToPascal(entry.Name)}.asset");
            return contract;
        }

        static string Dossier(Entry entry)
        {
            StringBuilder builder = new StringBuilder(entry.Field("True"));
            string lost = entry.Field("Lost");

            if (!string.IsNullOrEmpty(lost))
                builder.Append("\n\nWhen beaten: ").Append(lost);

            return builder.ToString();
        }

        static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = Regex.Replace(value, @"\*{1,2}", string.Empty);
            value = Regex.Replace(value, @"`\[.*?\]`", string.Empty);
            value = value.Replace("*", string.Empty);
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        static string Slug(string name) =>
            Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", ".").Trim('.');

        static string ToPascal(string name)
        {
            StringBuilder builder = new StringBuilder();

            foreach (string word in name.Split(' '))
            {
                if (word.Length > 0)
                    builder.Append(char.ToUpperInvariant(word[0])).Append(word.Substring(1).ToLowerInvariant());
            }

            return builder.ToString();
        }
    }
}
