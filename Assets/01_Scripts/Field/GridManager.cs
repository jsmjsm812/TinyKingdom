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
    // [ExecuteAlways]: Awake()가 Play를 눌러야만 실행되면 Scene 화면엔 씬 파일에 저장된
    // 예전 슬롯이 그대로 남아있고 Play를 눌러야만 최신 FieldConstants로 다시 그려진다 —
    // "Scene 화면이랑 Play 화면 슬롯이 다르다" 피드백의 원인. 이 어트리뷰트를 붙이면 스크립트를
    // 재컴파일하거나 씬을 열 때도(Play 여부와 무관하게) Awake가 실행돼 Scene 화면이 항상
    // 최신 FieldConstants 기준으로 자동 갱신된다 — FieldConstants 값을 바꾸고 저장하면
    // Play 없이도 바로 눈으로 확인/조절할 수 있다.
    [ExecuteAlways]
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

        // 존2 "붕괴된 슬롯" 기믹처럼 구역별로 슬롯을 잠가야 하는 코드(WaveSpawner)가 Slots
        // 목록에 접근해야 하는데, GuardianUnit 프리팹과 마찬가지로 씬 오브젝트를 직접 참조할
        // 방법이 없어서 BattlefieldRoot와 같은 패턴으로 정적 참조를 노출한다.
        public static GridManager Instance { get; private set; }

        private void Awake()
        {
            BattlefieldRoot = battlefieldRoot;
            Instance = this;

            // 씬에 이미 슬롯이 있으면(직접 배치해서 저장한 것이든, 예전에 BuildGrid가 구워둔
            // 것이든) 지우지 않고 그대로 쓴다 — "Scene에서 위치를 손으로 옮기면 게임에도
            // 반영되게 하고 싶다" 요청에 맞춘 것. 슬롯이 하나도 없을 때만(처음 씬을 만들었을 때,
            // 또는 아래 RebuildGrid로 일부러 초기화했을 때) FieldConstants 기준으로 새로 만든다.
            var existing = battlefieldRoot != null ? battlefieldRoot.GetComponentsInChildren<GridSlot>(true) : null;
            if (existing != null && existing.Length > 0)
            {
                _slots.Clear();
                // GridSlot.Index/Side/Row는 직렬화되는 필드가 아니라 순수 런타임 프로퍼티라서
                // (Init()으로만 세팅됨), 손으로 배치해 씬에 저장해둔 슬롯은 씬을 다시 열 때마다
                // 전부 기본값(Index=0)으로 리셋된다 — 지금까진 아무도 Index로 슬롯을 찾을 일이
                // 없어서 안 드러났는데, 체크포인트 복원(GuardianPlacementManager.PlaceAtSlot)이
                // Index로 슬롯을 찾으면서 전부 0이라 첫 번째 슬롯만 찾아지는 버그가 됐다("그만하기
                // 후 슬롯이 저장이 안 되어 있다" 피드백). 계층 순서(항상 안정적)로 Index를 다시
                // 매겨서 저장 시점과 복원 시점에 항상 같은 슬롯이 같은 Index를 갖게 한다.
                for (int i = 0; i < existing.Length; i++)
                    existing[i].Init(i, existing[i].Side, existing[i].Row);
                _slots.AddRange(existing);
                return;
            }

            BuildGrid();
        }

        /// <summary>
        /// 지금 씬에 있는 슬롯(손으로 옮긴 것 포함)을 전부 지우고 FieldConstants 기준값으로
        /// 초기 배치를 새로 굽는다 — "손대기 전 기본 배치로 되돌리고 싶을 때"만 쓴다.
        /// Inspector에서 GridManager 컴포넌트 우클릭 → 이 메뉴로 실행할 수 있다.
        /// </summary>
        [ContextMenu("Rebuild Grid From FieldConstants (기존 배치 초기화)")]
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
                float shelfOffsetX = FieldConstants.ShelfColumnOffsetX[shelf];

                foreach (GridSide side in new[] { GridSide.Left, GridSide.Right })
                {
                    float y = shelfY[shelf] + (side == GridSide.Left ? 1f : -1f) * FieldConstants.SlotOffsetFromPath;

                    for (int col = 0; col < columnXs.Length; col++)
                    {
                        var slot = CreateSlot(index, side, col, new Vector2(columnXs[col] + shelfOffsetX, y), slotSize, slotSize);
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
            slot.Init(index, side, row);
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
