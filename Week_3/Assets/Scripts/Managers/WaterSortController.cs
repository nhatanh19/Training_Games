using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WaterSortController : MonoBehaviour
{
    public static WaterSortController Instance { get; private set; }

    [Header("Audio SFX")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _selectSFX;
    [SerializeField] private AudioClip _pourSFX;
    [SerializeField] private AudioClip _errorSFX;
    [SerializeField] private AudioClip _completeSFX;
    [SerializeField] private AudioClip _winSFX;

    [Header("UI Reference")]
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _resetToLevelOneButton;
    [SerializeField] private WinPopupUI _winPopupUI;

    private List<BottleController> _bottles = new List<BottleController>();
    private BottleController _selectedBottle = null;
    private bool _isBusy = false;
    private Camera _mainCamera;

    void Awake()
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

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        _mainCamera = Camera.main;
    }

    public void OnLevelLoaded(List<BottleController> newBottles)
    {
        DOTween.KillAll();
        _isBusy = false;
        _selectedBottle = null;
        _bottles = newBottles;
        if (_winPopupUI != null)
        {
            _winPopupUI.Hide();
        }
    }

    void Update()
    {
        if (_isBusy || _bottles == null || _bottles.Count == 0) return;
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
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

    private void OnBottleClicked(BottleController clickedBottle)
    {
        if (_selectedBottle == null)
        {
            if (!clickedBottle.IsEmpty && !clickedBottle.IsCompleted() && !clickedBottle.IsLocked)
            {
                _selectedBottle = clickedBottle;
                _selectedBottle.Select();
                PlaySFX(_selectSFX);
            }
            return;
        }

        if (_selectedBottle == clickedBottle)
        {
            _selectedBottle.Deselect();
            _selectedBottle = null;
            PlaySFX(_selectSFX);
            return;
        }

        if (CanPour(_selectedBottle, clickedBottle))
        {
            _isBusy = true;
            BottleController source = _selectedBottle;
            BottleController target = clickedBottle;
            _selectedBottle = null;
            source.PourInto(target, () =>
            {
                if (target.IsCompleted() && !target.IsLocked)
                {
                    target.CompleteBottle();
                    PlaySFX(_completeSFX);
                }
                if (CheckWinCondition())
                {
                    OnGameWon();
                }
                _isBusy = false;
            });
        }
        else
        {
            _selectedBottle.Deselect();
            if (!clickedBottle.IsEmpty && !clickedBottle.IsCompleted() && !clickedBottle.IsLocked)
            {
                _selectedBottle = clickedBottle;
                _selectedBottle.Select();
                PlaySFX(_selectSFX);
            }
            else
            {
                _isBusy = true;
                PlaySFX(_errorSFX);
                _selectedBottle.PlayShakeError(() =>
                {
                    _selectedBottle = null;
                    _isBusy = false;
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
        foreach (var bottle in _bottles)
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
        PlaySFX(_winSFX);
        if (_winPopupUI != null)
        {
            _winPopupUI.Show(LevelManager.Instance.CurrentLevelIndex);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    public void PlayPourSFX() => PlaySFX(_pourSFX);
}