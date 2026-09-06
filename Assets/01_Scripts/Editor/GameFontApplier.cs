#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 폰트를 DNF BitBit(픽셀 폰트) 하나로 통일하기로 하면서, 이미 지어진 모든 씬/프리팹의
    /// Text 컴포넌트를 일괄로 바꿔주는 도구. 코드 쪽(GameFonts.Main)만 고쳐서는 이미 저장된
    /// 씬·프리팹 안의 기존 Text들은 안 바뀐다 — 전부 새로 지어야 하는 게 아니라면 이 툴로
    /// 한 번에 처리하는 게 훨씬 안전하다(4장짜리 상점 슬롯 하나 손으로 고치는 데도 한참
    /// 걸렸던 걸 생각하면, 프로젝트 전체 Text를 하나하나 찾아 고치는 건 현실적이지 않음).
    /// </summary>
    public static class GameFontApplier
    {
        [MenuItem("TinyKingdom/Apply Game Font To All Scenes And Prefabs")]
        public static void ApplyEverywhere()
        {
            Font font = GameFonts.Main;
            if (font == null)
            {
                Debug.LogError("[GameFontApplier] GameFonts.Main이 null입니다 — " +
                                "Assets/02_Data/Resources/Fonts/DNFBitBitv2.ttf가 있는지 확인하세요.");
                return;
            }

            int sceneCount = 0, sceneTextCount = 0;
            // 폴더 범위 없이 t:Scene으로 찾으면 TinyKingdom/Brackeys/SPUM 같은 임포트된 에셋
            // 패키지 안에 딸려오는 데모 씬까지 다 걸린다 — 그런 씬은 임베디드 패키지 안에 있어
            // 읽기 전용이라 OpenScene이 "read-only package" 오류로 그냥 실패해버렸다. 우리가
            // 실제로 만든 씬은 전부 06_Scenes 밑에 있으니 검색 범위를 거기로만 좁힌다.
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/06_Scenes" }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);

                // 폴더를 06_Scenes로 좁혀도 "read-only package" 팝업이 계속 떴다 — OpenScene을
                // 직접 부르기 전에 편집 가능한지 먼저 확인해서, 안 되는 씬은 그 자리에서 조용히
                // 건너뛰고 로그만 남긴다(Unity가 막는 모달 팝업 자체를 아예 안 띄우게).
                if (!AssetDatabase.IsOpenForEdit(scenePath))
                {
                    Debug.LogWarning($"[GameFontApplier] {scenePath}는 편집 불가능한 상태라 건너뜁니다 " +
                                      "(읽기 전용 패키지에 속해있거나 버전 관리 락 등).");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                int changed = 0;
                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    foreach (var text in rootGO.GetComponentsInChildren<Text>(true))
                    {
                        text.font = font;
                        changed++;
                    }
                }

                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                sceneCount++;
                sceneTextCount += changed;
                Debug.Log($"[GameFontApplier] {scenePath}: Text {changed}개 교체.");
            }

            int prefabCount = 0, prefabTextCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/05_Prefabs" }))
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var texts = contents.GetComponentsInChildren<Text>(true);
                    if (texts.Length == 0) continue;

                    foreach (var text in texts)
                        text.font = font;

                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    prefabCount++;
                    prefabTextCount += texts.Length;
                    Debug.Log($"[GameFontApplier] {prefabPath}: Text {texts.Length}개 교체.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GameFontApplier] 완료 — 씬 {sceneCount}개(Text {sceneTextCount}개), " +
                      $"프리팹 {prefabCount}개(Text {prefabTextCount}개)에 DNF BitBit 폰트를 적용했습니다.");
        }
    }
}
#endif
