using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Debug Settings")]
#pragma warning disable 0414
    [SerializeField] private bool _enableEditorLogs = false;
#pragma warning restore 0414

    private bool _isInitialized;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _vibrator;
    private AndroidJavaClass _vibrationEffectClass;
    private int _apiLevel;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        if (_isInitialized) return;
        _isInitialized = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }

            using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _apiLevel = versionClass.GetStatic<int>("SDK_INT");
            }

            if (_apiLevel >= 26)
            {
                _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HapticManager] Failed to initialize Android vibrator: {ex.Message}");
        }
#endif
    }

    private bool CanVibrate()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsHapticOn)
        {
            return false;
        }
        return true;
    }

    public void TriggerLight()
    {
        if (!CanVibrate()) return;

#if UNITY_EDITOR
        if (_enableEditorLogs) Debug.Log("[Haptic] TriggerLight (Light pulse)");
#elif UNITY_ANDROID
        VibrateAndroid(25, 60);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void TriggerImpact()
    {
        if (!CanVibrate()) return;

#if UNITY_EDITOR
        if (_enableEditorLogs) Debug.Log("[Haptic] TriggerImpact (Medium impact)");
#elif UNITY_ANDROID
        VibrateAndroid(50, 160);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void TriggerSuccess()
    {
        if (!CanVibrate()) return;

#if UNITY_EDITOR
        if (_enableEditorLogs) Debug.Log("[Haptic] TriggerSuccess (Decisive success)");
#elif UNITY_ANDROID
        VibrateAndroid(90, 220);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void TriggerWarning()
    {
        if (!CanVibrate()) return;

#if UNITY_EDITOR
        if (_enableEditorLogs) Debug.Log("[Haptic] TriggerWarning (Decisive warning/error)");
#elif UNITY_ANDROID
        VibrateAndroid(120, 255);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void VibrateAndroid(long milliseconds, int amplitude)
    {
        if (_vibrator == null)
        {
            Handheld.Vibrate();
            return;
        }

        try
        {
            if (_apiLevel >= 26 && _vibrationEffectClass != null)
            {
                using (AndroidJavaObject effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                {
                    _vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                _vibrator.Call("vibrate", milliseconds);
            }
        }
        catch
        {
            Handheld.Vibrate();
        }
    }
#endif
}
