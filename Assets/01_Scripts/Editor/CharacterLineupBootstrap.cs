#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 용사·침입자 전원을 한 화면에 나란히 줄 세워서 보여주는 디버그용 프리뷰 씬을 만들고,
    /// 각각을 개별 .prefab 에셋으로도 저장해 Project 창에서 캐릭터별로 훑어볼 수 있게 정리한다.
    /// "필드에서 캐릭터마다 발 높이가 안 맞는다"는 피드백을 눈으로 직접 확인/검증하려고 만들었다 —
    /// 전부 같은 바닥 기준선(빨간 줄)에 pivot을 맞춰서 배치하므로, 구운 스프라이트의 발 위치가
    /// 캐릭터마다 어긋나 있으면 여기서 바로 티가 난다. GuardianUnit/InvaderUnit 런타임 컴포넌트는
    /// Setup()/Spawn()이 코루틴(StartCoroutine)을 돌리는데 이건 플레이 모드가 아니면 동작하지
    /// 않으므로, 여기서는 그 컴포넌트를 붙이지 않고 idleFrames[0]만 순수 Image로 정적으로 보여준다.
    /// </summary>
    public static class CharacterLineupBootstrap
    {
        private const string ScenePath = "Assets/06_Scenes/CharacterLineup.unity";
        private const string GuardianPrefabDir = "Assets/05_Prefabs/CharacterPreviews/Guardians";
        private const string InvaderPrefabDir = "Assets/05_Prefabs/CharacterPreviews/Invaders";

        [MenuItem("WitchHour/Build Character Lineup (Debug Preview)")]
        public static void BuildLineup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);

            Font font = GameFonts.Main;

            const float guardianGroundY = 80f;
            const float invaderGroundY = -320f;
            const float cellWidth = 170f;
            const float portraitSize = 150f;

            DrawGroundLine("GuardianGroundLine", canvasGO.transform, guardianGroundY);
            DrawGroundLine("InvaderGroundLine", canvasGO.transform, invaderGroundY);

            // 프리팹 이름을 한글로 바꾸면서 폴더를 매번 지웠다 새로 만드는 로직을 같이 넣었더니
            // "Given path does not exist" 예외가 계속 나서(원인 특정 못 함, 여러 번 시도했지만
            // 재현이 계속됨) 폴더 삭제/재생성 로직 자체를 완전히 걷어냈다 — 폴더는 없을 때 한 번만
            // 만들고 그 외엔 절대 손 안 댐(EnsureFolder, 처음에 문제없이 동작했던 방식 그대로).
            // 용사는 한글 이름(guardianName)으로 저장 — 이건 문제없이 잘 됐던 부분. 침입자는
            // 일단 원래대로 data.name(영문 코드네임)으로 되돌려서 예외부터 확실히 없앰 — 침입자
            // 이름도 한글로 바꾸는 건 이 예외의 원인을 확실히 알고 난 뒤 따로 다시 시도한다.
            EnsureFolder(GuardianPrefabDir);
            EnsureFolder(InvaderPrefabDir);

            var guardianGuids = AssetDatabase.FindAssets("t:GuardianData", new[] { "Assets/02_Data/Guardians" });
            float x = -(guardianGuids.Length - 1) * cellWidth / 2f;
            foreach (var guid in guardianGuids)
            {
                var data = AssetDatabase.LoadAssetAtPath<GuardianData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                Sprite sprite = (data.idleFrames != null && data.idleFrames.Length > 0) ? data.idleFrames[0] : data.portrait;
                var go = PlaceCharacter(canvasGO.transform, data.guardianName, sprite, x, guardianGroundY, portraitSize, font);
                SavePreviewPrefab(go, GuardianPrefabDir, data.guardianName);
                x += cellWidth;
            }

            var invaderGuids = AssetDatabase.FindAssets("t:InvaderData", new[] { "Assets/02_Data/Invaders" });
            x = -(invaderGuids.Length - 1) * cellWidth / 2f;
            foreach (var guid in invaderGuids)
            {
                var data = AssetDatabase.LoadAssetAtPath<InvaderData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                Sprite sprite = (data.idleFrames != null && data.idleFrames.Length > 0) ? data.idleFrames[0] : data.sprite;
                var go = PlaceCharacter(canvasGO.transform, data.invaderName, sprite, x, invaderGroundY, portraitSize, font);
                SavePreviewPrefab(go, InvaderPrefabDir, data.name);
                x += cellWidth;
            }

            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(1200, 50);
            var titleText = titleGO.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "위: 용사 (빨간 줄 = 발 기준선)  /  아래: 침입자 — 전부 이 줄에 발이 닿아야 정상";

            if (!AssetDatabase.IsValidFolder("Assets/06_Scenes"))
                AssetDatabase.CreateFolder("Assets", "06_Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterLineupBootstrap] {guardianGuids.Length}명의 용사 + {invaderGuids.Length}종의 침입자를 " +
                      $"{ScenePath}에 나열하고, 캐릭터별 프리팹으로 {GuardianPrefabDir} / {InvaderPrefabDir}에 " +
                      "정리했습니다. 빨간 기준선에 발이 안 맞으면 재베이크가 필요합니다.");
        }

        // Assets/폴더/하위폴더 형태의 경로를 한 단계씩 만든다(CreateFolder는 한 단계씩만 되니까).
        // 폴더가 이미 있으면 아무것도 안 건드린다 — 지웠다 새로 만드는 시도를 여러 번 했다가
        // 전부 "Given path does not exist" 예외로 실패해서, 아예 손 안 대는 이 방식으로 되돌림.
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // 라인업 씬에 배치한 프리뷰 오브젝트를 그대로 개별 .prefab 에셋으로도 저장한다 —
        // 씬을 열지 않고도 Project 창에서 캐릭터별로 바로 훑어볼 수 있게.
        private static void SavePreviewPrefab(GameObject go, string folder, string assetName)
        {
            string safeName = string.IsNullOrEmpty(assetName) ? go.name : assetName;
            string path = $"{folder}/{safeName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        private static void DrawGroundLine(string name, Transform parent, float y)
        {
            var lineGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineGO.transform.SetParent(parent, false);
            var rect = lineGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(1800, 2);
            lineGO.GetComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.8f);
        }

        private static GameObject PlaceCharacter(Transform parent, string label, Sprite sprite, float x, float groundY, float size, Font font)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Character" : label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            // pivot을 바닥-중앙으로 둬서 anchoredPosition.y가 곧 "발이 닿는 높이"가 되게 한다 —
            // 모든 캐릭터를 같은 groundY에 놓으면 발이 실제로 다 맞는지 한눈에 비교된다.
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, groundY);
            rect.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            if (sprite == null) img.color = new Color(0.8f, 0.2f, 0.2f); // 스프라이트 없으면 빨간 박스로 표시

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0, -4);
            labelRect.sizeDelta = new Vector2(size + 20, 24);
            var labelText = labelGO.GetComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 13;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;

            return go;
        }
    }
}
#endif
