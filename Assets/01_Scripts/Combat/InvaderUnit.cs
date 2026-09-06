using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 침입자 런타임 인스턴스. 오브젝트 풀에서 재사용되므로 상태는 전부 Spawn()에서 리셋한다.
    /// FieldPosition은 LanePath.Waypoints를 순서대로 따라가는 논리 좌표(유닛*180px 기준)이고,
    /// RectTransform.anchoredPosition은 같은 값을 그대로 반영하는 시각 표현일 뿐이다.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(SpriteFrameAnimator))]
    public class InvaderUnit : MonoBehaviour
    {
        // GuardianUnit이 타겟을 찾을 때 매 프레임 FindObjectsOfType을 쓰지 않도록 두는 전역 활성 목록.
        public static readonly List<InvaderUnit> ActiveUnits = new List<InvaderUnit>();

        public InvaderData Data { get; private set; }
        public float CurrentHp { get; private set; }
        public Vector2 FieldPosition { get; private set; }
        public bool IsAlive => CurrentHp > 0f;

        [SerializeField] private Image hpFillImage;
        private float _maxHp;

        // 경로를 따라 이동한 누적 거리. 선반 구간에서는 Y가 고정이라 "성벽까지 얼마나
        // 남았는지"를 Y비교로 알 수 없으므로, 대신 이 값을 GuardianUnit 타겟 우선순위에 쓴다.
        public float PathProgress => LanePath.CumulativeDistances[_waypointIndex]
            + Vector2.Distance(LanePath.Waypoints[_waypointIndex], FieldPosition);

        // 하양 플래시는 있어도 잘 안 보인다는 피드백 — "맞으면 빨갛게 변하는" 전형적인 피격
        // 연출로 바꿈. 스프라이트 자체 색을 곱연산이 아니라 그대로 덮어써서(_image.color 대입)
        // 원본 색조가 진하든 연하든 항상 눈에 띄는 새빨간 색이 뜨게 한다.
        private static readonly Color HitFlashColor = new Color(1f, 0.15f, 0.15f);
        private static readonly Color ManaRewardTextColor = new Color(0.6f, 1f, 0.6f);
        private static readonly Color DamageTextColor = new Color(1f, 0.95f, 0.3f);
        private static readonly Color NoSpriteFallbackColor = new Color(0.8f, 0.2f, 0.2f);
        private const float HitFlashDuration = 0.15f; // 0.08초는 너무 짧아 거의 안 보인다는 피드백 — 늘림
        // 값이 상수라 맞을 때마다 새로 할당할 필요 없이 하나만 캐싱해서 재사용(GC 압박 감소).
        private static readonly WaitForSeconds HitFlashWait = new WaitForSeconds(HitFlashDuration);

        // 체력바가 목표치까지 1초에 이 비율만큼 채워진다(Mathf.MoveTowards 기준) — 즉 풀피에서
        // 빈 바까지는 최대 1/HpBarFillRatePerSecond초 걸림. 값이 클수록 더 빨리 쫓아간다.
        private const float HpBarFillRatePerSecond = 4f; // 0→1 전체 구간 기준 0.25초

        private RectTransform _rect;
        private Image _image;
        private SpriteFrameAnimator _frameAnimator;
        private RectTransform _hpBarRoot; // hpFillImage의 부모(HpBarBg) — 좌우 반전 시 같이 상쇄시킬 대상
        private Color _baseColor;
        private Coroutine _flashCoroutine;
        private bool _facingLeft;
        private float _moveSpeedPixelsPerSecond;
        private int _waypointIndex;
        private Action<InvaderUnit> _releaseCallback;
        private Action<InvaderUnit> _onReachWard;
        private Action<InvaderUnit> _onDeath;
        private Coroutine _dotCoroutine;
        private Coroutine _slowCoroutine;
        private float _slowMultiplier = 1f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _frameAnimator = GetComponent<SpriteFrameAnimator>();
            if (hpFillImage != null) _hpBarRoot = hpFillImage.transform.parent as RectTransform;
        }

        // ActiveUnits는 static이라 씬을 넘어 살아남는다 — "그만하기"로 배틀씬을 웨이브 도중에
        // 나가면(침입자가 아직 살아있는 채로) 유니티가 씬 언로드로 이 오브젝트들을 전부 자동
        // Destroy하는데, 그때 ReachWard/Die를 거치지 않으니 ActiveUnits에서 안 빠진 채 남는다 —
        // GuardianUnit과 같은 이유의 방어(다음 배틀씬에서 죽은 참조가 타겟팅에 잡히는 것 방지).
        private void OnDestroy()
        {
            ActiveUnits.Remove(this);
        }

        public void Spawn(
            InvaderData data,
            float hpMultiplier,
            float speedMultiplier,
            Action<InvaderUnit> releaseCallback,
            Action<InvaderUnit> onReachWard,
            Action<InvaderUnit> onDeath)
        {
            Data = data;
            CurrentHp = data.baseHp * hpMultiplier;
            _maxHp = CurrentHp;
            // 풀에서 재사용되는 인스턴스라 이전 생애의 체력바 상태가 남아있을 수 있다 — 즉시
            // 꽉 찬 상태로 리셋(스폰 순간부터 서서히 차오르는 건 오히려 어색함).
            if (hpFillImage != null) hpFillImage.rectTransform.localScale = Vector3.one;
            // 구역 속도배율(ZoneData.speedMultiplier — 존3 마왕의 영지 ×1.2 등)을 여기서 처음
            // 실제로 곱한다. 예전엔 필드는 있는데 이동속도 계산엔 안 곱해지고 있었다("미구현"
            // 상태로 GDD에 명시돼 있었음).
            // 아이템 상점의 "느림" 디버프(RunItemEffects.InvaderSpeedMultiplier)도 여기서 같이
            // 곱한다 — 구입 시점 이후 새로 스폰되는 침입자부터 적용됨(이미 걷고 있던 개체는
            // 그대로, 준비 시간에 사서 다음 웨이브부터 적용받는 게 자연스러움).
            _moveSpeedPixelsPerSecond = data.moveSpeed * FieldConstants.UnitSize
                * FieldConstants.GlobalInvaderSpeedMultiplier * speedMultiplier * RunItemEffects.InvaderSpeedMultiplier;
            _releaseCallback = releaseCallback;
            _onReachWard = onReachWard;
            _onDeath = onDeath;

            _waypointIndex = 0;
            FieldPosition = LanePath.Waypoints[0];
            _rect.anchoredPosition = FieldPosition;

            // TinyKingdom 배틀 스프라이트를 재생한다 — 없으면(아직 굽지 않은 상태) 빨간 사각형으로
            // 폴백. 색은 매 스폰마다 다시 정해야 하니 _baseColor를 여기서 갱신한다. 침입자는 스폰
            // 직후부터 계속 걷는 상태라 idle이 아니라 걷기 루프로 바로 시작한다.
            if (data.idleFrames != null && data.idleFrames.Length > 0)
            {
                _image.preserveAspect = true;
                _frameAnimator.SetFrames(data.idleFrames, data.actionFrames, data.walkFrames);
                _frameAnimator.PlayWalkLoop();
                _baseColor = Color.white;
            }
            else
            {
                _image.sprite = data.sprite; // 구 초상화라도 있으면 최소한 그거라도
                _baseColor = data.sprite != null ? Color.white : NoSpriteFallbackColor;
            }
            _image.color = _baseColor;

            // 좌우 반전 상태를 리셋(이전 생애에 왼쪽을 보고 죽었을 수 있음) — visualScale은
            // 부호와 별개로 크기 배율이라 항상 양수로 재적용한다.
            _facingLeft = false;
            _rect.localScale = Vector3.one * data.visualScale;
            if (_hpBarRoot != null) _hpBarRoot.localScale = Vector3.one;

            // 풀에서 재사용되는 인스턴스라 이전 생애의 피격 플래시가 진행 중이었을 수 있다 — 초기화.
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            // 도트도 마찬가지 — 이전 생애에 마녀한테 도트를 맞다가 죽어서 풀로 반납됐을 수 있다.
            if (_dotCoroutine != null)
            {
                StopCoroutine(_dotCoroutine);
                _dotCoroutine = null;
            }
            // 둔화(연금술사/빙결사)도 마찬가지 — 이전 생애의 감속 효과가 안 풀린 채로 재사용되면
            // 새로 태어난 침입자가 이유 없이 느리게 걷는 버그가 된다.
            if (_slowCoroutine != null)
            {
                StopCoroutine(_slowCoroutine);
                _slowCoroutine = null;
            }
            _slowMultiplier = 1f;

            ActiveUnits.Add(this);
        }

        private void Update()
        {
            // 살아있는 동안 매 프레임 실제 비율(CurrentHp/_maxHp)을 향해 서서히 채워지게 한다.
            // 예전엔 맞을 때마다 fillAmount를 즉시 대입해서, 사거리 안 용사 여러 기가 같은
            // 우선순위 타겟에 한 프레임 안에 몰아치면 꽉 찬 바가 그대로 0으로 스냅해버려 눈에는
            // "안 줄어들다가 갑자기 사라짐"으로 보였다 — 코루틴 재시작 경합도 없는 가장 단순한
            // 방식(매 프레임 목표치로 한 걸음씩)으로 바꿔서 반드시 보이게 한다.
            UpdateHpBarVisual();

            if (!IsAlive) return;

            _frameAnimator.PlayWalkLoop(); // 이미 걷는 중이면 내부에서 재시작 없이 무시됨

            // 예전엔 남은 이동거리(step)가 다음 waypoint까지 거리보다 크면 그냥 waypoint에 딱
            // 맞춰 스냅하고 그 프레임의 남은 이동량을 통째로 버렸다 — 코너(선반→낙하 전환)에서
            // 속도가 빠르면 "그 자리에 딱 멈췄다가 다음 프레임에 새 방향으로 출발"하는 것처럼
            // 보여서 "순간이동 후 방향 전환" 같은 끊기는 느낌의 원인이었다. 남은 거리를 다음
            // 구간으로 이월시켜 같은 프레임 안에서 바로 이어 걷게 하면 코너에서도 끊김이 없다.
            // (waypoint 개수만큼만 반복하도록 안전장치를 둬서 무한루프 위험은 없앰.)
            float remaining = _moveSpeedPixelsPerSecond * _slowMultiplier * Time.deltaTime;
            int safety = LanePath.Waypoints.Count;
            while (remaining > 0f && safety-- > 0)
            {
                Vector2 target = LanePath.Waypoints[_waypointIndex + 1];
                Vector2 toTarget = target - FieldPosition;
                float dist = toTarget.magnitude;

                UpdateFacing(toTarget);

                if (dist <= remaining)
                {
                    FieldPosition = target;
                    remaining -= dist;

                    if (_waypointIndex + 1 >= LanePath.Waypoints.Count - 1)
                    {
                        _rect.anchoredPosition = FieldPosition;
                        ReachWard();
                        return;
                    }
                    _waypointIndex++;
                }
                else
                {
                    FieldPosition += toTarget.normalized * remaining;
                    remaining = 0f;
                }
            }
            _rect.anchoredPosition = FieldPosition;
        }

        /// <summary>이동 방향의 좌우 성분으로 미러링한다. 낙하 구간(세로 이동만, dx≈0)에서는
        /// 마지막으로 걷던 좌우 방향을 그대로 유지한다.</summary>
        private void UpdateFacing(Vector2 toTarget)
        {
            if (Mathf.Abs(toTarget.x) < 0.01f) return;

            bool faceLeft = toTarget.x < 0f;
            if (faceLeft == _facingLeft) return;
            _facingLeft = faceLeft;

            float sign = faceLeft ? -1f : 1f;
            Vector3 s = _rect.localScale;
            _rect.localScale = new Vector3(sign * Mathf.Abs(s.x), s.y, s.z);

            // 체력바는 자식이라 부모가 미러링되면 같이 뒤집혀 왼쪽 pivot 기준으로 줄어들던 방향이
            // 반대로 보인다 — 부모와 같은 부호로 한 번 더 뒤집어(부호×부호=양수) 상쇄시킨다.
            if (_hpBarRoot != null) _hpBarRoot.localScale = new Vector3(sign, 1f, 1f);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            CurrentHp -= amount;
            // 맞은 만큼 숫자로 바로 보여준다 — 체력바만으로는 한 번에 얼마나 들어갔는지 체감이 안
            // 된다는 피드백. 죽어서 Die()로 빠지는 경우도 마지막 타격량은 보여줘야 하니 여기서
            // 먼저 띄우고 그 다음에 생사를 가른다.
            FloatingTextEffect.Spawn(FieldPosition, $"-{Mathf.RoundToInt(amount)}", DamageTextColor, fontSize: 38);

            if (CurrentHp <= 0f)
            {
                Die();
                return;
            }

            AudioManager.Instance?.PlayHit();
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashHit());
        }

        /// <summary>매 프레임 실제 비율을 향해 한 걸음씩 다가간다(MoveTowards) — 코루틴을 맞을
        /// 때마다 새로 시작/정지하는 방식이 아니라서, 같은 프레임에 여러 번 맞아도 재시작 경합 없이
        /// 항상 "지금 목표치"로 수렴한다. Image.fillAmount 대신 왼쪽 pivot 기준
        /// RectTransform.localScale.x를 직접 줄이는 방식 — fillAmount는 씬에서 텍스트는 맞는데
        /// 바 자체가 갱신 안 되는 문제가 있어서 훨씬 원시적인(반드시 반영되는) 방식으로 바꿨다.</summary>
        private void UpdateHpBarVisual()
        {
            if (hpFillImage == null) return;
            float target = _maxHp > 0f ? Mathf.Clamp01(CurrentHp / _maxHp) : 0f;
            var fillRect = hpFillImage.rectTransform;
            float current = fillRect.localScale.x;
            fillRect.localScale = new Vector3(Mathf.MoveTowards(current, target, HpBarFillRatePerSecond * Time.deltaTime), 1f, 1f);
        }

        private IEnumerator FlashHit()
        {
            _image.color = HitFlashColor;
            yield return HitFlashWait;
            _image.color = _baseColor;
            _flashCoroutine = null;
        }

        /// <summary>마녀(DotSingle)의 도트 — 즉발 적중 피해와 별개로 totalDamage를 durationSeconds에
        /// 걸쳐 tickInterval 간격으로 나눠서 준다. 이미 도트가 걸려 있으면(마녀가 다시 적중시킨 경우)
        /// 새 도트로 갈아치운다(중첩 대신 갱신 — 무한 중첩으로 밸런스가 터지는 걸 막기 위함).</summary>
        public void ApplyDot(float totalDamage, float durationSeconds, float tickInterval)
        {
            if (!IsAlive || totalDamage <= 0f) return;
            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);
            _dotCoroutine = StartCoroutine(RunDot(totalDamage, durationSeconds, tickInterval));
        }

        private IEnumerator RunDot(float totalDamage, float durationSeconds, float tickInterval)
        {
            int tickCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds / Mathf.Max(tickInterval, 0.01f)));
            float perTick = totalDamage / tickCount;
            var wait = new WaitForSeconds(tickInterval);

            for (int i = 0; i < tickCount; i++)
            {
                yield return wait;
                if (!IsAlive) yield break;
                TakeDamage(perTick);
            }
            _dotCoroutine = null;
        }

        /// <summary>연금술사(Area)/빙결사(Pierce)의 둔화 — 적중한 대상의 이동속도를 일정 시간
        /// 낮춘다. 다시 맞으면(같은 대상을 또 적중) 중첩 대신 지속시간을 갱신한다.</summary>
        public void ApplySlow(float multiplier, float durationSeconds)
        {
            if (!IsAlive) return;
            if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
            _slowCoroutine = StartCoroutine(RunSlow(multiplier, durationSeconds));
        }

        private IEnumerator RunSlow(float multiplier, float durationSeconds)
        {
            _slowMultiplier = multiplier;
            yield return new WaitForSeconds(durationSeconds);
            _slowMultiplier = 1f;
            _slowCoroutine = null;
        }

        private void ReachWard()
        {
            ActiveUnits.Remove(this);
            _onReachWard?.Invoke(this);
            _releaseCallback?.Invoke(this);
        }

        private void Die()
        {
            ActiveUnits.Remove(this);
            DeathPopEffect.Spawn(FieldPosition);
            FloatingTextEffect.Spawn(FieldPosition, $"+{Data.manaCrystalReward}", ManaRewardTextColor);
            AudioManager.Instance?.PlayInvaderDeath();
            _onDeath?.Invoke(this);

            // death 프레임이 있으면 다 재생한 뒤에 풀로 반납한다(resumeIdle:false — 마지막 프레임에서 멈춤).
            // 없으면(아직 안 구운 상태) 기존처럼 즉시 반납.
            if (_frameAnimator != null && Data.actionFrames != null && Data.actionFrames.Length > 0)
                _frameAnimator.PlayAction(resumeIdle: false, onComplete: () => _releaseCallback?.Invoke(this));
            else
                _releaseCallback?.Invoke(this);
        }
    }
}
