using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Import DOTween để dùng DOTween.KillAll()

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefab & Spawning")]
    [SerializeField] private BottleController bottlePrefab;
    [SerializeField] private float bottleSpacing = 2.0f;

    [Header("Level Colors Config")]
    [SerializeField] private Color colorRed = new Color(0.9f, 0.25f, 0.25f);
    [SerializeField] private Color colorGreen = new Color(0.25f, 0.85f, 0.35f);
    [SerializeField] private Color colorBlue = new Color(0.25f, 0.55f, 0.95f);

    [Header("Audio SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSFX;
    [SerializeField] private AudioClip pourSFX;
    [SerializeField] private AudioClip errorSFX;
    [SerializeField] private AudioClip completeSFX;
    [SerializeField] private AudioClip winSFX; // Âm thanh chiến thắng

    [Header("UI References (Task 5)")]
    [SerializeField] private GameObject winPopup; // Kéo WinPopup vào đây

    private BottleController selectedBottle = null;
    public bool isBusy = false;

    private List<BottleController> bottles = new List<BottleController>();
    private Camera mainCamera;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        SpawnBottles();
        SetupLevel();
    }

    void Update()
    {
        if (isBusy) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void SpawnBottles()
    {
        float startX = -((4 - 1) * bottleSpacing) / 2f;

        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = new Vector3(startX + (i * bottleSpacing), 0, 0);
            BottleController newBottle = Instantiate(bottlePrefab, spawnPos, Quaternion.identity);
            newBottle.name = $"Bottle_{i + 1}";
            bottles.Add(newBottle);
        }
    }

    /// <summary>
    /// Khởi tạo dữ liệu màu theo đúng đề bài Level 1
    /// </summary>
    void SetupLevel()
    {
        // Ống 1: 3 màu xáo trộn
        bottles[0].Initialize(new List<Color>() { colorRed, colorRed, colorGreen, colorGreen });

        // Ống 2: 3 màu xáo trộn
        bottles[1].Initialize(new List<Color>() { colorBlue, colorBlue, colorRed, colorRed });

        bottles[2].Initialize(new List<Color>() { colorGreen, colorGreen,colorBlue, colorBlue});
        bottles[3].Initialize(new List<Color>());

        // Đảm bảo WinPopup ẩn khi bắt đầu
        if (winPopup != null) winPopup.SetActive(false);
    }

    void HandleClick()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 clickPosition = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(clickPosition, Vector2.zero);

        if (hit.collider != null)
        {
            BottleController clickedBottle = hit.collider.GetComponent<BottleController>();
            if (clickedBottle != null)
            {
                OnBottleClicked(clickedBottle);
            }
        }
    }

    void OnBottleClicked(BottleController clickedBottle)
    {
        // 1. Chọn ống nguồn
        if (selectedBottle == null)
        {
            if (!clickedBottle.IsEmpty && !clickedBottle.IsCompleted() && !clickedBottle.IsLocked)
            {
                selectedBottle = clickedBottle;
                selectedBottle.Select();
                PlaySFX(selectSFX);
            }
            return;
        }

        // 2. Click lại chính nó
        if (selectedBottle == clickedBottle)
        {
            selectedBottle.Deselect();
            selectedBottle = null;
            PlaySFX(selectSFX);
            return;
        }

        // 3. Rót nước hợp lệ
        if (CanPour(selectedBottle, clickedBottle))
        {
            isBusy = true; // Khóa input
            BottleController source = selectedBottle;
            BottleController target = clickedBottle;
            selectedBottle = null;

            source.PourInto(target, () =>
            {
                // Kiểm tra hoàn thành ống đơn lẻ (Task 4)
                if (target.IsCompleted() && !target.IsLocked)
                {
                    target.CompleteBottle();
                    PlaySFX(completeSFX);
                }

                // KIỂM TRA ĐIỀU KIỆN THẮNG TOÀN CỤC (Task 5)
                if (CheckWinCondition())
                {
                    OnGameWon();
                }

                isBusy = false; // Mở khóa input sau khi kết thúc lượt
            });
        }
        else
        {
            // Click không rót được: đổi nguồn hoặc báo lỗi rung
            selectedBottle.Deselect();

            if (!clickedBottle.IsEmpty && !clickedBottle.IsCompleted() && !clickedBottle.IsLocked)
            {
                selectedBottle = clickedBottle;
                selectedBottle.Select();
                PlaySFX(selectSFX);
            }
            else
            {
                isBusy = true;
                PlaySFX(errorSFX);

                selectedBottle.PlayShakeError(() =>
                {
                    selectedBottle = null;
                    isBusy = false;
                });
            }
        }
    }

    public bool CanPour(BottleController fromBottle, BottleController toBottle)
    {
        if (toBottle.IsFull) return false;
        if (toBottle.IsEmpty) return true;
        return fromBottle.PeekColor() == toBottle.PeekColor();
    }

    /// <summary>
    /// Kiểm tra chiến thắng: Tất cả các ống hoặc là RỖNG hoặc đã ĐỦ 4 TẦNG CÙNG MÀU
    /// </summary>
    private bool CheckWinCondition()
    {
        foreach (var bottle in bottles)
        {
            // Nếu có 1 ống vừa không rỗng, vừa chưa hoàn thành -> Chưa thắng
            if (!bottle.IsEmpty && !bottle.IsCompleted())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Xử lý khi người chơi thắng cuộc
    /// </summary>
    private void OnGameWon()
    {
        Debug.Log("YOU WIN!");
        PlaySFX(winSFX);

        if (winPopup != null)
        {
            winPopup.SetActive(true); // Bật Popup chúc mừng[cite: 1]
        }
    }

    /// <summary>
    /// Sự kiện bấm nút Reset Level[cite: 1]
    /// </summary>
    public void ResetLevel()
    {
        // 1. Hủy sạch mọi tween đang chạy dở để tránh xung đột[cite: 1]
        DOTween.KillAll();

        // 2. Reset các biến cờ trạng thái[cite: 1]
        isBusy = false;
        selectedBottle = null;

        // 3. Reset trạng thái từng ống[cite: 1]
        foreach (var bottle in bottles)
        {
            bottle.ResetState();
        }

        // 4. Nạp lại cấu hình ban đầu[cite: 1]
        SetupLevel();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayPourSFX() => PlaySFX(pourSFX);
}