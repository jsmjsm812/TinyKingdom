#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.UI;

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

        [MenuItem("TinyKingdom/1) Slice DEVNIK Button Sheet")]
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

        // 로비 하단 도감/설정 버튼에만 픽셀 버튼 스킨을 입힌다 — 구역 카드(ZoneCard_1/2/3)는
        // 사용자가 씬 뷰에서 직접 다듬은 상태라 절대 건드리면 안 되므로, 전체 씬을 훑는
        // ApplySkin()이 아니라 이름을 콕 집어서만 바꾸는 좁은 범위의 메뉴로 따로 둔다.
        [MenuItem("TinyKingdom/Apply DEVNIK Skin To Lobby Buttons Only")]
        public static void ApplySkinToLobbyButtonsOnly()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            int changed = 0;
            foreach (var buttonName in new[] { "GuardianCodexButton", "SettingsButton", "TutorialButton" })
            {
                var go = GameObject.Find(buttonName);
                if (go == null)
                {
                    continue; // TutorialButton은 아직 안 만들었을 수도 있어서 경고 없이 건너뜀
                }
                var image = go.GetComponent<Image>();
                if (image == null) continue;

                image.sprite = blankButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                var existingShadow = go.GetComponent<Shadow>();
                if (existingShadow != null) Object.DestroyImmediate(existingShadow); // 픽셀 버튼 자체에 이미 입체감이 있어 그림자 중복 불필요
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[DevnikButtonSkinApplier] 로비 하단 버튼 {changed}개에만 픽셀 스킨 적용 완료(구역 카드는 안 건드림).");
        }

        // 구역 카드(ZoneCard_1/2/3) 배경만 픽셀 버튼 스킨으로 바꾼다 — 사용자가 요청한 경우에만
        // 실행할 것. 위치/크기/텍스트 내용·색은 전혀 건드리지 않고 패널 이미지만 교체한다.
        // 카드가 720x170으로 넓적해서 9-slice 보더(18,18,18,18) 스프라이트가 늘어나도 안 찌그러짐.
        [MenuItem("TinyKingdom/Apply DEVNIK Skin To Zone Cards")]
        public static void ApplySkinToZoneCards()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            int changed = 0;
            foreach (var cardName in new[] { "ZoneCard_1", "ZoneCard_2", "ZoneCard_3" })
            {
                var go = GameObject.Find(cardName);
                if (go == null)
                {
                    Debug.LogWarning($"[DevnikButtonSkinApplier] {cardName}을 못 찾았습니다.");
                    continue;
                }
                var image = go.GetComponent<Image>();
                if (image == null) continue;

                image.sprite = blankButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                var existingShadow = go.GetComponent<Shadow>();
                if (existingShadow != null) Object.DestroyImmediate(existingShadow); // 픽셀 버튼 자체 테두리로 이미 입체감이 있음
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[DevnikButtonSkinApplier] 구역 카드 {changed}개 배경에 픽셀 스킨 적용 완료(위치/텍스트는 그대로 유지).");
        }

        // "카드"류 UI 전부(로비의 구역 카드·용사 도감 칸, 배틀의 상점 슬롯)를 픽셀 버튼 스킨으로
        // 통일한다. ZoneCard는 위 ApplySkinToZoneCards()를 그대로 재사용하고, 도감 칸은
        // GuardianCodexController가 들고 있는 entryBackgrounds 목록을 그대로 따라가서(이름 검색
        // 아님 — 10칸 전부 정확히 잡힘), 상점 슬롯은 씬이 아니라 프리팹(ShopSlot.prefab)을
        // 직접 고쳐서 배틀 씬에 인스턴스가 몇 개든 한 번에 반영되게 한다.
        private const string ShopSlotPrefabPath = "Assets/05_Prefabs/UI/ShopSlot.prefab";

        [MenuItem("TinyKingdom/Apply DEVNIK Skin To All Card UI")]
        public static void ApplySkinToAllCardUI()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            ApplySkinToZoneCards();
            ApplySkinToCodexEntries(blankButton);
            ApplySkinToShopSlotPrefab(blankButton);
            ApplySkinToItemSlotPrefabs(blankButton);

            Debug.Log("[DevnikButtonSkinApplier] 구역 카드 + 도감 칸 + 상점/아이템 슬롯 프리팹까지 카드류 UI 전부 픽셀 스킨 적용 완료.");
        }

        // 아이템 상점 카드(배틀 탭용 ItemSlot, 로비 팝업용 ItemSlotLarge) — 둘 다 프리팹이라
        // 한 번만 고치면 씬에 이미 배치된 인스턴스에도 자동 반영된다. 아이콘(원형)과 텍스트는
        // 안 건드리고 카드 배경 이미지만 픽셀 버튼 스킨으로 바꾼다.
        private const string ItemSlotPrefabPath = "Assets/05_Prefabs/UI/ItemSlot.prefab";
        private const string ItemSlotLargePrefabPath = "Assets/05_Prefabs/UI/ItemSlotLarge.prefab";

        [MenuItem("TinyKingdom/Apply DEVNIK Skin To Item Cards")]
        public static void ApplySkinToItemSlotPrefabsMenu()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }
            ApplySkinToItemSlotPrefabs(blankButton);
        }

        private static void ApplySkinToItemSlotPrefabs(Sprite blankButton)
        {
            foreach (var path in new[] { ItemSlotPrefabPath, ItemSlotLargePrefabPath })
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null)
                {
                    Debug.LogWarning($"[DevnikButtonSkinApplier] {path}를 못 찾아 건너뜁니다.");
                    continue;
                }

                var image = prefabRoot.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = blankButton;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }

                // 배경이 어두운 색일 때 쓰려고 흰색/연회색으로 잡아뒀던 이름·설명 글자색이
                // ButtonBlank2(크림색 계열)로 바뀌면 거의 안 보이게 된다 — 다른 카드류(구역
                // 카드 등)와 같은 이유로 여기서도 진한 색으로 다시 칠한다.
                var view = prefabRoot.GetComponent<ItemSlotView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    if (so.FindProperty("nameText").objectReferenceValue is Text nameText)
                        nameText.color = Color.black;
                    if (so.FindProperty("descText").objectReferenceValue is Text descText)
                        descText.color = new Color(0.25f, 0.22f, 0.18f);
                    if (so.FindProperty("costText").objectReferenceValue is Text costText)
                        costText.color = new Color(0.55f, 0.38f, 0.05f);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            Debug.Log("[DevnikButtonSkinApplier] 아이템 카드(배틀/로비) 프리팹에 픽셀 스킨 + 글자색 보정 적용 완료.");
        }

        private static void ApplySkinToCodexEntries(Sprite blankButton)
        {
            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            var controller = Object.FindObjectOfType<WitchHour.UI.GuardianCodexController>(true);
            if (controller == null)
            {
                Debug.LogWarning("[DevnikButtonSkinApplier] GuardianCodexController를 못 찾아 도감 칸 스킨을 건너뜁니다.");
                return;
            }

            var so = new SerializedObject(controller);
            var backgrounds = so.FindProperty("entryBackgrounds");
            int changed = 0;
            for (int i = 0; i < backgrounds.arraySize; i++)
            {
                var image = backgrounds.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                if (image == null) continue;
                image.sprite = blankButton;
                image.type = Image.Type.Sliced;
                // 색은 그대로 둔다 — RarityColors가 매 프레임 등급별 색을 칠하는 로직과 별개로
                // 스프라이트(테두리 모양)만 바뀌는 것이라 서로 안 부딪힌다.
                changed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DevnikButtonSkinApplier] 도감 칸 {changed}개에 픽셀 스킨 적용 완료.");
        }

        private static void ApplySkinToShopSlotPrefab(Sprite blankButton)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(ShopSlotPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[DevnikButtonSkinApplier] {ShopSlotPrefabPath}를 못 찾아 상점 슬롯 스킨을 건너뜁니다.");
                return;
            }

            var image = prefabRoot.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = blankButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ShopSlotPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("[DevnikButtonSkinApplier] ShopSlot 프리팹에 픽셀 스킨 적용 완료(배틀 씬 전체 인스턴스에 자동 반영).");
        }

        [MenuItem("TinyKingdom/2) Apply DEVNIK Button Skin To All Buttons")]
        public static void ApplySkin()
        {
            Sprite blankButton = Get("ButtonBlank2");
            Sprite pillGreen = Get("ButtonPillGreen");
            if (blankButton == null || pillGreen == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            // "긍정/행동" 버튼(리롤, 배속 토글)은 초록 필, 나머지 일반 버튼은 회색 테두리 버튼.
            var pillNames = new HashSet<string> { "RerollButton", "SpeedToggleButton" };

            // ShopSlot: 등급별 배경색을 SetData가 매 프레임 새로 칠하는 로직(RarityColors)과
            // 충돌해서 제외. TapAnywhereButton: 타이틀 화면 전체를 덮는 완전 투명 히트박스라
            // 스킨을 씌우면 화면이 막혀버림 — 역시 제외. ZoneCard_1/2/3: 사용자가 씬 뷰에서 직접
            // 다듬은 로비 구역 카드라 이 전체 스캔 메뉴가 절대 손대면 안 됨(별도 스킨을 원하면
            // ApplySkinToZoneCards()를 명시적으로 실행할 것).
            var excludeNames = new HashSet<string>
            {
                "ShopSlot", "TapAnywhereButton", "ZoneCard_1", "ZoneCard_2", "ZoneCard_3"
            };

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

        // 위 ApplySkin()은 Button 컴포넌트가 달린 것만 훑는다 — 설정/튜토리얼/도감 상세/아이템
        // 상점 팝업의 "Card" 배경처럼 Button 없이 순수 Image만 있는 패널은 그 스캔에 안 걸려서
        // 여전히 유니티 기본 매끈한 UISprite/Background.psd로 남아있었다("모든 UI를 도트
        // 디자인으로" 요청 — 버튼만 바꾸면 팝업 배경이 혼자 붕 뜬 느낌이 남는다). Background.psd
        // 레퍼런스(참조 동일성)로 찾아서 같은 픽셀 패널 스프라이트로 바꾼다.
        [MenuItem("TinyKingdom/3) Apply DEVNIK Skin To All Panel Backgrounds")]
        public static void ApplySkinToAllPanelBackgrounds()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            Sprite builtinBackground = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            int sceneCount = 0, panelCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/06_Scenes" }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (scenePath.EndsWith("CharacterLineup.unity") || scenePath.EndsWith("Boot.unity"))
                    continue;
                if (!AssetDatabase.IsOpenForEdit(scenePath))
                {
                    Debug.LogWarning($"[DevnikButtonSkinApplier] {scenePath}는 편집 불가능해서 건너뜁니다.");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int changed = 0;
                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    foreach (var image in rootGO.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite != builtinBackground) continue;
                        // Button이 달린 이미지(닫기 버튼 등)는 위 ApplySkin()이 이미 담당 — 여기서
                        // 또 건드리면 pillNames/excludeNames 판단을 무시하고 덮어쓰게 되어 제외.
                        if (image.GetComponent<Button>() != null) continue;

                        image.sprite = blankButton;
                        image.type = Image.Type.Sliced;
                        changed++;
                    }
                }

                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                sceneCount++;
                panelCount += changed;
                if (changed > 0)
                    Debug.Log($"[DevnikButtonSkinApplier] {scenePath}: 패널 배경 {changed}개 스킨 적용.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DevnikButtonSkinApplier] 완료 — 씬 {sceneCount}개, 패널 배경 {panelCount}개.");
        }

        // 버튼(ApplySkin)·패널 배경(ApplySkinToAllPanelBackgrounds) 스킨을 입힐 때 아이템 카드처럼
        // 일부 프리팹은 글자색을 같이 진하게 고쳤지만, "CloseButtonLabel"/"Label"/"Name" 같은
        // 범용 텍스트들은 그때 놓쳐서 여전히 흰색 그대로 남아있었다 — 크림색 ButtonBlank2 배경
        // 위에 흰 글씨라 거의 안 보임("패널에 텍스트 글씨 흰색이잖아 검은색으로 바꿔줘" 피드백).
        // ButtonBlank2를 쓰는 Image를 찾아 그 자식 트리 안에서 지금 순백색(거의 흰색)인 Text만
        // 골라 검은색으로 바꾼다 — 이미 의도적으로 다른 색(금색 RarityText, 초록 StateText 등)을
        // 칠해둔 텍스트는 흰색이 아니므로 안 건드린다.
        [MenuItem("TinyKingdom/4) Fix White Text On Cream Panels")]
        public static void FixWhiteTextOnCreamPanels()
        {
            Sprite blankButton = Get("ButtonBlank2");
            if (blankButton == null)
            {
                Debug.LogError("[DevnikButtonSkinApplier] 먼저 'TinyKingdom > 1) Slice DEVNIK Button Sheet'를 실행하세요.");
                return;
            }

            int sceneCount = 0, textCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/06_Scenes" }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (scenePath.EndsWith("CharacterLineup.unity") || scenePath.EndsWith("Boot.unity"))
                    continue;
                if (!AssetDatabase.IsOpenForEdit(scenePath))
                {
                    Debug.LogWarning($"[DevnikButtonSkinApplier] {scenePath}는 편집 불가능해서 건너뜁니다.");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int changed = 0;
                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    foreach (var image in rootGO.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite != blankButton) continue;

                        foreach (var text in image.GetComponentsInChildren<Text>(true))
                        {
                            if (!IsNearWhite(text.color)) continue;
                            text.color = Color.black;
                            changed++;
                        }
                    }
                }

                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[DevnikButtonSkinApplier] {scenePath}: 흰 글씨 {changed}개를 검은색으로 고침.");
                }
                sceneCount++;
                textCount += changed;
            }

            // 프리팹(ItemSlot 등)은 씬이 아니라서 위 루프에 안 걸린다 — 필요하면 여기에 추가.
            Debug.Log($"[DevnikButtonSkinApplier] 완료 — 씬 {sceneCount}개, 텍스트 {textCount}개 검은색으로 변경.");
        }

        private static bool IsNearWhite(Color c) => c.r > 0.9f && c.g > 0.9f && c.b > 0.9f && c.a > 0.5f;
    }
}
#endif
