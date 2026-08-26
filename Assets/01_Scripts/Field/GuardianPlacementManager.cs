using UnityEngine;
using WitchHour.Combat;
using WitchHour.Merge;
using WitchHour.Shop;

namespace WitchHour.Field
{
    /// <summary>
    /// 소환 명부의 대기 수호자를 실제 필드 슬롯에 배치한다. 드래그 UI(2주차 상점 화면)가 이 클래스를 통해서만
    /// GuardianUnit을 생성하므로, 배치 관련 규칙(빈 슬롯 확인, 명부에서 제거, 합성 체크)이 한 곳에 모여 있다.
    /// </summary>
    public class GuardianPlacementManager : MonoBehaviour
    {
        [SerializeField] private GuardianUnit guardianPrefab;
        [SerializeField] private RosterManager roster;
        [SerializeField] private MergeSystem mergeSystem;

        public bool TryPlace(RosterEntry entry, GridSlot slot)
        {
            if (!slot.IsEmpty) return false;

            GuardianUnit unit = Instantiate(guardianPrefab, transform);
            unit.Setup(entry.Data, entry.StarLevel);

            if (!slot.TryPlace(unit))
            {
                Destroy(unit.gameObject);
                return false;
            }

            roster.Remove(entry);
            mergeSystem.TryMergeAll();
            return true;
        }
    }
}
