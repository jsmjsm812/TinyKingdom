using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.UI
{
    /// <summary>
    /// 용사 도감 — GuardianRegistry(Resources, 세이브 시스템과 공유)에 등록된 전체 용사를
    /// 순서대로 보여준다. 해금 안 된 용사는 실루엣(어둡게)+이름 "???"로 가린다.
    /// GameProgress(런타임 전역 상태)를 참조해야 해서 독립 프리팹으로는 안 만들고 씬 오브젝트로 둔다.
    /// </summary>
    public class GuardianCodexController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private GuardianRegistry registry;
        [SerializeField] private List<Image> entryBackgrounds;
        [SerializeField] private List<Image> entryPortraits;
        [SerializeField] private List<Text> entryNames;

        // 도감 칸을 누르면 뜨는 상세 스탯 팝업. GuardianData 원본 수치를 그대로 보여준다
        // (GDD.md의 용사 표와 동일한 항목: 사거리/공속/방식/공격력/특수효과).
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailPortrait;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailStats;
        [SerializeField] private Text detailSpecial;

        private static readonly Color LockedBg = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color LockedPortrait = new Color(0.1f, 0.1f, 0.1f);

        public void Open()
        {
            // 도감 패널이 씬 계층에서 구역 카드보다 먼저 만들어진 자식이라 형제 순서상 뒤에
            // 그려지는 카드들에 가려져 있었다("도감을 누르면 맵 바에 가려서 안 보여" 피드백) —
            // 열 때마다 맨 뒤 형제로 끌어올려서 항상 다른 UI 위에 뜨게 한다.
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            // OnEntryClicked(상세 팝업)와 같은 이유 — 꺼져있는 패널의 자식 Text/Image에 먼저 값을
            // 채우고 나중에 켜면, 처음 여는 그 프레임엔 유니티가 캔버스를 아직 안 그려서(특히
            // GridLayoutGroup 자식들이 레이아웃을 한 번도 계산 안 한 상태) 칸들이 자리를 못
            // 잡고 흰 사각형 하나로 뭉쳐 보이는 문제가 있었다("처음 도감 누르면 빈 팝업" 피드백,
            // 두 번째부터 정상인 이유는 그땐 이미 한 번 레이아웃이 계산돼 있어서). 반드시 켠
            // 뒤에 채운다.
            Refresh();
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            // detailPanel은 "WitchHour > Add Guardian Codex Detail Popup" 메뉴를 실행해야
            // 생기는 오브젝트라, 그 메뉴를 아직 안 돌렸으면 비어있는 게 정상이다 — 유니티 6부터
            // 인스펙터에서 한 번도 안 채운 참조는 ?. 로 접근해도 UnassignedReferenceException을
            // 던지길래(예전 버전처럼 조용히 넘어가지 않음) 명시적 null 체크로 방어한다.
            if (detailPanel != null) detailPanel.SetActive(false);
            panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }

        // CodexEntry 버튼마다(도감 칸) 자기 인덱스를 들고 이 메서드를 호출한다(Button.onClick은
        // 매개변수 없는 UnityEvent가 기본이라, 에디터 스크립트에서 UnityEventTools로 인덱스를
        // 박아 넣는 int 버전 리스너를 사용— CodexEntryButtonBootstrap 참고). 잠긴 용사는 아직
        // "???"만 보여주는 칸이라 상세창을 열지 않는다.
        public void OnEntryClicked(int index)
        {
            if (registry == null || index < 0 || index >= registry.allGuardians.Count) return;

            GuardianData data = registry.allGuardians[index];
            if (data == null || !IsUnlocked(data)) return;

            // 꺼져있는(SetActive(false)) 패널의 자식 Text/Image에 값을 먼저 채우고 나중에 켜면,
            // 유니티가 그 프레임엔 캔버스를 다시 안 그려서 처음 열 때 텍스트/스프라이트가 전부
            // 빈 채로 보이는 문제가 있었다("도감 젤 처음에 누르면 빈 팝업" 피드백) — 먼저 켜서
            // 캔버스가 활성 상태가 된 뒤에 값을 채우는 순서로 바꿨다.
            if (detailPanel != null) detailPanel.SetActive(true);

            if (detailPortrait != null) detailPortrait.sprite = data.portrait;
            if (detailName != null) detailName.text = data.guardianName;
            if (detailRarity != null) detailRarity.text = RarityLabel(data.rarity);
            if (detailStats != null) detailStats.text = BuildStatsText(data);
            if (detailSpecial != null)
                detailSpecial.text = "특수효과: " + (string.IsNullOrEmpty(data.specialEffectDescription) ? "—" : data.specialEffectDescription);

            AudioManager.Instance?.PlayButtonClick();
        }

        public void CloseDetail()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }

        private static string BuildStatsText(GuardianData data)
        {
            string typeLabel = AttackTypeLabel(data.attackType);
            string text = $"사거리 {data.range:0.#}\n공격속도 {data.attackSpeed:0.#}회/초\n공격방식 {typeLabel}\n공격력 {data.attackPower:0.#}";
            if (data.attackType == AttackType.Area)
                text += $"\n범위 반경 {data.areaRadius:0.#}";
            return text;
        }

        private static string RarityLabel(GuardianRarity rarity) => rarity switch
        {
            GuardianRarity.Star1 => "★1",
            GuardianRarity.Star2 => "★2",
            GuardianRarity.Star3 => "★3",
            _ => "?"
        };

        // GDD.md 용사 표의 "방식" 칸과 같은 한글 표기로 맞춘다.
        private static string AttackTypeLabel(AttackType type) => type switch
        {
            AttackType.Single => "단일",
            AttackType.Multi => "다중",
            AttackType.Area => "범위",
            AttackType.Pierce => "관통",
            AttackType.DotSingle => "단일+도트",
            AttackType.BuffSingle => "단일+버프",
            _ => type.ToString()
        };

        private void Refresh()
        {
            if (registry == null) return;

            int count = Mathf.Min(entryPortraits.Count, registry.allGuardians.Count);
            for (int i = 0; i < count; i++)
            {
                GuardianData data = registry.allGuardians[i];
                if (data == null) continue;

                bool unlocked = IsUnlocked(data);
                entryPortraits[i].sprite = data.portrait;
                entryPortraits[i].color = unlocked ? Color.white : LockedPortrait;
                entryNames[i].text = unlocked ? data.guardianName : "???";

                Color bg = unlocked ? RarityColors.GetCardColor(data.rarity) : LockedBg;
                entryBackgrounds[i].color = bg;
                // 카드 배경이 크림색 버튼 스킨(ButtonBlank2) 위에 등급별 어두운 색으로 틴트되는
                // 구조라, 배경 스프라이트만 보고 "무조건 검은 글씨"로 고쳤던 이전 일괄 수정이
                // 여기선 틀렸다(어두운 색 위에 검은 글씨 = 안 보임) — 실제 합성된 배경색 기준으로
                // 다시 계산한다.
                entryNames[i].color = RarityColors.GetReadableTextColor(bg);
            }
        }

        private bool IsUnlocked(GuardianData data)
        {
            return (registry.defaultUnlocked != null && registry.defaultUnlocked.Contains(data))
                || GameProgress.UnlockedGuardians.Contains(data);
        }
    }
}
