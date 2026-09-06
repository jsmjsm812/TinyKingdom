using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.DebugTools;
using WitchHour.Field;
using WitchHour.Shop;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 현재 열려있는 배틀 씬에 DebugOverlay를 한 번 심어준다 — 그 뒤로는 씬에 저장돼 있으니
    /// 다시 실행할 필요 없다(참조가 끊기면 다시 실행해서 재배선).
    /// </summary>
    public static class DebugOverlayBootstrap
    {
        private static readonly string[] ZoneDataPaths =
        {
            "Assets/02_Data/Zones/Zone1_FrontGate.asset",
            "Assets/02_Data/Zones/Zone2_Library.asset",
            "Assets/02_Data/Zones/Zone3_Greenhouse.asset",
        };

        [MenuItem("TinyKingdom/Add Debug Overlay To Scene")]
        public static void AddToScene()
        {
            var gridManager = Object.FindAnyObjectByType<GridManager>();
            var ward = Object.FindAnyObjectByType<WardHealth>();
            var currency = Object.FindAnyObjectByType<RunCurrency>();
            var waveSpawner = Object.FindAnyObjectByType<WaveSpawner>();
            var summonPool = Object.FindAnyObjectByType<SummonPool>();

            if (gridManager == null || ward == null || currency == null || waveSpawner == null)
            {
                Debug.LogError("[DebugOverlayBootstrap] 배틀 씬이 아니거나 필요한 컴포넌트(GridManager/" +
                                "WardHealth/RunCurrency/WaveSpawner)를 못 찾음 — Battle.unity를 열고 다시 실행하세요.");
                return;
            }
            if (summonPool == null)
                Debug.LogWarning("[DebugOverlayBootstrap] SummonPool을 못 찾음 — \"전체 캐릭터 해금\" 버튼은 안 뜸 " +
                                  "(Build Shop and Roster UI를 먼저 실행했는지 확인).");

            var zones = new System.Collections.Generic.List<ZoneData>();
            foreach (var path in ZoneDataPaths)
            {
                var zone = AssetDatabase.LoadAssetAtPath<ZoneData>(path);
                if (zone != null) zones.Add(zone);
                else Debug.LogWarning($"[DebugOverlayBootstrap] ZoneData를 못 찾음: {path}");
            }

            var guardianGuids = AssetDatabase.FindAssets("t:GuardianData", new[] { "Assets/02_Data/Guardians" });
            var guardians = new System.Collections.Generic.List<GuardianData>();
            foreach (var guid in guardianGuids)
            {
                var guardian = AssetDatabase.LoadAssetAtPath<GuardianData>(AssetDatabase.GUIDToAssetPath(guid));
                if (guardian != null) guardians.Add(guardian);
            }

            var existing = Object.FindAnyObjectByType<DebugOverlay>();
            GameObject go = existing != null ? existing.gameObject : new GameObject("DebugOverlay");
            var overlay = go.GetComponent<DebugOverlay>();
            if (overlay == null) overlay = go.AddComponent<DebugOverlay>();
            overlay.Configure(gridManager, ward, currency, waveSpawner, summonPool, zones.ToArray(), guardians.ToArray());

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log($"[DebugOverlayBootstrap] DebugOverlay를 씬에 추가/재배선했습니다 " +
                      $"(구역 {zones.Count}개, 수호자 {guardians.Count}명 연결됨). " +
                      "Ctrl+S로 씬을 저장하세요. Play 중 F1로 켜고 끕니다.");
        }
    }
}
