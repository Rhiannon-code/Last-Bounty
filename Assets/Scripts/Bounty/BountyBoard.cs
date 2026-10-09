using System;
using System.Collections.Generic;
using FPSParkour.Core;
using UnityEngine;

namespace FPSParkour.Bounty
{
    [DisallowMultipleComponent]
    public class BountyBoard : MonoBehaviour, IInteractable
    {
        [SerializeField] BountyRegistry registry;
        [SerializeField] string boardName = "Contract board";

        readonly List<BountyContract> available = new List<BountyContract>();

        public event Action<IReadOnlyList<BountyContract>> BoardOpened;

        public string Prompt => boardName;
        public IReadOnlyList<BountyContract> Available => available;

        public bool CanInteract(GameObject actor) => registry != null;

        public void Interact(GameObject actor)
        {
            if (registry == null)
                return;

            registry.CollectAvailable(available);
            BoardOpened?.Invoke(available);
        }

        public bool Accept(BountyContract contract) => registry != null && registry.Accept(contract);
    }
}
