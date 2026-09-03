#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Field;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 새로 임포트한 Pixel_HUD_UI_FreeKit 에셋으로 그리드 슬롯 / 성벽 체력바 / 침입자 체력바를
    /// 다시 꾸민다. DEVNIK 2D 버튼 팩은 낱장이 아니라 한 장짜리 시트("UNSPLIT")라 Sprite Editor로
    /// 먼저 잘라야 개별 버튼을 못 꺼내 쓴다 — 이번엔 이미 낱장으로 갈라져 있는 Pixel_HUD_UI_FreeKit만
    /// 적용한다.
    ///
    /// 배경(테두리 있는 프레임)은 Image.Type.Sliced로 9-slice해서 크기가 달라져도 테두리가 안
    /// 뭉개지게 하고, 필(fill)은 Image.Type.Simple로 스프라이트만 갈아끼운다 — 체력바 갱신 로직
    /// (HudUI/InvaderUnit)이 fillAmount가 아니라 RectTransform.localScale.x로 너비를 줄이는
    /// 방식이라(예전에 fillAmount 자체가 씬에 반영이 안 되는 버그가 있어서 이렇게 바꿨었음),
    /// Sliced 필을 쓰면 테두리까지 같이 눌려서 찌그러져 보인다 — Simple로 두면 필 전체가 고르게
    /// 줄어들어(끝부분이 살짝 눌리는 정도) 훨씬 자연스럽다.
    /// </summary>
    public static class PixelHudSkinApplier
    {
        private const string KitDir = "Assets/Pixel_HUD_UI_FreeKit/Sprites/UI Elements";
        private const string BattlePath = "Assets/06_Scenes/Battle.unity";

        [MenuItem("WitchHour/Apply Pixel HUD Kit Skin")]
        public static void ApplySkin()
        {
            // 배경류는 9-slice 테두리를 지정해서 임포트, 필류는 그냥 단일 스프라이트로 임포트.
            Sprite slotBg = EnsureSprite($"{KitDir}/UI_Slot_Selected.png", new Vector4(14, 14, 14, 14));
            // UI_StatusBar_Bg.png(395x57)는 왼쪽에 금테 "캡" 장식이 x=0~61까지 걸쳐 있는데,
            // 예전 보더값(16,10,16,10)은 그 캡을 반만 보호해서 나머지 절반이 스트레치 영역에
            // 걸려 860px로 늘어날 때 뭉개진 얼룩처럼 번져 보였다(성벽 체력바가 "이상하다"는
            // 원인). 픽셀을 직접 분석해 캡 전체(왼쪽 61px)와 오른쪽 테두리(23px)를 다 보더에
            // 넣도록 고쳤다.
            Sprite wardBg = EnsureSprite($"{KitDir}/UI_StatusBar_Bg.png", new Vector4(62, 15, 24, 11));
            Sprite wardFill = EnsureSprite($"{KitDir}/UI_StatusBar_Fill_HP.png", Vector4.zero);
            Sprite invaderBg = EnsureSprite($"{KitDir}/UI_Progress_Style2_Bg.png", new Vector4(8, 5, 8, 5));
            Sprite invaderFill = EnsureSprite($"{KitDir}/UI_Progress_Style2_Fill_Red.png", Vector4.zero);

            if (slotBg == null || wardBg == null || wardFill == null || invaderBg == null || invaderFill == null)
            {
                Debug.LogError("[PixelHudSkinApplier] 스프라이트 준비에 실패해서 중단합니다.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            // 1) 그리드 슬롯 22칸 — GridManager.slotSprite를 바꾸고 RebuildGrid로 전부 다시 그림.
            var gridManagerGO = GameObject.Find("GridManager");
            if (gridManagerGO != null)
            {
                var gridManager = gridManagerGO.GetComponent<GridManager>();
                var so = new SerializedObject(gridManager);
                so.FindProperty("slotSprite").objectReferenceValue = slotBg;
                so.ApplyModifiedPropertiesWithoutUndo();
                gridManager.RebuildGrid();
                Debug.Log("[PixelHudSkinApplier] 그리드 슬롯 22칸에 UI_Slot_Selected 적용.");
            }
            else
            {
                Debug.LogWarning("[PixelHudSkinApplier] GridManager를 못 찾아 슬롯 스킨은 건너뜀.");
            }

            // 2) 성벽(HUD) 체력바.
            var wardBgGO = GameObject.Find("WardHpBarBg");
            if (wardBgGO != null)
            {
                var bgImage = wardBgGO.GetComponent<Image>();
                bgImage.sprite = wardBg;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white;

                var fillT = wardBgGO.transform.Find("WardHpBarFill");
                if (fillT != null)
                {
                    var fillImage = fillT.GetComponent<Image>();
                    fillImage.sprite = wardFill;
                    fillImage.type = Image.Type.Simple;
                    fillImage.color = Color.white;
                }
                Debug.Log("[PixelHudSkinApplier] 성벽 체력바에 UI_StatusBar 적용.");
            }
            else
            {
                Debug.LogWarning("[PixelHudSkinApplier] WardHpBarBg를 못 찾아 성벽 체력바 스킨은 건너뜀.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 3) 침입자 개별 체력바 — InvaderUnit.prefab 하나만 고치면 전 개체에 반영됨.
            const string invaderPrefabPath = "Assets/05_Prefabs/InvaderUnit.prefab";
            var contents = PrefabUtility.LoadPrefabContents(invaderPrefabPath);
            try
            {
                var hpBarBgT = contents.transform.Find("HpBarBg");
                if (hpBarBgT != null)
                {
                    var bgImage = hpBarBgT.GetComponent<Image>();
                    bgImage.sprite = invaderBg;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                    // 원본 64x10은 새 스프라이트(원본 비율 104x39)의 9-slice 테두리보다 세로가
                    // 작아서 찌그러진다 — 세로를 좀 더 키운다(SceneBootstrap.BuildInvaderHpBar의
                    // "새로 지을 때" 기본값과 맞춤).
                    var bgRect = (RectTransform)hpBarBgT;
                    bgRect.sizeDelta = new Vector2(70, 16);

                    var fillT = hpBarBgT.Find("HpBarFill");
                    if (fillT != null)
                    {
                        var fillImage = fillT.GetComponent<Image>();
                        fillImage.sprite = invaderFill;
                        fillImage.type = Image.Type.Simple;
                        fillImage.color = Color.white;
                        var fillRect = (RectTransform)fillT;
                        fillRect.anchoredPosition = new Vector2(2, 0);
                        fillRect.sizeDelta = new Vector2(66, 11);
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, invaderPrefabPath);
                    Debug.Log("[PixelHudSkinApplier] 침입자 체력바(InvaderUnit.prefab)에 UI_Progress_Style2 적용.");
                }
                else
                {
                    Debug.LogWarning("[PixelHudSkinApplier] InvaderUnit.prefab 안에서 HpBarBg를 못 찾음.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelHudSkinApplier] 완료.");
        }

        private static Sprite EnsureSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[PixelHudSkinApplier] {path}를 못 찾았습니다.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[PixelHudSkinApplier] {path} 재임포트 후 Sprite 로드 실패.");
            return sprite;
        }
    }
}
#endif
