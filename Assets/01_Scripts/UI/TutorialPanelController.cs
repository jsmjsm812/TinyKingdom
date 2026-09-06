using UnityEngine;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>
    /// 로비 하단 "설명" 버튼으로 여는 튜토리얼(게임 설명) 패널 — 슬라이더/저장값 같은 상태가
    /// 없는 순수 읽기 전용 패널이라 Open/Close만 있으면 충분하다.
    /// </summary>
    public class TutorialPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        public void Open()
        {
            // 도감/설정 패널과 같은 이유 — 씬 계층상 나중에 열리는 패널이 먼저 만들어진 다른
            // UI(구역 카드 등)에 가려지지 않도록 열 때마다 맨 앞으로 끌어올린다.
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }
    }
}
