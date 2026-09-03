using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>화면 위쪽 HUD — 성벽 HP 바, 금화, 웨이브 진행도를 표시한다.</summary>
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private WardHealth ward;
        [SerializeField] private RunCurrency currency;
        [SerializeField] private WaveSpawner waveSpawner;

        [SerializeField] private Image wardFillImage;
        [SerializeField] private Text wardHpText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text waveText;

        private void OnEnable()
        {
            ward.OnHpChanged += HandleWardHpChanged;
            currency.OnChanged += HandleCurrencyChanged;
            waveSpawner.OnWaveStarted += HandleWaveStarted;

            HandleWardHpChanged(ward.CurrentHp, ward.MaxHp);
            HandleCurrencyChanged(currency.ManaCrystals);
        }

        private void OnDisable()
        {
            ward.OnHpChanged -= HandleWardHpChanged;
            currency.OnChanged -= HandleCurrencyChanged;
            waveSpawner.OnWaveStarted -= HandleWaveStarted;
        }

        // Image.Type.Filled + fillAmount로 했더니 텍스트는 정확히 갱신되는데 바 자체는 항상 꽉 차
        // 보이는(씬에서 재현됨) 문제가 있었다 — 원인을 더 못 찾아서 아예 fillAmount에 의존하지 않는
        // 방식으로 바꿈: 왼쪽 pivot 기준으로 RectTransform.localScale.x만 줄인다(가장 단순한 너비
        // 스케일링이라 렌더링이 안 따라올 여지가 없음).
        private void HandleWardHpChanged(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0f;
            var fillRect = wardFillImage.rectTransform;
            fillRect.localScale = new Vector3(ratio, fillRect.localScale.y, fillRect.localScale.z);
            wardHpText.text = $"성벽 HP {current}/{max}";
        }

        private void HandleCurrencyChanged(int amount)
        {
            manaText.text = $"금화 {amount}";
        }

        private void HandleWaveStarted(int waveNumber)
        {
            waveText.text = $"웨이브 {waveNumber}/{waveSpawner.TotalWaves}";
        }
    }
}
