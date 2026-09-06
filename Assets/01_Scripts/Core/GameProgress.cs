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

        // 배틀씬 "그만하기" 버튼으로 저장한 구역별 체크포인트(웨이브 번호뿐 아니라 골드·아이템·필드
        // 배치까지 전부) — hasCheckpoint=false면 체크포인트 없음(1웨이브부터 새로 시작). 실패(성벽
        // 함락)하면 GDD.md 원칙("실패 시 전부 초기화")대로 지워지고, 완전 클리어해도 더 이상
        // 필요 없으니 지워진다 — "그만하기로 스스로 멈춘 경우"에만 의미 있는 값이라서. 배열 원소는
        // JsonUtility가 null을 못 다뤄서(ZoneCheckpoint.cs 참고) 항상 3개 다 non-null이다.
        public static readonly ZoneCheckpoint[] Checkpoints =
            { new ZoneCheckpoint(), new ZoneCheckpoint(), new ZoneCheckpoint() };

        // GDD.md 세이브 데이터 항목 중 실제로 추적하기 시작한 통계 — 구역 해금 판정에는
        // 안 쓰이고 순수 기록용(설정/결과창 등에서 나중에 보여줄 수 있음).
        public static int TotalClears { get; private set; }
        public static int TotalSummons { get; private set; }

        public static void MarkZoneCleared(int zoneIndex, IEnumerable<GuardianData> rewardGuardians)
        {
            if (zoneIndex >= 1 && zoneIndex <= ZoneCleared.Length)
                ZoneCleared[zoneIndex - 1] = true;
            TotalClears++;
            ClearWaveCheckpoint(zoneIndex);

            if (rewardGuardians == null) return;
            foreach (var guardian in rewardGuardians)
                UnlockedGuardians.Add(guardian);
        }

        public static void IncrementSummons() => TotalSummons++;

        /// <summary>zoneIndex(1-based)의 체크포인트를 읽는다 — 범위 밖이면 항상 hasCheckpoint=false인
        /// 빈 체크포인트를 돌려줘서 호출부가 매번 null 체크할 필요가 없게 한다.</summary>
        public static ZoneCheckpoint GetCheckpoint(int zoneIndex)
        {
            if (zoneIndex >= 1 && zoneIndex <= Checkpoints.Length)
                return Checkpoints[zoneIndex - 1];
            return new ZoneCheckpoint();
        }

        /// <summary>배틀씬 "그만하기" 버튼 전용 — 지금 이 순간의 웨이브·골드·아이템·필드 배치를
        /// 통째로 저장해서, 다음에 같은 구역을 다시 고르면 1웨이브 빈 필드가 아니라 여기서부터
        /// 다시 시작하게 한다.</summary>
        public static void SaveCheckpoint(int zoneIndex, ZoneCheckpoint checkpoint)
        {
            if (zoneIndex >= 1 && zoneIndex <= Checkpoints.Length)
                Checkpoints[zoneIndex - 1] = checkpoint;
        }

        /// <summary>완전 클리어했거나(더 이상 체크포인트가 의미 없음) 실패했을 때(GDD.md 원칙:
        /// 실패 시 전부 초기화) 호출 — 다음 도전은 다시 1웨이브 빈 필드부터.</summary>
        public static void ClearWaveCheckpoint(int zoneIndex)
        {
            if (zoneIndex >= 1 && zoneIndex <= Checkpoints.Length)
                Checkpoints[zoneIndex - 1] = new ZoneCheckpoint();
        }

        /// <summary>SaveSystem.Load 전용 — 파일에서 읽은 누적치를 그대로 복원한다.</summary>
        public static void RestoreStats(int totalClears, int totalSummons)
        {
            TotalClears = totalClears;
            TotalSummons = totalSummons;
        }

        /// <summary>디버그 전용 "세이브 초기화" — 파일 삭제(SaveSystem.DeleteSave)와 짝을 이뤄서
        /// 메모리에 남아있는 진행 상태도 같이 지운다(파일만 지우고 이걸 안 하면, 지금 세션은
        /// 이미 로드된 값을 그대로 들고 있어서 화면엔 안 지워진 것처럼 보임).</summary>
        public static void ResetAll()
        {
            for (int i = 0; i < ZoneCleared.Length; i++) ZoneCleared[i] = false;
            UnlockedGuardians.Clear();
            for (int i = 0; i < Checkpoints.Length; i++) Checkpoints[i] = new ZoneCheckpoint();
            TotalClears = 0;
            TotalSummons = 0;
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
