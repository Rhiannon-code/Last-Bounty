using System.Collections.Generic;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Save
{
    [DisallowMultipleComponent]
    public class SaveCoordinator : MonoBehaviour
    {
        [SerializeField] string defaultSlot = "slot1";

        readonly List<ISaveable> participants = new List<ISaveable>();

        public IReadOnlyList<ISaveable> Participants => participants;

        public void Rebuild()
        {
            participants.Clear();

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is ISaveable saveable)
                    participants.Add(saveable);
            }
        }

        public bool Save(string slot = null)
        {
            Rebuild();
            return SaveSystem.Save(slot ?? defaultSlot, participants);
        }

        public bool Load(string slot = null)
        {
            GameSave save = SaveSystem.Load(slot ?? defaultSlot);
            if (save == null)
                return false;

            Rebuild();
            SaveSystem.Apply(save, participants);
            return true;
        }
    }
}
