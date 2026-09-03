using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchHour.Combat;
using WitchHour.Data;

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

        [Header("준비 시간 (롤토체스처럼 웨이브 시작 전 구매/재배치 시간)")]
        [SerializeField] private float prepDuration = 20f;
        [Header("웨이브 클리어 보상 (GDD.md 13번: 30)")]
        [SerializeField] private int waveClearReward = 30;

        // 상점 새로고침은 여기서 직접 안 하고 이 이벤트들만 쏜다 — WaveSpawner가 ShopManager를
        // 알아야 할 이유가 없어서(Core가 Shop에 의존하면 안 됨), UI 레이어(PrepTimerUI)가 구독해서 이어준다.
        public event Action OnPrepPhaseStarted;
        public event Action<float> OnPrepTimeChanged;
        public event Action<int> OnWaveStarted;
        public event Action OnAllWavesCleared;

        public int TotalWaves => waves.Count;

        // 웨이브 진행 중엔 필드 재배치(드래그)를 막으려고 GuardianUnit이 이 값을 읽는다.
        // 준비 시간에는 false, 침입자가 스폰되기 시작해서 전멸할 때까지는 true.
        public static bool IsBattleActive { get; private set; }

        private ObjectPool<InvaderUnit> _pool;
        private int _aliveCount;
        private Coroutine _runCoroutine;

        private void Awake()
        {
            // 홈 화면에서 구역을 골랐으면 그걸 우선한다 — 안 고르고 Battle 씬을 바로 열었을 때는
            // (에디터에서 테스트할 때 등) 인스펙터에 미리 연결해둔 값을 그대로 쓴다.
            if (RunSession.SelectedZone != null)
                currentZone = RunSession.SelectedZone;

            _pool = new ObjectPool<InvaderUnit>(invaderPrefab, spawnRoot, prewarm: 10);
            ward.OnDepleted += StopRun;
        }

        private void OnDestroy()
        {
            ward.OnDepleted -= StopRun;
        }

        private void Start()
        {
            _runCoroutine = StartCoroutine(RunWaves());
        }

        /// <summary>성벽 HP가 0이 되면 더 이상 웨이브를 진행하지 않는다(GDD.md: 실패 시 전부 초기화).</summary>
        private void StopRun()
        {
            if (_runCoroutine == null) return;
            StopCoroutine(_runCoroutine);
            _runCoroutine = null;
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                yield return StartCoroutine(RunPrepPhase());

                IsBattleActive = true;
                yield return StartCoroutine(RunSingleWave(waves[i]));
                yield return new WaitUntil(() => _aliveCount <= 0);
                IsBattleActive = false;

                currency.Add(waveClearReward);
            }

            OnAllWavesCleared?.Invoke();
        }

        /// <summary>웨이브 시작 전 준비 시간 — 이 시간 동안 상점 구매/필드 재배치를 하고, 시간이 다 되면 웨이브가 시작된다.</summary>
        private IEnumerator RunPrepPhase()
        {
            OnPrepPhaseStarted?.Invoke();

            float remaining = prepDuration;
            while (remaining > 0f)
            {
                OnPrepTimeChanged?.Invoke(remaining);
                yield return null;
                remaining -= Time.deltaTime;
            }
        }

        private IEnumerator RunSingleWave(WaveData wave)
        {
            OnWaveStarted?.Invoke(wave.waveNumber);
            AudioManager.Instance?.PlayWaveStart();

            // 이 웨이브 동안은 spawnInterval이 안 바뀌니 한 웨이브에 하나만 만들어서 스폰 루프
            // 내내 재사용한다 — 예전엔 스폰마다 새로 할당해서 웨이브가 클수록 GC 압박이 커졌었다.
            var spawnWait = new WaitForSeconds(wave.spawnInterval);

            // GDD.md 13번 "보스 먼저, 이후 1.0초": 보스 웨이브는 보스부터 내보내고 나머지가 뒤따른다.
            if (wave.isBossWave && currentZone.bossInvader != null)
                SpawnInvader(currentZone.bossInvader, wave.hpMultiplier);

            foreach (var entry in wave.spawnEntries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnInvader(entry.invader, wave.hpMultiplier);
                    yield return spawnWait;
                }
            }
        }

        private void SpawnInvader(InvaderData data, float waveHpMultiplier)
        {
            float totalHpMultiplier = waveHpMultiplier * currentZone.hpMultiplier;

            InvaderUnit unit = _pool.Get();
            unit.Spawn(
                data,
                totalHpMultiplier,
                releaseCallback: ReleaseInvader,
                onReachWard: u => ward.TakeDamage(u.Data.wardDamage),
                onDeath: u => currency.Add(u.Data.manaCrystalReward));

            AudioManager.Instance?.PlayInvaderSpawn();
            _aliveCount++;
        }

        private void ReleaseInvader(InvaderUnit unit)
        {
            _pool.Release(unit);
            _aliveCount--;
        }
    }
}
