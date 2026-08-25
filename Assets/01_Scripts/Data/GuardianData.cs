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

        [Header("전투 스탯 (1성 기준, GDD.md 표 그대로)")]
        public float range;
        [Tooltip("초당 공격 횟수")]
        public float attackSpeed;
        public AttackType attackType;
        public float attackPower;
        [Tooltip("범위 반경(유닛) — attackType이 Area일 때만 사용")]
        public float areaRadius;

        [Header("특수효과")]
        [TextArea] public string specialEffectDescription;

        [Header("소환 상점 비용")]
        public int shopCost;
    }
}
