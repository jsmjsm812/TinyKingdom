#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.DebugTools
{
    /// <summary>
    /// 로비씬 테스트용 디버그 오버레이(Battle의 DebugOverlay와 같은 F1 패턴). "도감에서 능력/스탯
    /// 확인하려고 캐릭터 다 풀고 싶다"는 요청 — GameProgress.UnlockedGuardians에 전부 넣어서
    /// 도감(GuardianCodexController.IsUnlocked)이 전부 해금된 것으로 보이게 한다. 저장은 안 되니
    /// (SaveSystem.Save를 안 부름) 앱을 재시작하면 원래대로 돌아간다 — 순수 확인용.
    /// </summary>
    public class HomeDebugOverlay : MonoBehaviour
    {
        private GameObject _panelRoot;
        private bool _visible;

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
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _panelRoot.SetActive(visible);
        }

        private void BuildUi()
        {
            var canvasGO = new GameObject("HomeDebugOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // 로비 화면 우측 상단은 "아이템 상점" 버튼이 이미 차지하고 있어서 거기 겹쳐 놓으면
            // 클릭이 그 버튼한테 먼저 먹혀버린다("디버그 버튼 눌러도 반응 없음" 원인이었음) —
            // 화면에서 비어있는 좌측 하단으로 옮긴다.
            _panelRoot = new GameObject("HomeDebugPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            _panelRoot.transform.SetParent(canvasGO.transform, false);
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(16, 16);
            panelRect.sizeDelta = new Vector2(260, 10);
            _panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var layout = _panelRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = _panelRoot.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddHeader("디버그 모드 (F1)");
            AddActionButton("전체 캐릭터 해금(도감 확인용)", OnClickUnlockAllGuardians);
        }

        private void OnClickUnlockAllGuardians()
        {
            var registry = Resources.Load<GuardianRegistry>("GuardianRegistry");
            if (registry == null)
            {
                Debug.LogWarning("[HomeDebugOverlay] GuardianRegistry를 Resources에서 못 찾음.");
                return;
            }

            int count = 0;
            foreach (var guardian in registry.allGuardians)
            {
                if (guardian == null) continue;
                if (GameProgress.UnlockedGuardians.Add(guardian)) count++;
            }
            Debug.Log($"[HomeDebugOverlay] 캐릭터 {count}명 추가 해금(도감에서 확인 가능, 세이브는 안 됨). 도감을 닫았다 다시 여세요.");
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

        private void AddActionButton(string label, UnityEngine.Events.UnityAction onClick)
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
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = label;
        }
    }
}
#endif
