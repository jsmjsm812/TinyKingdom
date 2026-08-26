using System;
using UnityEngine;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Merge;

namespace WitchHour.Shop
{
    /// <summary>
    /// 소환 상점: 4칸, 각 칸 독립 추첨(GDD.md 9번). 구매 비용은 GuardianData.shopCost를 그대로 쓴다
    /// (등급별 고정가라 별도 테이블을 코드에 중복해서 들고 있지 않음).
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        private const int SlotCount = 4;
        private const int RerollCost = 20;

        [SerializeField] private SummonPool summonPool;
        [SerializeField] private RunCurrency currency;
        [SerializeField] private RosterManager roster;
        [SerializeField] private MergeSystem mergeSystem;

        public GuardianData[] CurrentOffers { get; } = new GuardianData[SlotCount];
        public event Action OnOffersChanged;

        private void Start()
        {
            RerollAll(free: true);
        }

        /// <summary>웨이브 전환 시 무료 자동 새로고침, 그 외엔 유료 리롤(GDD.md 9번).</summary>
        public bool RerollAll(bool free = false)
        {
            if (!free && !currency.TrySpend(RerollCost)) return false;

            for (int i = 0; i < SlotCount; i++)
                CurrentOffers[i] = summonPool.DrawWeighted();

            OnOffersChanged?.Invoke();
            return true;
        }

        public bool TryPurchase(int slotIndex)
        {
            GuardianData data = CurrentOffers[slotIndex];
            if (data == null) return false;
            if (!roster.HasSpace) return false;
            if (!currency.TrySpend(data.shopCost)) return false;

            roster.Add(data);
            CurrentOffers[slotIndex] = null;
            OnOffersChanged?.Invoke();
            mergeSystem.TryMergeAll();
            return true;
        }
    }
}
