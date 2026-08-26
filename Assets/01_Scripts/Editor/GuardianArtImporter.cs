#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// Assets/03_Art/Sprites/Guardians/에 넣어둔 초상화 PNG를 Sprite로 임포트 설정하고
    /// 같은 이름의 GuardianData.portrait 필드에 자동으로 연결한다.
    /// </summary>
    public static class GuardianArtImporter
    {
        private static readonly string[] GuardianNames =
        {
            "Lumi", "Noa", "Mira", "Dori", "Sera", "Irene", "Chloe", "Astel", "Selene", "Ophelia"
        };

        private const string SpriteDir = "Assets/03_Art/Sprites/Guardians";
        private const string DataDir = "Assets/02_Data/Guardians";

        [MenuItem("WitchHour/Import Guardian Portraits")]
        public static void ImportGuardianPortraits()
        {
            int wired = 0;
            foreach (string name in GuardianNames)
            {
                string texPath = $"{SpriteDir}/{name}.png";

                if (AssetImporter.GetAtPath(texPath) is not TextureImporter importer)
                {
                    Debug.LogWarning($"[GuardianArtImporter] 텍스처 없음: {texPath}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
                var guardianData = AssetDatabase.LoadAssetAtPath<GuardianData>($"{DataDir}/{name}.asset");

                if (sprite == null || guardianData == null)
                {
                    Debug.LogWarning($"[GuardianArtImporter] 연결 실패: {name} (sprite={sprite != null}, data={guardianData != null})");
                    continue;
                }

                var so = new SerializedObject(guardianData);
                so.FindProperty("portrait").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                wired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GuardianArtImporter] 수호자 초상화 {wired}/{GuardianNames.Length}종 임포트 및 연결 완료.");
        }
    }
}
#endif
