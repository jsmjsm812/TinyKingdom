using UnityEngine;

namespace WitchHour.Data
{
    public enum InvaderTier { Normal, Elite, Boss }

    [CreateAssetMenu(fileName = "New Invader", menuName = "WitchHour/Invader Data")]
    public class InvaderData : ScriptableObject
    {
        [Header("기본 정보")]
        public string invaderName;
        public InvaderTier tier;
        public Sprite sprite;

        [Header("배틀 스프라이트 (SpumBattleSpriteBaker로 자동 생성됨)")]
        public Sprite[] idleFrames;
        [Tooltip("이동 중 재생되는 걷기 모션. 없으면(구버전 데이터) idleFrames로 대체 재생됨")]
        public Sprite[] walkFrames;
        public Sprite[] actionFrames; // 사망 모션
        [Tooltip("보스급처럼 같은 종족 프리팹을 재활용할 때 시각적으로만 크기를 키우는 배수")]
        public float visualScale = 1f;

        [Header("스탯 (기본값, GDD.md 표 그대로)")]
        public float baseHp;
        [Tooltip("유닛/초")]
        public float moveSpeed;
        [Tooltip("본성 도달 시 성벽에서 깎이는 체력")]
        public int wardDamage;
        [Tooltip("처치 시 지급되는 금화")]
        public int manaCrystalReward;
    }
}
