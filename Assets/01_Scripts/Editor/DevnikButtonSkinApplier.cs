#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// DEVNIK 2D의 "UI SIMPLE PIXEL UNSPLIT.png"는 낱장이 아니라 버튼 여러 개가 한 시트에
    /// 모여있다("UNSPLIT") — 파이썬(Pillow+scipy)으로 알파 채널 연결 성분을 분석해서 각 버튼의
    /// 픽셀 좌표를 정확히 뽑아낸 뒤, 그 좌표로 Multiple Sprite 모드로 잘라 이름을 붙였다.
    /// 그중 텍스트/아이콘이 안 그려진 "빈 버튼" 모양만 골라 우리 게임의 버튼들(리롤, 설정,
    /// 도감, 닫기, 배속 등)에 입힌다 — 텍스트가 이미 그려진 것("Play","Levels" 등)은 한글
    /// 라벨을 못 얹으니 안 씀.
    /// </summary>
    public static class DevnikButtonSkinApplier
    {
        private const string SheetPath = "Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/UI SIMPLE PIXEL UNSPLIT.png";

        // (이름, x, y, width, height) — 유니티 스프라이트 좌표계(원점 좌하단) 기준.
        // 파이썬으로 원본 PNG(1480x1444)의 알파 연결 성분을 분석해서 뽑은 정확한 픽셀 좌표.
        private static readonly (string name, int x, int y, int w, int h)[] Sprites =
        {
            ("IconPlaySmall", 45, 1281, 126, 130),
            ("IconPauseSmall", 221, 1281, 126, 130),
            ("IconSettingsSmall", 401, 1281, 126, 130),
            ("ButtonPlayText", 653, 1265, 366, 130),
            ("ButtonBlank1", 1077, 1265, 374, 134),
            ("IconBackSmall", 45, 1117, 126, 130),
            ("IconSpeakerOnSmall", 221, 1117, 126, 130),
            ("IconSpeakerOffSmall", 401, 1117, 126, 130),
            ("ButtonLevelsText", 657, 1121, 366, 130),
            ("ButtonBlank2", 1085, 1113, 366, 130),
            ("ButtonSettingsText", 653, 973, 366, 130),
            ("ButtonBlank3Beveled", 1085, 969, 362, 122),
            ("IconPlayLarge", 41, 865, 134, 134),
            ("IconPauseLarge", 217, 865, 134, 134),
            ("IconSettingsLarge", 397, 865, 134, 134),
            ("ButtonExitText", 653, 821, 366, 130),
            ("ButtonPillGreen", 1133, 829, 270, 122),
            ("IconBackLarge", 41, 701, 134, 134),
            ("IconSpeakerOnLarge", 217, 701, 134, 134),
            ("IconSpeakerOffLarge", 397, 701, 134, 134),
            ("PanelMenuTab", 837, 49, 606, 654),
            ("PanelBlankSquareLarge", 41, 257, 374, 386),
            ("ArrowRight", 577, 489, 154, 134),
            ("ArrowLeft", 565, 337, 154, 134),
            ("ButtonBlankSmallSquare", 61, 61, 134, 134),
            ("PillVerticalSmall", 233, 53, 150, 146),
            ("BarBlank1", 425, 169, 386, 42),
            ("BarBlank2Beveled", 425, 105, 382, 42),
            ("HandleTiny", 641, 17, 46, 74),
        };

        // 우리 버튼들은 스프라이트 원본 크기(365x130 등)와 전혀 다른 크기(85x60, 120x195 등)로
        // Sliced 스트레치가 걸린다 — border를 0으로 두면 둥근 모서리/베벨까지 같이 눌려 찌그러진다.
        // 실제로 Sliced로 쓸 것들만 골라 9-slice 보더를 지정한다(나머지는 원본 크기 그대로 쓸
        // 아이콘/텍스트버튼이라 필요 없음).
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            ["ButtonBlank1"] = new Vector4(18, 18, 18, 18),
            ["ButtonBlank2"] = new Vector4(18, 18, 18, 18),
            ["ButtonBlank3Beveled"] = new Vector4(18, 18, 18, 18),
            ["ButtonPillGreen"] = new Vector4(40, 40, 40, 40),
            ["ButtonBlankSmallSquare"] = new Vector4(16, 16, 16, 16),
            ["PanelBlankSquareLarge"] = new Vector4(24, 24, 24, 24),
        };

        [MenuItem("WitchHour/1) Slice DEVNIK Button Sheet")]
        public static void SliceSheet()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            if (importer == null)
            {
                Debug.LogError($"[DevnikButtonSkinApplier] {SheetPath}를 못 찾았습니다.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var metas = new List<SpriteMetaData>();
            foreach (var s in Sprites)
            {
                Borders.TryGetValue(s.name, out var border); // 없으면 기본값 Vector4.zero
                metas.Add(new SpriteMetaData
                {
                    name = s.name,
                    rect = new Rect(s.x, s.y, s.w, s.h),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = border,
                });
            }
#pragma warning disable CS0618 // spritesheet는 구식 API지만 단순 사각형 슬라이싱엔 여전히 잘 동작함
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[DevnikButtonSkinApplier] {Sprites.Length}개 스프라이트로 잘랐습니다.");
        }

        private static Dictionary<string, Sprite> _cache;

        private static Sprite Get(string name)
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, Sprite>();
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
                {
                    if (obj is Sprite sp) _cache[sp.name] = sp;
                }
            }
            _cache.TryGetValue(name, out var sprite);
            return sprite;
        }

        [MenuItem("WitchHour/2) Apply DEVNIK Button Skin To All Buttons")]
        public static void ApplySkin()
        {
            Sprite blankButton = Get("ButtonBlank2");
            Sprite pillGreen = Get("ButtonPillGreen");
            if (blankButton == null || pillGreen == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'WitchHour > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            // "긍정/행동" 버튼(리롤, 배속 토글)은 초록 필, 나머지 일반 버튼은 회색 테두리 버튼.
            var pillNames = new HashSet<string> { "RerollButton", "SpeedToggleButton" };

            // ShopSlot: 등급별 배경색을 SetData가 매 프레임 새로 칠하는 로직(RarityColors)과
            // 충돌해서 제외. TapAnywhereButton: 타이틀 화면 전체를 덮는 완전 투명 히트박스라
            // 스킨을 씌우면 화면이 막혀버림 — 역시 제외.
            var excludeNames = new HashSet<string> { "ShopSlot", "TapAnywhereButton" };

            int sceneCount = 0, buttonCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/06_Scenes" }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (scenePath.EndsWith("CharacterLineup.unity") || scenePath.EndsWith("Boot.unity"))
                    continue; // 디버그/부팅 전용 씬은 버튼 스킨 대상 아님
                if (!AssetDatabase.IsOpenForEdit(scenePath))
                {
                    Debug.LogWarning($"[DevnikButtonSkinApplier] {scenePath}는 편집 불가능해서 건너뜁니다.");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int changed = 0;
                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    foreach (var button in rootGO.GetComponentsInChildren<Button>(true))
                    {
                        var image = button.targetGraphic as Image;
                        if (image == null) image = button.GetComponent<Image>();
                        if (image == null) continue;

                        if (excludeNames.Contains(button.gameObject.name)) continue;

                        image.sprite = pillNames.Contains(button.gameObject.name) ? pillGreen : blankButton;
                        image.type = Image.Type.Sliced;
                        image.color = Color.white;
                        changed++;
                    }
                }

                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                sceneCount++;
                buttonCount += changed;
                Debug.Log($"[DevnikButtonSkinApplier] {scenePath}: 버튼 {changed}개 스킨 적용.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DevnikButtonSkinApplier] 완료 — 씬 {sceneCount}개, 버튼 {buttonCount}개.");
        }
    }
}
#endif
