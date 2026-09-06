using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.Combat
{
    /// <summary>
    /// TinyKingdom에서 구운 idle/action 프레임 배열(GuardianData.idleFrames 등)을 Image에 순환
    /// 재생하는 경량 컴포넌트. 별도 Animator 없이 UGUI Image 스프라이트만 갈아끼운다 —
    /// WeaponProjectile과 같은 "프레임 배열 + 코루틴" 패턴이라 전체 필드가 여전히 UGUI 하나로 통일된다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.12f; // 초당 약 8프레임

        // idle 루프가 전투 내내 필드의 모든 수호자·침입자마다 계속 도는 제일 뜨거운 코루틴이라,
        // 매번 new WaitForSeconds(FrameDuration)로 새로 할당하면 GC 압박이 컸다 — 값이 상수라
        // 인스턴스 하나만 만들어서 계속 재사용한다(성능 최적화 패스).
        private static readonly WaitForSeconds FrameWait = new WaitForSeconds(FrameDuration);

        private Image _image;
        private Sprite[] _idleFrames;
        private Sprite[] _walkFrames;
        private Sprite[] _actionFrames;
        private Coroutine _playingCoroutine;

        // 지금 루프 중인 상태 — Walk는 매 프레임(InvaderUnit.Update) 호출되므로, 이미 재생 중이면
        // 다시 호출해도 코루틴을 재시작하지 않게 이 값으로 막는다(재시작하면 프레임 인덱스가 매번
        // 0으로 리셋돼 애니메이션이 아예 안 움직이는 것처럼 보임).
        private enum LoopKind { None, Idle, Walk }
        private LoopKind _currentLoop = LoopKind.None;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetFrames(Sprite[] idleFrames, Sprite[] actionFrames, Sprite[] walkFrames = null)
        {
            _idleFrames = idleFrames;
            _walkFrames = walkFrames;
            _actionFrames = actionFrames;
            PlayIdleLoop();
        }

        public void PlayIdleLoop()
        {
            if (_idleFrames == null || _idleFrames.Length == 0) return;
            if (_currentLoop == LoopKind.Idle) return;
            if (_playingCoroutine != null) StopCoroutine(_playingCoroutine);
            _currentLoop = LoopKind.Idle;
            _playingCoroutine = StartCoroutine(Loop(_idleFrames));
        }

        /// <summary>이동 중 재생되는 걷기 루프. walkFrames가 없으면(아직 안 구운 상태) idle로 대체.
        /// 매 프레임 호출돼도 이미 걷는 중이면 재시작하지 않는다(위 _currentLoop 설명 참고).</summary>
        public void PlayWalkLoop()
        {
            var frames = (_walkFrames != null && _walkFrames.Length > 0) ? _walkFrames : _idleFrames;
            if (frames == null || frames.Length == 0) return;
            if (_currentLoop == LoopKind.Walk) return;
            if (_playingCoroutine != null) StopCoroutine(_playingCoroutine);
            _currentLoop = LoopKind.Walk;
            _playingCoroutine = StartCoroutine(Loop(frames));
        }

        /// <summary>action 프레임을 한 번 재생한다. 끝나면 idle로 복귀(resumeIdle=true, 수호자 공격용)
        /// 하거나 마지막 프레임에 멈춘 채로 둔다(resumeIdle=false, 침입자 사망용).</summary>
        public void PlayAction(bool resumeIdle = true, Action onComplete = null)
        {
            if (_actionFrames == null || _actionFrames.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }
            if (_playingCoroutine != null) StopCoroutine(_playingCoroutine);
            _currentLoop = LoopKind.None;
            _playingCoroutine = StartCoroutine(ActionOnce(resumeIdle, onComplete));
        }

        private IEnumerator Loop(Sprite[] frames)
        {
            int i = 0;
            while (true)
            {
                _image.sprite = frames[i % frames.Length];
                i++;
                yield return FrameWait;
            }
        }

        private IEnumerator ActionOnce(bool resumeIdle, Action onComplete)
        {
            foreach (var frame in _actionFrames)
            {
                _image.sprite = frame;
                yield return FrameWait;
            }
            onComplete?.Invoke();
            if (resumeIdle) PlayIdleLoop();
        }
    }
}
