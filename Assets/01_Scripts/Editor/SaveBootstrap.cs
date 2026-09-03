using System.Linq;
using UnityEditor;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// SaveSystem이 런타임에 이름→GuardianData를 되짚을 수 있도록 GuardianRegistry 에셋을
    /// Resources 폴더에 만들고 02_Data/Guardians의 전체 10종을 채워 넣는다.
    /// 새 수호자를 추가한 뒤에는 이 메뉴를 한 번 더 실행해야 레지스트리가 최신 상태가 된다.
    /// </summary>
    public static class SaveBootstrap
    {
        private const string GuardiansDir = "Assets/02_Data/Guardians";
        private const string ResourcesDir = "Assets/02_Data/Resources";
        private const string RegistryPath = ResourcesDir + "/GuardianRegistry.asset";

        [MenuItem("WitchHour/Build Guardian Registry (Save System)")]
        public static void BuildGuardianRegistry()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
                AssetDatabase.CreateFolder("Assets/02_Data", "Resources");

            var registry = AssetDatabase.LoadAssetAtPath<GuardianRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<GuardianRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:GuardianData", new[] { GuardiansDir });
            registry.allGuardians = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<GuardianData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(g => g != null)
                .OrderBy(g => g.name)
                .ToList();

            // ShopRosterUIBootstrap.BuildShopAndRosterUI가 SummonPool에 심어주는 기본 해금
            // 목록과 반드시 같아야 한다 — 둘 중 하나만 바뀌면 상점이랑 도감이 서로 다른 얘기를 하게 됨.
            string[] defaultUnlockedNames = { "Lumi", "Noa", "Mira", "Dori" };
            registry.defaultUnlocked = defaultUnlockedNames
                .Select(name => registry.allGuardians.Find(g => g.name == name))
                .Where(g => g != null)
                .ToList();

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SaveBootstrap] GuardianRegistry에 수호자 {registry.allGuardians.Count}종 등록, " +
                      $"기본 해금 {registry.defaultUnlocked.Count}종 반영 ({RegistryPath}).");
        }
    }
}
