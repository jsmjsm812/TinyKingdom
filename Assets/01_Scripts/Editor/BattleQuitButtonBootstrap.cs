#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// "웨이브별 저장 + 배틀씬 그만하기 버튼" 요청 — 설정 탭(SettingsPanel/Card)에 이미 있는
    /// CloseButton을 복제해서 왼쪽에 "그만하기" 버튼을 추가한다. 기존 배치(슬라이더 2개 +
    /// CloseButton)를 안 건드리려고 새로 만들지 않고 복제 방식을 썼다 — DEVNIK 픽셀 스킨과
    /// 폰트/색이 자동으로 그대로 따라온다.
    /// </summary>
    public static class BattleQuitButtonBootstrap
    {
        private const string BattlePath = "Assets/06_Scenes/Battle.unity";

        [MenuItem("TinyKingdom/Add Quit-And-Save Button (Battle Settings)")]
        public static void AddQuitButton()
        {
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(BattlePath)) return;

            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var battleFlow = Object.FindObjectOfType<BattleFlowController>(true);
            if (battleFlow == null)
            {
                Debug.LogError("[BattleQuitButtonBootstrap] BattleFlowController를 씬에서 못 찾음.");
                return;
            }

            GameObject settingsPanel = ShopRosterUIBootstrap.FindInScene(scene, "SettingsPanel");
            Transform card = settingsPanel != null ? settingsPanel.transform.Find("Card") : null;
            Transform closeButtonT = card != null ? card.Find("CloseButton") : null;
            if (closeButtonT == null)
            {
                Debug.LogError("[BattleQuitButtonBootstrap] SettingsPanel/Card/CloseButton을 못 찾음 — 구조가 예상과 다름.");
                return;
            }

            Transform quitButtonT = card.Find("QuitButton");
            if (quitButtonT == null)
            {
                // CloseButton을 오른쪽으로 옮기고, 복제본을 왼쪽에 "그만하기"로 배치한다(나란히 2버튼).
                var closeRect = closeButtonT.GetComponent<RectTransform>();
                closeRect.anchoredPosition = new Vector2(130f, closeRect.anchoredPosition.y);

                var quitGo = Object.Instantiate(closeButtonT.gameObject, card);
                quitGo.name = "QuitButton";
                var quitRect = quitGo.GetComponent<RectTransform>();
                quitRect.anchoredPosition = new Vector2(-130f, closeRect.anchoredPosition.y);

                var label = quitGo.GetComponentInChildren<Text>();
                if (label != null) label.text = "그만하기";

                quitButtonT = quitGo.transform;
            }

            var button = quitButtonT.GetComponent<Button>();
            // 복제 원본(CloseButton)의 onClick(SettingsPanelController.Close)이 그대로 딸려왔을 수
            // 있으니 전부 지우고 QuitAndSaveProgress 하나만 연결한다 — 재실행해도 중복 연결 없게.
            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(button.onClick, battleFlow.QuitAndSaveProgress);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BattleQuitButtonBootstrap] 설정 패널에 '그만하기' 버튼 추가/연결 완료.");
        }
    }
}
#endif
