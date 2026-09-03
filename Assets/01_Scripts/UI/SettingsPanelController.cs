using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>
    /// 설정 패널 — BGM/효과음 음량 슬라이더. AudioManager는 씬 바깥(DontDestroyOnLoad) 오브젝트라
    /// 이 컨트롤러를 독립 프리팹 에셋으로 저장할 수 없다(프리팹은 씬 오브젝트를 못 가리킴) —
    /// ShopUI/HudUI와 같은 이유로 그냥 씬 오브젝트로 둔다. 대신 SettingsBootstrap의 "이미 있으면
    /// 다시 안 지음" 가드로 재실행해도 위치가 안 지워지게 보호된다.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        // 설정창을 열면 전투를 일시정지한다(Time.timeScale = 0). Home 화면처럼 timeScale에
        // 의존하는 게 없는 씬에서 열어도 무해하고, UI 클릭/슬라이더는 timeScale과 무관하게
        // 계속 동작한다. 닫을 때는 열기 직전 배속(1배속/2배속)을 그대로 복원한다 — 무조건
        // 1배속으로 리셋하면 배속 버튼으로 2배속 중이던 유저가 설정만 열었다 닫아도 속도가
        // 풀려버린다.
        private float _timeScaleBeforeOpen = 1f;

        public void Open()
        {
            if (AudioManager.Instance != null)
            {
                bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
                sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            }
            _timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            panel.SetActive(true);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            Time.timeScale = _timeScaleBeforeOpen;
            panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void OnBgmVolumeChanged(float value)
        {
            AudioManager.Instance?.SetBgmVolume(value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
        }
    }
}
