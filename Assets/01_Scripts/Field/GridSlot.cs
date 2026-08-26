using UnityEngine;
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
    }
}
