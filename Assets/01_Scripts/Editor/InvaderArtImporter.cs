#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// Assets/03_Art/Sprites/Invaders/에 넣어둔 PNG를 Sprite로 임포트 설정하고
    /// 같은 이름의 InvaderData.sprite 필드에 자동으로 연결한다.
    /// </summary>
    public static class InvaderArtImporter
    {
        private static readonly string[] InvaderNames =
        {
            "WarpedCrow", "WarpedRat", "WarpedSnake", "WarpedBoar",
            "RabidWolf", "PrimordialHawk", "PrimordialBear", "PrimordialTiger"
        };

        private const string SpriteDir = "Assets/03_Art/Sprites/Invaders";
        private const string DataDir = "Assets/02_Data/Invaders";

        [MenuItem("TinyKingdom/Import Invader Portraits")]
        public static void ImportInvaderPortraits()
        {
            int wired = 0;
            foreach (string name in InvaderNames)
            {
                string texPath = $"{SpriteDir}/{name}.png";

                if (AssetImporter.GetAtPath(texPath) is not TextureImporter importer)
                {
                    Debug.LogWarning($"[InvaderArtImporter] 텍스처 없음: {texPath}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
                var invaderData = AssetDatabase.LoadAssetAtPath<InvaderData>($"{DataDir}/{name}.asset");

                if (sprite == null || invaderData == null)
                {
                    Debug.LogWarning($"[InvaderArtImporter] 연결 실패: {name} (sprite={sprite != null}, data={invaderData != null})");
                    continue;
                }

                var so = new SerializedObject(invaderData);
                so.FindProperty("sprite").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                wired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[InvaderArtImporter] 침입자 스프라이트 {wired}/{InvaderNames.Length}종 임포트 및 연결 완료.");
        }
    }
}
#endif
