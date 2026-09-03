using UnityEngine;
using UnityEngine.UI;
using WitchHour.Shop;

namespace WitchHour.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private ShopSlotView[] slotViews;

        // 리롤 버튼 클릭 연결은 에디터에서 AddVoidPersistentListener로 안 하고 여기서 코드로 한다 —
        // 리롤 버튼이 독립 프리팹 에셋(RerollButton.prefab)이라, 프리팹 안에 이 씬의 ShopUI를
        // 직접 참조하는 값을 저장할 수 없다(프리팹은 씬 오브젝트를 못 가리킴). 참조 방향을
        // 반대로 돌려서 — 씬 오브젝트인 ShopUI가 버튼을 들고 있다가 런타임에 리스너를 붙인다.
        [SerializeField] private Button rerollButton;

        private void OnEnable()
        {
            shopManager.OnOffersChanged += Refresh;
            Refresh();
        }

        private void Awake()
        {
            if (rerollButton != null)
                rerollButton.onClick.AddListener(OnClickReroll);
        }

        private void OnDisable()
        {
            shopManager.OnOffersChanged -= Refresh;
        }

        private void Refresh()
        {
            for (int i = 0; i < slotViews.Length; i++)
                slotViews[i].SetData(shopManager.CurrentOffers[i], i, shopManager);
        }

        public void OnClickReroll()
        {
            shopManager.RerollAll(free: false);
        }
    }
}
