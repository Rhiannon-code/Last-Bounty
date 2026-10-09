using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPSParkour.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [Header("Quick-bar")]
        [SerializeField] private int quickSlotCount = 4;

        [Header("Grid")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 6;

        private ItemStack[] _quickSlots;
        private bool[,] _occupied;
        private readonly List<PlacedItem> _placed = new();

        public bool IsOpen { get; private set; }
        public int SelectedQuickSlot { get; private set; }
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public IReadOnlyList<PlacedItem> PlacedItems => _placed;

        public event Action OnChanged;
        public event Action<bool> OnOpenStateChanged;
        public event Action<int, ItemStack> OnQuickSlotChanged; // Index, stack (may be null)
        public event Action<int> OnQuickSlotSelected;

        private void Awake()
        {
            _quickSlots = new ItemStack[quickSlotCount];
            _occupied = new bool[gridWidth, gridHeight];
        }

        public void ToggleOpen() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            OnOpenStateChanged?.Invoke(open);
        }

        public void SelectQuickSlot(int index)
        {
            if (index < 0 || index >= _quickSlots.Length) return;
            SelectedQuickSlot = index;
            OnQuickSlotSelected?.Invoke(index);
        }

        public ItemStack GetQuickSlot(int index) =>
            (index >= 0 && index < _quickSlots.Length) ? _quickSlots[index] : null;

        public ItemStack SelectedItem => GetQuickSlot(SelectedQuickSlot);

        public bool AssignQuickSlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= _quickSlots.Length) return false;
            if (stack != null && stack.item != null && !stack.item.quickSlottable) return false;

            _quickSlots[index] = stack;
            OnQuickSlotChanged?.Invoke(index, stack);
            return true;
        }

        public int AddItem(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0) return amount;
            int remaining = amount;

            if (item.IsStackable)
            {
                foreach (var placed in _placed)
                {
                    if (placed.stack.item != item) continue;
                    int add = Mathf.Min(placed.stack.SpaceLeft, remaining);
                    if (add <= 0) continue;
                    placed.stack.count += add;
                    remaining -= add;
                    if (remaining <= 0) break;
                }
            }

            while (remaining > 0)
            {
                if (!FindFreeCell(item.gridWidth, item.gridHeight, out int px, out int py))
                    break; // Out of space

                int stackCount = Mathf.Min(item.maxStack, remaining);
                var placed = new PlacedItem(new ItemStack(item, stackCount), px, py);
                _placed.Add(placed);
                SetRegion(px, py, item.gridWidth, item.gridHeight, true);
                remaining -= stackCount;
            }

            if (remaining != amount) OnChanged?.Invoke();
            return remaining;
        }

        public bool RemoveItem(PlacedItem placed)
        {
            if (placed == null || !_placed.Contains(placed)) return false;
            SetRegion(placed.x, placed.y, placed.Width, placed.Height, false);
            _placed.Remove(placed);
            OnChanged?.Invoke();
            return true;
        }

        public int CountOf(ItemDefinition item)
        {
            int total = 0;
            foreach (var p in _placed)
                if (p.stack.item == item) total += p.stack.count;
            return total;
        }

        private bool FindFreeCell(int w, int h, out int outX, out int outY)
        {
            for (int y = 0; y <= gridHeight - h; y++)
            {
                for (int x = 0; x <= gridWidth - w; x++)
                {
                    if (RegionFree(x, y, w, h))
                    {
                        outX = x; outY = y;
                        return true;
                    }
                }
            }
            outX = outY = -1;
            return false;
        }

        private bool RegionFree(int x, int y, int w, int h)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (_occupied[i, j]) return false;
            return true;
        }

        private void SetRegion(int x, int y, int w, int h, bool value)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    _occupied[i, j] = value;
        }
    }
}
