using System;

namespace WitchHour.Core
{
    /// <summary>
    /// JsonUtility로 그대로 직렬화되는 순수 데이터. GameProgress(런타임 상태)와 필드를
    /// 일부러 분리해뒀다 — 이쪽은 파일 포맷이라 함부로 리팩터하면 기존 세이브가 깨진다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public bool[] zoneCleared = new bool[3];
        public string[] unlockedGuardianNames = Array.Empty<string>();
        public int totalClears;
        public int totalSummons;
        public ZoneCheckpoint[] checkpoints =
            { new ZoneCheckpoint(), new ZoneCheckpoint(), new ZoneCheckpoint() };
    }
}
