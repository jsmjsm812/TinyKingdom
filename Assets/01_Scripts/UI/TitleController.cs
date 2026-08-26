using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.UI
{
    /// <summary>화면 아무 곳이나 탭하면 홈 화면으로 넘어간다 (풀스크린 버튼에 연결).</summary>
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private string homeScene = "Home";

        public void OnClickAnywhere()
        {
            SceneManager.LoadScene(homeScene);
        }
    }
}
