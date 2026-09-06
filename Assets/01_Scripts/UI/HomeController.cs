using UnityEngine;
using UnityEngine.SceneManagement;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.UI
{
    /// <summary>게임 메인(홈) 화면. 해금된 구역 중 하나를 골라 출전한다.</summary>
    public class HomeController : MonoBehaviour
    {
        [SerializeField] private string battleScene = "Battle";

        // 로비 화면을 열 때마다 이번 출전 계획(골드 200 + 아이템 상점)을 새로 시작한다 —
        // 아직 전투에 들어간 적 없이 로비만 오간 거라 이전 계획을 버려도 잃을 게 없다.
        private void Awake()
        {
            RunSession.BeginNewRun();
        }

        /// <summary>ZoneButtonView가 카드를 눌렀을 때 호출 — 잠긴 구역이면 아무 일도 안 한다.</summary>
        public void OnClickZone(ZoneData zone)
        {
            if (zone == null || !GameProgress.IsZoneUnlocked(zone.zoneIndex)) return;

            AudioManager.Instance?.PlayButtonClick();
            RunSession.SelectedZone = zone;
            SceneManager.LoadScene(battleScene);
        }
    }
}
