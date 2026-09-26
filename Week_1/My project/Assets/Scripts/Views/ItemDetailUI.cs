using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _stackText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Action Buttons")]
    [SerializeField] private Button _useButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Button _deselectButton;

    // Sự kiện gửi cho Controller xử lý logic (Observer Pattern)
    public event Action OnUseClicked;
    public event Action OnDeleteClicked;
    public event Action OnDeselectClicked;

    private void Awake()
    {
        if (_useButton != null)
            _useButton.onClick.AddListener(() => OnUseClicked?.Invoke());
        if (_deleteButton != null)
            _deleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke());
        if (_deselectButton != null)
            _deselectButton.onClick.AddListener(() => OnDeselectClicked?.Invoke());
    }

    public void Render(ItemDataSO data, int stackAmount, bool canUse)
    {
        if (data == null || stackAmount <= 0)
        {
            Clear();
            return;
        }

        if (_iconImage != null)
        {
            _iconImage.gameObject.SetActive(true);
            _iconImage.sprite = data.Icon;
            _iconImage.preserveAspect = true;
        }

        if (_nameText != null)
            _nameText.text = data.ItemName;

        if (_typeText != null)
            _typeText.text = $"Loại: <color=#FFD700>{data.Type}</color>";

        if (_stackText != null)
            _stackText.text = $"Số lượng: <color=#67C23A>{stackAmount}</color> / {data.MaxStack}";

        if (_descriptionText != null)
            _descriptionText.text = data.Description;

        // Quy tắc: Chỉ Consumable mới dùng được
        if (_useButton != null)
            _useButton.interactable = canUse;
        if (_deleteButton != null)
            _deleteButton.interactable = true;
        if (_deselectButton != null)
            _deselectButton.interactable = true;
    }

    public void Clear()
    {
        if (_iconImage != null)
            _iconImage.gameObject.SetActive(false);

        if (_nameText != null)
            _nameText.text = "Chưa chọn vật phẩm";

        if (_typeText != null)
            _typeText.text = string.Empty;

        if (_stackText != null)
            _stackText.text = string.Empty;

        if (_descriptionText != null)
            _descriptionText.text = "Hãy chạm vào một ô vật phẩm để xem thông tin chi tiết.";

        if (_useButton != null)
            _useButton.interactable = false;
        if (_deleteButton != null)
            _deleteButton.interactable = false;
        if (_deselectButton != null)
            _deselectButton.interactable = false;
    }
}