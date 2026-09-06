using UnityEngine;
using UnityEngine.SceneManagement;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>화면 아무 곳이나 탭하면 홈 화면으로 넘어간다 (풀스크린 버튼에 연결).</summary>
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private string homeScene = "Home";

        public void OnClickAnywhere()
        {
            AudioManager.Instance?.PlayButtonClick();
            SceneManager.LoadScene(homeScene);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Home/Battle 씬은 각자 디버그 오버레이(HomeDebugOverlay/DebugOverlay)의 Update()에서
        // F2를 듣지만, Title 씬엔 그런 오버레이 자체가 없어서 F2가 하나도 안 먹혔다 — 여기서
        // 직접 호출해준다("타이틀만 스크린샷이 안 됨" 버그).
        private void Update()
        {
            WitchHour.DebugTools.DebugScreenshotUtil.CheckHotkey();
        }
#endif
    }
}
