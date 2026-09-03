using System;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.Core
{
    /// <summary>
    /// 웨이브 전체 클리어 / 성벽 함락을 감지해서 구역 해금 처리(GameProgress)를 하고
    /// 결과 이벤트를 쏜다. 실제 결과창 UI는 이 이벤트를 구독만 하면 되므로 여기선 UI를 모른다.
    /// </summary>
    public class BattleFlowController : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private WardHealth ward;
        [SerializeField] private ZoneData currentZone;

        private bool _flowEnded;

        public event Action OnZoneCleared;
        public event Action OnZoneFailed;

        private void Awake()
        {
            // WaveSpawner와 같은 이유 — 홈 화면에서 고른 구역을 우선한다.
            if (RunSession.SelectedZone != null)
                currentZone = RunSession.SelectedZone;
        }

        private void OnEnable()
        {
            waveSpawner.OnAllWavesCleared += HandleClear;
            ward.OnDepleted += HandleFail;
        }

        private void OnDisable()
        {
            waveSpawner.OnAllWavesCleared -= HandleClear;
            ward.OnDepleted -= HandleFail;
        }

        private void HandleClear()
        {
            if (_flowEnded) return;
            _flowEnded = true;

            GameProgress.MarkZoneCleared(currentZone.zoneIndex, currentZone.unlockedGuardians);
            SaveSystem.Save();
            OnZoneCleared?.Invoke();
        }

        private void HandleFail()
        {
            if (_flowEnded) return;
            _flowEnded = true;

            OnZoneFailed?.Invoke();
        }
    }
}
