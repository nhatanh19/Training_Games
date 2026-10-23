using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip honkClip;
    [SerializeField] private AudioClip engineClip;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip crashClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    private AudioSource _sfxSource;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }

        _sfxSource.playOnAwake = false;

#if UNITY_EDITOR
        if (clickClip == null)
        {
            AutoAssignClips();
        }
#endif
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoAssignClips();
    }

    private void OnValidate()
    {
        if (clickClip == null)
        {
            AutoAssignClips();
        }
    }

    [ContextMenu("Auto Assign Audio Clips")]
    public void AutoAssignClips()
    {
        string basePath = "Assets/Casual Game Sounds U6/CasualGameSounds/";
        clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-01.wav");
        footstepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-32.wav");
        crashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-18.wav");
        honkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-28.wav");
        engineClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-04.wav");
        winClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-45.wav");
        loseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + "DM-CGS-19.wav");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public void PlayClick()
    {
        PlaySound(clickClip, 0.9f, 1.1f, 0.8f);
    }

    public void PlayHonk()
    {
        PlaySound(honkClip, 0.95f, 1.05f, 1.0f);
    }

    public void PlayEngine()
    {
        PlaySound(engineClip, 0.9f, 1.1f, 0.9f);
    }

    public void PlayFootstep()
    {
        PlaySound(footstepClip, 0.85f, 1.15f, 0.7f);
    }

    public void PlayCrash()
    {
        PlaySound(crashClip, 0.9f, 1.1f, 1.0f);
    }

    public void PlayWin()
    {
        PlaySound(winClip, 1.0f, 1.0f, 1.0f);
    }

    public void PlayLose()
    {
        PlaySound(loseClip, 1.0f, 1.0f, 1.0f);
    }

    private void PlaySound(AudioClip clip, float minPitch = 1.0f, float maxPitch = 1.0f, float volume = 1.0f)
    {
        if (clip == null) return;

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        _sfxSource.pitch = Random.Range(minPitch, maxPitch);
        _sfxSource.PlayOneShot(clip, volume);
    }
}
