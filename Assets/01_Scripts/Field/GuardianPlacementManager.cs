using UnityEngine;
using WitchHour.Combat;
using WitchHour.Data;
using WitchHour.Merge;

namespace WitchHour.Field
{
    /// <summary>
    /// 필드에 수호자를 직접 배치·재배치한다(롤토체스 방식 — 상점에서 사면 곧장 필드에 놓이고,
    /// 명부 같은 대기 단계 없이 플레이어가 필드 안에서 자유롭게 위치를 바꿈).
    /// </summary>
    public class GuardianPlacementManager : MonoBehaviour
    {
        [SerializeField] private GuardianUnit guardianPrefab;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private MergeSystem mergeSystem;

        public bool HasEmptySlot => gridManager.FindEmptySlot() != null;

        /// <summary>상점 구매 직후 호출 — 빈 슬롯에 자동 배치하고 합성 조건을 확인한다.</summary>
        public bool PlaceNew(GuardianData data)
        {
            GridSlot slot = gridManager.FindEmptySlot();
            if (slot == null) return false;

            GuardianUnit unit = Instantiate(guardianPrefab, transform);
            unit.Setup(data, starLevel: 1);

            if (!slot.TryPlace(unit))
            {
                Destroy(unit.gameObject);
                return false;
            }

            mergeSystem.TryMergeAll();
            return true;
        }

        /// <summary>필드 안에서 유닛을 드래그해 다른 슬롯 위에 놓았을 때 호출 — 비어 있으면 이동, 차 있으면 서로 자리를 바꾼다.</summary>
        public bool TryMove(GridSlot fromSlot, GridSlot toSlot)
        {
            if (fromSlot == null || toSlot == null || fromSlot == toSlot) return false;

            GuardianUnit moving = fromSlot.Occupant;
            if (moving == null) return false;

            GuardianUnit swapped = toSlot.Occupant;
            fromSlot.Clear();
            toSlot.Clear();
            toSlot.TryPlace(moving);
            if (swapped != null) fromSlot.TryPlace(swapped);
            return true;
        }
    }
}
