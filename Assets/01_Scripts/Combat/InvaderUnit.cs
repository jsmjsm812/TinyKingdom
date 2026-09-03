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

        private static readonly Color HitFlashColor = Color.white;
        private static readonly Color ManaRewardTextColor = new Color(0.6f, 1f, 0.6f);
        private static readonly Color NoSpriteFallbackColor = new Color(0.8f, 0.2f, 0.2f);
        private const float HitFlashDuration = 0.08f;
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

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _frameAnimator = GetComponent<SpriteFrameAnimator>();
            if (hpFillImage != null) _hpBarRoot = hpFillImage.transform.parent as RectTransform;
        }

        public void Spawn(
            InvaderData data,
            float hpMultiplier,
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
            _moveSpeedPixelsPerSecond = data.moveSpeed * FieldConstants.UnitSize * FieldConstants.GlobalInvaderSpeedMultiplier;
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
            float remaining = _moveSpeedPixelsPerSecond * Time.deltaTime;
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
