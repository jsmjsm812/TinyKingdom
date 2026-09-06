using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 배치된 수호자의 런타임 인스턴스. 사거리 안 InvaderUnit.ActiveUnits를 찾아 공속마다 공격하고,
    /// 롤토체스처럼 드래그로 다른 슬롯과 자리를 바꿀 수 있다(명부 같은 대기 단계 없이 필드에서 바로 조작).
    /// 등급(rarity)은 GuardianData에 고정, 성급(star)만 합성으로 바뀌므로 별도 필드로 들고 있는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup), typeof(SpriteFrameAnimator))]
    public class GuardianUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // MergeSystem이 필드 전체에서 성급별로 3기를 찾아야 해서 InvaderUnit과 같은 패턴으로 노출한다.
        public static readonly List<GuardianUnit> ActiveUnits = new List<GuardianUnit>();

        private static readonly float[] StarDamageMultiplier = { 1f, 1.8f, 3.0f };

        // 합성이 데미지만 올려주면 "성급이 올라도 하는 짓은 똑같다"는 인상이라, 공격 방식별로
        // 성급마다 능력 자체도 하나씩 강화되게 한다 — 궁수(Multi)는 화살 개수, 마법사류(Area)는
        // 범위. Pierce(명사수/빙결사)는 이미 사거리 안 전원을 맞히는 최대치라 늘릴 여지가 없어서
        // 제외. Single/DotSingle(마녀)/BuffSingle(왕)은 데미지(또는 도트 총량/오라 배율) 배율
        // 스케일링만 적용된다 — 별도의 "능력 자체" 강화 축은 아직 없음.
        private static readonly int[] MultiTargetCountByStar = { 2, 3, 4 };
        private static readonly float[] AreaRadiusMultiplierByStar = { 1f, 1.3f, 1.6f };

        private SpriteFrameAnimator _frameAnimator;

        [SerializeField] private Text starText;
        [SerializeField] private Text attackText;
        [SerializeField] private Image rangeIndicator;
        private Coroutine _mergeGlowCoroutine;

        public GuardianData Data { get; private set; }
        public int StarLevel { get; private set; } = 1;
        public Vector2 FieldPosition { get; private set; }
        public GridSlot CurrentSlot { get; private set; }

        private float _attackTimer;
        private bool _facingLeft;

        // 왕(BuffSingle)의 "인접 아군 공격력 +15%" 오라 — 공격 쿨다운과 무관하게 매 프레임 갱신되는
        // 상시 효과라 별도로 들고 있는다. 여러 왕이 동시에 사거리 안에 있으면 가장 높은 배율만 적용
        // (중첩 안 함 — 왕을 여러 기 합성으로 늘리면 배율이 무한히 쌓이는 것 방지).
        private float _auraAttackMultiplier = 1f;

        // 대마법사의 "3초마다 범위 속박" — 공격 쿨다운(attackSpeed)과 완전히 별개의 주기라 따로 잰다.
        private float _periodicRootTimer;

        // 베이크된 스프라이트를 256px 기준으로 구웠을 때 필드에서 보이는 크기 배율 — 캐릭터를
        // 발밑 기준으로 세워 그릴 때(아래 UpdateSpriteSize 참고) 이 배율로 UI 픽셀 크기를 정한다.
        // 300/256(예전 고정 300x300 박스)보다 키워서 "캐릭터가 더 컸으면" 피드백을 반영했었으나,
        // 슬롯 시각 크기(FieldConstants.SlotVisualSize)가 92.5로 작아진 뒤로는 반대로 캐릭터가
        // 슬롯보다 훨씬 커 보이는 문제가 생겼다 — 슬롯도 200으로 키우면서 캐릭터는 실측 300 기준을
        // 200으로(2/3배) 줄여 서로 크기가 맞게 했다.
        private const float NativeToUiScale = 340f / 256f * (2f / 3f);

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private GuardianPlacementManager _placementManager;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private bool _dragAllowed;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>();
            // 씬 구조상 GuardianPlacementManager(=Instantiate 직후 부모)가 Canvas 밑이 아니라
            // 루트에 따로 있어서 위 GetComponentInParent가 못 찾는 경우가 있다 — 그러면
            // OnBeginDrag에서 _rootCanvas.transform이 NullReferenceException을 던진다.
            // GridManager.BattlefieldRoot는 항상 Canvas 밑에 있는 걸 보장하는 참조(다른 곳,
            // 예: WeaponProjectile도 이걸로 부모를 찾음)라 이걸로 폴백한다.
            if (_rootCanvas == null && GridManager.BattlefieldRoot != null)
                _rootCanvas = GridManager.BattlefieldRoot.GetComponentInParent<Canvas>();
            _frameAnimator = GetComponent<SpriteFrameAnimator>();
            // Instantiate 직후(슬롯에 놓이기 전) 부모가 GuardianPlacementManager라 지금 캐시해둔다 —
            // 이후 슬롯으로 재배치돼도 이 참조 자체는 계속 유효하다.
            _placementManager = GetComponentInParent<GuardianPlacementManager>();
        }

        public void Setup(GuardianData data, int starLevel = 1)
        {
            Data = data;
            StarLevel = starLevel;
            // 0으로 고정하면 안 된다 — 준비 시간(최대 20초) 동안엔 사거리 안에 타겟이 없어서
            // TryAttack이 계속 실패하고, _attackTimer는 그 사이에도 Update에서 계속 누적된다.
            // 그러면 웨이브가 시작되는 순간 이미 배치돼 있던 용사 전원의 타이머가 쿨다운을 훌쩍
            // 넘긴 "과충전" 상태라, 첫 침입자가 사거리에 들어오자마자 전원이 같은 프레임에 동시
            // 발사해버린다 — 체력바가 한 번에 0으로 스냅해 "줄어드는 게 안 보인다"는 피드백의
            // 실제 원인이었다. 배치 시점에 쿨다운 범위 안에서 무작위로 위상을 흩어 놓으면 그 뒤로도
            // 상대적 위상차가 계속 유지되니(둘 다 같은 속도로 흐름) 다시는 완전히 동기화되지 않는다.
            _attackTimer = UnityEngine.Random.Range(0f, 1f / Mathf.Max(data.attackSpeed, 0.01f));

            // TinyKingdom 원본 스프라이트는 오른쪽을 보는 포즈라 언플립 상태(sign=+1)가 오른쪽이다.
            // 사거리 안에 타겟이 없으면 UpdateFacing이 아예 호출을 안 해서(방향 바꿀 근거가 없으니)
            // 계속 오른쪽으로 남아있었다 — 기본값을 왼쪽으로 바꾸려면 여기서 미리 한 번 뒤집어둬야
            // 타겟이 없는 상태에서도(대기 중) 왼쪽을 보고 서 있는다.
            _facingLeft = true;
            Vector3 initScale = _rect.localScale;
            _rect.localScale = new Vector3(-Mathf.Abs(initScale.x), initScale.y, initScale.z);
            if (starText != null) starText.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            if (attackText != null) attackText.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            if (_frameAnimator != null)
                _frameAnimator.SetFrames(data.idleFrames, data.actionFrames);

            UpdateSpriteSize(data);
            UpdateStarVisual();
            UpdateAttackVisual();
            UpdateRangeIndicator(data);
        }

        /// <summary>사거리를 눈으로 볼 수 있었으면 좋겠다는 피드백 — 캐릭터 발밑에 반투명 원으로
        /// 실제 공격 사거리(TryAttack이 쓰는 것과 동일한 rangePixels)를 항상 표시한다.</summary>
        private void UpdateRangeIndicator(GuardianData data)
        {
            if (rangeIndicator == null) return;
            float diameter = data.range * FieldConstants.UnitSize * 2f;
            rangeIndicator.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }

        /// <summary>
        /// 예전엔 300x300 고정 박스에 preserveAspect로 넣었는데, 베이크된 원본이 캐릭터마다
        /// 여백이 다른 정사각형(256x256)이라 실제 발 위치가 캐릭터마다 미묘하게 달라 보였다.
        /// 한때는 pivot을 바닥(0.5, 0)에 두고 "발이 슬롯 중심에 선다"는 방식을 썼는데,
        /// 캐릭터가 슬롯보다 커서(300 vs 92.5) 박스 대부분이 슬롯 위로 붕 뜬 것처럼 보이는
        /// 문제가 있었다 — 캐릭터/슬롯 크기를 200으로 맞춘 지금은 "슬롯 정중앙에 캐릭터"
        /// 요청에 맞춰 pivot을 중앙(0.5, 0.5)으로 바꿨다: 슬롯에 놓일 때 anchoredPosition이
        /// (0,0)이 되므로 박스 중심이 곧 슬롯 중심이 된다. 캐릭터 원래 크기 비율(트롤이 더
        /// 크고 마법사가 더 작은 것)은 원본 픽셀 크기를 그대로 쓰니 유지된다.
        /// </summary>
        private void UpdateSpriteSize(GuardianData data)
        {
            if (_rect == null) return;
            Sprite reference = (data.idleFrames != null && data.idleFrames.Length > 0)
                ? data.idleFrames[0]
                : data.portrait;
            if (reference == null) return;

            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(
                reference.rect.width * NativeToUiScale,
                reference.rect.height * NativeToUiScale);
        }

        /// <summary>합성으로 성급이 오르면 StarDamageMultiplier 배율이 바뀌어 실제 공격력도 같이
        /// 오른다 — 그 값을 눈으로 확인할 수 있게 표시한다(TryAttack의 damage 계산과 동일한 수식).</summary>
        private void UpdateAttackVisual()
        {
            if (attackText == null || Data == null) return;
            float effectiveAttack = Data.attackPower * EffectiveStarMultiplier() * RunItemEffects.GuardianAttackPowerMultiplier * _auraAttackMultiplier;
            // 임의의 기호(예: 검 이모지)는 현재 폰트(GameFonts.Main)가 글리프를 안 가지고 있을 수
            // 있어 ASCII 접두어로 안전하게 — "★"는 starText에서 잘 나오는 걸 확인함.
            attackText.text = $"ATK {Mathf.RoundToInt(effectiveAttack)}";
        }

        /// <summary>아이템 상점의 "일시적 등급 상승" 버프(RunItemEffects.GuardianBonusStarLevels)를
        /// 실제 합성 성급에 더해 데미지 배율 인덱스를 구한다 — 합성 성급 자체(StarLevel, ★ 표시)는
        /// 안 건드리고 전투 계산에만 반영한다(버프가 풀리면 원래 합성 상태 그대로 남아야 하므로).</summary>
        private float EffectiveStarMultiplier() => StarDamageMultiplier[EffectiveStarIndex()];

        /// <summary>합성 성급 + 아이템 상점의 일시적 등급 상승을 합친 0-based 인덱스. 데미지 배율뿐
        /// 아니라 화살 개수·범위 같은 능력 강화도 같은 기준으로 맞춰야 "성급 상승 버프 중엔 그
        /// 성급의 능력까지 그대로 흉내낸다"는 게 일관되게 성립한다.</summary>
        private int EffectiveStarIndex()
        {
            return Mathf.Clamp(StarLevel - 1 + RunItemEffects.GuardianBonusStarLevels, 0, StarDamageMultiplier.Length - 1);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 웨이브가 진행 중일 때는 재배치를 막는다 — 준비 시간에만 자리를 바꿀 수 있다.
            _dragAllowed = !WaveSpawner.IsBattleActive;
            if (!_dragAllowed) return;

            _originalParent = transform.parent;
            _originalAnchoredPosition = _rect.anchoredPosition;
            transform.SetParent(_rootCanvas.transform, true);
            NormalizeScale();
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragAllowed) return;
            _rect.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragAllowed) return;
            _canvasGroup.blocksRaycasts = true;

            GridSlot targetSlot = GridSlot.FindUnderPointer(eventData);
            if (targetSlot != null && _placementManager.TryMove(CurrentSlot, targetSlot))
            {
                NormalizeScale();
                return;
            }

            transform.SetParent(_originalParent, true);
            _rect.anchoredPosition = _originalAnchoredPosition;
            NormalizeScale();
        }

        // SetParent(worldPositionStays: true)는 "화면에 보이는 크기를 그대로 유지"하려고
        // localScale을 자동으로 다시 계산하는데, true/false를 섞어서 여러 번 부모를 바꾸다 보면
        // (집어들 때 true, 슬롯에 놓을 때는 GridSlot.TryPlace가 false) 그 보정값이 누적돼서
        // 슬롯을 옮길 때마다 캐릭터가 조금씩 작아지는 버그가 있었다 — localScale의 "크기"는
        // 항상 정확히 1이어야 하고(실제 크기는 UpdateSpriteSize가 정하는 sizeDelta가 전담),
        // 좌우 반전(_facingLeft)만 부호로 표현해야 하므로 부모가 바뀔 때마다 강제로 재설정한다.
        private void NormalizeScale()
        {
            float sign = _facingLeft ? -1f : 1f;
            _rect.localScale = new Vector3(sign, 1f, 1f);
        }

        /// <summary>GridSlot.TryPlace에서 호출 — 필드 좌표 갱신 + 합성 대상 목록 등록.</summary>
        public void OnPlaced(GridSlot slot)
        {
            CurrentSlot = slot;
            FieldPosition = slot.FieldPosition;
            if (!ActiveUnits.Contains(this))
                ActiveUnits.Add(this);
        }

        /// <summary>합성으로 소모될 때 호출.</summary>
        public void RemoveFromField()
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.Clear();
                CurrentSlot = null;
            }
            Destroy(gameObject);
        }

        // ActiveUnits는 static이라 씬을 넘어 살아남는다 — "그만하기"/결과창으로 배틀씬을 나가면
        // 유니티가 씬 언로드로 이 오브젝트들을 전부 자동 Destroy하는데, 그때도 여기가 불려서
        // 스스로 목록에서 빠진다. 이걸 안 하면 다음 배틀씬 진입 때 죽은 참조가 그대로 남아있다가
        // MergeSystem이 같은 (데이터,성급) 그룹으로 묶어버려서 "이미 죽은 오브젝트를 또 Destroy"
        // 하려다 MissingReferenceException이 나는 버그가 있었다.
        private void OnDestroy()
        {
            ActiveUnits.Remove(this);
        }

        /// <summary>
        /// 합성으로 성급이 오를 때 호출된다(MergeSystem.MergeGroup). 그냥 숫자만 바꾸면 플레이어가
        /// "방금 이 개체가 강화됐다"는 걸 알 방법이 없었다 — 별 배지를 갱신하고, 오르는 순간엔
        /// 잠깐 커졌다 돌아오는 튐 애니메이션으로 눈에 띄게 알려준다.
        /// </summary>
        public void SetStarLevel(int starLevel)
        {
            bool leveledUp = starLevel > StarLevel;
            StarLevel = starLevel;
            UpdateStarVisual();
            UpdateAttackVisual();

            if (leveledUp && gameObject.activeInHierarchy)
            {
                if (_mergeGlowCoroutine != null) StopCoroutine(_mergeGlowCoroutine);
                _mergeGlowCoroutine = StartCoroutine(PlayMergeGlow());
            }
        }

        private void UpdateStarVisual()
        {
            if (starText == null) return;
            starText.text = new string('★', Mathf.Clamp(StarLevel, 1, 3));
            // 상점 카드 등급 색과 같은 팔레트를 재사용 — ★1=회청, ★2=보라, ★3=금색으로
            // 이미 익숙해진 배색을 성급 표시에도 그대로 써서 "등급이 높을수록 화려하다"는
            // 시각 언어가 일관되게 유지된다.
            starText.color = RarityColors.GetAccentColor((GuardianRarity)Mathf.Clamp(StarLevel - 1, 0, 2));
        }

        private IEnumerator PlayMergeGlow()
        {
            const float duration = 0.16f;
            Vector3 baseScale = _rect.localScale;
            Vector3 bigScale = baseScale * 1.35f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rect.localScale = Vector3.Lerp(baseScale, bigScale, t / duration);
                yield return null;
            }
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rect.localScale = Vector3.Lerp(bigScale, baseScale, t / duration);
                yield return null;
            }
            _rect.localScale = baseScale;
            _mergeGlowCoroutine = null;
        }

        private void Update()
        {
            if (Data == null) return;

            // 사거리 원은 배치 계획 세우는 준비 시간에만 필요하고, 웨이브 진행 중(관전만 하는
            // 구간)엔 화면만 지저분하게 만든다 — 전투 중엔 꺼둔다.
            if (rangeIndicator != null)
            {
                bool shouldShow = !WaveSpawner.IsBattleActive;
                if (rangeIndicator.gameObject.activeSelf != shouldShow)
                    rangeIndicator.gameObject.SetActive(shouldShow);
            }

            // 공격 쿨다운과 무관하게 매 프레임 갱신 — 침입자가 사거리 안에서 계속 이동하므로
            // "지금 조준 중인" 타겟 쪽으로 실시간으로 몸을 돌려야 자연스럽다(쿨다운 중엔 안 움직이면
            // 방금 공격한 타겟이 지나가버려도 계속 그쪽을 보고 있는 것처럼 보임).
            UpdateFacing();
            UpdateAuraMultiplier();
            UpdatePeriodicRoot();

            _attackTimer += Time.deltaTime;
            float attackCooldown = 1f / (Data.attackSpeed * RunItemEffects.GuardianAttackSpeedMultiplier);
            if (_attackTimer < attackCooldown) return;

            if (TryAttack())
                _attackTimer = 0f;
        }

        /// <summary>사거리 안 우선순위 타겟(FindTargetsInRange와 동일한 기준) 방향으로 좌우 반전.
        /// TinyKingdom 프레임은 정면(Front) 뷰 하나만 구웠으므로 "시선 방향"은 좌우 미러링으로만
        /// 표현한다 — 원본 스프라이트가 오른쪽을 보고 있다는 전제(TinyKingdom CreatureController와
        /// 동일한 규칙: 왼쪽 타겟이면 scale.x를 음수로).</summary>
        private void UpdateFacing()
        {
            float rangePixels = Data.range * FieldConstants.UnitSize;
            var targets = FindTargetsInRange(rangePixels, 1);
            if (targets.Count == 0) return;

            bool faceLeft = targets[0].FieldPosition.x < FieldPosition.x;
            if (faceLeft == _facingLeft) return;
            _facingLeft = faceLeft;

            float sign = faceLeft ? -1f : 1f;
            Vector3 s = _rect.localScale;
            _rect.localScale = new Vector3(sign * Mathf.Abs(s.x), s.y, s.z);

            // starText/attackText는 GuardianUnit과 같은 RectTransform 아래 자식이라 부모가
            // 미러링되면 글자도 같이 뒤집혀 읽을 수 없게 된다 — 자식 스케일도 같은 부호로 한 번 더
            // 뒤집어서(부호×부호=양수) 상쇄시켜 텍스트만 항상 똑바로 보이게 한다.
            if (starText != null) starText.rectTransform.localScale = new Vector3(sign, 1f, 1f);
            if (attackText != null) attackText.rectTransform.localScale = new Vector3(sign, 1f, 1f);
        }

        private bool TryAttack()
        {
            float rangePixels = Data.range * FieldConstants.UnitSize;
            float damage = Data.attackPower * EffectiveStarMultiplier() * RunItemEffects.GuardianAttackPowerMultiplier * _auraAttackMultiplier;

            // 도적의 치명타 — specialEffectDescription에는 있었지만 실제로는 미구현이었던 효과.
            // 공격 하나당 한 번만 판정(다중/관통이라도 이번 공격 전체에 동일하게 적용).
            if (Data.critChance > 0f && UnityEngine.Random.value < Data.critChance)
                damage *= Data.critMultiplier;

            bool attacked;
            switch (Data.attackType)
            {
                case AttackType.Multi:
                    // 궁수류(Mira) — 합성할 때마다 화살이 1개씩 늘어난다(★1=2발 → ★2=3발 → ★3=4발).
                    attacked = AttackNearest(rangePixels, damage, maxTargets: MultiTargetCountByStar[EffectiveStarIndex()]);
                    break;
                case AttackType.Pierce:
                    attacked = AttackNearest(rangePixels, damage, maxTargets: int.MaxValue);
                    break;
                case AttackType.Area:
                    // 마법사류(Astel/Sera) — 합성할수록 범위 자체가 넓어진다(★1 기준 ×1.0 → ×1.3 → ×1.6).
                    attacked = AttackAreaAroundNearest(rangePixels, damage, AreaRadiusMultiplierByStar[EffectiveStarIndex()]);
                    break;
                case AttackType.DotSingle:
                    // 마녀(Chloe) — 즉발 피해(damage) + 별도로 dotTotalDamage를 dotDurationSeconds에
                    // 걸쳐 추가로 준다(적중 즉시 총 피해 = 즉발 + 도트, GuardianData.specialEffectDescription 그대로).
                    attacked = AttackDotSingle(rangePixels, damage);
                    break;
                case AttackType.BuffSingle:
                    // 왕(Ophelia) — 직접 공격하지 않는다. 사거리 안 아군에게 상시 오라를 주는 게
                    // 능력의 전부라서(Update의 UpdateAuraMultiplier가 매 프레임 처리) 여기선 할 게 없다.
                    attacked = false;
                    break;
                default:
                    // Single: 즉발 단일 피해.
                    attacked = AttackNearest(rangePixels, damage, maxTargets: 1);
                    break;
            }

            if (attacked)
            {
                if (_frameAnimator != null) _frameAnimator.PlayAction(resumeIdle: true);
                AudioManager.Instance?.PlayAttack();
            }

            return attacked;
        }

        /// <summary>왕(BuffSingle)이 사거리 안에 있으면 아군 전체(본인 제외)에게 공격력 배율을
        /// 상시 적용한다 — 공격 쿨다운이 아니라 매 프레임 재계산되는 오라라서 Update에서 직접 부른다.
        /// 왕이 여러 기라도 배율은 중첩하지 않고 가장 높은 값 하나만 적용한다.</summary>
        private void UpdateAuraMultiplier()
        {
            float best = 1f;
            foreach (var source in ActiveUnits)
            {
                if (source == this || source.Data == null) continue;
                if (source.Data.attackType != AttackType.BuffSingle) continue;

                float sourceRangePixels = source.Data.range * FieldConstants.UnitSize;
                if (Vector2.Distance(FieldPosition, source.FieldPosition) > sourceRangePixels) continue;

                if (source.Data.adjacentAllyAttackMultiplier > best)
                    best = source.Data.adjacentAllyAttackMultiplier;
            }

            if (Mathf.Approximately(best, _auraAttackMultiplier)) return;
            _auraAttackMultiplier = best;
            UpdateAttackVisual();
        }

        /// <summary>대마법사의 "3초마다 범위 속박" — 공격 쿨다운(attackSpeed)과 완전히 독립적인
        /// 주기다. 본인 위치를 중심으로 areaRadius 안 전체를 잠깐 묶는다(ApplySlow(0, ...)로
        /// 이동속도를 0으로 만드는 것 — 별도 "속박" 상태 없이 둔화 배율 0을 재사용).</summary>
        private void UpdatePeriodicRoot()
        {
            if (!Data.hasPeriodicRoot) return;

            _periodicRootTimer += Time.deltaTime;
            if (_periodicRootTimer < Data.periodicRootInterval) return;
            _periodicRootTimer = 0f;

            float radiusPixels = Data.areaRadius * AreaRadiusMultiplierByStar[EffectiveStarIndex()] * FieldConstants.UnitSize;
            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(invader.FieldPosition, FieldPosition) <= radiusPixels)
                    invader.ApplySlow(0f, Data.periodicRootDuration);
            }
        }

        private bool AttackDotSingle(float rangePixels, float instantDamage)
        {
            var targets = FindTargetsInRange(rangePixels, 1);
            if (targets.Count == 0) return false;

            var target = targets[0];
            target.TakeDamage(instantDamage);

            float dotTotal = Data.dotTotalDamage * EffectiveStarMultiplier() * RunItemEffects.GuardianAttackPowerMultiplier * _auraAttackMultiplier;
            target.ApplyDot(dotTotal, Data.dotDurationSeconds, Data.dotTickInterval);

            WeaponProjectile.Spawn(FieldPosition, target.FieldPosition, Data.projectileSprite, Data.projectileRotationOffsetDegrees);
            return true;
        }

        private bool AttackNearest(float rangePixels, float damage, int maxTargets)
        {
            var targets = FindTargetsInRange(rangePixels, maxTargets);
            if (targets.Count == 0) return false;

            foreach (var target in targets)
            {
                target.TakeDamage(damage);
                // 빙결사(Pierce)의 "관통 대상 전체 이동속도 감소" — specialEffectDescription에는
                // 적혀있었지만 실제로 구현이 안 돼있던 효과라 여기서 새로 연결한다.
                if (Data.appliesSlowOnHit) target.ApplySlow(Data.slowMultiplier, Data.slowDurationSeconds);
                // 야만전사의 "5% 확률 기절" — 마찬가지로 미구현이었던 효과. 별도 "기절" 상태 없이
                // 대마법사 속박과 같은 방식(ApplySlow(0, ...))으로 이동을 잠깐 완전히 멈춘다.
                if (Data.stunChance > 0f && UnityEngine.Random.value < Data.stunChance)
                    target.ApplySlow(0f, Data.stunDurationSeconds);
                WeaponProjectile.Spawn(FieldPosition, target.FieldPosition, Data.projectileSprite, Data.projectileRotationOffsetDegrees);
            }
            return true;
        }

        private bool AttackAreaAroundNearest(float rangePixels, float damage, float radiusMultiplier)
        {
            var nearest = FindTargetsInRange(rangePixels, 1);
            if (nearest.Count == 0) return false;

            float areaRadiusPixels = Data.areaRadius * radiusMultiplier * FieldConstants.UnitSize;
            Vector2 center = nearest[0].FieldPosition;
            WeaponProjectile.Spawn(FieldPosition, center, Data.projectileSprite, Data.projectileRotationOffsetDegrees);

            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(invader.FieldPosition, center) <= areaRadiusPixels)
                {
                    invader.TakeDamage(damage);
                    // 연금술사(Area)의 "피격 대상 이동속도 감소" — 마찬가지로 설명만 있고 실제로는
                    // 안 구현돼 있던 효과.
                    if (Data.appliesSlowOnHit) invader.ApplySlow(Data.slowMultiplier, Data.slowDurationSeconds);
                }
            }
            return true;
        }

        private List<InvaderUnit> FindTargetsInRange(float rangePixels, int maxCount)
        {
            var inRange = new List<InvaderUnit>();
            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(FieldPosition, invader.FieldPosition) <= rangePixels)
                    inRange.Add(invader);
            }

            // 가장 진행이 많이 된(=성벽에 가까운) 적을 우선 타격 — 방치 시 성벽 피해로 직결되는 개체부터 정리.
            // 선반 구간은 Y가 고정이라 더 이상 Y비교로 판단할 수 없어 경로 누적거리(PathProgress)로 비교한다.
            inRange.Sort((a, b) => b.PathProgress.CompareTo(a.PathProgress));

            if (inRange.Count > maxCount)
                inRange.RemoveRange(maxCount, inRange.Count - maxCount);

            return inRange;
        }
    }
}
