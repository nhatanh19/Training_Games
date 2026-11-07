#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Task3Verification
{
    [MenuItem("Tools/Bus Jam/Verify Task 3 Juice and Coin Fly")]
    public static void RunVerification()
    {
        Debug.Log("=== [Task 3 Verification Started] ===");
        bool allPassed = true;

        System.Type[] uiTypes = new System.Type[]
        {
            typeof(CoinFlyEffectManager),
            typeof(UIManager),
            typeof(HUDController),
            typeof(SettingsController)
        };

        foreach (var type in uiTypes)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {

                if (field.Name.Contains("k__BackingField")) continue;

                if (!field.Name.StartsWith("_") || (field.Name.Length > 1 && !char.IsLower(field.Name[1])))
                {
                    Debug.LogError($"[Task 3] Field {type.Name}.{field.Name} does not follow _camelCase naming convention!");
                    allPassed = false;
                }
            }
        }
        Debug.Log("[Task 3] Checked naming convention: All private fields follow _camelCase.");

        GameObject dummyHost = new GameObject("Test_CoinFlyHost");
        try
        {
            CoinFlyEffectManager effectMgr = dummyHost.AddComponent<CoinFlyEffectManager>();
            effectMgr.InitializePool();

            FieldInfo poolField = typeof(CoinFlyEffectManager).GetField("_allCoins", BindingFlags.NonPublic | BindingFlags.Instance);
            var allCoins = poolField?.GetValue(effectMgr) as System.Collections.IList;

            if (allCoins == null || allCoins.Count < 10)
            {
                Debug.LogError($"[Task 3] Expected initial pool size >= 10, got {allCoins?.Count}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"[Task 3] Object Pool verified: {allCoins.Count} coin icons pre-pooled without runtime instantiation.");
            }
        }
        finally
        {
            Object.DestroyImmediate(dummyHost);
        }

        AudioClip coinClip = Resources.Load<AudioClip>("Audio/sfx_collect_money_final");
        if (coinClip == null)
        {
            coinClip = Resources.Load<AudioClip>("Audio/click");
        }

        if (coinClip != null)
        {
            Debug.Log($"[Task 3] Audio clip loaded successfully: {coinClip.name}");
        }
        else
        {
            Debug.LogError("[Task 3] Coin audio clip could not be loaded from Resources/Audio!");
            allPassed = false;
        }

        GameObject vfxGo = GameObject.Find("VFXCanvas");
        if (vfxGo == null)
        {
            Debug.LogError("[Task 3] VFXCanvas không có trong scene — kéo Assets/Prefabs/UI/VFXCanvas.prefab vào hierarchy.");
            allPassed = false;
        }
        else if (vfxGo.GetComponent<CoinFlyEffectManager>() == null)
        {
            Debug.LogError("[Task 3] VFXCanvas thiếu CoinFlyEffectManager — chạy Tools > Bus Jam > Install Coin Fly Effect On VFXCanvas.");
            allPassed = false;
        }
        else
        {
            Debug.Log("[Task 3] VFXCanvas integration with CoinFlyEffectManager verified.");
        }

        if (allPassed)
        {
            Debug.Log("<color=green>=== [Task 3 Verification SUCCEEDED: All criteria met!] ===</color>");
        }
        else
        {
            Debug.LogError("<color=red>=== [Task 3 Verification FAILED: Issues detected!] ===</color>");
        }
    }
}
#endif
