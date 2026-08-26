#if UNITY_EDITOR
using System.Collections.Generic;
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
