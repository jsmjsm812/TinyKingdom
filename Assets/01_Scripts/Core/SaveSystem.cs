using System;
using System.IO;
using System.Linq;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// GameProgress(세션 동안만 유지되던 상태)를 JSON 파일로 저장/복원한다.
    /// GameProgress.cs 주석에 이미 "이 상태를 그대로 직렬화하기만 하면 되도록" 분리해뒀던 부분 —
    /// 그 약속대로 여기선 GameProgress의 두 필드만 그대로 옮겨 담는다.
    ///
    /// 명예 문장·총 클리어 횟수 등 GDD에 적혀 있던 다른 항목들은 아직 실제로 추적하는
    /// 코드가 없어서(구역 해금도 지금은 순수 "이전 구역 클리어" 조건이지 별도 화폐를
    /// 소모하지 않음) 일부러 이번 저장 스코프에서 뺐다 — 존재하지 않는 상태를 지어내서
    /// 저장하느니, 실제로 있는 상태만 정확하게 저장하는 쪽을 택함.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private const string RegistryResourcePath = "GuardianRegistry";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(FilePath);

        /// <summary>디버그 전용 — 세이브 파일을 지우고 메모리 상태(GameProgress)도 같이 초기화한다.
        /// "저장된 거 리셋은 어떻게 함" 질문에 대한 답 — 파일 경로는 Application.persistentDataPath
        /// (Windows 기준 %userprofile%\AppData\LocalLow\{companyName}\{productName}\save.json)라
        /// 직접 지워도 되지만, 플레이 중인 세션의 메모리 상태까지 지우려면 이 메서드가 필요하다.</summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 세이브 파일 삭제 실패: {e.Message}");
            }

            GameProgress.ResetAll();
        }

        public static void Save()
        {
            var data = new SaveData
            {
                zoneCleared = (bool[])GameProgress.ZoneCleared.Clone(),
                unlockedGuardianNames = GameProgress.UnlockedGuardians
                    .Where(g => g != null)
                    .Select(g => g.name)
                    .ToArray(),
                totalClears = GameProgress.TotalClears,
                totalSummons = GameProgress.TotalSummons,
                checkpoints = GameProgress.Checkpoints,
            };

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 저장 실패: {e.Message}");
            }
        }

        /// <summary>Boot 씬에서 첫 화면(Title)으로 넘어가기 전에 한 번 호출 — GameProgress를
        /// 채워두면 이후 Home/Battle 씬은 지금까지처럼 GameProgress만 보면 된다.</summary>
        public static void Load()
        {
            if (!HasSave()) return;

            SaveData data;
            try
            {
                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 세이브 파일을 읽지 못해 새 진행 상태로 시작합니다: {e.Message}");
                return;
            }

            if (data == null) return;

            for (int i = 0; i < GameProgress.ZoneCleared.Length && i < data.zoneCleared.Length; i++)
                GameProgress.ZoneCleared[i] = data.zoneCleared[i];
            GameProgress.RestoreStats(data.totalClears, data.totalSummons);

            if (data.checkpoints != null)
            {
                for (int i = 0; i < GameProgress.Checkpoints.Length && i < data.checkpoints.Length; i++)
                    GameProgress.Checkpoints[i] = data.checkpoints[i] ?? new ZoneCheckpoint();
            }

            if (data.unlockedGuardianNames != null && data.unlockedGuardianNames.Length > 0)
            {
                var registry = Resources.Load<GuardianRegistry>(RegistryResourcePath);
                if (registry == null)
                {
                    Debug.LogWarning("[SaveSystem] GuardianRegistry를 Resources에서 못 찾아서 해금된 수호자를 복원 못 했습니다.");
                    return;
                }

                foreach (string assetName in data.unlockedGuardianNames)
                {
                    var guardian = registry.FindByAssetName(assetName);
                    if (guardian != null)
                        GameProgress.UnlockedGuardians.Add(guardian);
                }
            }
        }
    }
}
