using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>
    /// 하단 패널 탭 전환 — 상점/아이템 중 하나만 보인다. 예전엔 "소환상점"/"아이템" 고정 탭
    /// 버튼이 2개였는데, 자리를 아끼려고 토글 버튼 1개로 합쳤다: 지금 상점 화면이면 라벨이
    /// "아이템"(누르면 아이템으로 전환), 아이템 화면이면 "소환\n상점"(누르면 다시 상점으로)으로
    /// 스스로 바뀐다. 설정 버튼은 이 토글과 별개(항상 고정, 상점/아이템 상태에 안 얽힘)라
    /// 여기서 다루지 않는다.
    /// </summary>
    public class BattleTabController : MonoBehaviour
    {
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject itemPanel;
        [SerializeField] private Text toggleLabel;

        private bool _showingItems;

        private void OnEnable()
        {
            _showingItems = itemPanel != null && itemPanel.activeSelf;
            UpdateLabel();
        }

        public void ToggleShopItems()
        {
            _showingItems = !_showingItems;
            shopPanel.SetActive(!_showingItems);
            itemPanel.SetActive(_showingItems);
            UpdateLabel();
            AudioManager.Instance?.PlayButtonClick();
        }

        private void UpdateLabel()
        {
            if (toggleLabel != null)
                toggleLabel.text = _showingItems ? "소환\n상점" : "아이템";
        }
    }
}
