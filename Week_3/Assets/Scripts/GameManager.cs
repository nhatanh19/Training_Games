using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    [SerializeField] private AudioClip winSFX;

    [Header("UI References")]
    [SerializeField] private GameObject winPopup;

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

    void SetupLevel()
    {
        bottles[0].Initialize(new List<Color>() { colorRed, colorRed, colorGreen, colorGreen });
        bottles[1].Initialize(new List<Color>() { colorBlue, colorBlue, colorRed, colorRed });
        bottles[2].Initialize(new List<Color>() { colorGreen, colorGreen, colorBlue, colorBlue });
        bottles[3].Initialize(new List<Color>());

        if (winPopup != null)
        {
            winPopup.SetActive(false);
        }
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

        if (selectedBottle == clickedBottle)
        {
            selectedBottle.Deselect();
            selectedBottle = null;
            PlaySFX(selectSFX);
            return;
        }

        if (CanPour(selectedBottle, clickedBottle))
        {
            isBusy = true;
            BottleController source = selectedBottle;
            BottleController target = clickedBottle;
            selectedBottle = null;

            source.PourInto(target, () =>
            {
                if (target.IsCompleted() && !target.IsLocked)
                {
                    target.CompleteBottle();
                    PlaySFX(completeSFX);
                }

                if (CheckWinCondition())
                {
                    OnGameWon();
                }

                isBusy = false;
            });
        }
        else
        {
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

    private bool CheckWinCondition()
    {
        foreach (var bottle in bottles)
        {
            if (!bottle.IsEmpty && !bottle.IsCompleted())
            {
                return false;
            }
        }
        return true;
    }

    private void OnGameWon()
    {
        Debug.Log("YOU WIN!");
        PlaySFX(winSFX);

        if (winPopup != null)
        {
            winPopup.SetActive(true);
        }
    }

    public void ResetLevel()
    {
        DOTween.KillAll();

        isBusy = false;
        selectedBottle = null;

        foreach (var bottle in bottles)
        {
            bottle.ResetState();
        }

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