using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.Field
{
    /// <summary>
    /// ㄹ자 통로(LanePath) 주변에 슬롯을 배치한다 — 가로 선반(Shelf) 3개는 위/아래로,
    /// 선반을 잇는 세로 낙하(Drop) 2개는 꺾이는 바깥쪽 한 줄에만 슬롯을 세로로 쌓는다.
    /// 선반/낙하 좌표는 전부 FieldConstants를 참조하므로 LanePath.cs와 값이 항상 맞는다.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("배치 대상 루트 (Canvas 하위 RectTransform)")]
        [SerializeField] private RectTransform battlefieldRoot;

        [Header("슬롯 시각 프리팹 (비워두면 코드로 반투명 사각형 생성)")]
        [SerializeField] private GameObject slotPrefab;

        [Tooltip("slotPrefab이 없을 때 코드로 만드는 슬롯에 쓸 둥근 모서리 스프라이트(선택, 에디터 부트스트랩이 주입)")]
        [SerializeField] private Sprite slotSprite;

        public IReadOnlyList<GridSlot> Slots => _slots;
        private readonly List<GridSlot> _slots = new List<GridSlot>();

        // GuardianUnit은 프리팹 하나를 복제해 쓰므로 씬의 battlefieldRoot를 직접 참조할 수 없다
        // (프리팹은 씬 오브젝트를 참조 못 함). 씬에 하나뿐인 GridManager를 통해 정적으로 노출해서
        // 공격 이펙트가 슬롯/침입자와 같은 좌표계(FieldPosition)에 놓이도록 한다.
        public static RectTransform BattlefieldRoot { get; private set; }

        private void Awake()
        {
            BattlefieldRoot = battlefieldRoot;
            // 씬을 껐다 켜거나(에디터 툴이 매번 OpenScene을 하니까 자주 일어남) 재컴파일되면
            // _slots 리스트는 비어있는 새 인스턴스로 시작하는데, 씬 파일 자체엔 예전에 저장해둔
            // Slot_* 오브젝트가 그대로 남아있다 — 그 상태에서 BuildGrid()가 그걸 모르고 위에
            // 새로 22개를 또 만들어서 계속 쌓여왔다(하이어라키에 Slot_Left_0이 여러 번 보이던 원인).
            // _slots가 아니라 battlefieldRoot 실제 자식을 직접 스캔해서 지워야 이 누적이 안 생긴다.
            ClearExistingSlots();
            BuildGrid();
        }

        /// <summary>
        /// FieldConstants 값을 바꾼 뒤 씬에 이미 저장된(예전 값으로 만들어진) 슬롯을 지우고
        /// 새 값으로 다시 만든다. 에디터 툴에서 FixBattleLayout 실행 전에 반드시 호출할 것 —
        /// 안 그러면 레이아웃 계산은 최신 상수를 쓰는데 실제 슬롯은 옛날 위치에 남아 어긋난다.
        /// </summary>
        public void RebuildGrid()
        {
            ClearExistingSlots();
            BuildGrid();
        }

        // _slots(런타임 전용 리스트)만 믿지 않고 battlefieldRoot 밑에 실제로 있는 GridSlot을
        // 전부 찾아서 지운다 — 이래야 이전 세션에 저장된 채 _slots엔 없는 고아 슬롯도 같이 청소된다.
        private void ClearExistingSlots()
        {
            if (battlefieldRoot == null) return;
            var existing = battlefieldRoot.GetComponentsInChildren<GridSlot>(true);
            foreach (var slot in existing)
            {
                if (slot == null) continue;
                if (Application.isPlaying) Destroy(slot.gameObject);
                else DestroyImmediate(slot.gameObject);
            }
            _slots.Clear();
        }

        private void BuildGrid()
        {
            int index = 0;
            float[] shelfY = FieldConstants.ShelfY;
            // 슬롯 위치/간격은 UnitSize 기준 그대로 두고, 화면에 그려지는 박스 크기만 더 작게
            // (SlotVisualSize) — 슬롯 사이 여백이 넓어져서 필드가 덜 답답해 보인다.
            float slotSize = FieldConstants.SlotVisualSize;

            // 선반마다: 선반 라인 위(Above=Left)/아래(Below=Right)로 슬롯 열을 나란히 둔다.
            for (int shelf = 0; shelf < shelfY.Length; shelf++)
            {
                float[] columnXs = CenteredOffsets(FieldConstants.SlotColumnsPerShelfSide, FieldConstants.SlotColumnSpacing);

                foreach (GridSide side in new[] { GridSide.Left, GridSide.Right })
                {
                    float y = shelfY[shelf] + (side == GridSide.Left ? 1f : -1f) * FieldConstants.SlotOffsetFromPath;

                    for (int col = 0; col < columnXs.Length; col++)
                    {
                        var slot = CreateSlot(index, side, col, new Vector2(columnXs[col], y), slotSize, slotSize);
                        _slots.Add(slot);
                        index++;
                    }
                }
            }

            // 낙하마다: 경로가 화면 가장자리 쪽으로 꺾이는 바깥쪽(outer)에만 슬롯을 세로로 쌓는다.
            // 안쪽은 다음 선반이 되돌아오는 좁은 공간이라 비워둔다(사용자 스케치 기준).
            float halfWidth = FieldConstants.ShelfHalfWidth;
            for (int drop = 0; drop < shelfY.Length - 1; drop++)
            {
                float dropX = (drop % 2 == 0) ? halfWidth : -halfWidth;
                float midY = (shelfY[drop] + shelfY[drop + 1]) / 2f;
                float[] rowYs = CenteredOffsets(FieldConstants.SlotRowsPerDropSide, FieldConstants.SlotRowSpacing);

                GridSide outerSide = dropX > 0f ? GridSide.Right : GridSide.Left;
                float x = dropX + Mathf.Sign(dropX) * FieldConstants.SlotOffsetFromPath;

                for (int row = 0; row < rowYs.Length; row++)
                {
                    var slot = CreateSlot(index, outerSide, row, new Vector2(x, midY + rowYs[row]), slotSize, slotSize);
                    _slots.Add(slot);
                    index++;
                }
            }
        }

        // count개를 spacing 간격으로 0을 중심에 두고 대칭 배치한 오프셋 배열을 만든다.
        private static float[] CenteredOffsets(int count, float spacing)
        {
            var offsets = new float[count];
            float start = -(count - 1) * spacing / 2f;
            for (int i = 0; i < count; i++)
                offsets[i] = start + spacing * i;
            return offsets;
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
                // 주의: 이 클래스는 런타임 스크립트라 UnityEditor API(둥근 스프라이트 등)를 못 쓴다 —
                // 그런 꾸미기는 에디터 전용 부트스트랩 스크립트 쪽에서 slotSprite로 주입해야 함.
                go = new GameObject($"Slot_{side}_{index}", typeof(RectTransform), typeof(Image), typeof(Outline));
                go.transform.SetParent(battlefieldRoot, false);
                var image = go.GetComponent<Image>();
                if (slotSprite != null)
                {
                    image.sprite = slotSprite;
                    image.type = Image.Type.Sliced;
                    // "롤토체스 슬롯처럼 반투명하게" 피드백 — 알파만 낮추는 건 부족했다. 이
                    // 슬롯 스프라이트는 테두리 안쪽에 원목 질감 채우기가 통짜로 칠해져 있어서,
                    // 알파를 낮춰도 그 채우기 색이 옅게 남아 "반투명"보다는 "흐릿한 불투명"으로
                    // 보인다. fillCenter를 꺼서 9-slice의 가운데(채우기)는 아예 안 그리고 테두리
                    // (빛나는 금테)만 남기면, 안쪽은 완전히 뚫려서 길/캐릭터가 그대로 비친다.
                    image.fillCenter = false;
                    image.color = new Color(1f, 1f, 1f, 0.85f);
                }
                else
                {
                    image.color = new Color(0.95f, 0.9f, 0.7f, 0.35f); // 은은한 소환진 느낌 — 순백색 박스보다 덜 튐
                }
                var outline = go.GetComponent<Outline>();
                outline.effectColor = new Color(1f, 0.95f, 0.75f, 0.6f);
                outline.effectDistance = new Vector2(2, -2);
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
