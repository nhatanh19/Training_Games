using UnityEngine;

public class BusStopSlot : MonoBehaviour
{
    public Transform SlotTransform => transform;
    public BusController CurrentBus { get; private set; }

    public bool IsAvailable => CurrentBus == null;

    public void AssignBus(BusController bus)
    {
        CurrentBus = bus;
    }

    public void ClearSlot()
    {
        CurrentBus = null;
    }
}
