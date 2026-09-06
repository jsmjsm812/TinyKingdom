#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;
using WitchHour.Shop;

namespace WitchHour.DebugTools
{
    /// <summary>
    /// 배틀 씬 테스트용 디버그 오버레이. F1로 켜고 끈다.
    /// - 슬롯마다 번호/좌표 라벨 + 실제로 드래그앤드롭이 먹히는지(레이캐스트가 그 슬롯에
    ///   닿는지) 초록/빨강 테두리로 표시 — "슬롯에 안 들어가진다" 버그를 정확한 슬롯 번호로
    ///   바로 짚을 수 있게 한다.
    /// - 금화 무한 / 성벽 무적 / 웨이브 즉시 클리어 / 준비시간 스킵 버튼.
    /// UNITY_EDITOR || DEVELOPMENT_BUILD에서만 컴파일되므로 정식 릴리즈 빌드에는 아예 포함되지
    /// 않는다 — 나중에 따로 걷어낼 필요 없음.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private WardHealth ward;
        [SerializeField] private RunCurrency currency;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private SummonPool summonPool;
        [SerializeField] private ZoneData[] zones;
        [SerializeField] private GuardianData[] allGuardians;

        private Canvas _canvas;
        private GameObject _panelRoot;
        private Text _infiniteManaButtonText;
        private Text _invincibleButtonText;
        private bool _visible;

        private readonly Dictionary<GridSlot, Text> _slotLabels = new Dictionary<GridSlot, Text>();
        private readonly Dictionary<GridSlot, Outline> _slotOutlines = new Dictionary<GridSlot, Outline>();
        private readonly Dictionary<GridSlot, Color> _originalOutlineColors = new Dictionary<GridSlot, Color>();

        private static readonly Color BlockedColor = new Color(1f, 0.2f, 0.2f, 1f);
        private static readonly Color ClearColor = new Color(0.3f, 1f, 0.4f, 1f);

        public void Configure(GridManager grid, WardHealth wardHealth, RunCurrency runCurrency, WaveSpawner spawner,
            SummonPool pool = null, ZoneData[] zoneList = null, GuardianData[] guardianList = null)
        {
            gridManager = grid;
            ward = wardHealth;
            currency = runCurrency;
            waveSpawner = spawner;
            if (pool != null) summonPool = pool;
            if (zoneList != null) zones = zoneList;
            if (guardianList != null) allGuardians = guardianList;
        }

        private void Awake()
        {
            BuildUi();
            SetVisible(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                SetVisible(!_visible);

            DebugScreenshotUtil.CheckHotkey();

            if (_visible)
                RefreshSlotOverlay();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _panelRoot.SetActive(visible);
            foreach (var label in _slotLabels.Values)
                label.gameObject.SetActive(visible);

            if (!visible)
                RestoreSlotOutlineColors();
        }

        // ── 슬롯 오버레이 ──────────────────────────────────────────────────

        private void RefreshSlotOverlay()
        {
            if (gridManager == null) return;

            foreach (var slot in gridManager.Slots)
            {
                if (slot == null) continue;
                EnsureSlotLabel(slot);

                var rect = (RectTransform)slot.transform;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                bool reachable = IsSlotReachable(slot, screenPoint);

                if (_slotOutlines.TryGetValue(slot, out var outline) && outline != null)
                    outline.effectColor = reachable ? ClearColor : BlockedColor;

                if (_slotLabels.TryGetValue(slot, out var label) && label != null)
                    label.text = $"#{slot.Index}\n{rect.anchoredPosition.x:F0},{rect.anchoredPosition.y:F0}";
            }
        }

        // 실제 GridSlot.FindUnderPointer가 하는 것과 똑같은 방식으로 레이캐스트해서, 이 슬롯의
        // 중심 지점을 클릭했을 때 실제로 이 슬롯이 히트되는지 확인한다 — 다른 UI가 위에 덮여서
        // 드래그앤드롭이 먹히지 않는 슬롯을 정확히 잡아낸다.
        private static bool IsSlotReachable(GridSlot slot, Vector2 screenPoint)
        {
            if (EventSystem.current == null) return true;

            var pointerData = new PointerEventData(EventSystem.current) { position = screenPoint };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                var hitSlot = result.gameObject.GetComponentInParent<GridSlot>();
                if (hitSlot != null) return hitSlot == slot;
            }
            return false; // 아무것도 안 맞음 = 이 슬롯도 못 맞춤
        }

