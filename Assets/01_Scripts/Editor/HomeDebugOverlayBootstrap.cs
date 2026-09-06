using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WitchHour.DebugTools;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 로비씬에 HomeDebugOverlay(F1 → "전체 캐릭터 해금" 버튼, 도감 스탯/능력 확인용)를 한 번
    /// 심어준다 — 그 뒤로는 씬에 저장돼 있으니 다시 실행할 필요 없다. Battle의
    /// DebugOverlayBootstrap과 같은 패턴.
    /// </summary>
    public static class HomeDebugOverlayBootstrap
    {
        private const string HomePath = "Assets/06_Scenes/Home.unity";

        [MenuItem("TinyKingdom/Add Debug Overlay To Home Scene")]
        public static void AddToScene()
        {
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(HomePath)) return;

            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            var existing = Object.FindAnyObjectByType<HomeDebugOverlay>();
            GameObject go = existing != null ? existing.gameObject : new GameObject("HomeDebugOverlay");
            if (existing == null) go.AddComponent<HomeDebugOverlay>();

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HomeDebugOverlayBootstrap] HomeDebugOverlay를 로비씬에 추가 완료. Play 중 F1로 켜고 끕니다.");
        }
    }
}
