using System;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>성벽(왕국 성) HP. GDD.md 3번: 기본 20.</summary>
    public class WardHealth : MonoBehaviour
    {
        [SerializeField] private int maxHp = 20;

        public int CurrentHp { get; private set; }
        public int MaxHp => maxHp;
        public bool IsDepleted => CurrentHp <= 0;

        public event Action<int, int> OnHpChanged; // (current, max)
        public event Action OnDepleted;

        // 디버그 모드용 — 켜져 있으면 TakeDamage가 아무것도 안 한다. DebugOverlay에서만 건드린다.
        public bool DebugInvincible { get; set; }

        private void Awake()
        {
            CurrentHp = maxHp;
        }

        /// <summary>디버그 모드 "구역 전환" 버튼용 — 체력을 가득 채우고 갱신 이벤트를 쏜다.</summary>
        public void DebugResetHp()
        {
            CurrentHp = maxHp;
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        public void TakeDamage(int amount)
        {
            if (IsDepleted || DebugInvincible) return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHpChanged?.Invoke(CurrentHp, maxHp);

            if (CurrentHp <= 0)
                OnDepleted?.Invoke();
        }
    }
}
