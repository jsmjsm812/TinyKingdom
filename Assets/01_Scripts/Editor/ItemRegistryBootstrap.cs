#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 웨이브 체크포인트("그만하기")가 저장된 아이템 이름을 다시 ItemData로 되짚으려면
    /// GuardianRegistry와 같은 레지스트리가 필요하다 — 이 메뉴로 Resources 밑에 만든다.
    /// 이미 있으면 목록만 다시 채운다(아이템을 나중에 더 추가해도 재실행하면 됨).
    /// </summary>
    public static class ItemRegistryBootstrap
    {
        private const string RegistryPath = "Assets/02_Data/Resources/ItemRegistry.asset";

        private static readonly string[] ItemPaths =
        {
            "Assets/02_Data/Items/공격력 강화.asset",
            "Assets/02_Data/Items/속공의 물약.asset",
            "Assets/02_Data/Items/축복의 유물.asset",
            "Assets/02_Data/Items/둔화의 저주.asset",
        };

        [MenuItem("TinyKingdom/Create Item Registry (Resources)")]
        public static void CreateOrUpdateItemRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(RegistryPath);
            bool isNew = registry == null;
            if (isNew) registry = ScriptableObject.CreateInstance<ItemRegistry>();

            registry.allItems.Clear();
            foreach (string path in ItemPaths)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null) registry.allItems.Add(item);
                else Debug.LogWarning($"[ItemRegistryBootstrap] 아이템 에셋을 못 찾음: {path}");
            }

            if (isNew) AssetDatabase.CreateAsset(registry, RegistryPath);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ItemRegistryBootstrap] ItemRegistry에 아이템 {registry.allItems.Count}개 등록 완료.");
        }
    }
}
#endif
