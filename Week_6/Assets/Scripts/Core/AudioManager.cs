using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _bgmSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _honkClip;
    [SerializeField] private AudioClip _engineClip;
    [SerializeField] private AudioClip _footstepClip;
    [SerializeField] private AudioClip _crashClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _loseClip;
    [SerializeField] private AudioClip _coinClip;
    [SerializeField] private AudioClip _popClip;
    [SerializeField] private AudioClip _bgmClip;

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

        SetupSources();
        EnsureClips();
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        SetupSources();
        EnsureClips();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged += HandleSoundSettingChanged;
            SettingsManager.Instance.OnMusicChanged += HandleMusicSettingChanged;

            HandleSoundSettingChanged(SettingsManager.Instance.IsSoundOn);
            HandleMusicSettingChanged(SettingsManager.Instance.IsMusicOn);
        }

        if (_bgmClip != null && _bgmSource != null)
        {
            _bgmSource.clip = _bgmClip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }
    }

    public void EnsureClips()
    {
        if (_clickClip == null) _clickClip = Resources.Load<AudioClip>("Audio/click");
        if (_honkClip == null) _honkClip = Resources.Load<AudioClip>("Audio/honk");
        if (_engineClip == null) _engineClip = Resources.Load<AudioClip>("Audio/engine");
        if (_footstepClip == null) _footstepClip = Resources.Load<AudioClip>("Audio/footstep");
        if (_crashClip == null) _crashClip = Resources.Load<AudioClip>("Audio/crash");
        if (_winClip == null) _winClip = Resources.Load<AudioClip>("Audio/win");
        if (_loseClip == null) _loseClip = Resources.Load<AudioClip>("Audio/lose");
        if (_coinClip == null) _coinClip = Resources.Load<AudioClip>("Audio/sfx_collect_money_final");
        if (_coinClip == null) _coinClip = Resources.Load<AudioClip>("Audio/click");
        if (_popClip == null) _popClip = Resources.Load<AudioClip>("Audio/sfx_fx_pop_balloon");
        if (_popClip == null) _popClip = Resources.Load<AudioClip>("Audio/click");
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSoundChanged -= HandleSoundSettingChanged;
            SettingsManager.Instance.OnMusicChanged -= HandleMusicSettingChanged;
        }
    }

    private void SetupSources()
    {
        if (_sfxSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 0) _sfxSource = sources[0];
            if (sources.Length > 1) _bgmSource = sources[1];

            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
        }

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

    public void PlayClick()
    {
        if (_clickClip == null) EnsureClips();
        PlaySound(_clickClip, 0.9f, 1.1f, 0.8f);
    }

    public void PlayHonk()
    {
        if (_honkClip == null) EnsureClips();
        PlaySound(_honkClip, 0.95f, 1.05f, 1.0f);
    }

    public void PlayEngine()
    {
        if (_engineClip == null) EnsureClips();
        PlaySound(_engineClip, 0.9f, 1.1f, 0.9f);
    }

    public void PlayFootstep()
    {
        if (_footstepClip == null) EnsureClips();
        if (!AllowRepeatedSound(_footstepClip, ref _lastFootstepTime, _footstepMinInterval)) return;
        PlaySound(_footstepClip, 0.85f, 1.15f, 0.7f);
    }

    public void PlayCrash()
    {
        if (_crashClip == null) EnsureClips();
        PlaySound(_crashClip, 0.9f, 1.1f, 1.0f);
    }

    public void PlayWin()
    {
        if (_winClip == null) EnsureClips();
        PlaySound(_winClip, 1.0f, 1.0f, 1.0f);
    }

    public void PlayLose()
    {
        if (_loseClip == null) EnsureClips();
        PlaySound(_loseClip, 1.0f, 1.0f, 1.0f);
    }

    public void PlayCoin(float pitch = 1.0f)
    {
        if (_coinClip == null) EnsureClips();
        if (!AllowRepeatedSound(_coinClip, ref _lastCoinTime, _coinMinInterval)) return;
        PlaySound(_coinClip, pitch, pitch, 0.9f);
    }

    private bool AllowRepeatedSound(AudioClip clip, ref float lastTime, float interval)
    {
        if (clip == null || (SettingsManager.Instance != null && !SettingsManager.Instance.IsSoundOn)) return false;
        if (Time.unscaledTime - lastTime < Mathf.Max(0f, interval)) return false;
        lastTime = Time.unscaledTime;
        return true;
    }

    public void PlayPop()
    {
        if (_popClip == null) EnsureClips();
        PlaySound(_popClip, 0.95f, 1.15f, 0.85f);
    }

    private void PlaySound(AudioClip clip, float minPitch = 1.0f, float maxPitch = 1.0f, float volume = 1.0f)
    {
        if (clip == null) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsSoundOn) return;

        if (_sfxSource == null) SetupSources();

        if (_sfxSource != null)
        {
            _sfxSource.pitch = Random.Range(minPitch, maxPitch);
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
