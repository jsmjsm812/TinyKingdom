using UnityEngine;
using WitchHour.Core;

namespace WitchHour.UI
{
    /// <summary>아이템 상점 패널. 두 가지 쓰임새를 겸한다 — 배틀 씬에서는 좌측 탭 전환으로만
    /// SetActive되는 상시 패널(panel 필드는 비워둠), 로비 씬에서는 Open/Close로 여닫는 팝업
    /// (panel = 백드롭+카드 루트)이다. 어느 쪽이든 패널이 활성화될 때마다 슬롯을 새로고침해서
    /// 다른 쪽에서 미리 산 아이템의 구매 개수가 항상 정확히 보이게 한다.</summary>
    public class ItemPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private ItemSlotView[] slots;

        // 배틀씬은 이 패널이 처음부터 활성 상태라(탭 전환은 앞으로 가져올 뿐 SetActive를 안 함),
        // 체크포인트 복원(WaveSpawner)이 RunItemEffects를 채운 뒤에도 슬롯이 그 값을 다시 읽어갈
        // 계기가 없다 — OnEnable이 이미 지나간 뒤라서. 복원 직후 이 인스턴스를 통해 강제로
        // 새로고침해준다(GridManager.Instance와 같은 패턴).
        public static ItemPanelController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            foreach (var slot in slots)
            {
                if (slot != null) slot.Init();
            }
        }

        public void Open()
        {
            if (panel != null) panel.transform.SetAsLastSibling();
            if (panel != null) panel.SetActive(true);
            RefreshSlots();
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }
    }
}
