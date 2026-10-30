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
    [SerializeField] private AudioClip _bgmClip;

    private bool _initialized;

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

        if (_sfxSource != null) _sfxSource.playOnAwake = false;
        if (_bgmSource != null)
        {
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
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
        PlaySound(_clickClip, 0.9f, 1.1f, 0.8f);
    }

    public void PlayHonk()
    {
        PlaySound(_honkClip, 0.95f, 1.05f, 1.0f);
    }

    public void PlayEngine()
    {
        PlaySound(_engineClip, 0.9f, 1.1f, 0.9f);
    }

    public void PlayFootstep()
    {
        PlaySound(_footstepClip, 0.85f, 1.15f, 0.7f);
    }

    public void PlayCrash()
    {
        PlaySound(_crashClip, 0.9f, 1.1f, 1.0f);
    }

    public void PlayWin()
    {
        PlaySound(_winClip, 1.0f, 1.0f, 1.0f);
    }

    public void PlayLose()
    {
        PlaySound(_loseClip, 1.0f, 1.0f, 1.0f);
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
