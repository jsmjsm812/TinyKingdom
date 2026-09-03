using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Data
{
    /// <summary>
    /// 세이브 파일엔 GuardianData 에셋을 직접 참조할 수 없어(JSON은 오브젝트 참조를 못 담음)
    /// 이름 문자열로만 저장하는데, 불러올 때 그 이름을 다시 실제 에셋으로 되짚어야 한다.
    /// Resources 밖의 에셋은 빌드된 플레이어에서 AssetDatabase(에디터 전용)로 못 찾으므로,
    /// 이 레지스트리 하나만 Resources에 두고 SaveSystem이 여기서 이름→에셋을 찾는다.
    /// </summary>
    [CreateAssetMenu(fileName = "GuardianRegistry", menuName = "WitchHour/Guardian Registry")]
    public class GuardianRegistry : ScriptableObject
    {
        public List<GuardianData> allGuardians = new List<GuardianData>();

        [Tooltip("처음부터 해금된 수호자 — ShopRosterUIBootstrap이 SummonPool에 심어주는 목록과 " +
                 "같은 4종이어야 한다(둘 다 바뀌면 같이 바꿀 것). 도감이 SummonPool 없이도 " +
                 "이 목록만으로 해금 여부를 판단할 수 있게 여기에도 둔다.")]
        public List<GuardianData> defaultUnlocked = new List<GuardianData>();

        /// <summary>키는 한글 표시명이 아니라 에셋 파일명(Lumi.asset -> "Lumi")을 쓴다 —
        /// 표시명은 나중에 기획 변경으로 바뀔 수 있지만 파일명은 코드 곳곳(SceneBootstrap 등)에서
        /// 이미 고정 식별자로 쓰이고 있어서 더 안전하다.</summary>
        public GuardianData FindByAssetName(string assetName)
        {
            foreach (var guardian in allGuardians)
                if (guardian != null && guardian.name == assetName)
                    return guardian;
            return null;
        }
    }
}
