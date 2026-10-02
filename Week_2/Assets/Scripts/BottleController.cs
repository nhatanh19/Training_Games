using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BottleController : MonoBehaviour
{
    private Vector3 originalPosition; // toạ độ gốc trên giá đỡ
    private bool isSelected = false;   // Cờ đánh dấu ống đang được chọn

    [Header("Tween Settings")]
    [SerializeField] private float liftHeight = 1f;      // Độ cao nhấc lên (Y)
    [SerializeField] private float liftDuration = 0.25f;   // Thời gian nhấc (giây)


    [Header("Visual References")]
    [Tooltip("Danh sách 4 tầng nước xếp từ đáy (0) lên miệng (3)")]
    [SerializeField] private SpriteRenderer[] waterLayers; // 4 tang nuoc

    [Header("VFX & SFX References")]
    [SerializeField] private ParticleSystem completeParticle;

    private bool isLocked = false; // Cờ khóa vĩnh viễn ống khi đã giải xong
    public bool IsLocked => isLocked;

    private Stack<Color> colorStack = new Stack<Color>();
    public const int MAX_CAPACITY = 4;

    // Thuoc tinh kiem tra nhanh so luong nuoc hien tai
    public int CurrentWaterCount => colorStack.Count;
    public bool IsFull => colorStack.Count >= MAX_CAPACITY;
    public bool IsEmpty => colorStack.Count == 0;

    private void Start()
    {
        originalPosition = transform.position;
    }

    public void CompleteBottle()
    {
        isLocked = true; // Khóa tương tác

        if (completeParticle != null)
        {
            completeParticle.Play(); // Nổ hiệu ứng hạt
        }
    }

    public void Select()
    {
        isSelected = true;
        // Dừng các tween di chuyển cũ tránh xung đột vị trí
        transform.DOKill();
        // Di chuyển lên trên toạ độ gốc một đoạn liftHeight
        transform.DOMoveY(originalPosition.y + liftHeight, liftDuration).SetEase(Ease.OutQuad);
    }

    public void Deselect()
    {
        isSelected = false;
        transform.DOKill();
        // Đưa về toạ độ gốc ban đầu
        transform.DOMoveY(originalPosition.y, liftDuration).SetEase(Ease.OutQuad);
    }

    public bool IsCompleted()
    {
        if (colorStack.Count != MAX_CAPACITY) return false;

        Color firstColor = colorStack.Peek();
        foreach (var color in colorStack)
        {
            // So sánh gần đúng các kênh màu RGBA
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

    /// ĐỒNG BỘ GIAO DIỆN (UI/VISUAL SYNC)
    public void UpdateVisual()
    {
        // Chuyển Stack sang mảng để dễ duyệt theo thứ tự từ đáy lên
        // ToArray() của Stack sẽ trả về theo thứ tự từ Đỉnh (Top) xuống Đáy (Bottom),
        // nên ta đảo ngược lại để index 0 khớp với đáy ống (waterLayers[0]).
        Color[] colors = colorStack.ToArray();
        System.Array.Reverse(colors);

        for (int i = 0; i < waterLayers.Length; i++)
        {
            if (i < colors.Length)
            {
                // Nếu có nước ở tầng này: BẬT hiển thị và gán màu
                waterLayers[i].gameObject.SetActive(true);
                waterLayers[i].color = colors[i];
            }
            else
            {
                // Tầng này trống: TẮT hiển thị
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
        // DOShakePosition(thời gian, cường độ rung, tần số rung)
        transform.DOShakePosition(0.35f, new Vector3(0.15f, 0, 0), 15, 90, false, true)
            .OnComplete(() =>
            {
                Deselect(); // Lắc xong thì trượt về chỗ cũ
                onComplete?.Invoke();
            });
    }

    public void PourInto(BottleController targetBottle, System.Action onFinish)
    {
        // 1. Xác định vị trí rót: Nằm chếch lên phía trên miệng ống đích
        // Nếu ống nguồn nằm bên trái ống đích -> bay sang bên trái miệng ống đích và nghiêng về bên phải (+Z hoặc -Z)
        bool isLeft = transform.position.x < targetBottle.transform.position.x;

        float xOffset = isLeft ? -0.55f : 0.55f;
        float targetAngle = isLeft ? -70f : 70f; // Nghiêng 60 - 80 độ

        Vector3 pourPosition = targetBottle.transform.position + new Vector3(xOffset, 1.4f, 0);

        // 2. Tạo chuỗi Sequence
        Sequence pourSeq = DOTween.Sequence();

        // Giai đoạn A: Bay đến miệng ống đích
        pourSeq.Append(transform.DOMove(pourPosition, 0.4f).SetEase(Ease.OutQuad));

        // Giai đoạn B: Nghiêng ống để rót
        pourSeq.Append(transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f).SetEase(Ease.InOutSine));

        // Giai đoạn C: Thực hiện tráo đổi màu nước giữa 2 ống
        pourSeq.AppendCallback(() =>
        {
            Color colorToTransfer = RemoveWater();     // Ống nguồn mất 1 tầng
            targetBottle.AddWater(colorToTransfer);    // Ống đích nhận 1 tầng

            // Gọi âm thanh rót nước (sẽ nối ở GameManager)
            GameManager.Instance.PlayPourSFX();
        });

        // Tạm dừng 0.25 giây ở trạng thái nghiêng để người chơi cảm nhận dòng nước chảy
        pourSeq.AppendInterval(0.25f);

        // Giai đoạn D: Xoay thẳng đứng lại về 0 độ
        pourSeq.Append(transform.DORotate(Vector3.zero, 0.25f).SetEase(Ease.InSine));

        // Giai đoạn E: Bay về vị trí ban đầu trên giá đỡ
        pourSeq.Append(transform.DOMove(originalPosition, 0.35f).SetEase(Ease.OutQuad));

        // Khi toàn bộ chuỗi hoàn tất:
        pourSeq.OnComplete(() =>
        {
            onFinish?.Invoke(); // Gọi callback báo xong để GameManager mở khóa click
        });
    }

    public void ResetState()
    {
        transform.DOKill();                 // Hủy mọi tween đang dính trên ống này
        transform.position = originalPosition; // Trả về tọa độ gốc
        transform.rotation = Quaternion.identity; // Dựng thẳng đứng
        isLocked = false;                   // Mở khóa tương tác
        isSelected = false;
        colorStack.Clear();                 // Xóa dữ liệu cũ
        UpdateVisual();                     // Tắt toàn bộ hiển thị nước
    }
}
