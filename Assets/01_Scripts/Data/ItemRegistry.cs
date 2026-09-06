using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Data
{
    /// <summary>
    /// GuardianRegistry와 같은 이유 — 세이브 파일(체크포인트)엔 ItemData를 직접 못 담으니 이름
    /// 문자열로만 저장하고, 불러올 때 이 레지스트리로 실제 에셋을 되짚는다.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "WitchHour/Item Registry")]
    public class ItemRegistry : ScriptableObject
    {
        public List<ItemData> allItems = new List<ItemData>();

        public ItemData FindByAssetName(string assetName)
        {
            foreach (var item in allItems)
                if (item != null && item.name == assetName)
                    return item;
            return null;
        }
    }
}
