using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WitchHour.Combat;
using WitchHour.Data;
using WitchHour.Shop;

namespace WitchHour.Merge
{
    /// <summary>
    /// 같은 수호자 + 같은 성급이 명부·필드 어디에 있든(GDD.md 10번) 3기 모이면 자동으로 성급+1로 합쳐진다.
    /// 로스터가 바뀌거나(구매) 필드 배치가 바뀔 때마다 TryMergeAll()을 호출해서 체크한다.
    /// </summary>
    public class MergeSystem : MonoBehaviour
    {
        [SerializeField] private RosterManager roster;

        public void TryMergeAll()
        {
            while (TryMergeOnce())
            {
                // 3기 합쳐서 나온 성급+1이 우연히 또 3기를 채우는 연쇄 합성까지 처리.
            }
        }

        private bool TryMergeOnce()
        {
            var groups = new Dictionary<(GuardianData data, int star), List<object>>();

            foreach (var entry in roster.Entries)
                AddToGroup(groups, entry.Data, entry.StarLevel, entry);

            foreach (var unit in GuardianUnit.ActiveUnits)
                AddToGroup(groups, unit.Data, unit.StarLevel, unit);

            foreach (var group in groups)
            {
                if (group.Key.star >= 3) continue; // 최대 3성, GDD.md 10번
                if (group.Value.Count < 3) continue;

                MergeGroup(group.Key.data, group.Key.star, group.Value.Take(3).ToList());
                return true;
            }
            return false;
        }

        private static void AddToGroup(
            Dictionary<(GuardianData, int), List<object>> groups, GuardianData data, int star, object source)
        {
            var key = (data, star);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new List<object>();
            list.Add(source);
        }

        private void MergeGroup(GuardianData data, int star, List<object> sources)
        {
            // 셋 중 하나라도 필드에 있었다면 그 슬롯을 그대로 승급시켜서 유지 — 필드 유닛이 갑자기 사라지는 걸 피함.
            var fieldUnit = sources.OfType<GuardianUnit>().FirstOrDefault();

            foreach (var source in sources)
            {
                if (source is RosterEntry entry)
                    roster.Remove(entry);
                else if (source is GuardianUnit unit && unit != fieldUnit)
                    unit.RemoveFromField();
            }

            int newStar = star + 1;
            if (fieldUnit != null)
                fieldUnit.SetStarLevel(newStar);
            else
                roster.Add(data, newStar);
        }
    }
}
