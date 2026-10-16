using System;
using System.Collections.Generic;
using UnityEngine;

public class BusStopManager : MonoBehaviour
{
    public static BusStopManager Instance { get; private set; }

    [SerializeField] private List<BusStopSlot> slots = new List<BusStopSlot>();

    public event Action<BusController> OnBusArrivedAtStop;

    public bool HasAvailableSlot => GetAvailableSlot() != null;

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

        if (slots.Count == 0)
        {
            slots.AddRange(GetComponentsInChildren<BusStopSlot>());
        }
    }

    public BusStopSlot GetAvailableSlot()
    {
        return slots.Find(slot => slot.IsAvailable);
    }

    public List<BusController> GetActiveBuses()
    {
        List<BusController> activeBuses = new List<BusController>();
        foreach (var slot in slots)
        {
            if (slot != null && slot.CurrentBus != null)
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

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.yellow;
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                Gizmos.DrawWireCube(slot.transform.position, new Vector3(2.2f, 1f, 0.1f));
            }
        }
    }
}
