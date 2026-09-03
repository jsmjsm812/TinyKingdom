using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using WitchHour.Combat;

namespace WitchHour.Field
{
    public enum GridSide { Left, Right }

    public class GridSlot : MonoBehaviour
    {
        public int Index { get; private set; }
        public GridSide Side { get; private set; }
        public int Row { get; private set; }
        public Vector2 FieldPosition { get; private set; }
        public GuardianUnit Occupant { get; private set; }

        public bool IsEmpty => Occupant == null;

        public void Init(int index, GridSide side, int row, Vector2 fieldPosition)
        {
            Index = index;
            Side = side;
            Row = row;
            FieldPosition = fieldPosition;
        }

        public bool TryPlace(GuardianUnit unit)
        {
            if (!IsEmpty) return false;

            Occupant = unit;
            unit.transform.SetParent(transform, false);
            var rect = unit.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = Vector2.zero;
            unit.OnPlaced(this);
            return true;
        }

        public void Clear()
        {
            Occupant = null;
        }

        /// <summary>드래그 드롭 지점 아래의 GridSlot을 찾는다. 필드 유닛 드래그(GuardianUnit)에서 공용으로 쓴다.</summary>
        public static GridSlot FindUnderPointer(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var result in results)
            {
                var slot = result.gameObject.GetComponentInParent<GridSlot>();
                if (slot != null) return slot;
            }
            return null;
        }
    }
}
