using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchHour.Combat;

namespace WitchHour.Field
{
    public enum GridSide { Left, Right }

    public class GridSlot : MonoBehaviour
    {
        // 존2 "붕괴된 슬롯" 기믹용 — 무너진 잔해로 표시하고 배치를 막는다.
        private static readonly Color BlockedColor = new Color(0.22f, 0.2f, 0.18f, 0.9f);
        private static readonly Color BlockedOutlineColor = new Color(0.4f, 0.38f, 0.35f, 0.5f);

        private Image _image;
        private Outline _outline;
        private Color _originalImageColor;
        private Color _originalOutlineColor;
        private bool _originalColorsCached;

        public int Index { get; private set; }
        public GridSide Side { get; private set; }
        public int Row { get; private set; }

        // 예전엔 BuildGrid가 계산한 좌표를 Init 시점에 별도 필드로 캐싱했는데, 그러면 Scene에서
        // 손으로 슬롯을 옮겨도(RectTransform만 바뀜) 사거리/공격 계산이 쓰는 이 값은 그대로라
        // "씬에서 옮겨도 게임엔 반영 안 됨" 문제가 있었다. RectTransform.anchoredPosition을 매번
        // 그대로 읽어오게 바꿔서, 슬롯을 어떻게 옮기든(코드로 생성했든 손으로 드래그했든) 항상
        // 실제 위치와 일치하게 한다.
        public Vector2 FieldPosition => ((RectTransform)transform).anchoredPosition;

        public GuardianUnit Occupant { get; private set; }

        public bool IsBlocked { get; private set; }

        // IsBlocked도 여기서 같이 걸러서, TryPlace/GridManager.FindEmptySlot 둘 다 따로 안
        // 고쳐도 자동으로 막힌 슬롯을 "빈 자리 아님" 취급하게 된다.
        public bool IsEmpty => Occupant == null && !IsBlocked;

        public void Init(int index, GridSide side, int row)
        {
            Index = index;
            Side = side;
            Row = row;
        }

        /// <summary>존2 "붕괴된 슬롯" 기믹 — WaveSpawner.ApplyZoneVisuals가 구역 시작/전환 시
        /// ZoneData.blockedSlotIndices 기준으로 호출한다. 이미 유닛이 있으면(디버그 구역 전환
        /// 등으로) 무시 — 강제로 쫓아내지 않는다.</summary>
        public void SetBlocked(bool blocked)
        {
            if (blocked && Occupant != null) return;

            if (!_originalColorsCached)
            {
                _image = GetComponent<Image>();
                _outline = GetComponent<Outline>();
                if (_image != null) _originalImageColor = _image.color;
                if (_outline != null) _originalOutlineColor = _outline.effectColor;
                _originalColorsCached = true;
            }

            IsBlocked = blocked;
            if (_image != null) _image.color = blocked ? BlockedColor : _originalImageColor;
            if (_outline != null) _outline.effectColor = blocked ? BlockedOutlineColor : _originalOutlineColor;
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
