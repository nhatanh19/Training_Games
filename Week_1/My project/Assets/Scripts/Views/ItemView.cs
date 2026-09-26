using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _stackText;
    [SerializeField] private CanvasGroup _canvasGroup;

    public CanvasGroup CanvasGroup => _canvasGroup;
    public int SlotIndex { get; private set; }

    // Sự kiện click (Observer Pattern)
    public static event Action<int> OnItemClicked;

    private Transform _originalParent;
    private Canvas _rootCanvas;
    private Transform _dragLayer;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    public void InitDragLayer(Transform dragLayer)
    {
        _dragLayer = dragLayer;
    }

    public void Render(int slotIndex, Sprite icon, int stackAmount)
    {
        SlotIndex = slotIndex;
        if (icon != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = true;
            _iconImage.preserveAspect = true;
        }
        else
        {
            _iconImage.enabled = false;
        }

        if (_stackText != null)
        {
            // Hiển thị định dạng x91, x18... giống ảnh mẫu tham chiếu
            _stackText.text = stackAmount > 1 ? $"x{stackAmount}" : string.Empty;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemClicked?.Invoke(SlotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;

        // Đưa item lên layer kéo trên cùng để không bị các UI khác che khuất
        if (_dragLayer != null)
        {
            transform.SetParent(_dragLayer);
        }
        else if (_rootCanvas != null)
        {
            transform.SetParent(_rootCanvas.transform);
        }
        transform.SetAsLastSibling();

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1.0f;

        // Nếu item chưa được reparent về slot mới thì hoàn vị trí về lại slot cũ
        if (transform.parent == _dragLayer || (_rootCanvas != null && transform.parent == _rootCanvas.transform))
        {
            ResetToParent(_originalParent);
        }
    }

    public void ResetToParent(Transform newParent)
    {
        _originalParent = newParent;
        transform.SetParent(newParent);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}