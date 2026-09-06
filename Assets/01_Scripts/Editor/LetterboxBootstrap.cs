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
    /// PC 전체화면(가로 모니터)에서 세로 게임 화면(9:16)이 늘어나 보이는 문제를 고치기 위해,
    /// 각 씬의 Canvas 밑에 "실제 게임 UI를 담는 GameRoot"와 "그 밖을 채우는 검은 여백 바
    /// 4개(좌/우/상/하)"를 만들고 LetterboxFitter를 붙인다. 실제 크기/활성 계산은
    /// LetterboxFitter.cs가 런타임에 화면 비율을 보고 매번 다시 한다 — 여기선 뼈대만 짠다.
    ///
    /// CharacterLineup/Boot 씬은 제외한다(DevnikButtonSkinApplier와 같은 이유 —
    /// 디버그/부팅 전용 씬이라 실제 플레이어가 보는 화면이 아님).
    /// </summary>
    public static class LetterboxBootstrap
    {
        private static readonly string[] TargetScenes =
        {
            "Assets/06_Scenes/Title.unity",
            "Assets/06_Scenes/Home.unity",
            "Assets/06_Scenes/Battle.unity",
        };

        [MenuItem("TinyKingdom/Add Fullscreen Letterbox To All Scenes")]
        public static void Apply()
        {
            int sceneCount = 0;
            foreach (var scenePath in TargetScenes)
            {
                if (!AssetDatabase.IsOpenForEdit(scenePath))
                {
                    Debug.LogWarning($"[LetterboxBootstrap] {scenePath}는 편집 불가능해서 건너뜁니다.");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Canvas canvas = null;
                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    canvas = rootGO.GetComponentInChildren<Canvas>(true);
                    if (canvas != null) break;
                }

                if (canvas == null)
                {
                    Debug.LogWarning($"[LetterboxBootstrap] {scenePath}에 Canvas가 없어 건너뜁니다.");
                    continue;
                }

                // 이미 적용돼 있으면(재실행 대비) 건드리지 않는다 — 두 번 실행해서 GameRoot를
                // 중첩으로 또 감싸면 앵커 계산이 꼬인다.
                if (canvas.transform.Find("GameRoot") != null)
                {
                    Debug.Log($"[LetterboxBootstrap] {scenePath}는 이미 적용돼 있어 건너뜁니다.");
                    continue;
                }

                var canvasRect = canvas.GetComponent<RectTransform>();

                // 1. GameRoot — Canvas의 기존 자식을 전부 옮겨 담을 새 컨테이너.
                //    지금은 Canvas와 완전히 같은 크기(0~1 풀스크린)라, 기존 자식을 여기로
                //    옮겨도(worldPositionStays: false) 화면상 위치가 전혀 안 바뀐다.
                var gameRootGO = new GameObject("GameRoot", typeof(RectTransform));
                var gameRoot = gameRootGO.GetComponent<RectTransform>();
                gameRoot.SetParent(canvasRect, worldPositionStays: false);
                gameRoot.anchorMin = Vector2.zero;
                gameRoot.anchorMax = Vector2.one;
                gameRoot.offsetMin = Vector2.zero;
                gameRoot.offsetMax = Vector2.zero;
                gameRoot.SetAsFirstSibling(); // 검은 바보다 먼저 그려지게(뒤에 깔리게) 맨 앞 sibling으로.

                // 기존 Canvas 자식들(GameRoot 자기 자신 제외)을 스냅샷으로 먼저 뽑아둔다 —
                // Transform.SetParent를 순회 중에 바로 호출하면 자식 목록이 실시간으로 바뀌어서
                // foreach가 일부를 건너뛸 수 있다.
                var existingChildren = new List<Transform>();
                foreach (Transform child in canvasRect)
                {
                    if (child == gameRoot) continue;
                    existingChildren.Add(child);
                }
                foreach (var child in existingChildren)
                    child.SetParent(gameRoot, worldPositionStays: false);

                // 2. 검은 여백 바 4개 — 평소엔 꺼져 있다가 LetterboxFitter가 필요할 때만 켠다.
                var barLeft = CreateBar(canvasRect, "LetterboxBarLeft");
                var barRight = CreateBar(canvasRect, "LetterboxBarRight");
                var barTop = CreateBar(canvasRect, "LetterboxBarTop");
                var barBottom = CreateBar(canvasRect, "LetterboxBarBottom");

                // 3. LetterboxFitter — Canvas 오브젝트에 붙이고 위 참조들을 연결.
                var fitter = canvas.gameObject.AddComponent<LetterboxFitter>();
                var so = new SerializedObject(fitter);
                so.FindProperty("gameRoot").objectReferenceValue = gameRoot;
                so.FindProperty("barLeft").objectReferenceValue = barLeft;
                so.FindProperty("barRight").objectReferenceValue = barRight;
                so.FindProperty("barTop").objectReferenceValue = barTop;
                so.FindProperty("barBottom").objectReferenceValue = barBottom;
                so.FindProperty("canvasScaler").objectReferenceValue = canvas.GetComponent<CanvasScaler>();
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneCount++;
                Debug.Log($"[LetterboxBootstrap] {scenePath}: 레터박스 적용 완료.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LetterboxBootstrap] 완료 — 씬 {sceneCount}개.");
        }

        private static RectTransform CreateBar(RectTransform canvasRect, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRect, worldPositionStays: false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false; // 화면 가장자리 장식일 뿐 클릭을 막을 이유가 없다.

            go.SetActive(false); // LetterboxFitter가 화면 비율을 보고 필요할 때만 켠다.
            return rt;
        }
    }
}
#endif
