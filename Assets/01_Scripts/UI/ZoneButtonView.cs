using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.UI
{
    /// <summary>
    /// 구역 선택 카드 하나. Button.onClick은 매개변수를 못 받으므로, 이 컴포넌트가 자기 zone을
    /// 들고 있다가 매개변수 없는 OnClick()에서 HomeController로 넘겨준다.
    /// </summary>
    // [ExecuteAlways] 없이는 OnEnable이 플레이 모드에서만 돌아서, 씬 뷰(에디터)에서는 이름/상태
    // 텍스트가 유니티가 새 Text에 기본으로 넣는 "New Text" 그대로 보였다("씬에서는 왜 바뀐 게
    // 안 보여" 피드백의 원인) — GridManager와 같은 이유로 에디터에서도 미리보기가 돌게 한다.
    [ExecuteAlways]
    public class ZoneButtonView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private HomeController homeController;
        [SerializeField] private ZoneData zone;
        [SerializeField] private Button button;
        [SerializeField] private Text nameText;
        [SerializeField] private Text stateText;

        // 로비 배경 그림 속 탁자 지도에서 이 구역에 해당하는 아이콘 위에 겹쳐진 은은한 빛 오브젝트.
        // 카드를 누르는 순간 잠깐 켜져서 "이 카드가 지도의 저 지역"이라는 걸 시각적으로 이어준다
        // — 클릭하면 곧장 씬 전환이라 오래 보이진 않지만, 누르는 손맛(피드백)은 확실히 남는다.
        [SerializeField] private GameObject mapGlow;

        // 출전 가능/잠김을 색으로도 구분해서 시인성을 높인다 — 초록=출전 가능, 회색=잠김.
        private static readonly Color AvailableColor = new Color(0.1f, 0.55f, 0.2f, 1f);
        private static readonly Color LockedColor = new Color(0.4f, 0.4f, 0.42f, 1f);

        private void OnEnable()
        {
            // 유니티 6부터 인스펙터에서 한 번도 안 채운 참조는 ?. 로 접근해도
            // UnassignedReferenceException을 던져서(예전처럼 조용히 안 넘어감) 명시적 null
            // 체크로 방어한다 — "Add Lobby Background Only" 메뉴를 아직 안 돌렸으면 비어있는
            // 게 정상.
            if (mapGlow != null) mapGlow.SetActive(false);

            // 에디터 부트스트랩이 오브젝트를 만들자마자(혹은 [ExecuteAlways]로 에디터에서)
            // OnEnable이 먼저 도는 경우가 있어서 zone/button/텍스트 참조가 아직 안 채워졌거나
            // 막 재생성되는 도중일 수 있다 — 전부 null 가드 후 진행.
            if (zone == null || button == null || nameText == null || stateText == null) return;

            bool unlocked = GameProgress.IsZoneUnlocked(zone.zoneIndex);
            button.interactable = unlocked;
            nameText.text = zone.zoneName;
            stateText.text = BuildStateText(zone, unlocked);
            stateText.color = unlocked ? AvailableColor : LockedColor;
        }

        /// <summary>"그만하기"로 저장해둔 진행 웨이브가 있으면 이름 옆 상태 텍스트에 같이
        /// 보여준다 — 클리어한 구역은 체크포인트가 없으니(GameProgress.MarkZoneCleared가 지움)
        /// 그냥 "출전 가능"으로 보인다(재도전은 항상 1웨이브부터).</summary>
        private static string BuildStateText(ZoneData zone, bool unlocked)
        {
            if (!unlocked) return "잠김";

            int savedWave = GameProgress.GetCheckpoint(zone.zoneIndex).wave;
            if (savedWave > 1) return $"진행 중 · {savedWave}/10웨이브";

            return "출전 가능";
        }

        // Button.onClick보다 먼저 도는 포인터 다운 시점에 지도 아이콘을 밝혀서, 씬이 곧장
        // 전환되더라도 "탭한 순간" 반응이 눈에 들어오게 한다.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (zone == null || !GameProgress.IsZoneUnlocked(zone.zoneIndex)) return;
            if (mapGlow != null) mapGlow.SetActive(true);
        }

        public void OnClick()
        {
            homeController.OnClickZone(zone);
        }
    }
}
