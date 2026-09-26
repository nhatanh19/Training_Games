using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _highlightObject;

    public int SlotIndex { get; private set; }

    // Sự kiện bắn về Controller (Observer Pattern)
    public static event Action<int, int> OnItemDropped;
    public static event Action<int> OnSlotClicked;

    public void Setup(int slotIndex)
    {
        SlotIndex = slotIndex;
        gameObject.name = $"Slot_{slotIndex}";
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Khi click vào ô slot (kể cả ô trống)
        OnSlotClicked?.Invoke(SlotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ItemView draggedItem = eventData.pointerDrag.GetComponent<ItemView>();
            if (draggedItem != null)
            {
                // Báo cho Controller biết vật phẩm từ draggedItem.SlotIndex được thả vào ô this.SlotIndex
                OnItemDropped?.Invoke(draggedItem.SlotIndex, this.SlotIndex);
            }
        }
    }
}