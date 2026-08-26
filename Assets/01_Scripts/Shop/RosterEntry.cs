using WitchHour.Data;

namespace WitchHour.Shop
{
    /// <summary>
    /// 소환 명부(대기 명단)에 있는, 아직 필드에 배치되지 않은 수호자 한 기.
    /// 필드에 배치되면 GuardianUnit 컴포넌트로 바뀌고 이 엔트리는 명부에서 제거된다.
    /// </summary>
    public class RosterEntry
    {
        public readonly GuardianData Data;
        public int StarLevel;

        public RosterEntry(GuardianData data, int starLevel = 1)
        {
            Data = data;
            StarLevel = starLevel;
        }
    }
}
