using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BottleController : MonoBehaviour
{
    private Vector3 originalPosition;
    private bool isSelected = false;

    [Header("Tween Settings")]
    [SerializeField] private float liftHeight = 1f;
    [SerializeField] private float liftDuration = 0.25f;

    [Header("Visual References")]
    [SerializeField] private SpriteRenderer[] waterLayers;

    [Header("VFX & SFX References")]
    [SerializeField] private ParticleSystem completeParticle;
    
    [Header("Mouth Reference")]
    [SerializeField] private Transform _mouthPoint;

    private bool isLocked = false;
    public bool IsLocked => isLocked;

    private Stack<Color> colorStack = new Stack<Color>();
    public const int MAX_CAPACITY = 4;

    public int CurrentWaterCount => colorStack.Count;
    public bool IsFull => colorStack.Count >= MAX_CAPACITY;
    public bool IsEmpty => colorStack.Count == 0;

    private void Start()
    {
        originalPosition = transform.position;
    }

    public void CompleteBottle()
    {
        isLocked = true;

        if (completeParticle != null)
        {
            completeParticle.Play();
        }
    }

    public void Select()
    {
        isSelected = true;
        transform.DOKill();
        transform.DOMoveY(originalPosition.y + liftHeight, liftDuration).SetEase(Ease.OutQuad);
    }

    public void Deselect()
    {
        isSelected = false;
        transform.DOKill();
        transform.DOMoveY(originalPosition.y, liftDuration).SetEase(Ease.OutQuad);
    }

    public bool IsCompleted()
    {
        if (colorStack.Count != MAX_CAPACITY) return false;

        Color firstColor = colorStack.Peek();
        foreach (var color in colorStack)
        {
            if (!Mathf.Approximately(color.r, firstColor.r) ||
                !Mathf.Approximately(color.g, firstColor.g) ||
                !Mathf.Approximately(color.b, firstColor.b))
            {
                return false;
            }
        }
        return true;
    }

    public void Initialize(List<Color> initialColors)
    {
        colorStack.Clear();

        if (initialColors != null)
        {
            foreach (var color in initialColors)
            {
                if (colorStack.Count < MAX_CAPACITY)
                {
                    colorStack.Push(color);
                }
            }
        }

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        Color[] colors = colorStack.ToArray();
        System.Array.Reverse(colors);

        for (int i = 0; i < waterLayers.Length; i++)
        {
            if (i < colors.Length)
            {
                waterLayers[i].gameObject.SetActive(true);
                waterLayers[i].color = colors[i];
            }
            else
            {
                waterLayers[i].gameObject.SetActive(false);
            }
        }
    }

    public Color PeekColor()
    {
        return colorStack.Peek();
    }

    public void AddWater(Color color)
    {
        if (!IsFull)
        {
            colorStack.Push(color);
            UpdateVisual();
        }
    }

    public Color RemoveWater()
    {
        if (!IsEmpty)
        {
            Color removedColor = colorStack.Pop();
            UpdateVisual();
            return removedColor;
        }
        return Color.clear;
    }

    public void PlayShakeError(System.Action onComplete = null)
    {
        transform.DOKill();
        transform.DOShakePosition(0.35f, new Vector3(0.15f, 0, 0), 15, 90, false, true)
            .OnComplete(() =>
            {
                Deselect();
                onComplete?.Invoke();
            });
    }

    public void PourInto(BottleController targetBottle, System.Action onFinish)
    {
        int sourceColorCount = GetTopColorCount();
        int targetSpace = targetBottle.AvailableSpace;
        int amountToPour = Mathf.Min(sourceColorCount, targetSpace);

        bool isLeft = transform.position.x < targetBottle.transform.position.x;

        float xOffset = isLeft ? -0.55f : 0.55f;
        float targetAngle = isLeft ? -70f : 70f;

        Vector3 pourPosition = targetBottle.MouthPosition + new Vector3(xOffset, 0.2f, 0);

        Sequence pourSeq = DOTween.Sequence();

        pourSeq.Append(transform.DOMove(pourPosition, 0.4f).SetEase(Ease.OutQuad));
        pourSeq.Append(transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f).SetEase(Ease.InOutSine));
        pourSeq.AppendCallback(() =>
        {
            for (int i = 0; i < amountToPour; i++)
            {
                Color colorToTransfer = RemoveWater();
                targetBottle.AddWater(colorToTransfer);
            }
            WaterSortController.Instance.PlayPourSFX();
        });
        pourSeq.AppendInterval(0.25f);
        pourSeq.Append(transform.DORotate(Vector3.zero, 0.25f).SetEase(Ease.InSine));
        pourSeq.Append(transform.DOMove(originalPosition, 0.35f).SetEase(Ease.OutQuad));
        pourSeq.OnComplete(() =>
        {
            onFinish?.Invoke();
        });
    }


    //=====================
    public void SetupBottle(Vector3 spawnPosition, List<Color> initialColors)
    {
        transform.DOKill();
        originalPosition = spawnPosition;
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        isLocked = false;
        isSelected = false;
        Initialize(initialColors);
    }
    public void ResetState()
    {
        transform.DOKill();
        transform.rotation = Quaternion.identity;
        isLocked = false;
        isSelected = false;
        if (completeParticle != null)
        {
            completeParticle.Stop();
            completeParticle.Clear();
        }
        colorStack.Clear();
        UpdateVisual();
    }
    //=====================


    public void SetOriginPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        transform.position = newPos;
    }

    public int GetTopColorCount(){
        if(IsEmpty) return 0;
        Color topColor = colorStack.Peek();
        int count  = 0;
        foreach (Color color in colorStack){
            if(color == topColor){
                count++;
            } else{
                break;
            }
        }
        return count;
    }

    //lấy dung tích còn lại của ống
    public int AvailableSpace => MAX_CAPACITY - CurrentWaterCount;

    //lấy tọa độ miệng ống
    public Vector3 MouthPosition => _mouthPoint != null ? _mouthPoint.position : transform.position + Vector3.up * 1.5f;

    private void OnDestroy(){
        transform.DOKill();
    }
}
