#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Combat;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// GuardianUnit.prefab에 사거리 표시용 원(RangeIndicator)을 추가한다. 실제 원 크기는
    /// GuardianUnit.UpdateRangeIndicator가 배치 시점에 GuardianData.range로 매번 다시 계산하므로
    /// 여기서 넣는 sizeDelta는 자리만 잡는 임시값이다.
    ///
    /// 처음엔 꽉 찬 반투명 원(Knob.psd)이었는데 "롤 사거리 표시처럼 끝에 얇은 링만 보이면
    /// 좋겠다"는 피드백으로, 유니티 기본 스프라이트 중엔 링(도넛) 모양이 없어서 직접 텍스처를
    /// 만들어(Assets/03_Art/Sprites/UI/RangeRingSprite.png) 얇은 원형 테두리만 그린다.
    /// </summary>
    public static class GameplayVisualsBootstrap
    {
        private const string GuardianUnitPrefabPath = "Assets/05_Prefabs/GuardianUnit.prefab";
        private const string RingSpritePath = "Assets/03_Art/Sprites/UI/RangeRingSprite.png";

        [MenuItem("TinyKingdom/Add Guardian Range Indicator")]
        public static void AddGuardianRangeIndicator()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(GuardianUnitPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[GameplayVisualsBootstrap] {GuardianUnitPrefabPath}를 못 찾았습니다.");
                return;
            }

            var guardianUnit = prefabRoot.GetComponent<GuardianUnit>();
            if (guardianUnit == null)
            {
                Debug.LogError("[GameplayVisualsBootstrap] GuardianUnit 컴포넌트를 못 찾았습니다.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            Transform existing = prefabRoot.transform.Find("RangeIndicator");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("RangeIndicator", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(prefabRoot.transform, false);
                // 캐릭터 스프라이트/텍스트보다 먼저 그려져야(맨 뒤) 원 위에 캐릭터가 서 있는 것처럼 보인다.
                go.transform.SetAsFirstSibling();
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100f, 100f); // 실제 크기는 배치 시점에 range 기준으로 재계산됨

            var image = go.GetComponent<Image>();
            image.sprite = GetOrCreateRangeRingSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            // 배경(초록 잔디/갈색 흙길)이 계속 바뀌니 어디서나 무난하게 읽히는 흰색 —
            // 금색(재화)/빨강(위험)/파랑(저주) 등 이미 쓰이는 색 언어와도 안 겹침.
            // "약간 반투명" 요청으로 알파를 0.85→0.5로 낮춤.
            image.color = new Color(1f, 1f, 1f, 0.5f);
            image.raycastTarget = false;

            var so = new SerializedObject(guardianUnit);
            so.FindProperty("rangeIndicator").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, GuardianUnitPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("[GameplayVisualsBootstrap] GuardianUnit 프리팹에 사거리 표시 링 추가/갱신 완료.");
        }

        /// <summary>
        /// 유니티 기본 제공 스프라이트 중엔 "링(도넛)" 모양이 없어서(Knob.psd는 꽉 찬 원) 직접
        /// 텍스처를 그린다. 중심에서의 거리(dist)가 반지름(outerRadius)에 가까운 픽셀만 남기고
        /// 나머지는 완전 투명 — 가장자리 1.5px 정도만 부드럽게 페이드시켜 계단현상 없이 깔끔하게
        /// 보이게 한다. 한 번 만들면 파일로 저장되니 재실행해도 다시 안 만든다.
        /// </summary>
        private static Sprite GetOrCreateRangeRingSprite()
        {
            // 굵기/색 조정 요청이 계속 들어와서 캐싱하지 않고 메뉴 실행할 때마다 항상 새로
            // 굽는다(색은 Image.color 틴트만 바꾸면 되지만 굵기는 텍스처 자체를 다시 그려야 함).
            const int size = 256;
            const float outerRadius = size / 2f - 6f; // 텍스처 가장자리에 살짝 여백을 둬서 잘리지 않게
            const float ringThickness = 2.5f; // 기존 5의 절반
            const float edgeSoftness = 0.8f; // 얇아진 만큼 경계도 더 또렷하게

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float distFromRing = Mathf.Abs(dist - outerRadius);
                    float alpha = 1f - Mathf.Clamp01((distFromRing - ringThickness / 2f) / edgeSoftness);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            EnsureFolder("Assets/03_Art/Sprites/UI");
            File.WriteAllBytes(RingSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(RingSpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(RingSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
