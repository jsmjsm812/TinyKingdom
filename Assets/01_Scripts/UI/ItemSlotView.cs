using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.UI
{
    /// <summary>아이템 상점 한 칸(로비/배틀 공용). 리롤 없이 항상 같은 아이템을 보여주고,
    /// 같은 아이템도 최대 RunItemEffects.MaxStackPerItem개까지 중복 구매해 효과를 누적시킬 수
    /// 있다. 금화는 RunSession이 씬을 넘어 들고 있는 단일 지갑이라 별도 참조 없이 바로 쓴다.</summary>
    public class ItemSlotView : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descText;
        [SerializeField] private Text costText;
        [SerializeField] private Button buyButton;

        /// <summary>에디터 빌드 스크립트 전용(Editor 어셈블리에서 호출하려면 public이어야 함) —
        /// SerializedObject.FindProperty("item")로 늦게 덮어쓰는 대신 직접 필드를 대입한다
        /// (둘 다 씬 저장 시 똑같이 직렬화되지만, 후자가 훨씬 단순하고 실패할 여지가 적다).</summary>
        public void EditorAssignItem(ItemData data) => item = data;

        // ItemPanelController가 상위 패널이 열릴 때 Init()을 호출해주지만, 그 호출이
        // 부모→자식 활성화 순서 등으로 씹히는 경우에도(로비 팝업에서 아이콘/이름/설명이 전부
        // 프리팹 기본값("아이템"/"설명"/"0")으로만 보이는 문제가 있었음) 이 슬롯 자신이 켜질
        // 때 스스로도 채워 넣도록 자체 방어한다 — item 필드는 에디터 스크립트가 이미 물려놨으니
        // 외부 호출 없이도 혼자 완결적으로 그릴 수 있다.
        private void OnEnable()
        {
            Init();
        }

        /// <summary>패널이 열릴 때마다(로비 팝업을 다시 열거나, 배틀 아이템 탭으로 전환할 때)
        /// 호출 — 아이콘/이름/설명은 한 번만 세팅하면 되지만 어차피 가벼워서 매번 다시 써도 무해.</summary>
        public void Init()
        {
            if (item == null) return;

            // cardBackground 색은 여기서 더 이상 자동으로 안 칠한다 — 사용자가 배틀씬에서
            // 카드 배경색을 직접 골라 칠했는데, 패널이 열릴 때마다(탭 전환/Play) 이 코드가
            // item.cardAccent 기준으로 매번 도로 덮어써서 "색을 바꿔도 적용이 안 된다"는
            // 문제가 있었다. 색은 이제 순수하게 씬에 저장된 값 그대로 유지된다.

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.color = item.iconTint;
            }
            if (nameText != null) nameText.text = item.itemName;
            if (descText != null) descText.text = item.description;
            Refresh();
        }

        private void Refresh()
        {
            if (item == null || costText == null || buyButton == null) return;

            int count = RunItemEffects.GetCount(item);
            bool maxed = count >= RunItemEffects.MaxStackPerItem;
            costText.text = maxed ? "MAX" : $"{item.cost}G ({count}/{RunItemEffects.MaxStackPerItem})";
            buyButton.interactable = !maxed;
        }

        public void OnClickBuy()
        {
            if (item == null) return;
            if (!RunItemEffects.CanPurchase(item)) return;
            if (!RunSession.TrySpendGold(item.cost)) return;

            RunItemEffects.Apply(item);
            AudioManager.Instance?.PlayPurchase();
            Refresh();
        }
    }
}
