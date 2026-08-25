using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.Field
{
    /// <summary>
    /// GDD.md 3번: 좌측 2열x5행 + 우측 2열x5행 = 20칸을 통로(2유닛 폭, 화면 정중앙) 좌우에 생성한다.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("배치 대상 루트 (Canvas 하위 RectTransform)")]
        [SerializeField] private RectTransform battlefieldRoot;

        [Header("슬롯 시각 프리팹 (비워두면 코드로 반투명 사각형 생성)")]
        [SerializeField] private GameObject slotPrefab;

        public IReadOnlyList<GridSlot> Slots => _slots;
        private readonly List<GridSlot> _slots = new List<GridSlot>();

        private void Awake()
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            float rowHeight = (FieldConstants.LaneLengthUnits / FieldConstants.RowsPerSide) * FieldConstants.UnitSize;
            float columnWidth = FieldConstants.UnitSize;
            float laneHalfWidth = (FieldConstants.LaneWidthUnits / 2f) * FieldConstants.UnitSize;
            float topY = (FieldConstants.LaneLengthUnits / 2f) * FieldConstants.UnitSize;

            int index = 0;
            foreach (GridSide side in new[] { GridSide.Left, GridSide.Right })
            {
                for (int row = 0; row < FieldConstants.RowsPerSide; row++)
                {
                    for (int col = 0; col < FieldConstants.ColumnsPerSide; col++)
                    {
                        float x = laneHalfWidth + columnWidth * (col + 0.5f);
                        if (side == GridSide.Left) x = -x;
                        float y = topY - rowHeight * (row + 0.5f);

                        var slot = CreateSlot(index, side, row, new Vector2(x, y), columnWidth, rowHeight);
                        _slots.Add(slot);
                        index++;
                    }
                }
            }
        }

        private GridSlot CreateSlot(int index, GridSide side, int row, Vector2 fieldPosition, float width, float height)
        {
            GameObject go;
            if (slotPrefab != null)
            {
                go = Instantiate(slotPrefab, battlefieldRoot);
            }
            else
            {
                go = new GameObject($"Slot_{side}_{index}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(battlefieldRoot, false);
                go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = fieldPosition;

            var slot = go.GetComponent<GridSlot>();
            if (slot == null) slot = go.AddComponent<GridSlot>();
            slot.Init(index, side, row, fieldPosition);
            return slot;
        }

        public GridSlot FindEmptySlot()
        {
            foreach (var slot in _slots)
                if (slot.IsEmpty) return slot;
            return null;
        }
    }
}
