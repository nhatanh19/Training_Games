using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (Assigned in Editor)")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    [Header("Audio Library Asset")]
    [SerializeField] private AudioLibrarySO _audioLibrary;

    [Header("Repeated SFX")]
    [SerializeField, Min(0f)] private float _footstepMinInterval = 0.08f;
    [SerializeField, Min(0f)] private float _coinMinInterval = 0.08f;

    private bool _initialized;
    private float _lastFootstepTime = float.NegativeInfinity;
    private float _lastCoinTime = float.NegativeInfinity;

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

        ConfigureSources();
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        ConfigureSources();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged += HandleSoundSettingChanged;
            SettingsManager.Instance.OnMusicChanged += HandleMusicSettingChanged;

            HandleSoundSettingChanged(SettingsManager.Instance.IsSoundOn);
            HandleMusicSettingChanged(SettingsManager.Instance.IsMusicOn);
        }

        if (_audioLibrary != null && _audioLibrary.bgm != null && _bgmSource != null)
        {
            _bgmSource.clip = _audioLibrary.bgm;
            _bgmSource.loop = true;
            if (!_bgmSource.isPlaying) _bgmSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged -= HandleSoundSettingChanged;
            SettingsManager.Instance.OnMusicChanged -= HandleMusicSettingChanged;
        }
    }

    private void ConfigureSources()
    {
        if (_sfxSource != null)
        {
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
        }

        if (_bgmSource != null)
        {
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
        }
    }

    private void HandleSoundSettingChanged(bool isEnabled)
    {
        if (_sfxSource != null)
        {
            _sfxSource.mute = !isEnabled;
        }
    }

    private void HandleMusicSettingChanged(bool isEnabled)
    {
        if (_bgmSource != null)
        {
            _bgmSource.mute = !isEnabled;
        }
    }

    public void PlayClick() => PlaySound(_audioLibrary != null ? _audioLibrary.click : null, 0.92f, 1.08f, 0.8f);
    public void PlayHonk() => PlaySound(_audioLibrary != null ? _audioLibrary.honk : null, 0.95f, 1.05f, 0.9f);
    public void PlayEngine() => PlaySound(_audioLibrary != null ? _audioLibrary.engine : null, 0.9f, 1.1f, 0.9f);
    public void PlayCrash() => PlaySound(_audioLibrary != null ? _audioLibrary.crash : null, 0.9f, 1.1f, 1.0f);
    public void PlayWin() => PlaySound(_audioLibrary != null ? _audioLibrary.win : null, 1.0f, 1.0f, 1.0f);
    public void PlayLose() => PlaySound(_audioLibrary != null ? _audioLibrary.lose : null, 1.0f, 1.0f, 1.0f);
    public void PlayPop() => PlaySound(_audioLibrary != null ? _audioLibrary.pop : null, 0.95f, 1.15f, 0.85f);
    public void PlayBooster() => PlaySound(_audioLibrary != null ? _audioLibrary.booster : null, 0.98f, 1.05f, 0.95f);
    public void PlayUnlock() => PlaySound(_audioLibrary != null ? _audioLibrary.unlock : null, 0.98f, 1.06f, 0.95f);

    public void PlayFootstep()
    {
        AudioClip clip = _audioLibrary != null ? _audioLibrary.footstep : null;
        if (!AllowRepeatedSound(clip, ref _lastFootstepTime, _footstepMinInterval)) return;
        PlaySound(clip, 0.88f, 1.12f, 0.65f);
    }

    public void PlayCoin(float pitch = 1.0f)
    {
        AudioClip clip = _audioLibrary != null ? _audioLibrary.coin : null;
        if (!AllowRepeatedSound(clip, ref _lastCoinTime, _coinMinInterval)) return;
        PlaySound(clip, pitch, pitch, 0.9f);
    }

    private bool AllowRepeatedSound(AudioClip clip, ref float lastTime, float interval)
    {
        if (clip == null || (SettingsManager.Instance != null && !SettingsManager.Instance.IsSoundOn)) return false;
        if (Time.unscaledTime - lastTime < Mathf.Max(0f, interval)) return false;
        lastTime = Time.unscaledTime;
        return true;
    }

    private void PlaySound(AudioClip clip, float minPitch = 1.0f, float maxPitch = 1.0f, float volume = 1.0f)
    {
        if (clip == null || _sfxSource == null) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsSoundOn) return;

        _sfxSource.pitch = Random.Range(minPitch, maxPitch);
        _sfxSource.PlayOneShot(clip, volume);
    }
}
