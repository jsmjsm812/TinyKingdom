using System;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>결계(학원) HP. GDD.md 3번: 기본 20.</summary>
    public class WardHealth : MonoBehaviour
    {
        [SerializeField] private int maxHp = 20;

        public int CurrentHp { get; private set; }
        public bool IsDepleted => CurrentHp <= 0;

        public event Action<int, int> OnHpChanged; // (current, max)
        public event Action OnDepleted;

        private void Awake()
        {
            CurrentHp = maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (IsDepleted) return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHpChanged?.Invoke(CurrentHp, maxHp);

            if (CurrentHp <= 0)
                OnDepleted?.Invoke();
        }
    }
}
