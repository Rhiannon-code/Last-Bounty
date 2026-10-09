using System;
using System.Collections.Generic;
using System.IO;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Save
{
    [Serializable]
    public class SaveEntry
    {
        public string Key;
        public string Json;
    }

    [Serializable]
    public class GameSave
    {
        public int Version = 1;
        public string SavedAtIso;
        public List<SaveEntry> Entries = new List<SaveEntry>();
    }

    public static class SaveSystem
    {
        public const int CurrentVersion = 1;

        public static string DefaultDirectory => Path.Combine(Application.persistentDataPath, "saves");
        public static string PathFor(string slot) => Path.Combine(DefaultDirectory, slot + ".json");

        public static bool Save(string slot, IEnumerable<ISaveable> participants)
        {
            GameSave save = new GameSave { Version = CurrentVersion, SavedAtIso = DateTime.UtcNow.ToString("o") };

            foreach (ISaveable participant in participants)
            {
                if (participant != null)
                    save.Entries.Add(new SaveEntry { Key = participant.SaveKey, Json = participant.CaptureJson() });
            }

            try
            {
                Directory.CreateDirectory(DefaultDirectory);
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(save, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Save to '{slot}' failed: {e.Message}");
                return false;
            }
        }

        public static GameSave Load(string slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonUtility.FromJson<GameSave>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"Load of '{slot}' failed: {e.Message}");
                return null;
            }
        }

        public static void Apply(GameSave save, IEnumerable<ISaveable> participants)
        {
            if (save == null)
                return;

            Dictionary<string, string> byKey = new Dictionary<string, string>();
            foreach (SaveEntry entry in save.Entries)
                byKey[entry.Key] = entry.Json;

            foreach (ISaveable participant in participants)
            {
                if (participant != null && byKey.TryGetValue(participant.SaveKey, out string json))
                    participant.RestoreJson(json);
            }
        }

        public static List<string> ListSlots()
        {
            List<string> slots = new List<string>();
            if (!Directory.Exists(DefaultDirectory))
                return slots;

            foreach (string file in Directory.GetFiles(DefaultDirectory, "*.json"))
                slots.Add(Path.GetFileNameWithoutExtension(file));

            return slots;
        }
    }
}
