#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WitchHour.Combat;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 1주차 전투 프로토타입을 씬에 자동으로 배선하는 에디터 전용 스크립트.
    /// .unity 파일을 손으로 고쳐 쓰는 대신 Unity API로 오브젝트를 만들어서
    /// 항상 유효한 씬 파일이 나오도록 한다. Player 빌드에는 포함되지 않음(Editor 폴더).
    /// </summary>
    public static class SceneBootstrap
    {
        private const string ScenePath = "Assets/06_Scenes/SampleScene.unity";
        private const string PrefabDir = "Assets/05_Prefabs";

        /// <summary>
        /// InvaderUnit.prefab만 직접 갱신한다(씬 오픈 불필요 — 프리팹 에셋만 건드림).
        /// "Build Battle Scene"은 씬 구조 재편(RestructureScenes) 이후로 SampleScene.unity 경로가
        /// 더 이상 존재하지 않아 지금은 못 쓰므로, 체력바 같은 프리팹 전용 패치는 이걸로 따로 뺐다.
        /// </summary>
        [MenuItem("TinyKingdom/Patch Invader Prefab (HP Bar)")]
        public static void PatchInvaderPrefab()
        {
            BuildInvaderPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBootstrap] InvaderUnit 프리팹 갱신 완료 — 체력바가 없었다면 추가됨.");
        }

        [MenuItem("TinyKingdom/Build Battle Scene (Week 1)")]
        public static void BuildBattleScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RectTransform battlefieldRoot = BuildCanvas();
            InvaderUnit invaderPrefab = BuildInvaderPrefab();
            BuildBattleManager(battlefieldRoot, invaderPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[SceneBootstrap] 전투 씬 배선 완료: Canvas/BattlefieldRoot/InvaderUnit 프리팹/BattleManager 생성 및 참조 연결됨.");
        }

        /// <summary>
        /// 씬 하나짜리 프로토타입을 Boot -> Title -> Home -> Battle 4씬 구조로 재편한다.
        /// 실제 모바일 게임처럼 "타이틀(탭하여 계속) -> 홈(게임 시작) -> 전투" 흐름을 갖추기 위함.
        /// 이미 열려 있는 에디터 세션에서 메뉴로 실행해야 한다 — 배치모드는 씬을 새로
        /// 만들 때 안전한 "저장할까요?" 대화상자를 띄울 수 없어서 이 작업엔 부적합하다.
        /// </summary>
        [MenuItem("TinyKingdom/Restructure Into Boot-Title-Home-Battle Scenes")]
        public static void RestructureScenes()
        {
            const string oldPath = "Assets/06_Scenes/SampleScene.unity";
            const string battlePath = "Assets/06_Scenes/Battle.unity";

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(oldPath) != null)
            {
                string error = AssetDatabase.RenameAsset(oldPath, "Battle");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning("[SceneBootstrap] Battle 씬 이름 변경 실패: " + error);
            }

            FixBattleSceneInputModule();

            string titlePath = BuildTitleScene();
            string homePath = BuildHomeScene();
            string bootPath = BuildBootScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootPath, true),
                new EditorBuildSettingsScene(titlePath, true),
                new EditorBuildSettingsScene(homePath, true),
                new EditorBuildSettingsScene(battlePath, true),
            };
            AssetDatabase.SaveAssets();

            SetMobileGameView();

            Debug.Log("[SceneBootstrap] Boot -> Title -> Home -> Battle 씬 구조 완성, Build Settings 갱신, 모바일 Game 뷰 적용 완료. " +
                      "지금 열려있는 씬은 Boot입니다 — Battle 작업을 이어가려면 Project 창에서 Battle.unity를 다시 여세요.");
        }

        /// <summary>
        /// Battle 씬은 GridManager/BattleManager가 이미 배선돼 있어서 통째로 재생성할 수 없다.
        /// EventSystem의 입력 모듈만 새 Input System용으로 교체하고, 카메라가 없으면 하나 추가한다.
        /// </summary>
        [MenuItem("TinyKingdom/Fix Battle Scene Input Module")]
        public static void FixBattleSceneInputModule()
        {
            const string path = "Assets/06_Scenes/Battle.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var standalone = UnityEngine.Object.FindAnyObjectByType<StandaloneInputModule>();
            if (standalone != null)
            {
                var go = standalone.gameObject;
                UnityEngine.Object.DestroyImmediate(standalone);
                go.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[SceneBootstrap] Battle 씬 EventSystem을 InputSystemUIInputModule로 교체했습니다.");
            }

            if (UnityEngine.Object.FindAnyObjectByType<Camera>() == null)
            {
                var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGO.tag = "MainCamera";
                var cam = camGO.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static string BuildBootScene()
        {
            const string path = "Assets/06_Scenes/Boot.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("BootLoader", typeof(BootLoader));

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static (RectTransform canvasRoot, Font font) BuildUiSceneShell()
        {
            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // 프로젝트가 activeInputHandler=1(새 Input System 전용)이라 구형 StandaloneInputModule은
            // UnityEngine.Input 호출에서 예외를 던져 클릭이 조용히 씹힌다 — InputSystemUIInputModule을 써야 함.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;

            return (canvasGO.GetComponent<RectTransform>(), GameFonts.Main);
        }

        private static Text CreateLabel(Transform parent, string name, string text, int fontSize, Font font,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.font = font;
            label.color = Color.white;
            return label;
        }

        private static string BuildTitleScene()
        {
            const string path = "Assets/06_Scenes/Title.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var (canvasRoot, font) = BuildUiSceneShell();

            CreateLabel(canvasRoot, "TitleText", "작은 왕국", 110, font, new Vector2(0, 100), new Vector2(900, 220));
            CreateLabel(canvasRoot, "TapPrompt", "화면을 탭하여 계속", 40, font, new Vector2(0, -500), new Vector2(700, 100));

            var controllerGO = new GameObject("TitleController", typeof(TitleController));
            var titleController = controllerGO.GetComponent<TitleController>();

            // 화면 전체를 덮는 투명 버튼 — "탭 애니웨어" 진입은 별도 UI 없이 이거 하나로 처리.
            var tapAreaGO = new GameObject("TapAnywhereButton", typeof(RectTransform), typeof(Image), typeof(Button));
            tapAreaGO.transform.SetParent(canvasRoot, false);
            var tapRect = tapAreaGO.GetComponent<RectTransform>();
            tapRect.anchorMin = Vector2.zero;
            tapRect.anchorMax = Vector2.one;
            tapRect.offsetMin = Vector2.zero;
            tapRect.offsetMax = Vector2.zero;
            var tapImage = tapAreaGO.GetComponent<Image>();
            tapImage.color = new Color(0, 0, 0, 0); // 완전 투명이지만 raycast는 받음

            var tapButton = tapAreaGO.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(tapButton.onClick, titleController.OnClickAnywhere);

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildHomeScene()
        {
            const string path = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var (canvasRoot, font) = BuildUiSceneShell();

            CreateLabel(canvasRoot, "LogoText", "작은 왕국", 70, font, new Vector2(0, 700), new Vector2(800, 150));

            // 구역 선택 카드(3개)는 별도의 "TinyKingdom/Build Zone Select (Home Scene)" 메뉴가 채운다 —
            // 여기서는 HomeController만 붙여두고, 그 메뉴가 언제든 다시 실행해도 안전하게 idempotent함.
            new GameObject("HomeController", typeof(HomeController));

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>
        /// Home 씬에 구역 선택 카드 3개를 만든다. 지금까진 "게임 시작" 버튼 하나가 곧장 Zone1로
        /// 진입해서 2·3구역은 아무리 클리어해도 실제로 들어가볼 방법이 없었다 — 그 구멍을 메운다.
        /// 몇 번을 다시 실행해도 안전하게 이전 카드들을 지우고 새로 만든다.
        /// </summary>
        [MenuItem("TinyKingdom/Build Zone Select (Home Scene)")]
        public static void BuildZoneSelect()
        {
            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            var homeControllerGO = GameObject.Find("HomeController");
            var canvasGO = GameObject.Find("Canvas");
            if (homeControllerGO == null || canvasGO == null)
            {
                Debug.LogError("[SceneBootstrap] Home 씬에서 HomeController 또는 Canvas를 못 찾았습니다.");
                return;
            }

            var old = GameObject.Find("ZoneSelectPanel");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var oldStartButton = GameObject.Find("StartGameButton"); // 예전 버튼이 남아있으면 같이 정리
            if (oldStartButton != null) UnityEngine.Object.DestroyImmediate(oldStartButton);

            var homeController = homeControllerGO.GetComponent<HomeController>();
            Font font = GameFonts.Main;

            var panelGO = new GameObject("ZoneSelectPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0, -150);
            panelRect.sizeDelta = new Vector2(760, 700);

            string[] zoneAssetPaths =
            {
                "Assets/02_Data/Zones/Zone1_FrontGate.asset",
                "Assets/02_Data/Zones/Zone2_Library.asset",
                "Assets/02_Data/Zones/Zone3_Greenhouse.asset",
            };
            float[] cardYs = { 260f, 0f, -260f };

            for (int i = 0; i < zoneAssetPaths.Length; i++)
            {
                var zoneData = AssetDatabase.LoadAssetAtPath<ZoneData>(zoneAssetPaths[i]);
                if (zoneData == null)
                {
                    Debug.LogWarning($"[SceneBootstrap] {zoneAssetPaths[i]}를 못 찾아서 건너뜁니다.");
                    continue;
                }
                BuildZoneCard(panelGO.transform, zoneData, homeController, cardYs[i], font);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBootstrap] 구역 선택 카드 3개 배선 완료.");
        }

        private static void BuildZoneCard(Transform parent, ZoneData zone, HomeController homeController, float y, Font font)
        {
            var cardGO = new GameObject($"ZoneCard_{zone.zoneIndex}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(ZoneButtonView));
            cardGO.transform.SetParent(parent, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0, y);
            cardRect.sizeDelta = new Vector2(720, 220);
            cardGO.GetComponent<Image>().color = new Color(0.22f, 0.2f, 0.3f);

            var nameText = CreateLabel(cardGO.transform, "NameText", "", 44, font, new Vector2(0, 30), new Vector2(680, 70));
            var stateText = CreateLabel(cardGO.transform, "StateText", "", 30, font, new Vector2(0, -50), new Vector2(680, 50));
            stateText.color = new Color(1f, 0.85f, 0.3f);

            var view = cardGO.GetComponent<ZoneButtonView>();
            var so = new SerializedObject(view);
            so.FindProperty("homeController").objectReferenceValue = homeController;
            so.FindProperty("zone").objectReferenceValue = zone;
            so.FindProperty("button").objectReferenceValue = cardGO.GetComponent<Button>();
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("stateText").objectReferenceValue = stateText;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(cardGO.GetComponent<Button>().onClick, view.OnClick);
        }

        /// <summary>
        /// Home 씬에 설정 버튼+패널(BGM/효과음 슬라이더)을 만든다. ShopRosterUIBootstrap의
        /// 상점 UI 때처럼 이미 있으면(SettingsPanel 존재) 레이아웃은 다시 안 건드리고 넘어간다 —
        /// 유저가 씬에서 위치를 옮겨놨을 수 있어서.
        /// </summary>
        [MenuItem("TinyKingdom/Build Settings Panel (Home Scene)")]
        public static void BuildSettingsPanel()
        {
            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[SceneBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            if (GameObject.Find("SettingsPanel") != null)
            {
                Debug.Log("[SceneBootstrap] SettingsPanel이 이미 있어서 레이아웃은 그대로 두고 넘어갑니다. " +
                          "새로 짜려면 씬에서 SettingsPanel/SettingsButton을 직접 지우고 다시 실행하세요.");
                return;
            }

            Font font = GameFonts.Main;
            Transform canvasT = canvasGO.transform;

            // 여는 버튼 — 화면 하단, 구역 카드들과 안 겹치는 자리.
            var openGO = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            openGO.transform.SetParent(canvasT, false);
            var openRect = openGO.GetComponent<RectTransform>();
            openRect.anchorMin = openRect.anchorMax = new Vector2(0.5f, 0f);
            openRect.pivot = new Vector2(0.5f, 0f);
            openRect.anchoredPosition = new Vector2(0, 60);
            openRect.sizeDelta = new Vector2(220, 80);
            openGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f);
            CreateLabel(openGO.transform, "SettingsButtonLabel", "설정", 34, font, Vector2.zero, new Vector2(200, 60));

            var controller = BuildSettingsPanelCard(canvasT, font);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(openGO.GetComponent<Button>().onClick, controller.Open);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBootstrap] 설정 버튼+패널(BGM/효과음 슬라이더) 배선 완료.");
        }

        /// <summary>
        /// 설정 패널 본체(반투명 오버레이 + BGM/효과음 슬라이더 카드 + 닫기 버튼)만 짓는다.
        /// 여는 버튼은 씬마다 자리가 달라서(Home은 화면 하단 중앙, Battle은 왼쪽 탭 자리) 각
        /// 호출부가 따로 만들고 반환된 컨트롤러의 Open()에 연결한다 — Home/Battle 둘 다 같은
        /// 패널 디자인을 재사용하기 위해 분리했다.
        /// </summary>
        internal static SettingsPanelController BuildSettingsPanelCard(Transform canvasT, Font font)
        {
            // 패널 — 반투명 배경 + 카드. 시작은 꺼진 상태. 화면 전체를 덮는 오버레이라 어느
            // 씬/탭 상태에서 열어도 그 위에 그냥 뜬다(상점/아이템 패널을 안 꺼도 됨).
            var panelGO = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image),
                typeof(SettingsPanelController));
            panelGO.transform.SetParent(canvasT, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            panelGO.SetActive(false);

            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760, 640);
            cardGO.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.19f, 0.98f);

            CreateLabel(cardGO.transform, "SettingsTitle", "설정", 56, font, new Vector2(0, 250), new Vector2(600, 100));

            CreateLabel(cardGO.transform, "BgmLabel", "배경음악", 34, font, new Vector2(0, 90), new Vector2(600, 60));
            var bgmSlider = BuildSlider(cardGO.transform, "BgmSlider", new Vector2(0, 20), new Vector2(560, 50));

            CreateLabel(cardGO.transform, "SfxLabel", "효과음", 34, font, new Vector2(0, -90), new Vector2(600, 60));
            var sfxSlider = BuildSlider(cardGO.transform, "SfxSlider", new Vector2(0, -160), new Vector2(560, 50));

            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 40);
            closeRect.sizeDelta = new Vector2(240, 80);
            closeGO.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.75f);
            CreateLabel(closeGO.transform, "CloseButtonLabel", "닫기", 32, font, Vector2.zero, new Vector2(220, 60));

            var controller = panelGO.GetComponent<SettingsPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panelGO;
            so.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(closeGO.GetComponent<Button>().onClick, controller.Close);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(bgmSlider.onValueChanged, controller.OnBgmVolumeChanged);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, controller.OnSfxVolumeChanged);

            return controller;
        }

        private static Slider BuildSlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(go.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f);

            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGO.GetComponent<Image>().color = new Color(0.65f, 0.5f, 0.85f);

            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28, 28);
            handleGO.GetComponent<Image>().color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleGO.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0.8f;

            return slider;
        }

        /// <summary>
        /// Home 씬에 수호자 도감 버튼+패널을 만든다. GuardianRegistry(Resources, 세이브 시스템과
        /// 공유)에 등록된 순서 그대로 10칸을 채운다 — SaveBootstrap의 "Build Guardian Registry"를
        /// 먼저 실행해서 레지스트리에 수호자가 들어있어야 카드가 만들어진다.
        /// 설정 패널과 같은 이유로 이미 있으면(GuardianCodexPanel 존재) 다시 안 짓는다.
        /// </summary>
        [MenuItem("TinyKingdom/Build Guardian Codex (Home Scene)")]
        public static void BuildGuardianCodex()
        {
            const string homePath = "Assets/06_Scenes/Home.unity";
            var scene = EditorSceneManager.OpenScene(homePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[SceneBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            if (GameObject.Find("GuardianCodexPanel") != null)
            {
                Debug.Log("[SceneBootstrap] GuardianCodexPanel이 이미 있어서 레이아웃은 그대로 두고 넘어갑니다. " +
                          "새로 짜려면 씬에서 GuardianCodexPanel/GuardianCodexButton을 직접 지우고 다시 실행하세요.");
                return;
            }

            const string registryPath = "Assets/02_Data/Resources/GuardianRegistry.asset";
            var registry = AssetDatabase.LoadAssetAtPath<GuardianRegistry>(registryPath);
            if (registry == null)
            {
                Debug.LogError("[SceneBootstrap] GuardianRegistry를 못 찾았습니다 — " +
                                "TinyKingdom > Build Guardian Registry (Save System)을 먼저 실행하세요.");
                return;
            }

            Font font = GameFonts.Main;
            Transform canvasT = canvasGO.transform;

            // 여는 버튼 — 설정 버튼 왼쪽 옆자리.
            var openGO = new GameObject("GuardianCodexButton", typeof(RectTransform), typeof(Image), typeof(Button));
            openGO.transform.SetParent(canvasT, false);
            var openRect = openGO.GetComponent<RectTransform>();
            openRect.anchorMin = openRect.anchorMax = new Vector2(0.5f, 0f);
            openRect.pivot = new Vector2(0.5f, 0f);
            openRect.anchoredPosition = new Vector2(-240, 60);
            openRect.sizeDelta = new Vector2(220, 80);
            openGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f);
            CreateLabel(openGO.transform, "GuardianCodexButtonLabel", "도감", 34, font, Vector2.zero, new Vector2(200, 60));

            // 패널 — 반투명 배경 + 카드(5x2 그리드). 시작은 꺼진 상태.
            var panelGO = new GameObject("GuardianCodexPanel", typeof(RectTransform), typeof(Image),
                typeof(GuardianCodexController));
            panelGO.transform.SetParent(canvasT, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            panelGO.SetActive(false);

            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(800, 700);
            cardGO.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.19f, 0.98f);

            CreateLabel(cardGO.transform, "GuardianCodexTitle", "수호자 도감", 52, font, new Vector2(0, 290), new Vector2(700, 100));

            const float entryWidth = 130f, entryHeight = 160f, colSpacing = 150f, rowSpacing = 180f;
            float[] colXs = { -2 * colSpacing, -colSpacing, 0, colSpacing, 2 * colSpacing };
            float[] rowYs = { 95f, -95f };

            var entryBackgrounds = new List<Image>();
            var entryPortraits = new List<Image>();
            var entryNames = new List<Text>();

            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;
                var pos = new Vector2(colXs[col], rowYs[row]);
                var (bg, portrait, nameText) = BuildCodexEntry(cardGO.transform, pos, entryWidth, entryHeight, font);
                entryBackgrounds.Add(bg);
                entryPortraits.Add(portrait);
                entryNames.Add(nameText);
            }

            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 40);
            closeRect.sizeDelta = new Vector2(240, 80);
            closeGO.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.75f);
            CreateLabel(closeGO.transform, "CloseButtonLabel", "닫기", 32, font, Vector2.zero, new Vector2(220, 60));

            var controller = panelGO.GetComponent<GuardianCodexController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panelGO;
            so.FindProperty("registry").objectReferenceValue = registry;
            SetObjectList(so, "entryBackgrounds", entryBackgrounds);
            SetObjectList(so, "entryPortraits", entryPortraits);
            SetObjectList(so, "entryNames", entryNames);
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(openGO.GetComponent<Button>().onClick, controller.Open);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(closeGO.GetComponent<Button>().onClick, controller.Close);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBootstrap] 수호자 도감 버튼+패널(10칸) 배선 완료.");
        }

        private static (Image background, Image portrait, Text nameText) BuildCodexEntry(
            Transform parent, Vector2 anchoredPosition, float width, float height, Font font)
        {
            var go = new GameObject("CodexEntry", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(width, height);
            var background = go.GetComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.26f);

            var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(go.transform, false);
            var portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.28f);
            portraitRect.anchorMax = new Vector2(1f, 1f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            var portrait = portraitGO.GetComponent<Image>();
            portrait.preserveAspect = true;

            var nameText = CreateLabel(go.transform, "Name", "", 20, font,
                new Vector2(0, -height / 2f + 18f), new Vector2(width - 10f, 32f));

            return (background, portrait, nameText);
        }

        private static void SetObjectList<T>(SerializedObject so, string propName, List<T> values) where T : UnityEngine.Object
        {
            var prop = so.FindProperty(propName);
            prop.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>
        /// Game 뷰 프리셋에 GDD.md 3번 기준 해상도(1080x1920, 세로)를 추가하고 선택한다.
        /// GameViewSizes는 UnityEditor 내부(internal) 클래스라 리플렉션으로 접근한다 —
        /// 순수 에디터 미리보기 편의 설정이라 실패해도 게임 자체(CanvasScaler)에는 영향 없음.
        /// </summary>
        [MenuItem("TinyKingdom/Set Mobile Game View (1080x1920)")]
        public static void SetMobileGameView()
        {
            const string sizeName = "TinyKingdom Portrait (1080x1920)";
            try
            {
                Assembly editorAssembly = typeof(Editor).Assembly;

                Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);

                object gameViewSizes = scriptableSingletonType.GetProperty("instance").GetValue(null, null);
                object currentGroupType = gameViewSizesType.GetProperty("currentGroupType").GetValue(gameViewSizes, null);
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(gameViewSizes, new[] { currentGroupType });

                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                var ctor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                object newSize = ctor.Invoke(new object[] { fixedResolution, 1080, 1920, sizeName });

                Type groupClassType = editorAssembly.GetType("UnityEditor.GameViewSizeGroup");
                var getDisplayTexts = groupClassType.GetMethod("GetDisplayTexts");
                string[] existingNames = (string[])getDisplayTexts.Invoke(group, null);

                int existingIndex = Array.FindIndex(existingNames, n => n.Contains(sizeName));
                if (existingIndex < 0)
                {
                    groupClassType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                    existingIndex = ((string[])getDisplayTexts.Invoke(group, null)).Length - 1;
                }

                var gameViewWindowType = editorAssembly.GetType("UnityEditor.GameView");
                EditorWindow gameView = EditorWindow.GetWindow(gameViewWindowType);
                gameViewWindowType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(gameView, new object[] { existingIndex, null });

                Debug.Log($"[SceneBootstrap] Game 뷰 해상도를 '{sizeName}'로 설정했습니다.");
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "[SceneBootstrap] Game 뷰 프리셋 자동 설정 실패(내부 API 리플렉션). " +
                    "수동으로: Game 탭 → 좌상단 비율 드롭다운 → '+' → Width 1080 / Height 1920 입력. " +
                    "원인: " + e);
            }
        }

        private static RectTransform BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // GDD.md 3번
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var fieldRootGO = new GameObject("BattlefieldRoot", typeof(RectTransform));
            fieldRootGO.transform.SetParent(canvasGO.transform, false);
            var fieldRootRect = fieldRootGO.GetComponent<RectTransform>();
            fieldRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            fieldRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            fieldRootRect.anchoredPosition = Vector2.zero;
            fieldRootRect.sizeDelta = new Vector2(1080, 1920);

            var gridManagerGO = new GameObject("GridManager", typeof(GridManager));
            gridManagerGO.transform.SetParent(canvasGO.transform, false);
            var gridSO = new SerializedObject(gridManagerGO.GetComponent<GridManager>());
            gridSO.FindProperty("battlefieldRoot").objectReferenceValue = fieldRootRect;
            gridSO.ApplyModifiedPropertiesWithoutUndo();

            return fieldRootRect;
        }

        private static InvaderUnit BuildInvaderPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets", "05_Prefabs");

            string prefabPath = PrefabDir + "/InvaderUnit.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                bool needsSave = false;
                if (existing.GetComponent<SpriteFrameAnimator>() == null)
                {
                    var patchInstance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                    patchInstance.AddComponent<SpriteFrameAnimator>();
                    PrefabUtility.SaveAsPrefabAsset(patchInstance, prefabPath);
                    UnityEngine.Object.DestroyImmediate(patchInstance);
                    existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                // 체력바 도입 이전에 이미 저장돼 있던 프리팹을 위한 마이그레이션 — hpFillImage가
                // 비어 있으면(구버전) 바를 새로 붙여준다.
                var invaderComp = existing.GetComponent<InvaderUnit>();
                var checkSO = new SerializedObject(invaderComp);
                if (checkSO.FindProperty("hpFillImage").objectReferenceValue == null)
                {
                    var patchInstance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                    Image fill = BuildInvaderHpBar(patchInstance.transform);
                    var patchSO = new SerializedObject(patchInstance.GetComponent<InvaderUnit>());
                    patchSO.FindProperty("hpFillImage").objectReferenceValue = fill;
                    patchSO.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(patchInstance, prefabPath);
                    UnityEngine.Object.DestroyImmediate(patchInstance);
                    existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                return existing.GetComponent<InvaderUnit>();
            }

            var go = new GameObject("InvaderUnit", typeof(RectTransform), typeof(Image), typeof(SpriteFrameAnimator), typeof(InvaderUnit));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(FieldConstants.InvaderSize, FieldConstants.InvaderSize);
            go.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

            Image hpFill = BuildInvaderHpBar(go.transform);
            var so = new SerializedObject(go.GetComponent<InvaderUnit>());
            so.FindProperty("hpFillImage").objectReferenceValue = hpFill;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            return prefab.GetComponent<InvaderUnit>();
        }

        /// <summary>침입자 머리 위 체력바(배경+필) — InvaderUnit.RectTransform이 data.visualScale로
        /// 스케일되므로, 이 자식도 같이 커져서 보스급 침입자는 바도 자동으로 커진다.</summary>
        private static Image BuildInvaderHpBar(Transform parent)
        {
            float barY = FieldConstants.InvaderSize / 2f + 12f;

            // Pixel_HUD_UI_FreeKit의 UI_Progress_Style2 프레임을 쓰면(TinyKingdom > Apply Pixel HUD
            // Kit Skin) 원본 비율(104x39)이 예전 64x10짜리 얇은 바에는 안 맞아서(9-slice 테두리가
            // 세로 공간보다 커짐) 세로를 좀 더 키웠다.
            var bgGO = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(parent, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0, barY);
            bgRect.sizeDelta = new Vector2(70, 16);
            var bgImage = bgGO.GetComponent<Image>();
            Sprite invaderBarBgSprite = ShopRosterUIBootstrap.LoadPixelKitSprite("UI_Progress_Style2_Bg.png");
            if (invaderBarBgSprite != null)
            {
                bgImage.sprite = invaderBarBgSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white;
            }
            else
            {
                bgImage.color = new Color(0.1f, 0.05f, 0.05f, 0.9f);
            }
            bgImage.raycastTarget = false;

            // Image.Type.Filled + fillAmount 조합이 이 프로젝트에서 씬에 실제로 반영이 안 되는(맞는
            // 수치로 대입은 되는데 화면에는 항상 꽉 차 보이는) 문제가 있어서, 왼쪽 pivot에 고정한 채
            // RectTransform.localScale.x만 줄이는 훨씬 원시적인 방식으로 바꿨다(InvaderUnit.
            // UpdateHpBarVisual 참고) — 너비 자체를 늘였다 줄였다 하는 거라 렌더링이 안 따라올
            // 여지가 없다.
            var fillGO = new GameObject("HpBarFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2, 0);
            fillRect.sizeDelta = new Vector2(66, 11); // 꽉 찼을 때(scale.x=1) 크기 — 배경보다 안쪽으로
            var fillImage = fillGO.GetComponent<Image>();
            Sprite invaderBarFillSprite = ShopRosterUIBootstrap.LoadPixelKitSprite("UI_Progress_Style2_Fill_Red.png");
            if (invaderBarFillSprite != null)
            {
                fillImage.sprite = invaderBarFillSprite;
                fillImage.type = Image.Type.Simple;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = new Color(0.85f, 0.2f, 0.25f);
            }
            fillImage.raycastTarget = false;

            return fillImage;
        }

        private static void BuildBattleManager(RectTransform battlefieldRoot, InvaderUnit invaderPrefab)
        {
            var managerGO = new GameObject("BattleManager", typeof(WardHealth), typeof(RunCurrency), typeof(WaveSpawner));
            var ward = managerGO.GetComponent<WardHealth>();
            var currency = managerGO.GetComponent<RunCurrency>();
            var spawner = managerGO.GetComponent<WaveSpawner>();

            var waves = new List<WaveData>();
            for (int i = 1; i <= 10; i++)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>($"Assets/02_Data/Waves/Wave{i:D2}.asset");
                if (wave != null) waves.Add(wave);
            }

            var zone1 = AssetDatabase.LoadAssetAtPath<ZoneData>("Assets/02_Data/Zones/Zone1_FrontGate.asset");

            var spawnerSO = new SerializedObject(spawner);
            var wavesProp = spawnerSO.FindProperty("waves");
            wavesProp.ClearArray();
            for (int i = 0; i < waves.Count; i++)
            {
                wavesProp.InsertArrayElementAtIndex(i);
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            }
            spawnerSO.FindProperty("currentZone").objectReferenceValue = zone1;
            spawnerSO.FindProperty("invaderPrefab").objectReferenceValue = invaderPrefab;
            spawnerSO.FindProperty("spawnRoot").objectReferenceValue = battlefieldRoot;
            spawnerSO.FindProperty("ward").objectReferenceValue = ward;
            spawnerSO.FindProperty("currency").objectReferenceValue = currency;
            spawnerSO.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
