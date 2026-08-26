using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.UI
{
    /// <summary>
    /// 게임 메인(홈) 화면. 지금은 "게임 시작" 버튼 하나뿐이지만,
    /// 구역 선택·수호자 도감·설정 진입점이 늘어날 자리.
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [SerializeField] private string battleScene = "Battle";

        public void OnClickStartGame()
        {
            SceneManager.LoadScene(battleScene);
        }
    }
}
