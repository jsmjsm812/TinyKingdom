using UnityEngine;
using UnityEngine.UI;
using WitchHour.Data;
using WitchHour.Shop;

namespace WitchHour.UI
{
    /// <summary>상점 카드 한 칸. 카드 전체가 버튼이라 별도 구매 버튼 없이 카드를 누르면 바로 구매된다.</summary>
    public class ShopSlotView : MonoBehaviour
    {
        private static readonly Color EmptyCardColor = new Color(0.14f, 0.14f, 0.16f);

        [SerializeField] private Image cardBackground;
        [SerializeField] private Outline cardOutline;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private Text statsText;

        private int _slotIndex;
        private ShopManager _shopManager;

        public void SetData(GuardianData data, int slotIndex, ShopManager shopManager)
        {
            _slotIndex = slotIndex;
            _shopManager = shopManager;

            bool hasData = data != null;
            portraitImage.enabled = hasData;
            portraitImage.sprite = hasData ? data.portrait : null;
            nameText.text = hasData ? data.guardianName : "";
            costText.text = hasData ? data.shopCost.ToString() : "";
            // 사기 전에 공격력/공속/사거리를 보고 판단할 수 있어야 한다는 피드백 — 실제 전투
            // 스탯(GuardianUnit이 쓰는 것과 동일한 필드)을 카드 오른쪽에 세로 3줄로 보여준다.
            // 한 글자짜리 라벨(공/속/사)은 "사람들은 모르지" 피드백으로 알아볼 수 있는 짧은
            // 단어(공격/속도/사거리)로 바꿨다.
            if (statsText != null)
                statsText.text = hasData
                    ? $"공격 {Mathf.RoundToInt(data.attackPower)}\n속도 {data.attackSpeed:0.0}\n사거리 {data.range:0.0}"
                    : "";

            // 등급별로 카드 색/테두리를 다르게 줘서 한눈에 희귀도가 보이게 한다.
            Color bg = hasData ? RarityColors.GetCardColor(data.rarity) : EmptyCardColor;
            cardBackground.color = bg;
            cardOutline.enabled = hasData;
            if (hasData) cardOutline.effectColor = RarityColors.GetAccentColor(data.rarity);

            // 배경이 등급마다 동적으로 바뀌는데 글자색은 고정이면, 나중에 등급 색을 밝게 바꿨을 때
            // 안 보이게 될 수 있다("배경이 어두운 카드는 흰색, 밝은 카드는 검은색" 피드백) —
            // 실제 배경 밝기 기준으로 다시 계산한다. costText는 제외 — HUD 금화색과 맞춘 고정
            // 금색이 의도된 디자인이라(ShopRosterUIBootstrap 참고) 여기서 덮어쓰면 안 됨.
            Color textColor = RarityColors.GetReadableTextColor(bg);
            nameText.color = textColor;
            if (statsText != null) statsText.color = textColor;
        }

        public void OnClickBuy()
        {
            _shopManager.TryPurchase(_slotIndex);
        }
    }
}
