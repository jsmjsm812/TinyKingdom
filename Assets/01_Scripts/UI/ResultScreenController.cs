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
        [SerializeField] private Text goldText;
        [SerializeField] private Text killsText;
        [SerializeField] private string homeScene = "Home";

        // 씬 스크립트가 SerializedObject 대입 대신 직접 필드를 채워 넣을 때 쓰는 안전한 경로
        // (ItemSlotView.EditorAssignItem과 같은 이유 — SerializedObject 대입이 안 먹힐 때가 있었음).
        public void EditorAssignStatTexts(Text gold, Text kills)
        {
            goldText = gold;
            killsText = kills;
        }

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
            ApplyStatTexts();
            panel.SetActive(true);
            AudioManager.Instance?.PlayVictory();
        }

        private void ShowFail()
        {
            titleText.text = "성벽 함락...";
            ApplyStatTexts();
            panel.SetActive(true);
            AudioManager.Instance?.PlayDefeat();
        }

        private void ApplyStatTexts()
        {
            if (goldText != null) goldText.text = $"획득한 골드: {RunSession.TotalGoldEarned}";
            if (killsText != null) killsText.text = $"처치한 침입자: {RunSession.InvadersDefeated}마리";
        }

        public void OnClickConfirm()
        {
            AudioManager.Instance?.PlayButtonClick();
            SceneManager.LoadScene(homeScene);
        }
    }
}
