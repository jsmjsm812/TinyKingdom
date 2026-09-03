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

        private static readonly Color LockedBg = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color LockedPortrait = new Color(0.1f, 0.1f, 0.1f);

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
            AudioManager.Instance?.PlayButtonClick();
        }

        public void Close()
        {
            panel.SetActive(false);
            AudioManager.Instance?.PlayButtonClick();
        }

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
                entryBackgrounds[i].color = unlocked ? RarityColors.GetCardColor(data.rarity) : LockedBg;
            }
        }

        private bool IsUnlocked(GuardianData data)
        {
            return (registry.defaultUnlocked != null && registry.defaultUnlocked.Contains(data))
                || GameProgress.UnlockedGuardians.Contains(data);
        }
    }
}
