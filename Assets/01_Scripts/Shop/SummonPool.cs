using System.Collections.Generic;
using UnityEngine;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.Shop
{
    /// <summary>
    /// 현재 해금된 수호자 명단과 등급별 추첨을 담당한다. 해금은 영구 상태(구역 클리어로 늘어남)이고,
    /// 인스턴스 생성(구매)과는 별개 — Unlock()은 명단에 추가할 뿐 소환하지 않는다.
    /// </summary>
    public class SummonPool : MonoBehaviour
    {
        [Tooltip("기본 해금 수호자 (GDD.md: 루미·노아)")]
        [SerializeField] private List<GuardianData> defaultUnlocked;

        private readonly HashSet<GuardianData> _unlocked = new HashSet<GuardianData>();
        private readonly Dictionary<GuardianRarity, List<GuardianData>> _byRarity = new Dictionary<GuardianRarity, List<GuardianData>>();

        private void Awake()
        {
            foreach (var guardian in defaultUnlocked)
                Unlock(guardian);

            // 이전 출전에서 구역 클리어로 풀어둔 수호자(GameProgress, 세션 동안 유지)도 반영.
            foreach (var guardian in GameProgress.UnlockedGuardians)
                Unlock(guardian);
        }

        public void Unlock(GuardianData guardian)
        {
            if (guardian == null || !_unlocked.Add(guardian)) return;

            if (!_byRarity.TryGetValue(guardian.rarity, out var list))
                _byRarity[guardian.rarity] = list = new List<GuardianData>();
            list.Add(guardian);
        }

        public void Unlock(IEnumerable<GuardianData> guardians)
        {
            foreach (var guardian in guardians)
                Unlock(guardian);
        }

        /// <summary>
        /// 등급별 추첨. 해당 등급이 아직 하나도 해금 안 됐으면 한 단계 아래로 대체한다(GDD.md 9번).
        /// </summary>
        public GuardianData Draw(GuardianRarity rarity)
        {
            for (int tier = (int)rarity; tier >= 0; tier--)
            {
                var candidateRarity = (GuardianRarity)tier;
                if (_byRarity.TryGetValue(candidateRarity, out var list) && list.Count > 0)
                    return list[Random.Range(0, list.Count)];
            }
            return null;
        }

        /// <summary>GDD.md 9번 확률: ★1 70% · ★2 25% · ★3 5%.</summary>
        public GuardianData DrawWeighted()
        {
            float roll = Random.value;
            GuardianRarity rarity =
                roll < 0.70f ? GuardianRarity.Star1 :
                roll < 0.95f ? GuardianRarity.Star2 :
                GuardianRarity.Star3;
            return Draw(rarity);
        }
    }
}
