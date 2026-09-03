using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    public class ResultScreenController : MonoBehaviour
    {
        [SerializeField] private BattleFlowController battleFlow;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private string homeScene = "Home";

        private void OnEnable()
        {
            battleFlow.OnZoneCleared += ShowClear;
            battleFlow.OnZoneFailed += ShowFail;
        }

        private void OnDisable()
        {
            battleFlow.OnZoneCleared -= ShowClear;
            battleFlow.OnZoneFailed -= ShowFail;
        }

        private void ShowClear()
        {
            titleText.text = "구역 클리어!";
            panel.SetActive(true);
            AudioManager.Instance?.PlayVictory();
        }

        private void ShowFail()
        {
            titleText.text = "성벽 함락...";
            panel.SetActive(true);
            AudioManager.Instance?.PlayDefeat();
        }

        public void OnClickConfirm()
        {
            AudioManager.Instance?.PlayButtonClick();
            SceneManager.LoadScene(homeScene);
        }
    }
}
