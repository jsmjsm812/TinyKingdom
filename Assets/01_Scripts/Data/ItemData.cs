using UnityEngine;

namespace WitchHour.Data
{
    public enum ItemEffectType
    {
        GuardianAttackPowerBuff,
        GuardianAttackSpeedBuff,
        GuardianStarLevelBuff,
        InvaderSlowDebuff,
    }

    /// <summary>
    /// 출전 한정(한 라운드) 버프/디버프 아이템. 영구 성장 수단이 아니라, 이번 출전 동안만
    /// 유지되는 즉발 효과다(WitchHour.Core.RunItemEffects가 실제 수치를 들고 있음).
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "WitchHour/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        public Color iconTint = Color.white;
        [Tooltip("카드 배경에 옅게 섞는 색 — 4칸이 다 똑같은 크림색이면 구별이 안 가서, 효과별로 " +
                 "살짝 다른 색을 줘서 한눈에 구분되게 한다(아이콘 색은 원본 그대로 유지).")]
        public Color cardAccent = Color.white;

        public ItemEffectType effectType;
        [Tooltip("버프/디버프 배율(예: 0.2 = +20%, GuardianStarLevelBuff는 정수로 취급)")]
        public float effectValue;

        public int cost;
    }
}
