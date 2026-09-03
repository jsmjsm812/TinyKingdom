using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WitchHour.Combat;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.Merge
{
    /// <summary>
    /// 필드에 같은 수호자 + 같은 성급이 3기 모이면(GDD.md 10번) 자동으로 성급+1로 합쳐진다.
    /// 롤토체스처럼 구매하면 곧장 필드에 놓이므로, 배치 직후(GuardianPlacementManager.PlaceNew)
    /// 한 번씩만 체크하면 충분하다 — 명부 같은 대기 단계가 없어서 다른 트리거는 필요 없다.
    /// </summary>
    public class MergeSystem : MonoBehaviour
    {
        public void TryMergeAll()
        {
            while (TryMergeOnce())
            {
                // 3기 합쳐서 나온 성급+1이 우연히 또 3기를 채우는 연쇄 합성까지 처리.
            }
        }

        private bool TryMergeOnce()
        {
            var groups = new Dictionary<(GuardianData data, int star), List<GuardianUnit>>();

            foreach (var unit in GuardianUnit.ActiveUnits)
            {
                var key = (unit.Data, unit.StarLevel);
                if (!groups.TryGetValue(key, out var list))
                    groups[key] = list = new List<GuardianUnit>();
                list.Add(unit);
            }

            foreach (var group in groups)
            {
                if (group.Key.star >= 3) continue; // 최대 3성, GDD.md 10번
                if (group.Value.Count < 3) continue;

                MergeGroup(group.Key.star, group.Value.Take(3).ToList());
                return true;
            }
            return false;
        }

        private static void MergeGroup(int star, List<GuardianUnit> units)
        {
            // 하나는 자리를 유지한 채 성급만 올리고, 나머지 둘은 필드에서 제거한다.
            GuardianUnit keep = units[0];
            for (int i = 1; i < units.Count; i++)
                units[i].RemoveFromField();

            keep.SetStarLevel(star + 1);
            AudioManager.Instance?.PlayMerge();
        }
    }
}
