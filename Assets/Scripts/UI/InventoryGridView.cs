using System.Collections.Generic;
using FPSParkour.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.UI
{
    [DisallowMultipleComponent]
    public class InventoryGridView : MonoBehaviour
    {
        [SerializeField] InventorySystem inventory;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform content;
        [SerializeField] Button rowPrefab;

        readonly List<Button> rows = new List<Button>();

        void OnEnable()
        {
            if (inventory == null)
                return;

            inventory.OnChanged += Rebuild;
            inventory.OnOpenStateChanged += OnOpenStateChanged;
            OnOpenStateChanged(inventory.IsOpen);
        }

        void OnDisable()
        {
            if (inventory == null)
                return;

            inventory.OnChanged -= Rebuild;
            inventory.OnOpenStateChanged -= OnOpenStateChanged;
        }

        void OnOpenStateChanged(bool open)
        {
            if (root != null)
                root.SetActive(open);

            if (open)
                Rebuild();
        }

        void Rebuild()
        {
            foreach (Button row in rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            rows.Clear();

            if (inventory == null || content == null || rowPrefab == null)
                return;

            foreach (PlacedItem placed in inventory.PlacedItems)
            {
                if (placed == null)
                    continue;

                Button row = Instantiate(rowPrefab, content);

                row.gameObject.SetActive(true);

                Text label = row.GetComponentInChildren<Text>();
                if (label != null && placed.stack?.item != null)
                {
                    ItemDefinition item = placed.stack.item;
                    string name = string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;

                    label.text = placed.stack.count > 1
                        ? $"{name} x{placed.stack.count}"
                        : name;
                }

                rows.Add(row);
            }
        }
    }
}
