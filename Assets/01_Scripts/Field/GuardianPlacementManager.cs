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

        // WaveSpawner가 체크포인트 복원 시(씬 오브젝트를 직접 참조 못 하는 static 컨텍스트에서)
        // 필드에 저장된 수호자를 다시 배치하려고 찾는 정적 참조 — GridManager.Instance와 같은 패턴.
        public static GuardianPlacementManager Instance { get; private set; }

        public bool HasEmptySlot => gridManager.FindEmptySlot() != null;

        private void Awake()
        {
            Instance = this;
        }

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

        /// <summary>체크포인트 복원 전용 — 저장된 슬롯 인덱스·성급 그대로 되살린다. 저장 당시
        /// 이미 합성이 끝난 상태를 그대로 저장/복원하는 거라 TryMergeAll을 부르지 않는다(합성
        /// 가능한 조합이 남아있을 리 없음 — 있었다면 저장 시점에 이미 합쳐졌을 것).</summary>
        public bool PlaceAtSlot(GuardianData data, int slotIndex, int starLevel)
        {
            GridSlot slot = FindSlotByIndex(slotIndex);
            if (slot == null || !slot.IsEmpty) return false;

            GuardianUnit unit = Instantiate(guardianPrefab, transform);
            unit.Setup(data, starLevel);

            if (!slot.TryPlace(unit))
            {
                Destroy(unit.gameObject);
                return false;
            }
            return true;
        }

        private GridSlot FindSlotByIndex(int index)
        {
            foreach (var slot in gridManager.Slots)
                if (slot.Index == index) return slot;
            return null;
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
