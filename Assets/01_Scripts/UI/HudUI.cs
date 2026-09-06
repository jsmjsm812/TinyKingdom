using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>화면 위쪽 HUD — 성벽 HP 바, 금화를 표시한다. 웨이브 진행도("웨이브 N/10")는
    /// 예전엔 여기 별도 텍스트로 있었는데, 그 텍스트는 OnWaveStarted가 처음 한 번 불리기 전
    /// (첫 준비 시간 동안)엔 빈 채로 남아있어서 그 구간엔 진행도를 전혀 알 수 없었다. 대신
    /// PrepTimerUI의 배너(준비 중/전투 중 내내 갱신됨)에 합쳐서 표시하도록 옮겼다.</summary>
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private WardHealth ward;
        [SerializeField] private RunCurrency currency;

        [SerializeField] private Image wardFillImage;
        [SerializeField] private Text wardHpText;
        [SerializeField] private Text manaText;

        private void OnEnable()
        {
            ward.OnHpChanged += HandleWardHpChanged;
            currency.OnChanged += HandleCurrencyChanged;
        }

        // WardHealth.Awake()가 CurrentHp를 maxHp로 초기화하는데, Unity는 서로 다른 오브젝트끼리
        // Awake/OnEnable 실행 순서를 보장하지 않는다 — HudUI.OnEnable이 WardHealth.Awake보다
        // 먼저 돌면 그 시점 CurrentHp는 아직 C# 기본값 0이라 "성벽 HP 0/20"이 찍히고, 첫 피격
        // 이후엔 이미 모든 Awake가 끝난 뒤라 정상으로 보였다("피해를 입으면 정상으로 돌아온다"
        // 피드백의 원인). Start()는 씬의 모든 Awake가 끝난 뒤 호출되는 게 보장되므로 초기 동기화는
        // 여기서 한다.
        private void Start()
        {
            HandleWardHpChanged(ward.CurrentHp, ward.MaxHp);
            HandleCurrencyChanged(currency.ManaCrystals);
        }

        private void OnDisable()
        {
            ward.OnHpChanged -= HandleWardHpChanged;
            currency.OnChanged -= HandleCurrencyChanged;
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
    }
}
