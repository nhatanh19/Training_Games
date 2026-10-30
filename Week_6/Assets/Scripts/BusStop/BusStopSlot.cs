using UnityEngine;

public class BusStopSlot : MonoBehaviour
{
    [SerializeField] private SpriteRenderer slotRenderer;

    public Transform SlotTransform => transform;
    public BusController CurrentBus { get; private set; }

    public bool IsAvailable => CurrentBus == null;

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
