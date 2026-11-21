using System;
using System.Collections.Generic;
using UnityEngine;

public class BusStopManager : MonoBehaviour
{
    public static BusStopManager Instance { get; private set; }

    [Header("7 Diagonal Bays (Ordered Left to Right in Scene)")]
    [SerializeField] private List<BusStopSlot> slots = new List<BusStopSlot>();
    [SerializeField] private List<int> unlockOrder = new List<int> { 0, 1, 2, 3, 4, 5, 6 };

    [Header("Exit Roadway (Authored in Scene)")]
    [SerializeField] private Transform leftExitPoint;
    [SerializeField] private Transform rightExitPoint;
    [SerializeField] private float apronExitY = 2.55f;

    public event Action<BusController> OnBusArrivedAtStop;
    public event Action OnBusDepartedFromStop;

    public IReadOnlyList<BusStopSlot> Slots => slots;
    public float ApronExitY => apronExitY;
    public bool HasAvailableSlot => GetAvailableSlot() != null;
    public int OccupiedSlotCount => slots.FindAll(slot => slot != null && !slot.IsAvailable).Count;
    public int TotalSlotCount => slots.Count;
    public int UnlockedSlotCount => slots.FindAll(slot => slot != null && !slot.IsLocked).Count;
    public bool HasLockedSlot => slots.Exists(slot => slot != null && slot.IsLocked);
    public bool HasBusesInTransit => slots.Exists(slot =>
        slot != null && slot.CurrentBus != null && !slot.CurrentBus.IsReadyForPassengers);

    public int ExtraUnlockCount { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void ConfigureSlots(int unlockedCount)
    {
        ExtraUnlockCount = 0;
        if (slots == null || slots.Count == 0) return;

        int targetUnlocked = Mathf.Clamp(unlockedCount, 1, slots.Count);
        var unlockedSet = new HashSet<int>();

        if (unlockOrder != null && unlockOrder.Count > 0)
        {
            for (int i = 0; i < unlockOrder.Count && unlockedSet.Count < targetUnlocked; i++)
            {
                int idx = unlockOrder[i];
                if (idx >= 0 && idx < slots.Count) unlockedSet.Add(idx);
            }
        }

        for (int i = 0; i < slots.Count && unlockedSet.Count < targetUnlocked; i++)
        {
            unlockedSet.Add(i);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetLocked(!unlockedSet.Contains(i));
            }
        }

        UpdateNearFullWarning();
    }

    public bool UnlockOneSlot()
    {
        if (unlockOrder != null)
        {
            for (int i = 0; i < unlockOrder.Count; i++)
            {
                int idx = unlockOrder[i];
                if (idx >= 0 && idx < slots.Count && slots[idx] != null && slots[idx].IsLocked)
                {
                    return UnlockSlot(slots[idx]);
                }
            }
        }

        foreach (BusStopSlot slot in slots)
        {
            if (slot != null && slot.IsLocked)
            {
                return UnlockSlot(slot);
            }
        }
        return false;
    }

    public bool UnlockSlot(BusStopSlot slot)
    {
        if (slot == null || !slot.IsLocked || !slots.Contains(slot)) return false;
        slot.SetLocked(false);
        ExtraUnlockCount++;
        AudioManager.Instance?.PlayUnlock();
        UpdateNearFullWarning();
        return true;
    }

    public BusStopSlot GetAvailableSlot()
    {
        return slots.Find(slot => slot != null && slot.IsAvailable);
    }

    public Vector3 GetExitPoint(BusStopSlot slot)
    {
        int index = slot != null ? slots.IndexOf(slot) : -1;
        bool goLeft = index >= 0 && index < slots.Count / 2;

        if (goLeft && leftExitPoint != null) return leftExitPoint.position;
        if (!goLeft && rightExitPoint != null) return rightExitPoint.position;
        if (rightExitPoint != null) return rightExitPoint.position;
        if (leftExitPoint != null) return leftExitPoint.position;

        return new Vector3(goLeft ? -6.5f : 6.5f, apronExitY, 0f);
    }

    public void UpdateNearFullWarning()
    {
        int availableCount = 0;
        BusStopSlot lastAvailable = null;

        for (int i = 0; i < slots.Count; i++)
        {
            BusStopSlot slot = slots[i];
            if (slot != null && slot.IsAvailable)
            {
                availableCount++;
                lastAvailable = slot;
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            BusStopSlot slot = slots[i];
            if (slot != null)
            {
                slot.SetNearFullWarning(availableCount == 1 && slot == lastAvailable);
            }
        }
    }

    public void ShowFullFeedback()
    {
        foreach (BusStopSlot slot in slots)
        {
            if (slot != null) slot.PlayFullFeedback();
        }
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
        if (slots != null)
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    Matrix4x4 prev = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(slot.transform.position, slot.transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.55f, 1.05f, 0.05f));
                    Gizmos.matrix = prev;
                }
            }
        }

        Gizmos.color = UnityEngine.Color.magenta;
        if (leftExitPoint != null) Gizmos.DrawWireSphere(leftExitPoint.position, 0.25f);
        if (rightExitPoint != null) Gizmos.DrawWireSphere(rightExitPoint.position, 0.25f);
    }
}
