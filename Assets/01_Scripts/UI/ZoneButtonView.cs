using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.UI
{
    /// <summary>
    /// 구역 선택 카드 하나. Button.onClick은 매개변수를 못 받으므로, 이 컴포넌트가 자기 zone을
    /// 들고 있다가 매개변수 없는 OnClick()에서 HomeController로 넘겨준다.
    /// </summary>
    public class ZoneButtonView : MonoBehaviour
    {
        [SerializeField] private HomeController homeController;
        [SerializeField] private ZoneData zone;
        [SerializeField] private Button button;
        [SerializeField] private Text nameText;
        [SerializeField] private Text stateText;

        private void OnEnable()
        {
            // 에디터 부트스트랩이 오브젝트를 만들자마자 OnEnable이 먼저 도는 경우가 있어서(zone을
            // SerializedObject로 아직 안 넣은 시점) null 가드가 필요하다 — 실제 플레이 시에는
            // 씬이 로드되며 zone이 이미 채워진 상태로 다시 OnEnable이 돌아서 문제없다.
            if (zone == null) return;

            bool unlocked = GameProgress.IsZoneUnlocked(zone.zoneIndex);
            button.interactable = unlocked;
            nameText.text = zone.zoneName;
            stateText.text = unlocked ? "출전" : "잠김";
        }

        public void OnClick()
        {
            homeController.OnClickZone(zone);
        }
    }
}
