#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchHour.Combat;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;

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
                Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
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

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

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
            Object.DestroyImmediate(go);

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
