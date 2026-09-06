#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;

namespace WitchHour.DebugTools
{
    /// <summary>
    /// F2 한 번으로 에디터 UI 없이 게임 화면만 고해상도 PNG로 저장한다(포폴용 스크린샷/영상
    /// 캡처 편의 목적). 배틀/로비 디버그 오버레이(DebugOverlay/HomeDebugOverlay) 양쪽 Update()에서
    /// CheckHotkey()만 호출하면 되도록 공용 유틸로 뺐다 — F1 패널 표시 여부와 무관하게 항상 눌리게
    /// (캡처하려는 화면에 디버그 패널 자체가 찍히면 안 되니 패널이 꺼진 상태에서도 동작해야 함).
    /// superSize: 2 = 실제 Game 뷰 해상도의 2배로 저장(1080x1920 기준이면 2160x3840) — 원본
    /// 해상도 그대로 저장하면 포폴/영상 편집 시 확대했을 때 흐려지는 걸 방지.
    /// </summary>
    public static class DebugScreenshotUtil
    {
        // 프로젝트 폴더(Assets 옆) 대신 바탕화면에 바로 쌓이게 — 유니티 프로젝트 폴더를 안
        // 뒤져도 되고, .gitignore에 신경 쓸 필요도 없어진다.
        private const string FolderName = "작은왕국_스크린샷";

        public static void CheckHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.F2)) return;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string folder = Path.Combine(desktop, FolderName);
            Directory.CreateDirectory(folder);

            string fileName = $"TinyKingdom_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(folder, fileName);

            ScreenCapture.CaptureScreenshot(fullPath, superSize: 2);
            Debug.Log($"[DebugScreenshotUtil] 스크린샷 저장: {fullPath}");
        }
    }
}
#endif
