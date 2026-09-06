using UnityEngine;

namespace WitchHour.Data
{
    public enum GuardianRarity { Star1, Star2, Star3 }

    public enum AttackType { Single, Multi, Area, Pierce, DotSingle, BuffSingle }

    [CreateAssetMenu(fileName = "New Guardian", menuName = "WitchHour/Guardian Data")]
    public class GuardianData : ScriptableObject
    {
        [Header("기본 정보")]
        public string guardianName;
        public GuardianRarity rarity;
        public Sprite portrait;

        [Header("배틀 스프라이트 (SpumBattleSpriteBaker로 자동 생성됨)")]
        public Sprite[] idleFrames;
        public Sprite[] actionFrames; // 공격 모션
        [Tooltip("TinyKingdom 리그의 \"Weapon\" 자식 스프라이트를 그대로 추출한 것 — 캐릭터 정보 표시용으로만 " +
                 "쓰고 투사체엔 안 쓴다(무기 자체를 날리면 어색해서 projectileSprite로 대체함).")]
        public Sprite weaponSprite;

        [Tooltip("공격 시 적에게 날아가는 투사체 스프라이트(WeaponProjectile) — 궁수/마법사 계열은 " +
                 "화살·파이어볼 등 클래스에 맞는 기성 스프라이트, 근접 무기 계열은 자기 weaponSprite를 " +
                 "그대로 던진다. SpumBattleSpriteBaker의 AssignProjectileSprites가 전원 지정한다. " +
                 "(비어있으면 안전장치로 MeleeImpactEffect의 즉시 임팩트 섬광으로 대체됨)")]
        public Sprite projectileSprite;

        [Tooltip("projectileSprite 원본 그림이 \"오른쪽(0도)\"이 아니라 다른 방향을 기본값으로 그려진 " +
                 "경우의 보정 각도. 예: 근접 무기 원본은 전부 날 끝이 위쪽(90도)을 향하게 그려져 있어서, " +
                 "그대로 날아가는 방향으로 회전시키면 창처럼 끝이 앞을 향하는 게 아니라 옆으로 누워 " +
                 "날아간다 — 여기 90을 넣으면 WeaponProjectile이 (진행방향 각도 - 이 값)으로 돌려서 " +
                 "끝이 항상 목표를 향하게 보정한다. 화살처럼 이미 오른쪽을 향해 그려진 경우는 0.")]
        public float projectileRotationOffsetDegrees;

        [Header("전투 스탯 (1성 기준, GDD.md 표 그대로)")]
        public float range;
        [Tooltip("초당 공격 횟수")]
        public float attackSpeed;
        public AttackType attackType;
        public float attackPower;
        [Tooltip("범위 반경(유닛) — attackType이 Area일 때만 사용")]
        public float areaRadius;

        [Header("도트 (DotSingle 전용 — 마녀)")]
        [Tooltip("적중 즉발 피해와 별개로, 이 총량을 dotDurationSeconds에 걸쳐 나눠서 추가로 준다.")]
        public float dotTotalDamage;
        public float dotDurationSeconds = 2f;
        [Tooltip("도트 틱 간격(초)")]
        public float dotTickInterval = 0.5f;

        [Header("버프 (BuffSingle 전용 — 왕)")]
        [Tooltip("인접 슬롯(상하좌우) 아군에게 상시 적용되는 공격력 배율 — 공격이 아니라 배치 즉시 켜지는 오라.")]
        public float adjacentAllyAttackMultiplier = 1.15f;

        [Header("둔화 (연금술사/빙결사 — specialEffectDescription에 적혀있었지만 실제로는 구현이 " +
                 "안 돼있던 효과. 연금술사·빙결사 에셋에서만 appliesSlowOnHit를 켠다)")]
        [Tooltip("공격이 적중한 대상의 이동속도를 이 배율로(예: 0.8 = -20%) slowDurationSeconds 동안 낮춘다.")]
        public bool appliesSlowOnHit;
        public float slowMultiplier = 0.8f;
        public float slowDurationSeconds = 2f;

        [Header("주기 광역 속박 (대마법사 전용 — 마찬가지로 설명만 있고 미구현이던 효과)")]
        [Tooltip("공격 쿨다운과 별개로 이 간격마다 본인 주변 areaRadius 안 전체를 잠깐 묶는다(이동속도 0).")]
        public bool hasPeriodicRoot;
        public float periodicRootInterval = 3f;
        public float periodicRootDuration = 1.5f;

        [Header("치명타 (도적 전용 — 시작부터 해금된 유닛인데 마찬가지로 설명만 있고 미구현이었음)")]
        [Range(0f, 1f)] public float critChance;
        public float critMultiplier = 2f;

        [Header("기절 (야만전사 전용 — 위와 같은 이유로 미구현이었음. 둔화 배율 0으로 재사용)")]
        [Range(0f, 1f)] public float stunChance;
        public float stunDurationSeconds = 0.5f;

        [Header("특수효과")]
        [TextArea] public string specialEffectDescription;

        [Header("소환 상점 비용")]
        public int shopCost;
    }
}
