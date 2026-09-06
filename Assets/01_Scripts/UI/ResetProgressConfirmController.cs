using UnityEngine;
using UnityEngine.SceneManagement;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>
    /// 로비 설정 패널의 "진행 상황 초기화" 버튼이 여는 확인 팝업 — 플레이어가 직접 세이브를
    /// 지울 수 있게 해달라는 요청(디버그 전용 F1 오버레이 말고 실제 플레이어용 기능)이라,
    /// 실수로 전부 날리는 일이 없도록 한 번 더 확인받는다.
    /// </summary>
    public class ResetProgressConfirmController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        public void Open()
        {
            if (panel != null) panel.transform.SetAsLastSibling();
            if (panel != null) panel.SetActive(true);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void OnClickConfirmReset()
        {
            SaveSystem.DeleteSave();
            AudioManager.Instance?.PlayButtonClick();
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
