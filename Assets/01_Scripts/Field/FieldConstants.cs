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

        // 슬롯 "위치/간격" 계산은 전부 UnitSize(180) 기준을 유지하고(사거리·클리어런스 등 게임
        // 수치와 얽혀있어 건드리면 밸런스가 흔들림), 화면에 그려지는 슬롯 박스 자체만 이 값으로
        // 그린다. 92.5로 작게 그렸을 때 캐릭터(약 300px)가 슬롯보다 훨씬 커서 슬롯 밖으로
        // 떠 보이는 문제가 있어, 캐릭터 크기(200으로 축소, GuardianUnit.NativeToUiScale 참고)에
        // 맞춰 200으로 키웠다. 간격(SlotColumnSpacing=220·SlotRowSpacing=200)은 그대로라
        // 세로 방향은 슬롯끼리 거의 맞닿게 되지만 겹치지는 않는다.
        public const float SlotVisualSize = 200f;

        // GDD.md 표의 이동속도(유닛/초)는 침입자 간 상대적 빠르기 밸런스라 값 자체는 안 건드리고,
        // 실제 체감 속도만 이 배율 하나로 조절한다(플레이테스트 결과 너무 빠르다는 피드백 반영).
        public const float GlobalInvaderSpeedMultiplier = 0.65f;

        // InvaderUnit 프리팹의 실제 크기(SceneBootstrap.BuildInvaderPrefab 참고, 80x80).
        // 슬롯이 통로에 얼마나 붙어도 되는지 계산할 때 이 값을 빼야 침입자 스프라이트가
        // 슬롯 박스에 실제로 겹치는 걸 막을 수 있다 — 여태 슬롯끼리만 안 겹치게 계산하고
        // 침입자 크기를 안 빼서, 선반 슬롯 근처를 지나가는 침입자가 슬롯과 겹쳐 보였었다.
        public const float InvaderSize = 80f;

        // 침입자 스프라이트 가장자리와 슬롯 사이 최소 여유. InvaderData.visualScale로 침입자를
        // 시각적으로 최대 2.7배까지 키운 뒤로는(원래 InvaderSize=80 기준 여유였음) 20으로 부족해서
        // 슬롯 네모 칸과 겹쳐 보이는 문제가 실제로 생겼다 — 정예/보스 최대 시각 반폭
        // (40*2.7=108)에 슬롯 반칸(SlotVisualSize/2=46.25)과 약간의 버퍼(10)를 더한 만큼
        // 필요해서 35로 올림.
        public const float SlotClearanceMargin = 35f;

        // 슬롯 중심이 통로 중심선에서 최소 이만큼은 떨어져야 침입자와 안 겹친다:
        // (슬롯 반칸 90) + (침입자 반폭 40) + (여유 35) = 165. 선반이든 낙하든 침입자
        // 크기는 같으니 오프셋도 하나로 통일한다.
        public const float SlotOffsetFromPath = UnitSize / 2f + InvaderSize / 2f + SlotClearanceMargin;

        // ㄹ자 통로: 우→하→좌→하→우로 꺾이는 가로 중심 경로.
        // 가로 선반(Shelf) 3개를 세로 낙하(Drop) 2개로 잇는다 — 짝수 선반은 왼쪽에서
        // 오른쪽으로, 홀수 선반은 오른쪽에서 왼쪽으로 진행(번갈아 좌우 반전).
        // 선반 간격 산출: 낙하 슬롯이 좌/우 각각 2칸씩 세로로 쌓이려면
        // (SlotRowSpacing + UnitSize + 여유*2 = 200+180+40 = 420) 만큼의 복도 폭이 필요한데,
        // 선반 위/아래 슬롯이 각각 (오프셋+반칸 = 165+90 = 255)씩 복도 양끝을 파먹으므로
        // 간격 930이면 복도 = 930 - 2*255 = 420 → 딱 맞고 여유는 위 마진에 이미 포함됨.
        // (SlotClearanceMargin을 20→35로 올리면서 오프셋이 150→165로 늘어난 만큼, 복도 폭을
        // 지키려고 간격도 900→930으로 같이 늘렸다 — 둘 중 하나만 바꾸면 낙하 슬롯이 선반과 겹친다.)
        public static readonly float[] ShelfY = { 1030f, 100f, -830f };
        public const float ShelfHalfWidth = 400f; // 선반 좌우 끝 X거리 (스폰/성벽 포함)

        // 맨 아래 선반(ShelfY[2])만 오른쪽으로 100 밀려있는 비대칭 배치 — 기획 참고 스크린샷
        // 기준(위 2개 선반은 같은 X중심, 맨 아래 선반만 오른쪽으로 밀림). 나머지 선반은 0.
        public static readonly float[] ShelfColumnOffsetX = { 0f, 0f, 100f };

        public const int SlotColumnsPerShelfSide = 3;  // 선반 위/아래 한 줄에 놓는 슬롯 수
        public const int SlotRowsPerDropSide = 2;       // 낙하 좌/우 한 줄에 세로로 쌓는 슬롯 수
        public const float SlotColumnSpacing = 220f;    // 선반 슬롯 간 가로 간격
        public const float SlotRowSpacing = 200f;       // 낙하 슬롯 간 세로 간격
    }
}
