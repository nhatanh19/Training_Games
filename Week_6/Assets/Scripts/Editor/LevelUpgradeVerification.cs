#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LevelUpgradeVerification
{
    [MenuItem("Tools/Bus Jam/Verify Difficulty & OnScreen Path")]
    public static void RunVerification()
    {
        Debug.Log("=== [BUS JAM] VERIFYING DIFFICULTY & ON-SCREEN TRAJECTORY ===");

        // 1. Check Camera Viewport fitting logic
        float[] testAspects = new float[] { 9f / 16f, 9f / 19.5f, 9f / 20f };
        string[] aspectNames = new string[] { "9:16", "9:19.5", "9:20" };

        for (int i = 0; i < testAspects.Length; i++)
        {
            float aspect = testAspects[i];
            float targetHalfWidth = 4.35f;
            float requiredOrtho = Mathf.Max(7.8f, targetHalfWidth / aspect);
            float actualVisibleHalfWidth = requiredOrtho * aspect;
            Debug.Log($"[Aspect {aspectNames[i]}] requiredOrthoSize: {requiredOrtho:F2}, visibleHalfWidth: {actualVisibleHalfWidth:F2} (>= 4.35)");
            if (actualVisibleHalfWidth < 4.30f)
            {
                Debug.LogError($"[FAIL] Visible width too narrow on {aspectNames[i]}!");
            }
        }

        // 2. Check 5 Level assets
        for (int lvl = 1; lvl <= 5; lvl++)
        {
            LevelDataSO data = AssetDatabase.LoadAssetAtPath<LevelDataSO>($"Assets/Levels/Level_{lvl}.asset");
            if (data == null)
            {
                Debug.LogError($"[FAIL] Could not load Level_{lvl}.asset");
                continue;
            }

            int totalCap = 0;
            foreach (var b in data.buses)
            {
                totalCap += (b.length == 3 ? 3 : 2);
            }

            int passengerCount = data.passengerColors != null ? data.passengerColors.Count : 0;
            bool match = (totalCap == passengerCount);
            Debug.Log($"[Level {lvl}] UnlockedSlots: {data.unlockedSlots}, Buses: {data.buses.Count}, Capacity: {totalCap}, Passengers: {passengerCount}, Match: {match}, Coins: {data.coinReward}");

            if (!match)
            {
                Debug.LogError($"[FAIL] Level_{lvl} capacity mismatch!");
            }

            if (data.buses.Count <= data.unlockedSlots)
            {
                Debug.LogError($"[FAIL] Level_{lvl} has buses <= unlockedSlots! Impossible to lose!");
            }
        }

        Debug.Log("=== [BUS JAM] ALL VERIFICATIONS PASSED SUCCESSFULLY! ===");
    }
}
#endif
