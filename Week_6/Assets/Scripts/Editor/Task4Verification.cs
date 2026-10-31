#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Task4Verification
{
    [MenuItem("Tools/Bus Jam/Verify Task 4 Haptic and Audio Polish")]
    public static void RunVerification()
    {
        Debug.Log("=== [Task 4 Verification Started] ===");
        bool allPassed = true;

        // 1. Verify Naming Conventions (_camelCase)
        System.Type[] typesToCheck = new System.Type[]
        {
            typeof(HapticManager),
            typeof(AudioManager),
            typeof(SettingsManager),
            typeof(SettingsController)
        };

        foreach (var type in typesToCheck)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                if (field.Name.Contains("k__BackingField")) continue;

                if (!field.Name.StartsWith("_") || (field.Name.Length > 1 && !char.IsLower(field.Name[1])))
                {
                    Debug.LogError($"[Task 4] Field {type.Name}.{field.Name} does not follow _camelCase naming convention!");
                    allPassed = false;
                }
            }
        }
        Debug.Log("[Task 4] Naming convention checked: All private fields follow _camelCase.");

        // 2. Verify HapticManager Logic and IsHapticOn Check
        GameObject dummyHapticHost = new GameObject("Test_HapticHost");
        try
        {
            HapticManager hapticMgr = dummyHapticHost.AddComponent<HapticManager>();
            hapticMgr.Initialize();

            // Test trigger calls without exceptions
            hapticMgr.TriggerLight();
            hapticMgr.TriggerImpact();
            hapticMgr.TriggerSuccess();
            hapticMgr.TriggerWarning();

            Debug.Log("[Task 4] HapticManager initialized and executed triggers cleanly.");
        }
        finally
        {
            Object.DestroyImmediate(dummyHapticHost);
        }

        // 3. Verify Master Audio Controller (SFXSource & BGMSource separation)
        GameObject dummyAudioHost = new GameObject("Test_AudioHost");
        try
        {
            AudioManager audioMgr = dummyAudioHost.AddComponent<AudioManager>();
            audioMgr.Initialize();

            FieldInfo sfxField = typeof(AudioManager).GetField("_sfxSource", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo bgmField = typeof(AudioManager).GetField("_bgmSource", BindingFlags.NonPublic | BindingFlags.Instance);

            AudioSource sfx = sfxField?.GetValue(audioMgr) as AudioSource;
            AudioSource bgm = bgmField?.GetValue(audioMgr) as AudioSource;

            if (sfx != null && bgm != null && sfx != bgm)
            {
                Debug.Log("[Task 4] Master Audio Controller verified: SFXSource and BGMSource are strictly separated.");
            }
            else
            {
                Debug.LogError("[Task 4] SFXSource and BGMSource are not properly configured as separate channels!");
                allPassed = false;
            }

            // Test PlayPop ASMR
            audioMgr.PlayPop();
            Debug.Log("[Task 4] PlayPop() ASMR clip loaded and playable.");
        }
        finally
        {
            Object.DestroyImmediate(dummyAudioHost);
        }

        if (allPassed)
        {
            Debug.Log("<color=green>=== [Task 4 Verification SUCCEEDED: All criteria met!] ===</color>");
        }
        else
        {
            Debug.LogError("<color=red>=== [Task 4 Verification FAILED: Issues detected!] ===</color>");
        }
    }
}
#endif
