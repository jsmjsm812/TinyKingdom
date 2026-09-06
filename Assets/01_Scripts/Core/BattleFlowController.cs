using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WitchHour.Combat;
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
        [SerializeField] private string homeScene = "Home";

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

            // 실패해도 이번 출전 동안 쌓인 totalSummons 같은 누적 통계는 저장해야 한다 —
            // zoneCleared/unlockedGuardians는 안 바뀌니 예전엔 저장할 게 없어서 안 불렀었음.
            // GDD.md 원칙("실패 시 전부 초기화")대로, 혹시 "그만하기"로 저장해둔 체크포인트가
            // 있었더라도 실패하면 지운다 — 다음 도전은 다시 1웨이브부터.
            GameProgress.ClearWaveCheckpoint(currentZone.zoneIndex);
            SaveSystem.Save();
            OnZoneFailed?.Invoke();
        }

        /// <summary>배틀씬 "그만하기" 버튼 — 클리어/실패처럼 결과창을 띄우지 않고, 지금 이 순간의
        /// 웨이브·골드·산 아이템·필드에 배치된 수호자(슬롯+성급)를 통째로 체크포인트로 저장한 뒤
        /// 곧장 로비로 돌아간다. 다음에 같은 구역을 다시 고르면 이 상태 그대로 이어서 시작한다.</summary>
        public void QuitAndSaveProgress()
        {
            if (_flowEnded) return;
            _flowEnded = true;

            var checkpoint = new ZoneCheckpoint
            {
                hasCheckpoint = true,
                wave = waveSpawner.CurrentWaveNumber,
                gold = RunSession.Gold,
            };

            foreach (var kv in RunItemEffects.GetAllPurchases())
            {
                if (kv.Key == null || kv.Value <= 0) continue;
                checkpoint.items.Add(new SavedItemCount { itemAssetName = kv.Key.name, count = kv.Value });
            }

            foreach (var unit in GuardianUnit.ActiveUnits)
            {
                if (unit == null || unit.Data == null || unit.CurrentSlot == null) continue;
                checkpoint.guardians.Add(new SavedGuardianPlacement
                {
                    guardianAssetName = unit.Data.name,
                    slotIndex = unit.CurrentSlot.Index,
                    starLevel = unit.StarLevel,
                });
            }

            GameProgress.SaveCheckpoint(currentZone.zoneIndex, checkpoint);
            SaveSystem.Save();
            SceneManager.LoadScene(homeScene);
        }
    }
}
