using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Combat;
using WitchHour.Data;
using WitchHour.Field;
using WitchHour.UI;

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

        [Header("준비 시간 (롤토체스처럼 웨이브 시작 전 구매/재배치 시간, GDD.md 13번: 20초)")]
        [SerializeField] private float prepDuration = 20f;
        // 필드 슬롯이 22칸에서 17칸으로 줄어든 만큼(사용자가 필드 레이아웃을 직접 다듬으며 5칸
        // 정리함) 확보 가능한 총 화력이 줄어서, 웨이브 클리어 보상을 30→35로 올려 그 차이를
        // 살짝 보전한다(GDD.md 13번).
        [Header("웨이브 클리어 보상 (GDD.md 13번: 35, 구역 금화배율 적용)")]
        [SerializeField] private int waveClearReward = 35;

        // 상점 새로고침은 여기서 직접 안 하고 이 이벤트들만 쏜다 — WaveSpawner가 ShopManager를
        // 알아야 할 이유가 없어서(Core가 Shop에 의존하면 안 됨), UI 레이어(PrepTimerUI)가 구독해서 이어준다.
        // int 인자 = 곧 시작될 웨이브 번호(1-based) — PrepTimerUI가 "웨이브 3/10 · 다음 웨이브까지
        // N초"처럼 진행도와 카운트다운을 한 배너에 합쳐서 보여주는 데 쓴다.
        public event Action<int> OnPrepPhaseStarted;
        public event Action<float> OnPrepTimeChanged;
        public event Action<int> OnWaveStarted;
        public event Action OnAllWavesCleared;

        public int TotalWaves => waves.Count;

        // "그만하기" 버튼(BattleFlowController.QuitAndSaveProgress)이 체크포인트로 저장할 때 읽는
        // 값 — 지금 진행 중이거나(전투 중) 막 시작하려는(준비 시간) 웨이브 번호. RunWaves가 갱신한다.
        public int CurrentWaveNumber { get; private set; } = 1;

        // 웨이브 진행 중엔 필드 재배치(드래그)를 막으려고 GuardianUnit이 이 값을 읽는다.
        // 준비 시간에는 false, 침입자가 스폰되기 시작해서 전멸할 때까지는 true.
        public static bool IsBattleActive { get; private set; }

        private ObjectPool<InvaderUnit> _pool;
        private int _aliveCount;
        private Coroutine _runCoroutine;
        private bool _skipPrepRequested; // 디버그 모드 "준비시간 스킵" 버튼용

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
            // BattlefieldRoot/FieldBackground를 찾아 대입하는 거라, GridManager.Awake가 먼저 끝나
            // BattlefieldRoot 정적 참조가 채워져 있는 게 보장되는 Start에서 한다(Awake는 오브젝트 간
            // 순서가 보장 안 됨 — GuardianUnit _rootCanvas 때와 같은 이유). 체크포인트 복원도
            // 마찬가지로 GuardianPlacementManager.Instance/GridManager.Instance가 이미 채워져
            // 있어야 해서(둘 다 Awake에서 세팅됨) 여기 Start에서 한다.
            ApplyZoneVisuals();
            RestoreCheckpointIfAny();
            _runCoroutine = StartCoroutine(RunWaves());
        }

        /// <summary>"그만하기"로 저장해둔 체크포인트가 있으면 골드·산 아이템·필드 배치를 전부
        /// 복원한다 — 웨이브 시작 지점(RunWaves의 startIndex)은 저장된 wave 값을 그대로 다시
        /// 읽으므로 여기서 따로 처리 안 해도 된다. 체크포인트가 없으면(hasCheckpoint=false)
        /// 아무 것도 안 하고 조용히 리턴 — 기존처럼 골드 260 + 빈 필드로 시작.</summary>
        private void RestoreCheckpointIfAny()
        {
            if (currentZone == null) return;
            var checkpoint = GameProgress.GetCheckpoint(currentZone.zoneIndex);
            if (!checkpoint.hasCheckpoint) return;

            RunSession.SetGold(checkpoint.gold);

            var guardianRegistry = Resources.Load<GuardianRegistry>("GuardianRegistry");
            var itemRegistry = Resources.Load<ItemRegistry>("ItemRegistry");

            if (checkpoint.items != null && itemRegistry != null)
            {
                foreach (var saved in checkpoint.items)
                {
                    var item = itemRegistry.FindByAssetName(saved.itemAssetName);
                    if (item == null) continue;
                    for (int i = 0; i < saved.count; i++)
                        RunItemEffects.Apply(item);
                }
            }
            // 슬롯이 활성화될 계기(탭 전환)가 아직 안 지나갔을 수도 있지만, 이미 지나간 경우엔
            // OnEnable이 다시 안 불려서 방금 채운 RunItemEffects 값을 못 읽어간다 — 강제 새로고침.
            ItemPanelController.Instance?.RefreshSlots();

            if (checkpoint.guardians != null && guardianRegistry != null && GuardianPlacementManager.Instance != null)
            {
                foreach (var saved in checkpoint.guardians)
                {
                    var guardian = guardianRegistry.FindByAssetName(saved.guardianAssetName);
                    if (guardian == null) continue;
                    GuardianPlacementManager.Instance.PlaceAtSlot(guardian, saved.slotIndex, saved.starLevel);
                }
            }
        }

        /// <summary>현재 currentZone의 배경 일러스트 + 통로 색을 필드에 적용한다. 구역 시작
        /// 시(Start)와 디버그 구역 전환(DebugSwitchZone) 양쪽에서 재사용.</summary>
        private void ApplyZoneVisuals()
        {
            if (currentZone == null || GridManager.BattlefieldRoot == null) return;

            if (currentZone.fieldBackground != null)
            {
                var bgT = GridManager.BattlefieldRoot.Find("FieldBackground");
                var bgImage = bgT != null ? bgT.GetComponent<Image>() : null;
                if (bgImage != null) bgImage.sprite = currentZone.fieldBackground;
            }

            // 통로(ShopRosterUIBootstrap.BuildLanePathVisual이 구운 "PathSegment_N"/"PathTrim"
            // 오브젝트들)도 배경 테마에 맞게 다시 칠한다 — 초록 풀 테두리가 폐허/마왕성 배경
            // 위에 그대로 남으면 안 어울려서("타일도 배경에 맞게 새로 깔아야 할 것 같다" 피드백).
            var pathRoot = GridManager.BattlefieldRoot.Find("FieldPathVisual");
            if (pathRoot != null)
            {
                foreach (Transform child in pathRoot)
                {
                    var img = child.GetComponent<Image>();
                    if (img == null) continue;

                    if (child.name.StartsWith("PathSegment"))
                        img.color = currentZone.pathColor;
                    else if (child.name == "PathTrim")
                        img.color = currentZone.pathTrimTint;
                }
            }

            // 존2 "붕괴된 슬롯" 기믹 — 이 구역에서 막혀야 할 슬롯만 잠그고, 나머지는(예: 존 전환
            // 디버그 테스트로 이전 구역의 잠금이 남아있을 수 있으니) 전부 풀어준다.
            if (GridManager.Instance != null)
            {
                foreach (var slot in GridManager.Instance.Slots)
                {
                    bool shouldBlock = currentZone.blockedSlotIndices != null
                        && currentZone.blockedSlotIndices.Contains(slot.Index);
                    slot.SetBlocked(shouldBlock);
                }
            }
        }

        /// <summary>디버그 모드 "구역 전환" 버튼 — 실행 중인 웨이브 진행을 멈추고 필드에 남은
        /// 침입자를 정리한 뒤, 새 구역 기준(배율·보스·배경)으로 웨이브 1부터 다시 시작한다.
        /// 성벽 체력도 새로 시작하는 느낌이 나도록 가득 채운다.</summary>
        public void DebugSwitchZone(ZoneData zone)
        {
            if (zone == null) return;

            if (_runCoroutine != null)
            {
                StopCoroutine(_runCoroutine);
                _runCoroutine = null;
            }

            DebugKillAllInvaders();
            _aliveCount = 0;
            IsBattleActive = false;

            currentZone = zone;
            ApplyZoneVisuals();
            ward.DebugResetHp();

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
            // "그만하기"로 저장해둔 체크포인트가 있으면 1웨이브가 아니라 거기서부터 다시 시작한다
            // (BattleFlowController.QuitAndSaveProgress가 저장, GameProgress.ClearWaveCheckpoint로
            // 클리어/실패 시 지워짐 — 여기 도달했다는 건 아직 유효한 체크포인트일 수 있다는 뜻).
            int startIndex = 0;
            if (currentZone != null)
            {
                int savedWave = GameProgress.GetCheckpoint(currentZone.zoneIndex).wave;
                if (savedWave > 1) startIndex = Mathf.Clamp(savedWave - 1, 0, waves.Count - 1);
            }

            for (int i = startIndex; i < waves.Count; i++)
            {
                CurrentWaveNumber = waves[i].waveNumber;
                yield return StartCoroutine(RunPrepPhase(waves[i].waveNumber));

                // 존3(마왕의 영지) 기믹 — 웨이브 시작마다 성벽 HP를 깎는다(ward.TakeDamage를
                // 그대로 재사용해서 DebugInvincible 등 기존 로직과 일관되게 동작).
                if (currentZone.wardHpLossPerWave > 0)
                    ward.TakeDamage(currentZone.wardHpLossPerWave);

                IsBattleActive = true;
                yield return StartCoroutine(RunSingleWave(waves[i]));
                yield return new WaitUntil(() => _aliveCount <= 0);
                IsBattleActive = false;

                // 구역 금화배율(GDD.md 14번) — 3구역처럼 더 위험한 구역일수록 웨이브 클리어
                // 보상도 더 많이 줘야 한다는 의도인데, 값만 있고 실제 계산엔 안 곱해지고 있었다.
                currency.Add(Mathf.RoundToInt(waveClearReward * currentZone.manaMultiplier));
            }

            OnAllWavesCleared?.Invoke();
        }

        /// <summary>웨이브 시작 전 준비 시간 — 이 시간 동안 상점 구매/필드 재배치를 하고, 시간이 다 되면 웨이브가 시작된다.</summary>
        private IEnumerator RunPrepPhase(int upcomingWaveNumber)
        {
            OnPrepPhaseStarted?.Invoke(upcomingWaveNumber);
            _skipPrepRequested = false;

            float remaining = prepDuration;
            while (remaining > 0f && !_skipPrepRequested)
            {
                OnPrepTimeChanged?.Invoke(remaining);
                yield return null;
                remaining -= Time.deltaTime;
            }
        }

        /// <summary>디버그 모드 "준비시간 스킵" 버튼 — 지금 준비 중이면 바로 웨이브를 시작시킨다.</summary>
        public void DebugSkipPrep() => _skipPrepRequested = true;

        /// <summary>디버그 모드 "웨이브 즉시 클리어" 버튼 — 필드에 살아있는 침입자를 전부 즉사시킨다
        /// (성벽 피해 없이 TakeDamage/사망 경로를 그대로 타게 해서 골드 보상·오브젝트풀 반납까지 정상 처리됨).</summary>
        public void DebugKillAllInvaders()
        {
            var snapshot = new List<InvaderUnit>(InvaderUnit.ActiveUnits);
            foreach (var invader in snapshot)
            {
                if (invader != null) invader.TakeDamage(1e9f);
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
                SpawnInvader(currentZone.bossInvader);

            foreach (var entry in wave.spawnEntries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnInvader(entry.invader);
                    yield return spawnWait;
                }
            }
        }

        // 웨이브가 지날수록 체력이 오르던 걸(WaveData.hpMultiplier) 없앴다 — "체력은 같은데
        // 나오는 숫자만 다르게" 요청. 이제 체력 스케일링은 구역이 바뀔 때(ZoneData.hpMultiplier)만
        // 일어나고, 같은 구역 안에서는 웨이브가 진행돼도 개체당 체력은 그대로다. 웨이브 난이도는
        // 스폰 수·구성(방패병/전투 와그 등 상위 개체 등장 시점)만으로 올라간다.
        private void SpawnInvader(InvaderData data)
        {
            InvaderUnit unit = _pool.Get();
            unit.Spawn(
                data,
                currentZone.hpMultiplier,
                currentZone.speedMultiplier,
                releaseCallback: ReleaseInvader,
                // _aliveCount는 반드시 여기(사망/도달 "판정" 시점)에서 줄여야 한다 — 예전엔
                // releaseCallback(오브젝트 풀 반납) 시점에 줄였는데, 사망 시 releaseCallback은
                // 사망 애니메이션 코루틴이 끝난 뒤에야 불린다(InvaderUnit.Die). 그 코루틴이 씬
                // 전환 등 어떤 이유로든 끝까지 못 돌면(유니티 코루틴은 GameObject가 비활성화되면
                // 조용히 그냥 멈추고 콜백을 영영 안 부름) releaseCallback이 평생 안 불려서
                // _aliveCount가 0이 안 되고, WaitUntil(_aliveCount<=0)이 영원히 안 풀려 웨이브도
                // 게임도 그대로 멈춘다("웨이브 10/10 끝났는데 게임이 안 끝남, 에러도 없음" 버그) —
                // 애니메이션 타이밍과 무관하게 판정 즉시 카운트를 줄이도록 분리했다.
                onReachWard: u =>
                {
                    ward.TakeDamage(u.Data.wardDamage);
                    _aliveCount--;
                },
                onDeath: u =>
                {
                    currency.Add(u.Data.manaCrystalReward);
                    RunSession.IncrementInvadersDefeated();
                    _aliveCount--;
                });

            AudioManager.Instance?.PlayInvaderSpawn();
            _aliveCount++;
        }

        // 순수하게 오브젝트 풀 반납(시각적 정리)만 담당 — _aliveCount는 위 onDeath/onReachWard에서
        // 이미 줄였으니 여기서 또 줄이면 중복 차감된다.
        private void ReleaseInvader(InvaderUnit unit)
        {
            _pool.Release(unit);
        }
    }
}
