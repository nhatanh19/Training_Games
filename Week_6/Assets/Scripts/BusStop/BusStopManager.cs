using System;
using System.Collections.Generic;
using UnityEngine;

public class BusStopManager : MonoBehaviour
{
    public static BusStopManager Instance { get; private set; }

    [SerializeField] private float bayY = 3.25f;

    [SerializeField] private float bayYOffset = 0.30f;
    [SerializeField] private Vector2 slotScale = new Vector2(0.85f, 0.74f);
    [SerializeField] private List<BusStopSlot> slots = new List<BusStopSlot>();

    public event Action<BusController> OnBusArrivedAtStop;
    public event Action OnBusDepartedFromStop;

    public bool HasAvailableSlot => GetAvailableSlot() != null;
    public int OccupiedSlotCount => slots.FindAll(slot => !slot.IsAvailable).Count;
    public int TotalSlotCount => slots.Count;
    public int UnlockedSlotCount => slots.FindAll(slot => slot != null && !slot.IsLocked).Count;

    public int ExtraUnlockCount { get; private set; }

    public bool UnlockOneSlot()
    {
        BusStopSlot best = null;
        float bestDist = float.MaxValue;
        foreach (BusStopSlot slot in slots)
        {
            if (slot == null || !slot.IsLocked) continue;
            float d = Mathf.Abs(slot.transform.position.x);
            if (d < bestDist) { bestDist = d; best = slot; }
        }
        if (best == null) return false;
        best.SetLocked(false);
        ExtraUnlockCount++;
        return true;
    }

    public void ConfigureSlots(int unlockedCount)
    {
        SetupSlots(allowCreate: false);
        ExtraUnlockCount = 0;
        if (slots == null || slots.Count == 0) return;

        unlockedCount = Mathf.Clamp(unlockedCount, 1, slots.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;

            bool isUnlocked = false;
            switch (unlockedCount)
            {
                case 1:
                    isUnlocked = (i == 2);
                    break;
                case 2:
                    isUnlocked = (i == 1 || i == 2);
                    break;
                case 3:
                    isUnlocked = (i >= 1 && i <= 3);
                    break;
                case 4:
                    isUnlocked = (i <= 3);
                    break;
                case 5:
                default:
                    isUnlocked = true;
                    break;
            }

            slots[i].SetLocked(!isUnlocked);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupSlots();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SetupSlots(allowCreate: false);
    }
#endif

    public void SetupSlots(bool allowCreate = true)
    {
        if (slots == null) slots = new List<BusStopSlot>();
        slots.RemoveAll(s => s == null);

        if (slots.Count == 0)
        {
            slots.AddRange(GetComponentsInChildren<BusStopSlot>());
        }

        float[] xPositions = new float[] { -2.6f, -1.3f, 0f, 1.3f, 2.6f };
        float bayY = this.bayY + bayYOffset;

        for (int i = 0; i < xPositions.Length; i++)
        {
            BusStopSlot slot = null;
            if (i < slots.Count)
            {
                slot = slots[i];
            }
            else if (allowCreate)
            {
                GameObject slotGo = new GameObject("Slot_" + i);
                slotGo.transform.SetParent(transform, false);
                slot = slotGo.AddComponent<BusStopSlot>();
                slots.Add(slot);
            }
            if (slot != null)
            {
                slot.transform.position = new Vector3(xPositions[i], bayY, 0f);
                slot.transform.localScale = new Vector3(slotScale.x, slotScale.y, 1f);
            }
        }
    }

    public BusStopSlot GetAvailableSlot()
    {
        return slots.Find(slot => slot != null && slot.IsAvailable);
    }

    public List<BusController> GetActiveBuses()
    {
        List<BusController> activeBuses = new List<BusController>();
        foreach (var slot in slots)
        {
            if (slot != null && slot.CurrentBus != null && slot.CurrentBus.IsReadyForPassengers)
            {
                activeBuses.Add(slot.CurrentBus);
            }
        }
        return activeBuses;
    }

    public void NotifyBusArrived(BusController bus)
    {
        OnBusArrivedAtStop?.Invoke(bus);
    }

    public void NotifyBusDeparted()
    {
        OnBusDepartedFromStop?.Invoke();
    }

    public void ClearAllSlots()
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.yellow;
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                Gizmos.DrawWireCube(slot.transform.position, new Vector3(0.92f, 2.2f, 0.1f));
            }
        }
    }
}
