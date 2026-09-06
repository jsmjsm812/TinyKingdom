using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>지금 보유한 출전 금화를 보여주는 라벨. RunSession이 로비/배틀 씬을 넘나들며
    /// 골드를 들고 있는 단일 출처라, Battle의 HudUI(RunCurrency 경유)와 별개로 로비에서도
    /// 이걸 바로 구독해서 쓸 수 있다.</summary>
    public class GoldDisplayText : MonoBehaviour
    {
        [SerializeField] private Text label;

        private void OnEnable()
        {
            RunSession.OnGoldChanged += Refresh;
            Refresh(RunSession.Gold);
        }

        private void OnDisable()
        {
            RunSession.OnGoldChanged -= Refresh;
        }

        private void Refresh(int amount)
        {
            if (label != null) label.text = $"금화 {amount}";
        }
    }
}
