using System;

namespace FPSParkour.Inventory
{
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        public int count;

        public ItemStack(ItemDefinition item, int count = 1)
        {
            this.item = item;
            this.count = count;
        }

        public bool IsEmpty => item == null || count <= 0;
        public int SpaceLeft => item == null ? 0 : item.maxStack - count;

        public ItemStack Clone() => new(item, count);
    }

    [Serializable]
    public class PlacedItem
    {
        public ItemStack stack;
        public int x;
        public int y;

        public PlacedItem(ItemStack stack, int x, int y)
        {
            this.stack = stack;
            this.x = x;
            this.y = y;
        }

        public int Width => stack.item.gridWidth;
        public int Height => stack.item.gridHeight;
    }
}
