using System.Collections.Generic;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// 아이템 상점(로비/배틀 공용)에서 산 버프/디버프의 실제 수치를 들고 있는 정적 상태.
    /// 출전 한정(한 라운드) 효과라 RunSession의 골드와 같은 생명주기로 리셋된다
    /// (RunSession.BeginNewRun에서 호출) — 로비에 새로 들어오면 전부 초기화됨.
    /// 같은 아이템도 최대 3개까지 중복으로 사서 효과를 누적시킬 수 있다.
    /// </summary>
    public static class RunItemEffects
    {
        public const int MaxStackPerItem = 3;

        public static float GuardianAttackPowerMultiplier { get; private set; } = 1f;
        public static float GuardianAttackSpeedMultiplier { get; private set; } = 1f;
        public static int GuardianBonusStarLevels { get; private set; }
        public static float InvaderSpeedMultiplier { get; private set; } = 1f;

        private static readonly Dictionary<ItemData, int> PurchaseCounts = new Dictionary<ItemData, int>();

        public static void ResetForNewRun()
        {
            GuardianAttackPowerMultiplier = 1f;
            GuardianAttackSpeedMultiplier = 1f;
            GuardianBonusStarLevels = 0;
            InvaderSpeedMultiplier = 1f;
            PurchaseCounts.Clear();
        }

        public static int GetCount(ItemData item) => item != null && PurchaseCounts.TryGetValue(item, out int c) ? c : 0;

        /// <summary>배틀씬 "그만하기" 체크포인트 저장 전용 — 지금까지 산 아이템과 개수를 전부 읽는다.</summary>
        public static IReadOnlyDictionary<ItemData, int> GetAllPurchases() => PurchaseCounts;

        public static bool CanPurchase(ItemData item) => item != null && GetCount(item) < MaxStackPerItem;

        public static bool Apply(ItemData item)
        {
            if (!CanPurchase(item)) return false;
            PurchaseCounts[item] = GetCount(item) + 1;

            switch (item.effectType)
            {
                case ItemEffectType.GuardianAttackPowerBuff:
                    GuardianAttackPowerMultiplier += item.effectValue;
                    break;
                case ItemEffectType.GuardianAttackSpeedBuff:
                    GuardianAttackSpeedMultiplier += item.effectValue;
                    break;
                case ItemEffectType.GuardianStarLevelBuff:
                    GuardianBonusStarLevels += Mathf.RoundToInt(item.effectValue);
                    break;
                case ItemEffectType.InvaderSlowDebuff:
                    // 여러 개 사도 이동속도가 음수로 뒤집히진 않게 최소 10%는 남긴다.
                    InvaderSpeedMultiplier = Mathf.Max(0.1f, InvaderSpeedMultiplier - item.effectValue);
                    break;
            }
            return true;
        }
    }
}
