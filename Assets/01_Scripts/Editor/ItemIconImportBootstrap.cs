#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 사용자가 직접 만든 아이템 아이콘 PNG 4장(Assets/03_Art/Sprites/Items)을 픽셀아트에 맞는
    /// 임포트 설정(Point 필터, 무압축)으로 세팅하고, 각 ItemData 에셋의 icon 필드에 연결한다.
    /// 지금까지는 원형 도형에 색만 입힌 자리끼움(iconTint)이었는데, 실제 아트가 생겼으니
    /// iconTint는 흰색(원본 색 그대로)으로 되돌린다.
    /// </summary>
    public static class ItemIconImportBootstrap
    {
        private const string IconDir = "Assets/03_Art/Sprites/Items";
        private const string ItemDataDir = "Assets/02_Data/Items";
        private static readonly string[] ItemNames = { "공격력 강화", "속공의 물약", "축복의 유물", "둔화의 저주" };

        [MenuItem("TinyKingdom/Apply Item Icon Art")]
        public static void ApplyItemIconArt()
        {
            int applied = 0;
            foreach (var name in ItemNames)
            {
                string pngPath = $"{IconDir}/{name}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
                if (importer == null)
                {
                    Debug.LogWarning($"[ItemIconImportBootstrap] {pngPath}를 못 찾았습니다.");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point; // 픽셀아트가 흐려지지 않게
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                var itemData = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemDataDir}/{name}.asset");
                if (sprite == null || itemData == null)
                {
                    Debug.LogWarning($"[ItemIconImportBootstrap] {name}: 스프라이트 또는 ItemData를 못 찾아 건너뜁니다.");
                    continue;
                }

                itemData.icon = sprite;
                itemData.iconTint = Color.white; // 이제 실제 아트라 색을 곱연산으로 덧씌우지 않는다
                EditorUtility.SetDirty(itemData);
                applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemIconImportBootstrap] 아이템 아이콘 {applied}/{ItemNames.Length}개 적용 완료. " +
                      "이제 'TinyKingdom > Build Item Shop'과 'TinyKingdom > Add Lobby Item Shop'을 다시 실행해서 " +
                      "카드에 반영하세요.");
        }
    }
}
#endif
