using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchHour.Combat;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Core
{
    /// <summary>
    /// 10웨이브를 순서대로 진행한다. 웨이브 구성/배율은 전부 WaveData·ZoneData에서 읽어오므로
    /// 새 웨이브·새 구역을 추가해도 이 스크립트는 건드릴 필요가 없다.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("웨이브 구성 (10개, GDD.md 13번 표 그대로)")]
        [SerializeField] private List<WaveData> waves;

        [Header("현재 출전 구역")]
        [SerializeField] private ZoneData currentZone;

        [Header("스폰 대상")]
        [SerializeField] private InvaderUnit invaderPrefab;
        [SerializeField] private RectTransform spawnRoot;
        [SerializeField] private WardHealth ward;
        [SerializeField] private RunCurrency currency;

        [Header("웨이브 사이 대기 (GDD.md 13번: 5초)")]
        [SerializeField] private float interWaveDelay = 5f;
        [Header("웨이브 클리어 보상 (GDD.md 13번: 30)")]
        [SerializeField] private int waveClearReward = 30;

        public event Action<int> OnWaveStarted;
        public event Action OnAllWavesCleared;

        private ObjectPool<InvaderUnit> _pool;
        private int _aliveCount;

        private void Awake()
        {
            _pool = new ObjectPool<InvaderUnit>(invaderPrefab, spawnRoot, prewarm: 10);
        }

        private void Start()
        {
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                yield return StartCoroutine(RunSingleWave(waves[i]));
                yield return new WaitUntil(() => _aliveCount <= 0);

                currency.Add(waveClearReward);

                if (i < waves.Count - 1)
                    yield return new WaitForSeconds(interWaveDelay);
            }

            OnAllWavesCleared?.Invoke();
        }

        private IEnumerator RunSingleWave(WaveData wave)
        {
            OnWaveStarted?.Invoke(wave.waveNumber);

            // GDD.md 13번 "보스 먼저, 이후 1.0초": 보스 웨이브는 보스부터 내보내고 나머지가 뒤따른다.
            if (wave.isBossWave && currentZone.bossInvader != null)
                SpawnInvader(currentZone.bossInvader, wave.hpMultiplier);

            foreach (var entry in wave.spawnEntries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnInvader(entry.invader, wave.hpMultiplier);
                    yield return new WaitForSeconds(wave.spawnInterval);
                }
            }
        }

        private void SpawnInvader(InvaderData data, float waveHpMultiplier)
        {
            float totalHpMultiplier = waveHpMultiplier * currentZone.hpMultiplier;

            float laneHalfWidthPixels = (FieldConstants.LaneWidthUnits / 2f) * FieldConstants.UnitSize;
            float spawnX = UnityEngine.Random.Range(-laneHalfWidthPixels, laneHalfWidthPixels);
            float topY = (FieldConstants.LaneLengthUnits / 2f) * FieldConstants.UnitSize;
            float wardY = -topY;

            InvaderUnit unit = _pool.Get();
            unit.Spawn(
                data,
                totalHpMultiplier,
                new Vector2(spawnX, topY),
                wardY,
                releaseCallback: ReleaseInvader,
                onReachWard: u => ward.TakeDamage(u.Data.wardDamage),
                onDeath: u => currency.Add(u.Data.manaCrystalReward));

            _aliveCount++;
        }

        private void ReleaseInvader(InvaderUnit unit)
        {
            _pool.Release(unit);
            _aliveCount--;
        }
    }
}
