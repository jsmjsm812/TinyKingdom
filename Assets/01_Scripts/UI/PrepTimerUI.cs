using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Shop;

namespace WitchHour.UI
{
    /// <summary>
    /// 웨이브 시작 전 준비 시간 카운트다운을 표시한다. WaveSpawner는 상점을 몰라야 하므로,
    /// 준비 시간이 시작될 때 상점을 무료로 새로고침하는 것도 이 클래스가 대신 해준다
    /// (GDD.md 9번: 웨이브 전환 시 4칸 무료 자동 새로고침).
    /// </summary>
    public class PrepTimerUI : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private Text timerText;

        // OnPrepTimeChanged는 매 프레임 실수 값으로 불려서, 초 단위가 실제로 바뀔 때만
        // 틱 소리를 내려고 마지막으로 재생한 정수 초를 기억해둔다.
        private int _lastTickSecond = -1;

        private void OnEnable()
        {
            waveSpawner.OnPrepPhaseStarted += HandlePrepStarted;
            waveSpawner.OnPrepTimeChanged += HandlePrepTimeChanged;
            waveSpawner.OnWaveStarted += HandleWaveStarted;
        }

        private void OnDisable()
        {
            waveSpawner.OnPrepPhaseStarted -= HandlePrepStarted;
            waveSpawner.OnPrepTimeChanged -= HandlePrepTimeChanged;
            waveSpawner.OnWaveStarted -= HandleWaveStarted;
        }

        private void HandlePrepStarted()
        {
            shopManager.RerollAll(free: true);
            _lastTickSecond = -1;
        }

        private void HandlePrepTimeChanged(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            timerText.text = $"다음 웨이브까지 {seconds}초";

            // 마지막 3초만 틱 — 매 프레임 불리는 이벤트라 정수 초가 실제로 바뀐 순간에만 재생.
            if (seconds <= 3 && seconds >= 1 && seconds != _lastTickSecond)
            {
                _lastTickSecond = seconds;
                AudioManager.Instance?.PlayCountdownTick();
            }
        }

        private void HandleWaveStarted(int waveNumber)
        {
            timerText.text = $"{waveNumber}웨이브 진행 중";
        }
    }
}
