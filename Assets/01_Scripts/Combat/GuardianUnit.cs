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

        private SpriteFrameAnimator _frameAnimator;

        [SerializeField] private Text starText;
        [SerializeField] private Text attackText;
        private Coroutine _mergeGlowCoroutine;

        public GuardianData Data { get; private set; }
        public int StarLevel { get; private set; } = 1;
        public Vector2 FieldPosition { get; private set; }
        public GridSlot CurrentSlot { get; private set; }

        private float _attackTimer;
        private bool _facingLeft;

        // 베이크된 스프라이트를 256px 기준으로 구웠을 때 필드에서 보이는 크기 배율 — 캐릭터를
        // 발밑 기준으로 세워 그릴 때(아래 UpdateSpriteSize 참고) 이 배율로 UI 픽셀 크기를 정한다.
        // 300/256(예전 고정 300x300 박스)보다 키워서 "캐릭터가 더 컸으면" 피드백을 반영했다.
        private const float NativeToUiScale = 340f / 256f;

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
        }

        /// <summary>
        /// 예전엔 300x300 고정 박스에 preserveAspect로 넣었는데, 베이크된 원본이 캐릭터마다
        /// 여백이 다른 정사각형(256x256)이라 실제 발 위치가 캐릭터마다 미묘하게 달라 보였다
        /// ("슬롯이랑 캐릭터 위치가 안 맞다" 피드백의 원인 — 자세한 건 대화 기록 참고).
        /// 스프라이트를 발밑 기준으로 빡빡하게 잘라낸 뒤(파이썬으로 알파 바운딩 박스 크롭),
        /// 여기서 원본 픽셀 크기 그대로(NativeToUiScale 배율만 곱해서) 박스 크기를 잡고
        /// pivot을 바닥(0.5, 0)으로 둔다 — 슬롯에 놓일 때 anchoredPosition이 (0,0)이 되므로,
        /// 박스 바닥(=크롭된 스프라이트의 발밑)이 정확히 슬롯 중심에 선다. 캐릭터 원래 크기
        /// 비율(트롤이 더 크고 마법사가 더 작은 것)은 원본 픽셀 크기를 그대로 쓰니 유지된다.
        /// </summary>
        private void UpdateSpriteSize(GuardianData data)
        {
            if (_rect == null) return;
            Sprite reference = (data.idleFrames != null && data.idleFrames.Length > 0)
                ? data.idleFrames[0]
                : data.portrait;
            if (reference == null) return;

            _rect.pivot = new Vector2(0.5f, 0f);
            _rect.sizeDelta = new Vector2(
                reference.rect.width * NativeToUiScale,
                reference.rect.height * NativeToUiScale);
        }

        /// <summary>합성으로 성급이 오르면 StarDamageMultiplier 배율이 바뀌어 실제 공격력도 같이
        /// 오른다 — 그 값을 눈으로 확인할 수 있게 표시한다(TryAttack의 damage 계산과 동일한 수식).</summary>
        private void UpdateAttackVisual()
        {
            if (attackText == null || Data == null) return;
            float effectiveAttack = Data.attackPower * StarDamageMultiplier[StarLevel - 1];
            // 임의의 기호(예: 검 이모지)는 현재 폰트(GameFonts.Main)가 글리프를 안 가지고 있을 수
            // 있어 ASCII 접두어로 안전하게 — "★"는 starText에서 잘 나오는 걸 확인함.
            attackText.text = $"ATK {Mathf.RoundToInt(effectiveAttack)}";
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 웨이브가 진행 중일 때는 재배치를 막는다 — 준비 시간에만 자리를 바꿀 수 있다.
            _dragAllowed = !WaveSpawner.IsBattleActive;
            if (!_dragAllowed) return;

            _originalParent = transform.parent;
            _originalAnchoredPosition = _rect.anchoredPosition;
            transform.SetParent(_rootCanvas.transform, true);
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
                return;

            transform.SetParent(_originalParent, true);
            _rect.anchoredPosition = _originalAnchoredPosition;
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
            ActiveUnits.Remove(this);
            if (CurrentSlot != null)
            {
                CurrentSlot.Clear();
                CurrentSlot = null;
            }
            Destroy(gameObject);
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

            // 공격 쿨다운과 무관하게 매 프레임 갱신 — 침입자가 사거리 안에서 계속 이동하므로
            // "지금 조준 중인" 타겟 쪽으로 실시간으로 몸을 돌려야 자연스럽다(쿨다운 중엔 안 움직이면
            // 방금 공격한 타겟이 지나가버려도 계속 그쪽을 보고 있는 것처럼 보임).
            UpdateFacing();

            _attackTimer += Time.deltaTime;
            float attackCooldown = 1f / Data.attackSpeed;
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
            float damage = Data.attackPower * StarDamageMultiplier[StarLevel - 1];

            bool attacked;
            switch (Data.attackType)
            {
                case AttackType.Multi:
                    attacked = AttackNearest(rangePixels, damage, maxTargets: 2);
                    break;
                case AttackType.Pierce:
                    attacked = AttackNearest(rangePixels, damage, maxTargets: int.MaxValue);
                    break;
                case AttackType.Area:
                    attacked = AttackAreaAroundNearest(rangePixels, damage);
                    break;
                default:
                    // Single / DotSingle / BuffSingle: MVP는 즉발 단일 피해로 처리.
                    // 도트·버프 연출은 이 타겟팅 로직을 안 건드리고 이펙트 레이어만 나중에 얹으면 됨.
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

        private bool AttackNearest(float rangePixels, float damage, int maxTargets)
        {
            var targets = FindTargetsInRange(rangePixels, maxTargets);
            if (targets.Count == 0) return false;

            foreach (var target in targets)
            {
                target.TakeDamage(damage);
                FireBoltProjectile.Spawn(FieldPosition, target.FieldPosition);
            }
            return true;
        }

        private bool AttackAreaAroundNearest(float rangePixels, float damage)
        {
            var nearest = FindTargetsInRange(rangePixels, 1);
            if (nearest.Count == 0) return false;

            float areaRadiusPixels = Data.areaRadius * FieldConstants.UnitSize;
            Vector2 center = nearest[0].FieldPosition;
            FireBoltProjectile.Spawn(FieldPosition, center);

            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(invader.FieldPosition, center) <= areaRadiusPixels)
                    invader.TakeDamage(damage);
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
