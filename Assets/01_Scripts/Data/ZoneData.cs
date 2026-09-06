using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Data
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "WitchHour/Zone Data")]
    public class ZoneData : ScriptableObject
    {
        [Header("기본 정보")]
        public string zoneName;
        [Tooltip("1, 2, 3")]
        public int zoneIndex;

        [Tooltip("배틀 필드 배경 일러스트 — WaveSpawner.ApplyZoneVisuals가 구역 시작/전환 시 " +
                 "FieldBackground Image에 대입한다. 비어있으면 씬에 이미 깔려있던 배경을 그대로 둔다.")]
        public Sprite fieldBackground;

        [Tooltip("구역 전용 기믹 — 웨이브가 시작될 때마다(1웨이브 포함) 성벽 HP를 이만큼 깎는다. " +
                 "존3(마왕의 영지) 전용 — 0이면 효과 없음. 준비 단계에서 '더 빨리 강해져야 한다'는 " +
                 "압박을 주는 장치라, 존3 속도배율(빠른 침입자)과 합쳐져 이중 압박이 된다.")]
        public int wardHpLossPerWave;

        [Tooltip("구역 전용 기믹 — 이 인덱스의 슬롯은 처음부터 무너진 잔해로 막혀서 배치할 수 " +
                 "없다(GridSlot.Index, F1 디버그 오버레이에 슬롯마다 표시되는 #번호와 동일). " +
                 "존2(폐허 성채) 전용 — 배치 폭이 좁아지는 게 난이도가 되도록 한 것. 안개(시야 " +
                 "가림)는 이 게임 구조(전투 중엔 재배치 불가, 관전만 함)와 안 맞다고 판단해 대체함.")]
        public List<int> blockedSlotIndices;

        [Header("통로 색 (배경 테마에 맞게)")]
        [Tooltip("통로 채우기(단색 사각형) 색 — 존1 기본값은 TinyKingdom Road.png에서 실측한 흙길 색.")]
        public Color pathColor = new Color(0.851f, 0.725f, 0.4f);
        [Tooltip("통로 테두리 장식(원본은 초록 풀 텍스처)에 곱해지는 틴트 — 흰색(1,1,1,1)이면 " +
                 "원본 그대로. 배경이 초원이 아닌 구역에서 풀 색을 그 구역 톤에 맞게 눌러준다 " +
                 "(완전히 새 텍스처를 그리는 대신 곱연산 틴트로 근사한 것 — 이상하면 이 값만 조정).")]
        public Color pathTrimTint = Color.white;

        [Header("배율 (GDD.md 14번 표 그대로)")]
        public float hpMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float manaMultiplier = 1f;

        [Header("구역 전용 보스 (10웨이브)")]
        public InvaderData bossInvader;

        [TextArea]
        public string specialRuleDescription;

        [Header("클리어 시 해금되는 수호자")]
        public List<GuardianData> unlockedGuardians;
    }
}
