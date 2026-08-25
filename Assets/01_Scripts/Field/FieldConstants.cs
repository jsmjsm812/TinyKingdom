namespace WitchHour.Field
{
    /// <summary>
    /// GDD.md 3번 "필드 · 그리드"에 명시된 고정 스펙.
    /// 그리드 배치와 웨이브 스포너가 서로 다른 값을 쓰면 사거리/충돌 판정이 어긋나므로
    /// 두 시스템 모두 이 상수 하나만 참조하도록 강제한다.
    /// </summary>
    public static class FieldConstants
    {
        public const float UnitSize = 180f;
        public const float LaneWidthUnits = 2f;
        public const float LaneLengthUnits = 10f;
        public const int RowsPerSide = 5;
        public const int ColumnsPerSide = 2;
    }
}
