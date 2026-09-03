using System;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>
    /// 출전 한정 금화. 용병 주점에서 용사를 고용하는 데 쓰인다.
    /// </summary>
    public class RunCurrency : MonoBehaviour
    {
        [Tooltip("출전 시작 보너스 (GDD.md 9번: 200)")]
        [SerializeField] private int startingCurrency = 200;

        public int ManaCrystals { get; private set; }
        public event Action<int> OnChanged;

        private void Awake()
        {
            ManaCrystals = startingCurrency;
        }

        public void Add(int amount)
        {
            ManaCrystals += amount;
            OnChanged?.Invoke(ManaCrystals);
        }

        public bool TrySpend(int amount)
        {
            if (ManaCrystals < amount) return false;

            ManaCrystals -= amount;
            OnChanged?.Invoke(ManaCrystals);
            return true;
        }
    }
}
