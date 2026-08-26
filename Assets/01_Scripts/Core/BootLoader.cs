using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.Core
{
    /// <summary>
    /// 항상 가장 먼저 로드되는 부트 씬의 진입점. 지금은 바로 타이틀로 넘어가지만,
    /// 세이브 로드·오디오 초기화 등 전역 매니저를 두는 자리로 비워둔다.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = "Title";

        private void Start()
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
