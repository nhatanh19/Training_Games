using TMPro;
using UnityEngine;

public class BusStopSlot : MonoBehaviour
{
    [SerializeField] private SpriteRenderer slotRenderer;

    private TextMeshPro _lockLabel;
    private BoxCollider2D _clickCollider;

    public Transform SlotTransform => transform;
    public BusController CurrentBus { get; private set; }
    public bool IsLocked { get; private set; } = false;

    public bool IsAvailable => !IsLocked && CurrentBus == null;

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        EnsureSlotVisual();
        if (slotRenderer != null)
        {
            string spritePath = locked ? "Park/park_slot_lock" : "Park/park_slot_unlock";
            Sprite sp = Resources.Load<Sprite>(spritePath);
            if (sp != null)
            {
                slotRenderer.sprite = sp;
            }
            slotRenderer.color = Color.white;
        }
        EnsureClickCollider();
        RefreshLockLabel();
    }

    public void OnLockedSlotClicked()
    {
        if (!IsLocked) return;
        AudioManager.Instance?.PlayClick();
        HapticManager.Instance?.TriggerLight();
        UIManager.Instance?.OpenContinuePopup(this);
    }

    private void EnsureClickCollider()
    {
        if (_clickCollider == null)
        {
            _clickCollider = GetComponent<BoxCollider2D>();
            if (_clickCollider == null)
            {
                _clickCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        _clickCollider.size = new Vector2(1.25f, 2.50f);
        _clickCollider.offset = Vector2.zero;
        _clickCollider.enabled = IsLocked;
    }

    private void OnEnable()
    {
        SettingsController.LanguageChanged += OnLanguageChanged;
        EnsureClickCollider();
        RefreshLockLabel();
    }

    private void OnDisable()
    {
        SettingsController.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string language)
    {
        RefreshLockLabel();
    }

    private void RefreshLockLabel()
    {
        if (!IsLocked)
        {
            if (_lockLabel != null) _lockLabel.gameObject.SetActive(false);
            return;
        }

        if (_lockLabel == null)
        {
            Transform existing = transform.Find("LockLabel");
            GameObject go = existing != null ? existing.gameObject : new GameObject("LockLabel", typeof(TextMeshPro));
            go.transform.SetParent(transform, false);
            _lockLabel = go.GetComponent<TextMeshPro>();
        }

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SettingsDynamic SDF");
        if (font != null) _lockLabel.font = font;
        _lockLabel.transform.localPosition = new Vector3(0f, -0.22f, 0f);
        _lockLabel.fontSize = 1.65f;
        _lockLabel.lineSpacing = -12f;
        _lockLabel.fontStyle = FontStyles.Bold;
        _lockLabel.alignment = TextAlignmentOptions.Center;
        _lockLabel.color = Color.white;
        _lockLabel.rectTransform.sizeDelta = new Vector2(0.95f, 0.55f);
        _lockLabel.enableWordWrapping = false;
        _lockLabel.GetComponent<MeshRenderer>().sortingOrder = -9;

        Vector3 ls = transform.lossyScale;
        _lockLabel.transform.localScale = new Vector3(
            ls.x > 0.01f ? 1f / ls.x : 1f, ls.y > 0.01f ? 1f / ls.y : 1f, 1f);
        _lockLabel.text = SettingsController.IsVietnameseSaved ? "MỞ\nKHÓA" : "UNLOCK";
        _lockLabel.gameObject.SetActive(true);
    }

    private void Awake()
    {
        EnsureSlotVisual();
    }

    public void EnsureSlotVisual()
    {
        if (slotRenderer == null)
        {
            slotRenderer = GetComponent<SpriteRenderer>();
            if (slotRenderer == null)
            {
                slotRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (slotRenderer != null)
        {
            if (slotRenderer.sprite == null)
            {
                Sprite sp = Resources.Load<Sprite>("Park/park_slot_unlock");
                if (sp != null)
                {
                    slotRenderer.sprite = sp;
                }
            }
            slotRenderer.sortingOrder = -10;
        }
    }

    public void AssignBus(BusController bus)
    {
        CurrentBus = bus;
    }

    public void ClearSlot()
    {
        CurrentBus = null;
    }
}
