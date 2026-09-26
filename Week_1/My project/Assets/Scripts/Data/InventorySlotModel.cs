using System;

[Serializable]
public class InventorySlotModel
{
    public int SlotIndex { get; private set; }
    public ItemDataSO ItemData { get; private set; }
    public int StackAmount { get; private set; }

    // Kiểm tra ô trống
    public bool IsEmpty => ItemData == null || StackAmount <= 0;

    public InventorySlotModel(int slotIndex)
    {
        SlotIndex = slotIndex;
        Clear();
    }

    public void SetItem(ItemDataSO data, int amount)
    {
        ItemData = data;
        StackAmount = amount;
    }

    public void ReduceStack(int amount = 1)
    {
        StackAmount -= amount;
        if (StackAmount <= 0)
        {
            Clear();
        }
    }

    public void Clear()
    {
        ItemData = null;
        StackAmount = 0;
    }
}