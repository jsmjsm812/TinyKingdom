using System;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>
    /// 출전 한정 금화의 MonoBehaviour 얼굴 — 실제 값은 전부 RunSession(씬을 넘어 유지되는 정적
    /// 상태)이 들고 있다. 로비에서 이미 아이템을 사서 금화를 쓴 채로 Battle 씬에 들어올 수
    /// 있어야 해서(RunSession.BeginNewRun은 로비 진입 시 호출됨) 여기서는 값을 리셋하지 않고
    /// RunSession이 들고 있던 값을 그대로 이어받는다.
    /// </summary>
    public class RunCurrency : MonoBehaviour
    {
        public int ManaCrystals => RunSession.Gold;
        public event Action<int> OnChanged;

        public bool DebugInfiniteMana => RunSession.DebugInfiniteGold;

        private void Awake()
        {
            // Home을 거치지 않고 Battle 씬을 바로 열어 테스트하는 경우 대비한 안전장치 —
            // 정상 플로우에서는 HomeController.Awake가 이미 BeginNewRun을 불러놔서 아무 일도 안 함.
            RunSession.EnsureRunBegun();
            RunSession.OnGoldChanged += HandleGoldChanged;
        }

        private void OnDestroy()
        {
            RunSession.OnGoldChanged -= HandleGoldChanged;
        }

        private void HandleGoldChanged(int amount) => OnChanged?.Invoke(amount);

        public void Add(int amount) => RunSession.AddGold(amount);

        public bool TrySpend(int amount) => RunSession.TrySpendGold(amount);

        public void DebugSetInfiniteMana(bool on)
        {
            RunSession.DebugInfiniteGold = on;
            if (on) RunSession.SetGold(99999);
        }
    }
}
