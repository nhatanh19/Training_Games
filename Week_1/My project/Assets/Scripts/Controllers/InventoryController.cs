using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    [Header("Model Configurations")]
    [SerializeField] private List<ItemDataSO> _sampleItemConfigs = new List<ItemDataSO>();
    private readonly List<InventorySlotModel> _slotModels = new List<InventorySlotModel>();
    private int _selectedSlotIndex = -1;

    [Header("Views & Hierarchy")]
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private Transform _dragLayer;
    [SerializeField] private SlotUI _slotPrefab;
    [SerializeField] private ItemView _itemPrefab;
    [SerializeField] private ItemDetailUI _detailUI;
    [SerializeField] private Button _resetButton;

    private readonly List<SlotUI> _slotViews = new List<SlotUI>();
    private readonly Dictionary<int, ItemView> _itemViews = new Dictionary<int, ItemView>();

    private void Awake()
    {
        InitGrid(16); // Lưới 4x4 gồm 16 slots
    }

    private void OnEnable()
    {
        ItemView.OnItemClicked += HandleItemClicked;
        SlotUI.OnSlotClicked += HandleSlotClicked;
        SlotUI.OnItemDropped += HandleItemDropped;

        if (_detailUI != null)
        {
            _detailUI.OnUseClicked += HandleUseItem;
            _detailUI.OnDeleteClicked += HandleDeleteItem;
            _detailUI.OnDeselectClicked += HandleDeselect;
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.AddListener(ResetAndSpawnItems);
        }
    }

    private void OnDisable()
    {
        ItemView.OnItemClicked -= HandleItemClicked;
        SlotUI.OnSlotClicked -= HandleSlotClicked;
        SlotUI.OnItemDropped -= HandleItemDropped;

        if (_detailUI != null)
        {
            _detailUI.OnUseClicked -= HandleUseItem;
            _detailUI.OnDeleteClicked -= HandleDeleteItem;
            _detailUI.OnDeselectClicked -= HandleDeselect;
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveListener(ResetAndSpawnItems);
        }
    }

    private void Start()
    {
        ResetAndSpawnItems();
    }

    private void InitGrid(int totalSlots)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            _slotModels.Add(new InventorySlotModel(i));
            SlotUI slotView = Instantiate(_slotPrefab, _gridContainer);
            slotView.Setup(i);
            _slotViews.Add(slotView);
        }
    }

    public void ResetAndSpawnItems()
    {
        HandleDeselect();

        for (int i = 0; i < _slotModels.Count; i++)
        {
            _slotModels[i].Clear();

            // Tỉ lệ 50% xuất hiện item ngẫu nhiên
            if (Random.value > 0.4f && _sampleItemConfigs.Count > 0)
            {
                ItemDataSO randomItem = _sampleItemConfigs[Random.Range(0, _sampleItemConfigs.Count)];
                int randomAmount = Random.Range(1, randomItem.MaxStack > 1 ? Mathf.Min(randomItem.MaxStack, 6) : 2);
                _slotModels[i].SetItem(randomItem, randomAmount);
            }

            RefreshSlotView(i);
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotModels.Count) return;

        var model = _slotModels[slotIndex];
        if (!model.IsEmpty)
        {
            HandleItemClicked(slotIndex);
        }
        else
        {
            HandleDeselect();
        }
    }

    private void HandleItemClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotModels.Count) return;

        _selectedSlotIndex = slotIndex;
        var model = _slotModels[slotIndex];

        // Cập nhật trạng thái Highlight cho toàn bộ các Slots
        for (int i = 0; i < _slotViews.Count; i++)
        {
            _slotViews[i].SetSelected(i == _selectedSlotIndex);
        }

        if (!model.IsEmpty)
        {
            bool canUse = model.ItemData.Type == ItemType.Consumable;
            _detailUI.Render(model.ItemData, model.StackAmount, canUse);
        }
        else
        {
            HandleDeselect();
        }
    }

    private void HandleItemDropped(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= _slotModels.Count || toIndex < 0 || toIndex >= _slotModels.Count) return;

        var fromModel = _slotModels[fromIndex];
        var toModel = _slotModels[toIndex];

        // Hoán đổi dữ liệu ở tầng Model
        ItemDataSO tempItem = fromModel.ItemData;
        int tempAmount = fromModel.StackAmount;

        fromModel.SetItem(toModel.ItemData, toModel.StackAmount);
        toModel.SetItem(tempItem, tempAmount);

        // Đồng bộ hiển thị lại View
        RefreshSlotView(fromIndex);
        RefreshSlotView(toIndex);

        if (_selectedSlotIndex == fromIndex)
        {
            HandleItemClicked(toIndex);
        }
        else if (_selectedSlotIndex == toIndex)
        {
            HandleItemClicked(fromIndex);
        }
        else if (_selectedSlotIndex != -1)
        {
            HandleItemClicked(_selectedSlotIndex);
        }
    }

    private void HandleUseItem()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slotModels.Count) return;
        var model = _slotModels[_selectedSlotIndex];

        if (!model.IsEmpty && model.ItemData.Type == ItemType.Consumable)
        {
            model.ReduceStack(1);
            RefreshSlotView(_selectedSlotIndex);

            if (model.IsEmpty)
            {
                HandleDeselect();
            }
            else
            {
                _detailUI.Render(model.ItemData, model.StackAmount, true);
            }
        }
    }

    private void HandleDeleteItem()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slotModels.Count) return;
        _slotModels[_selectedSlotIndex].Clear();
        RefreshSlotView(_selectedSlotIndex);
        HandleDeselect();
    }

    private void HandleDeselect()
    {
        _selectedSlotIndex = -1;
        for (int i = 0; i < _slotViews.Count; i++)
        {
            if (_slotViews[i] != null)
            {
                _slotViews[i].SetSelected(false);
            }
        }

        if (_detailUI != null)
        {
            _detailUI.Clear();
        }
    }

    private void RefreshSlotView(int index)
    {
        if (index < 0 || index >= _slotModels.Count) return;

        var model = _slotModels[index];
        Transform slotTransform = _slotViews[index].transform;

        if (_itemViews.TryGetValue(index, out ItemView view))
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
            _itemViews.Remove(index);
        }

        if (!model.IsEmpty)
        {
            ItemView newView = Instantiate(_itemPrefab, slotTransform);
            newView.InitDragLayer(_dragLayer);
            newView.ResetToParent(slotTransform);
            newView.Render(index, model.ItemData.Icon, model.StackAmount);
            _itemViews[index] = newView;
        }
    }
}