using System;
using UnityEngine;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Shop
{
    /// <summary>
    /// 소환 상점: 4칸, 각 칸 독립 추첨(GDD.md 9번). 구매 비용은 GuardianData.shopCost를 그대로 쓴다
    /// (등급별 고정가라 별도 테이블을 코드에 중복해서 들고 있지 않음).
    /// 롤토체스처럼 구매하면 명부 같은 대기 단계 없이 곧장 필드 빈 슬롯에 배치되고, 그 자리에서
    /// 3기가 모이면 자동으로 합성된다(GuardianPlacementManager.PlaceNew 참고).
    /// 무료 새로고침은 이 클래스가 스스로 하지 않고 PrepTimerUI가 준비 시간 시작마다 호출한다
    /// (WaveSpawner는 상점을 몰라야 하므로 UI 레이어에서 둘을 이어줌).
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        private const int SlotCount = 4;
        private const int RerollCost = 20;

        [SerializeField] private SummonPool summonPool;
        [SerializeField] private RunCurrency currency;
        [SerializeField] private GuardianPlacementManager placementManager;

        public GuardianData[] CurrentOffers { get; } = new GuardianData[SlotCount];
        public event Action OnOffersChanged;

        /// <summary>웨이브 전환 시 무료 자동 새로고침, 그 외엔 유료 리롤(GDD.md 9번).</summary>
        public bool RerollAll(bool free = false)
        {
            if (!free && !currency.TrySpend(RerollCost)) return false;

            for (int i = 0; i < SlotCount; i++)
                CurrentOffers[i] = summonPool.DrawWeighted();

            OnOffersChanged?.Invoke();
            // 유료 리롤(유저가 직접 누른 것)만 소리 낸다 — 웨이브 전환마다 자동으로 도는
            // 무료 새로고침까지 매번 울리면 시끄럽다.
            if (!free) AudioManager.Instance?.PlayReroll();
            return true;
        }

        public bool TryPurchase(int slotIndex)
        {
            GuardianData data = CurrentOffers[slotIndex];
            if (data == null) return false;
            if (!placementManager.HasEmptySlot) return false;
            if (!currency.TrySpend(data.shopCost)) return false;

            placementManager.PlaceNew(data);
            CurrentOffers[slotIndex] = null;
            OnOffersChanged?.Invoke();
            AudioManager.Instance?.PlayPurchase();
            GameProgress.IncrementSummons();
            return true;
        }
    }
}
