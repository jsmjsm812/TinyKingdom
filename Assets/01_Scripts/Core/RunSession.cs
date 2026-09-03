using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// 홈 화면에서 고른 구역을 Battle 씬으로 넘길 때 쓰는 정적 상태. 씬을 SceneManager.LoadScene으로
    /// 완전히 새로 로드하면 인스펙터에 미리 연결해둔 참조는 그대로 남아있지만(직렬화된 값이라)
    /// "이번엔 어떤 구역인지"는 씬 로드 전에 결정되는 값이라 별도로 들고 있어야 한다.
    /// </summary>
    public static class RunSession
    {
        public static ZoneData SelectedZone;
    }
}
