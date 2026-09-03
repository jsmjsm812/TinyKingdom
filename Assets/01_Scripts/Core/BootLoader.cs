using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.Core
{
    /// <summary>
    /// 항상 가장 먼저 로드되는 부트 씬의 진입점. AudioManager는 같은 Boot 씬의 별도
    /// GameObject로 자리를 잡았고(AudioBootstrap.cs), 다음 씬으로 넘어가기 전에
    /// 세이브 파일을 읽어 GameProgress를 채운다(SaveSystem.cs) — 그래야 Home 화면이
    /// 뜨자마자 이전 진행 상태(해금된 구역·수호자)가 바로 반영된다.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene = "Title";

        private void Start()
        {
            SaveSystem.Load();
            SceneManager.LoadScene(nextScene);
        }
    }
}
