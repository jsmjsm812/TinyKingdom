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

        [Header("스탯 (기본값, GDD.md 표 그대로)")]
        public float baseHp;
        [Tooltip("유닛/초")]
        public float moveSpeed;
        [Tooltip("본관 도달 시 결계에서 깎이는 체력")]
        public int wardDamage;
        [Tooltip("처치 시 지급되는 마나결정")]
        public int manaCrystalReward;
    }
}
