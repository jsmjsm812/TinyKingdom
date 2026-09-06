using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// "플레이어들도 리셋할 수 있게 해달라" 요청 — F1 디버그 오버레이 말고 실제 플레이어가 쓸 수
    /// 있는 기능으로, 로비 타이틀 글씨 바로 아래에 작은 "진행 상황 초기화" 버튼을 두고, 실수로
    /// 전부 날리는 걸 막기 위한 확인 팝업(ResetConfirmPanel)을 새로 만든다. 처음엔 설정 패널
    /// 안에 넣었었는데 "설정 말고 로비 글씨 아래에" 피드백으로 위치를 옮김 — 혹시 예전 버전
    /// 실행으로 설정 패널 쪽에 남아있던 버튼이 있으면 이 메뉴가 같이 정리한다.
    /// </summary>
    public static class HomeResetProgressBootstrap
    {
        private const string HomePath = "Assets/06_Scenes/Home.unity";
        private static readonly Color DangerColor = new Color(0.55f, 0.16f, 0.16f, 1f);

        [MenuItem("TinyKingdom/Add Reset-Progress Button (Home Lobby)")]
        public static void AddResetProgressButton()
        {
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(HomePath)) return;

            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            GameObject canvasGo = ShopRosterUIBootstrap.FindInScene(scene, "Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[HomeResetProgressBootstrap] Canvas를 못 찾음.");
                return;
            }
            Transform canvasT = canvasGo.transform;

            GameObject titleGo = ShopRosterUIBootstrap.FindInScene(scene, "LobbyTitleLabel");
            if (titleGo == null)
            {
                Debug.LogError("[HomeResetProgressBootstrap] LobbyTitleLabel을 못 찾음.");
                return;
            }

            RemoveOldSettingsPanelButton(canvasT);

            // 확인 팝업(없으면 새로 생성)
            Transform confirmPanelT = canvasT.Find("ResetConfirmPanel");
            Transform closeButtonTemplate = FindAnyCloseButtonTemplate(canvasT);
            ResetProgressConfirmController confirmController = confirmPanelT != null
                ? confirmPanelT.GetComponent<ResetProgressConfirmController>()
                : BuildConfirmPanel(canvasT, closeButtonTemplate);

            // 로비 타이틀 글씨 바로 아래의 "진행 상황 초기화" 버튼(없으면 새로 생성)
            Transform resetButtonT = canvasT.Find("ResetProgressButton");
            if (resetButtonT == null)
            {
                var titleRect = titleGo.GetComponent<RectTransform>();
                // 타이틀은 top-anchor(pivot y=1)라 anchoredPosition.y - sizeDelta.y가 실제 아래쪽
                // 끝 — 거기서 조금 더 내려서 배치한다. 눈에 띄면 안 되는 위험한 액션이라 작고
                // 수수하게(회갈색 톤, 작은 글자) 만든다.
                float belowTitleY = titleRect.anchoredPosition.y - titleRect.sizeDelta.y - 24f;

                var resetGo = new GameObject("ResetProgressButton", typeof(RectTransform), typeof(Image), typeof(Button));
                resetGo.transform.SetParent(canvasT, false);
                var resetRect = resetGo.GetComponent<RectTransform>();
                resetRect.anchorMin = resetRect.anchorMax = new Vector2(0.5f, 1f);
                resetRect.pivot = new Vector2(0.5f, 1f);
                resetRect.sizeDelta = new Vector2(260f, 56f);
                resetRect.anchoredPosition = new Vector2(0f, belowTitleY);
                resetGo.GetComponent<Image>().color = new Color(0.3f, 0.24f, 0.24f, 0.85f);

                var labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(resetGo.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                var label = labelGo.GetComponent<Text>();
                label.font = WitchHour.Core.GameFonts.Main;
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(1f, 0.85f, 0.8f);
                label.text = "진행 상황 초기화";

                resetButtonT = resetGo.transform;
            }

            var resetButton = resetButtonT.GetComponent<Button>();
            while (resetButton.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(resetButton.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(resetButton.onClick, confirmController.Open);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HomeResetProgressBootstrap] 로비 타이틀 아래에 '진행 상황 초기화' 버튼 + 확인 팝업 추가/연결 완료.");
        }

        /// <summary>이전 버전(WitchHour/Add Reset-Progress Button (Home Settings))이 이미 실행돼
        /// 설정 패널 안에 버튼을 심어놨을 수 있으니, CloseButton 위치도 되돌리고 지운다.</summary>
        private static void RemoveOldSettingsPanelButton(Transform canvasT)
        {
            Transform settingsPanel = canvasT.Find("SettingsPanel");
            Transform card = settingsPanel != null ? settingsPanel.Find("Card") : null;
            if (card == null) return;

            Transform oldButton = card.Find("ResetProgressButton");
            if (oldButton == null) return;

            Object.DestroyImmediate(oldButton.gameObject);

            Transform closeButtonT = card.Find("CloseButton");
            if (closeButtonT != null)
            {
                var rect = closeButtonT.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }
        }

        /// <summary>확인 팝업의 버튼 스킨을 맞추려고 아무 씬에나 있는 CloseButton 하나를 템플릿으로
        /// 빌려온다 — 설정 패널의 CloseButton이면 충분(배틀씬 것과 같은 DEVNIK 스킨 공유).</summary>
        private static Transform FindAnyCloseButtonTemplate(Transform canvasT)
        {
            Transform settingsPanel = canvasT.Find("SettingsPanel");
            Transform card = settingsPanel != null ? settingsPanel.Find("Card") : null;
            return card != null ? card.Find("CloseButton") : null;
        }

        private static ResetProgressConfirmController BuildConfirmPanel(Transform canvasT, Transform closeButtonTemplate)
        {
            var root = new GameObject("ResetConfirmPanel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasT, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            // 화면 전체를 어둡게 덮어서 카드 뒤 구역 카드들이 안 눌리게(레이캐스트 차단) 막는다.
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 480f);
            cardRect.anchoredPosition = Vector2.zero;
            // 설정 패널 Card와 같은 색(짙은 남보라, 스프라이트 없는 단색 패널) — 도트 UI 톤 유지.
            card.GetComponent<Image>().color = new Color(0.14f, 0.13f, 0.19f, 0.98f);

            CreateText(card.transform, "Title", "진행 상황을 초기화할까요?",
                new Vector2(0f, 160f), new Vector2(680f, 100f), 36, Color.white);

            var warning = CreateText(card.transform, "Warning",
                "구역 해금·캐릭터 해금·저장된 웨이브 진행이 전부 사라지며\n되돌릴 수 없습니다.",
                new Vector2(0f, 30f), new Vector2(640f, 160f), 26, new Color(1f, 0.75f, 0.6f));
            warning.lineSpacing = 1.2f;

            GameObject cancelGo, confirmGo;
            if (closeButtonTemplate != null)
            {
                cancelGo = Object.Instantiate(closeButtonTemplate.gameObject, card.transform);
                confirmGo = Object.Instantiate(closeButtonTemplate.gameObject, card.transform);
            }
            else
            {
                cancelGo = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
                cancelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 80f);
                confirmGo = Object.Instantiate(cancelGo, card.transform);
                cancelGo.transform.SetParent(card.transform, false);
            }

            cancelGo.name = "CancelButton";
            cancelGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130f, -170f);
            var cancelLabel = cancelGo.GetComponentInChildren<Text>();
            if (cancelLabel != null) cancelLabel.text = "취소";
            else CreateText(cancelGo.transform, "Text", "취소", Vector2.zero, new Vector2(220f, 60f), 28, Color.white);

            confirmGo.name = "ConfirmResetButton";
            confirmGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(130f, -170f);
            confirmGo.GetComponent<Image>().color = DangerColor;
            var confirmLabel = confirmGo.GetComponentInChildren<Text>();
            if (confirmLabel != null)
            {
                confirmLabel.text = "초기화하기";
                confirmLabel.resizeTextForBestFit = true;
                confirmLabel.resizeTextMinSize = 12;
                confirmLabel.resizeTextMaxSize = 28;
            }
            else
            {
                CreateText(confirmGo.transform, "Text", "초기화하기", Vector2.zero, new Vector2(220f, 60f), 22, Color.white);
            }

            var controller = root.AddComponent<ResetProgressConfirmController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = root;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 템플릿에서 복제해왔다면 기존 onClick(SettingsPanelController.Close 등)이 딸려왔을 수 있다.
            var cancelButton = cancelGo.GetComponent<Button>();
            while (cancelButton.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(cancelButton.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(cancelButton.onClick, controller.Close);

            var confirmButton = confirmGo.GetComponent<Button>();
            while (confirmButton.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(confirmButton.onClick, 0);
            UnityEventTools.AddVoidPersistentListener(confirmButton.onClick, controller.OnClickConfirmReset);

            root.SetActive(false);
            return controller;
        }

        private static Text CreateText(Transform parent, string name, string content, Vector2 anchoredPos,
            Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var text = go.GetComponent<Text>();
            text.font = WitchHour.Core.GameFonts.Main;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }
    }
}
