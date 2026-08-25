using System;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>
    /// 출전 한정 마나결정. 소환 상점(2주차)이 아직 없어서 지금은 적립만 되지만,
    /// 상점 붙일 때 이 클래스만 참조하면 되도록 미리 분리해둔다.
    /// </summary>
    public class RunCurrency : MonoBehaviour
    {
        public int ManaCrystals { get; private set; }
        public event Action<int> OnChanged;

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
