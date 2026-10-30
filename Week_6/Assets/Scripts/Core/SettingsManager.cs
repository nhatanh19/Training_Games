using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    private const string SoundPrefKey = "SETTINGS_SOUND";
    private const string MusicPrefKey = "SETTINGS_MUSIC";
    private const string HapticPrefKey = "SETTINGS_HAPTIC";

    public static SettingsManager Instance { get; private set; }

    private bool _isSoundOn = true;
    private bool _isMusicOn = true;
    private bool _isHapticOn = true;
    private bool _initialized;

    public bool IsSoundOn => _isSoundOn;
    public bool IsMusicOn => _isMusicOn;
    public bool IsHapticOn => _isHapticOn;

    public event Action<bool> OnSoundChanged;
    public event Action<bool> OnMusicChanged;
    public event Action<bool> OnHapticChanged;

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

        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        LoadSettings();
    }

    public void LoadSettings()
    {
        _isSoundOn = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
        _isMusicOn = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        _isHapticOn = PlayerPrefs.GetInt(HapticPrefKey, 1) == 1;

        OnSoundChanged?.Invoke(_isSoundOn);
        OnMusicChanged?.Invoke(_isMusicOn);
        OnHapticChanged?.Invoke(_isHapticOn);
    }

    public void SetSound(bool enabled)
    {
        if (_isSoundOn == enabled) return;
        _isSoundOn = enabled;
        PlayerPrefs.SetInt(SoundPrefKey, _isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
        OnSoundChanged?.Invoke(_isSoundOn);
    }

    public void SetMusic(bool enabled)
    {
        if (_isMusicOn == enabled) return;
        _isMusicOn = enabled;
        PlayerPrefs.SetInt(MusicPrefKey, _isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
        OnMusicChanged?.Invoke(_isMusicOn);
    }

    public void SetHaptic(bool enabled)
    {
        if (_isHapticOn == enabled) return;
        _isHapticOn = enabled;
        PlayerPrefs.SetInt(HapticPrefKey, _isHapticOn ? 1 : 0);
        PlayerPrefs.Save();
        OnHapticChanged?.Invoke(_isHapticOn);
    }

    public void ToggleSound() => SetSound(!_isSoundOn);
    public void ToggleMusic() => SetMusic(!_isMusicOn);
    public void ToggleHaptic() => SetHaptic(!_isHapticOn);
}
