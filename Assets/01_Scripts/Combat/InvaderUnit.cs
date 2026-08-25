using System;
using System.Collections.Generic;
using UnityEngine;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 침입자 런타임 인스턴스. 오브젝트 풀에서 재사용되므로 상태는 전부 Spawn()에서 리셋한다.
    /// FieldPosition은 통로를 따라 내려가는 논리 좌표(유닛*180px 기준)이고,
    /// RectTransform.anchoredPosition은 같은 값을 그대로 반영하는 시각 표현일 뿐이다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class InvaderUnit : MonoBehaviour
    {
        // GuardianUnit이 타겟을 찾을 때 매 프레임 FindObjectsOfType을 쓰지 않도록 두는 전역 활성 목록.
        public static readonly List<InvaderUnit> ActiveUnits = new List<InvaderUnit>();

        public InvaderData Data { get; private set; }
        public float CurrentHp { get; private set; }
        public Vector2 FieldPosition { get; private set; }
        public bool IsAlive => CurrentHp > 0f;

        private RectTransform _rect;
        private float _moveSpeedPixelsPerSecond;
        private float _wardY;
        private Action<InvaderUnit> _releaseCallback;
        private Action<InvaderUnit> _onReachWard;
        private Action<InvaderUnit> _onDeath;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Spawn(
            InvaderData data,
            float hpMultiplier,
            Vector2 startPosition,
            float wardY,
            Action<InvaderUnit> releaseCallback,
            Action<InvaderUnit> onReachWard,
            Action<InvaderUnit> onDeath)
        {
            Data = data;
            CurrentHp = data.baseHp * hpMultiplier;
            _moveSpeedPixelsPerSecond = data.moveSpeed * FieldConstants.UnitSize;
            _wardY = wardY;
            _releaseCallback = releaseCallback;
            _onReachWard = onReachWard;
            _onDeath = onDeath;

            FieldPosition = startPosition;
            _rect.anchoredPosition = FieldPosition;

            ActiveUnits.Add(this);
        }

        private void Update()
        {
            if (!IsAlive) return;

            FieldPosition += Vector2.down * (_moveSpeedPixelsPerSecond * Time.deltaTime);
            _rect.anchoredPosition = FieldPosition;

            if (FieldPosition.y <= _wardY)
                ReachWard();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            CurrentHp -= amount;
            if (CurrentHp <= 0f) Die();
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
            _onDeath?.Invoke(this);
            _releaseCallback?.Invoke(this);
        }
    }
}
