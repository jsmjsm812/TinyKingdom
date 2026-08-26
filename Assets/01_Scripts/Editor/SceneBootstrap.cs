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

        [MenuItem("WitchHour/Build Battle Scene (Week 1)")]
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
        [MenuItem("WitchHour/Restructure Into Boot-Title-Home-Battle Scenes")]
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
        [MenuItem("WitchHour/Fix Battle Scene Input Module")]
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

            return (canvasGO.GetComponent<RectTransform>(), Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
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

            CreateLabel(canvasRoot, "TitleText", "위치 아워", 110, font, new Vector2(0, 100), new Vector2(900, 220));
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

            CreateLabel(canvasRoot, "LogoText", "위치 아워", 70, font, new Vector2(0, 700), new Vector2(800, 150));

            var controllerGO = new GameObject("HomeController", typeof(HomeController));
            var homeController = controllerGO.GetComponent<HomeController>();

            var buttonGO = new GameObject("StartGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(canvasRoot, false);
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0, -600);
            buttonRect.sizeDelta = new Vector2(440, 150);
            buttonGO.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.8f);

            CreateLabel(buttonGO.transform, "Label", "게임 시작", 52, font, Vector2.zero, new Vector2(440, 150));

            var button = buttonGO.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, homeController.OnClickStartGame);

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>
        /// Game 뷰 프리셋에 GDD.md 3번 기준 해상도(1080x1920, 세로)를 추가하고 선택한다.
        /// GameViewSizes는 UnityEditor 내부(internal) 클래스라 리플렉션으로 접근한다 —
        /// 순수 에디터 미리보기 편의 설정이라 실패해도 게임 자체(CanvasScaler)에는 영향 없음.
        /// </summary>
        [MenuItem("WitchHour/Set Mobile Game View (1080x1920)")]
        public static void SetMobileGameView()
        {
            const string sizeName = "WitchHour Portrait (1080x1920)";
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
                return existing.GetComponent<InvaderUnit>();

            var go = new GameObject("InvaderUnit", typeof(RectTransform), typeof(Image), typeof(InvaderUnit));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            go.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            return prefab.GetComponent<InvaderUnit>();
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
