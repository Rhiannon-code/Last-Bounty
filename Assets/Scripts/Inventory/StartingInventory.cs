using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSParkour.Inventory
{
    [RequireComponent(typeof(InventorySystem))]
    public class StartingInventory : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public ItemDefinition item;
            [Min(1)] public int count;
        }

        [SerializeField] InventorySystem inventory;
        [SerializeField] List<Entry> contents = new();

        void Awake()
        {
            if (inventory == null)
                inventory = GetComponent<InventorySystem>();
        }

        void Start()
        {
            foreach (Entry entry in contents)
            {
                if (entry.item == null)
                    continue;

                int leftOver = inventory.AddItem(entry.item, Mathf.Max(1, entry.count));
                if (leftOver > 0)
                    Debug.LogWarning($"Starting inventory is short of space: {leftOver} x {entry.item.displayName} did not fit.");
            }
        }
    }
}
