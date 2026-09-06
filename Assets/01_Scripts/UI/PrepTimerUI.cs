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

        // HudUI에 따로 있던 "웨이브 X/10" 표시가 준비 단계에선 아직 한 번도 안 갱신돼서(첫
        // OnWaveStarted 전까지 빈 텍스트) 그 구간엔 진행도를 전혀 알 수 없었다("웨이브 진행 중
        // 바에 현재 웨이브도 합쳐서 넣자" 피드백) — 이제 이 배너 하나가 준비 중/전투 중 내내
        // 항상 "웨이브 N/10" 진행도를 같이 보여준다.
        private int _upcomingWaveNumber = 1;

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

        private void HandlePrepStarted(int upcomingWaveNumber)
        {
            _upcomingWaveNumber = upcomingWaveNumber;
            shopManager.RerollAll(free: true);
            _lastTickSecond = -1;
        }

        private void HandlePrepTimeChanged(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            timerText.text = $"웨이브 {_upcomingWaveNumber}/{waveSpawner.TotalWaves} · 다음 웨이브까지 {seconds}초";

            // 마지막 3초만 틱 — 매 프레임 불리는 이벤트라 정수 초가 실제로 바뀐 순간에만 재생.
            if (seconds <= 3 && seconds >= 1 && seconds != _lastTickSecond)
            {
                _lastTickSecond = seconds;
                AudioManager.Instance?.PlayCountdownTick();
            }
        }

        private void HandleWaveStarted(int waveNumber)
        {
            timerText.text = $"웨이브 {waveNumber}/{waveSpawner.TotalWaves} 진행 중";
        }
    }
}