        private void EnsureSlotLabel(GridSlot slot)
        {
            if (_slotLabels.ContainsKey(slot)) return;

            var outline = slot.GetComponent<Outline>();
            if (outline != null)
            {
                _slotOutlines[slot] = outline;
                _originalOutlineColors[slot] = outline.effectColor;
            }

            var labelGO = new GameObject("DebugLabel", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(slot.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(180, 60);

            var text = labelGO.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false; // 드래그앤드롭 판정에 절대 끼어들면 안 됨
            var outline2 = labelGO.AddComponent<Outline>();
            outline2.effectColor = new Color(0, 0, 0, 0.9f);
            outline2.effectDistance = new Vector2(1.5f, -1.5f);

            _slotLabels[slot] = text;
            labelGO.SetActive(_visible);
        }

        private void RestoreSlotOutlineColors()
        {
            foreach (var kv in _originalOutlineColors)
            {
                if (kv.Key != null && _slotOutlines.TryGetValue(kv.Key, out var outline) && outline != null)
                    outline.effectColor = kv.Value;
            }
        }

        // ── 치트 패널 ──────────────────────────────────────────────────

        private void BuildUi()
        {
            var canvasGO = new GameObject("DebugOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000; // 다른 UI 다 덮고 항상 맨 위
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Battle 씬 캔버스와 동일
            scaler.matchWidthOrHeight = 0.5f;

            _panelRoot = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            _panelRoot.transform.SetParent(canvasGO.transform, false);
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-16, -260); // HUD 아래
            panelRect.sizeDelta = new Vector2(260, 10); // 높이는 LayoutGroup이 콘텐츠에 맞춰 늘림
            _panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var layout = _panelRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = _panelRoot.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddHeader("디버그 모드 (F1)");
            _infiniteManaButtonText = AddToggleButton("금화 무한", OnToggleInfiniteMana);
            _invincibleButtonText = AddToggleButton("성벽 무적", OnToggleInvincible);
            AddActionButton("웨이브 즉시 클리어", () => waveSpawner?.DebugKillAllInvaders());
            AddActionButton("준비시간 스킵", () => waveSpawner?.DebugSkipPrep());

            if (zones != null && zones.Length > 0)
            {
                AddHeader("구역 전환");
                foreach (var zone in zones)
                {
                    if (zone == null) continue;
                    var z = zone; // 클로저 캡처용 로컬 복사
                    AddActionButton($"→ {z.zoneName}", () => waveSpawner?.DebugSwitchZone(z));
                }
            }

            if (summonPool != null && allGuardians != null && allGuardians.Length > 0)
            {
                AddHeader("수호자");
                AddActionButton("전체 캐릭터 해금", () => summonPool.Unlock(allGuardians));
            }

            AddHeader("슬롯: 초록=정상 / 빨강=막힘");
        }

        private void AddHeader(string label)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_panelRoot.transform, false);
            var text = go.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.4f);
            text.text = label;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 28;
        }

        private Text AddActionButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panelRoot.transform, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGO.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return text;
        }

        private Text AddToggleButton(string label, System.Action<Text, bool> onToggle)
        {
            bool state = false;
            Text text = null;
            text = AddActionButton($"{label}: OFF", () =>
            {
                state = !state;
                text.text = $"{label}: {(state ? "ON" : "OFF")}";
                onToggle(text, state);
            });
            return text;
        }

        private void OnToggleInfiniteMana(Text buttonText, bool on) => currency?.DebugSetInfiniteMana(on);

        private void OnToggleInvincible(Text buttonText, bool on)
        {
            if (ward != null) ward.DebugInvincible = on;
        }
    }
}
#endif
