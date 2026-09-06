#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// Battle.unity에 ResultScreenController(결과창)가 하나가 아니라 여러 개(과거 부트스트랩
    /// 스크립트가 "이미 있으면 다시 안 만듦" 가드 없이 반복 실행되며 쌓인 것으로 추정) 중복
    /// 생성돼 있었다 — 성벽 HP가 0이 돼도 게임이 안 끝나던 원인. 여러 인스턴스가 같은
    /// BattleFlowController 이벤트에 동시에 구독돼 있어서, 그중 하나라도 참조가 깨져있으면
    /// (panel이 null인 등) 이벤트 호출 도중 예외가 터져 나머지 구독자(진짜 작동하는 결과창
    /// 포함)의 실행까지 막혔을 수 있다. 하나만 남기고 나머지는 통째로 지운다.
    /// </summary>
    public static class SceneDuplicateCleanupBootstrap
    {
        private const string BattlePath = "Assets/06_Scenes/Battle.unity";

        /// <summary>결과창에 "이번 출전 획득 골드 / 처치한 침입자 수" 텍스트 2줄을 추가한다.
        /// (침입자 처치 골드가 너무 적다는 피드백과 별개로, 클리어/실패 모두 결과창에 성과가
        /// 안 보인다는 요청) 실행 전에 중복 컨트롤러 정리도 같이 해서, 엉뚱한(곧 지워질) 사본에
        /// 텍스트를 연결하는 일이 없게 한다. 이미 텍스트가 있으면 재생성하지 않고 재사용한다.</summary>
        [MenuItem("TinyKingdom/Add Result Screen Gold+Kills Stats")]
        public static void AddResultScreenStats()
        {
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(BattlePath)) return;

            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);
            var kept = KeepSingleResultScreenController(out int removed);
            if (kept == null)
            {
                Debug.LogError("[SceneDuplicateCleanupBootstrap] ResultScreenController를 씬에서 찾을 수 없음.");
                return;
            }

            var so = new SerializedObject(kept);
            var panel = so.FindProperty("panel").objectReferenceValue as GameObject;
            if (panel == null)
            {
                Debug.LogError("[SceneDuplicateCleanupBootstrap] ResultScreenController의 panel 참조가 비어있음.");
                return;
            }

            var card = panel.transform.Find("Card");
            if (card == null)
            {
                Debug.LogError("[SceneDuplicateCleanupBootstrap] ResultPanel 안에서 Card를 못 찾음 — 구조가 예상과 다름.");
                return;
            }

            var titleLabel = card.Find("Label")?.GetComponent<Text>();
            Font font = titleLabel != null ? titleLabel.font : null;

            Text goldText = card.Find("GoldEarnedText")?.GetComponent<Text>();
            if (goldText == null)
                goldText = CreateStatText(card, "GoldEarnedText", new Vector2(0f, 30f), font);

            Text killsText = card.Find("KillsText")?.GetComponent<Text>();
            if (killsText == null)
                killsText = CreateStatText(card, "KillsText", new Vector2(0f, -30f), font);

            kept.EditorAssignStatTexts(goldText, killsText);
            EditorUtility.SetDirty(kept);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string dedupeNote = removed > 0 ? $" (중복 컨트롤러 {removed}개도 같이 정리함)" : "";
            Debug.Log($"[SceneDuplicateCleanupBootstrap] 결과창에 획득 골드/처치 수 텍스트 연결 완료{dedupeNote}.");
        }

        /// <summary>필드가 전부 제대로 물려있는(가장 온전한) 인스턴스 하나만 남기고 나머지
        /// ResultScreenController(및 그 panel)를 지운다. 이미 하나뿐이면 아무 것도 안 하고
        /// removed=0으로 그 하나를 반환한다.</summary>
        private static ResultScreenController KeepSingleResultScreenController(out int removed)
        {
            removed = 0;
            var controllers = Object.FindObjectsOfType<ResultScreenController>(true);
            if (controllers.Length == 0) return null;
            if (controllers.Length == 1) return controllers[0];

            ResultScreenController keep = null;
            foreach (var c in controllers)
            {
                var so = new SerializedObject(c);
                bool complete = so.FindProperty("battleFlow").objectReferenceValue != null
                                && so.FindProperty("panel").objectReferenceValue != null
                                && so.FindProperty("titleText").objectReferenceValue != null;
                if (complete) { keep = c; break; }
            }
            if (keep == null) keep = controllers[0];

            foreach (var c in controllers)
            {
                if (c == keep) continue;

                var so = new SerializedObject(c);
                var panelRef = so.FindProperty("panel").objectReferenceValue as GameObject;
                GameObject controllerGo = c.gameObject;

                // panelRef(ResultPanel)가 controllerGo(ResultScreenController 오브젝트 자신)를
                // 자식으로 물고 있는 구조라(씬 실제 계층: ResultPanel > Card, ResultScreenController),
                // panelRef를 지우면 controllerGo도 같이 지워진다 — 그 뒤에 controllerGo를 또
                // DestroyImmediate하면 이미 죽은 오브젝트를 건드려 MissingReferenceException이 난다.
                // 따라서 panelRef를 먼저 지우고, 그래도 살아있으면(= panelRef 밖에 따로 있던
                // 경우) 그때만 별도로 지운다.
                if (panelRef != null) Object.DestroyImmediate(panelRef);
                if (controllerGo != null) Object.DestroyImmediate(controllerGo);
                removed++;
            }

            return keep;
        }

        private static Text CreateStatText(Transform card, string name, Vector2 anchoredPos, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(card, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 60f);
            rect.anchoredPosition = anchoredPos;

            var text = go.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = name;
            return text;
        }
    }
}
#endif
