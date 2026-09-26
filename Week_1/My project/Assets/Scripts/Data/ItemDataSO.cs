using UnityEngine;

public enum ItemType
{
    Consumable,
    Equipment,
    Material
}

[CreateAssetMenu(fileName = "New_ItemData", menuName = "Inventory/Item Data")] 
public class ItemDataSO : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _itemName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _maxStack;
    [SerializeField] private ItemType _type;

    [TextArea(3,5)]
    [SerializeField] private string _description;

    public string Id => _id;
    public string ItemName => _itemName;
    public Sprite Icon => _icon;
    public int MaxStack => _maxStack;
    public ItemType Type => _type;
    public string Description => _description;

}
