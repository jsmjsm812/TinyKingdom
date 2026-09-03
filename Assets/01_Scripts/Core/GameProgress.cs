using System.Collections.Generic;
using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// 세션 동안 유지되는 진행 상태(정적 클래스라 씬을 넘나들어도 값이 유지됨).
    /// 파일 저장/불러오기(3주차)는 이 상태를 그대로 직렬화하기만 하면 되도록 미리 분리해둔다.
    /// </summary>
    public static class GameProgress
    {
        public static readonly bool[] ZoneCleared = new bool[3];
        public static readonly HashSet<GuardianData> UnlockedGuardians = new HashSet<GuardianData>();

        public static void MarkZoneCleared(int zoneIndex, IEnumerable<GuardianData> rewardGuardians)
        {
            if (zoneIndex >= 1 && zoneIndex <= ZoneCleared.Length)
                ZoneCleared[zoneIndex - 1] = true;

            if (rewardGuardians == null) return;
            foreach (var guardian in rewardGuardians)
                UnlockedGuardians.Add(guardian);
        }

        /// <summary>1구역은 항상 해금, 그 뒤로는 바로 앞 구역을 클리어해야 열린다(GDD.md 14번 순서).</summary>
        public static bool IsZoneUnlocked(int zoneIndex)
        {
            if (zoneIndex <= 1) return true;
            if (zoneIndex > ZoneCleared.Length + 1) return false;
            return ZoneCleared[zoneIndex - 2];
        }
    }
}
