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
