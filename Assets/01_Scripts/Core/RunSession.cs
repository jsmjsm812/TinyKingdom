using System;
using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// 홈 화면에서 고른 구역과 이번 출전 금화를 Battle 씬으로 넘길 때 쓰는 정적 상태. 씬을
    /// SceneManager.LoadScene으로 완전히 새로 로드해도 살아남아야 하는 값들이라(직렬화된
    /// 씬 데이터가 아니라 "이번 출전" 단위로 도는 값) 정적 클래스로 둔다.
    ///
    /// 금화를 여기로 옮긴 이유: 아이템을 로비에서 미리 살 수 있게 하면서(RunCurrency는
    /// Battle 씬에만 있는 MonoBehaviour라 로비에서는 못 씀) 로비/배틀 양쪽에서 "같은 지갑"을
    /// 보고 쓰게 만들어야 했다 — 로비에서 아이템 사고 남은 돈이 그대로 배틀 상점에 이어진다.
    /// </summary>
    public static class RunSession
    {
        public static ZoneData SelectedZone;

        // 200으로는 상점 뽑기 운이 나쁘면(비싼 궁수/도적만 계속 뜨는 등) 1웨이브에 용사를
        // 2~3기밖에 못 배치하는 경우가 있어서 "초반부터 몹이 안 죽는다" 피드백의 원인 중
        // 하나였다 — 최소한의 화력을 보장하도록 올림.
        public const int StartingGold = 260;
        public static int Gold { get; private set; }
        public static bool DebugInfiniteGold;
        public static event Action<int> OnGoldChanged;

        // 결과창에 "이번 출전에서 얻은 골드/처치 수"를 보여주려고 별도로 누적한다. Gold는
        // 상점에서 쓰면 줄어들어서 "총 획득량"을 보여주는 용도로는 못 쓴다.
        public static int TotalGoldEarned { get; private set; }
        public static int InvadersDefeated { get; private set; }

        private static bool _runBegun;

        /// <summary>로비 화면을 열 때마다 호출 — 이번 출전 계획(골드/아이템)을 처음부터 다시
        /// 시작한다. 아직 전투에 들어간 적 없이 로비만 오간 거라 이전 계획을 버려도 잃을 게 없다.</summary>
        public static void BeginNewRun()
        {
            Gold = StartingGold;
            TotalGoldEarned = 0;
            InvadersDefeated = 0;
            RunItemEffects.ResetForNewRun();
            _runBegun = true;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>로비를 거치지 않고 Battle 씬을 바로 열어 테스트하는 경우(에디터) 대비 —
        /// BeginNewRun이 한 번도 호출된 적 없으면 대신 시작해준다.</summary>
        public static void EnsureRunBegun()
        {
            if (!_runBegun) BeginNewRun();
        }

        public static bool TrySpendGold(int amount)
        {
            EnsureRunBegun();
            if (DebugInfiniteGold) return true;
            if (Gold < amount) return false;

            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public static void AddGold(int amount)
        {
            EnsureRunBegun();
            Gold += amount;
            TotalGoldEarned += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public static void IncrementInvadersDefeated()
        {
            InvadersDefeated++;
        }

        public static void SetGold(int amount)
        {
            EnsureRunBegun();
            Gold = amount;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
