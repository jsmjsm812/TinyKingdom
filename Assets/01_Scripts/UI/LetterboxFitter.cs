using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.UI
{
    /// <summary>
    /// 게임 화면을 9:16(1080x1920, CanvasScaler 기준 해상도와 동일) 비율로 고정하고, 실제
    /// 창/모니터 비율이 다르면 남는 영역을 검은 여백으로 채운다(가로로 넓으면 좌우 필러박스,
    /// 세로로 길면 상하 레터박스). PC 전체화면(가로 모니터)에서 세로 게임 화면이 억지로
    /// 늘어나 보이는 문제와, 안드로이드 기기별 화면비 차이(18:9·19.5:9·20:9 등)로 UI가 미묘하게
    /// 눌리거나 늘어나는 문제를 같은 메커니즘 하나로 해결한다.
    ///
    /// gameRoot(실제 게임 UI를 전부 담는 자식)의 anchor를 화면 비율에 맞게 매 프레임 재계산하고,
    /// 남는 영역에 검은 Image 바(좌/우 또는 상/하)를 활성화한다. 씬에 미리 준비해두는 건
    /// LetterboxBootstrap.cs(TinyKingdom > Add Fullscreen Letterbox To All Scenes)가 담당한다.
    /// </summary>
    public class LetterboxFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform gameRoot;
        [SerializeField] private RectTransform barLeft;
        [SerializeField] private RectTransform barRight;
        [SerializeField] private RectTransform barTop;
        [SerializeField] private RectTransform barBottom;

        // CanvasScaler(Scale With Screen Size)의 matchWidthOrHeight도 화면비에 맞춰 같이
        // 바꿔야 한다 — anchor만 좁혀도 Canvas 자신의 "캔버스 단위" 크기가 실제 화면비를
        // 그대로 따라가므로(1080x1920이 아니게 됨), FieldConstants 등이 쓰는 절대 픽셀
        // 오프셋 기준 레이아웃이 안쪽에서 미묘하게 눌리거나 늘어난다. 가로로 넓을 땐
        // "높이 기준 맞춤"(1), 세로로 길 땐 "너비 기준 맞춤"(0)으로 강제하면, gameRoot의
        // 캔버스 단위 크기가 정확히 1080x1920으로 고정된다(계산은 클래스 하단 주석 참고).
        [SerializeField] private CanvasScaler canvasScaler;

        // 프로젝트 기준 해상도 — CanvasScaler의 m_ReferenceResolution(1080x1920)과 반드시
        // 같은 값이어야 한다(GDD.md "필드 · 그리드": 기준 해상도 1080×1920).
        private const float TargetWidth = 1080f;
        private const float TargetHeight = 1920f;
        private const float TargetAspect = TargetWidth / TargetHeight;

        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        private void Update()
        {
            // 매 프레임 다시 배치할 필요는 없다 — 창 크기 조절/전체화면 토글/기기 회전처럼
            // Screen.width·height가 실제로 바뀐 프레임에만 재계산한다.
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            Apply();
        }

        private void Apply()
        {
            if (gameRoot == null) return;
            float screenAspect = (float)Screen.width / Screen.height;

            if (screenAspect > TargetAspect)
            {
                // 화면이 기준보다 가로로 넓다(PC 전체화면의 가로 모니터 등) — 좌우를 검게
                // 채우고(필러박스) gameRoot 너비를 화면 높이 기준 9:16 폭으로 좁힌다.
                // CanvasScaler를 "높이 기준 맞춤"으로 바꿔 Canvas 자신의 높이(캔버스 단위)를
                // 정확히 1920으로 고정 — 그래야 아래 widthFraction 계산이 gameRoot를
                // 정확히 1080 단위 너비로 만든다(1920*screenAspect가 실제 canvas 폭 단위,
                // 거기에 TargetAspect/screenAspect를 곱하면 1920*TargetAspect=1080).
                if (canvasScaler != null) canvasScaler.matchWidthOrHeight = 1f;
                float widthFraction = TargetAspect / screenAspect;
                SetAnchors(gameRoot, (1f - widthFraction) / 2f, (1f + widthFraction) / 2f, 0f, 1f);
                SetBar(barLeft, 0f, (1f - widthFraction) / 2f, horizontal: true);
                SetBar(barRight, (1f + widthFraction) / 2f, 1f, horizontal: true);
                SetActive(barTop, false);
                SetActive(barBottom, false);
            }
            else if (screenAspect < TargetAspect)
            {
                // 화면이 기준보다 세로로 길다 — 위아래를 검게 채운다(레터박스). 위와 대칭으로
                // CanvasScaler를 "너비 기준 맞춤"으로 바꿔 Canvas 너비를 정확히 1080으로 고정.
                if (canvasScaler != null) canvasScaler.matchWidthOrHeight = 0f;
                float heightFraction = screenAspect / TargetAspect;
                SetAnchors(gameRoot, 0f, 1f, (1f - heightFraction) / 2f, (1f + heightFraction) / 2f);
                SetBar(barTop, (1f + heightFraction) / 2f, 1f, horizontal: false);
                SetBar(barBottom, 0f, (1f - heightFraction) / 2f, horizontal: false);
                SetActive(barLeft, false);
                SetActive(barRight, false);
            }
            else
            {
                SetAnchors(gameRoot, 0f, 1f, 0f, 1f);
                SetActive(barLeft, false);
                SetActive(barRight, false);
                SetActive(barTop, false);
                SetActive(barBottom, false);
            }
        }

        private static void SetAnchors(RectTransform rt, float xMin, float xMax, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // horizontal=true면 좌/우 바(x축 min~max 구간, y는 0~1 풀높이),
        // horizontal=false면 상/하 바(y축 min~max 구간, x는 0~1 풀너비).
        private static void SetBar(RectTransform rt, float minFraction, float maxFraction, bool horizontal)
        {
            if (rt == null) return;
            rt.gameObject.SetActive(true);
            if (horizontal)
                SetAnchors(rt, minFraction, maxFraction, 0f, 1f);
            else
                SetAnchors(rt, 0f, 1f, minFraction, maxFraction);
        }

        private static void SetActive(RectTransform rt, bool active)
        {
            if (rt != null) rt.gameObject.SetActive(active);
        }
    }
}
