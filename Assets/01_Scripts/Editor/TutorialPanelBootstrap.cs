#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 로비 하단에 "설명" 버튼 + 게임 방법을 보여주는 튜토리얼 패널을 만든다. 슬라이더 같은
    /// 상태가 없는 순수 읽기 전용 패널이라 GDD.md 핵심 내용을 요약한 정적 텍스트 하나로 충분하다.
    /// </summary>
    public static class TutorialPanelBootstrap
    {
        private const string HomePath = "Assets/06_Scenes/Home.unity";

        private const string TutorialText =
            "▶ 목표\n" +
            "10웨이브를 버텨 왕국을 지키세요. 성벽 HP가 0이 되면 실패합니다.\n\n" +
            "▶ 준비 시간\n" +
            "용병 주점에서 용사를 고용하면 필드 빈 슬롯에 바로 배치됩니다. " +
            "배치된 용사는 자유롭게 드래그해서 자리를 바꿀 수 있어요.\n\n" +
            "▶ 합성\n" +
            "같은 용사 + 같은 성급 3기가 모이면 자동으로 성급이 올라 공격력이 강해집니다.\n\n" +
            "▶ 웨이브\n" +
            "준비 시간이 끝나면 자동으로 전투가 시작돼요. 전투 중에는 배치를 바꿀 수 없으니 " +
            "미리 준비해두세요.\n\n" +
            "▶ 재화\n" +
            "금화는 이번 출전에서만 쓰는 재화예요(고용·리롤·아이템 구매). 구역을 클리어하면 " +
            "다음 구역이 자동으로 해금됩니다.\n\n" +
            "▶ 아이템\n" +
            "주점 옆 '아이템' 탭에서 이번 출전 한정 버프/디버프를 살 수 있어요. 한 번 사면 " +
            "즉시 적용되고 이번 출전이 끝날 때까지 유지됩니다.\n\n" +
            "▶ 구역\n" +
            "구역은 순서대로 해금돼요. 뒤로 갈수록 몬스터가 강해지고 구역마다 고유한 기믹이 있습니다.";

        [MenuItem("TinyKingdom/Add Tutorial Panel And Button")]
        public static void AddTutorialPanelAndButton()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TutorialPanelBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            var controller = BuildPanel(canvasGO.transform);
            BuildButton(canvasGO.transform, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TutorialPanelBootstrap] 튜토리얼 버튼 + 패널 생성 완료. " +
                      "'TinyKingdom > Fix Bottom Buttons Layout'을 다시 실행해서 버튼 3개 간격을 맞추세요.");
        }

        private static TutorialPanelController BuildPanel(Transform canvasT)
        {
            var old = canvasT.Find("TutorialPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var panelGO = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasT, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var backdrop = panelGO.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);
            panelGO.SetActive(false);

            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(880f, 1300f);
            var cardImage = cardGO.GetComponent<Image>();
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.97f, 0.93f, 0.84f, 1f);

            var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(cardGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -30f);
            titleRect.sizeDelta = new Vector2(800f, 60f);
            var titleText = titleGO.GetComponent<Text>();
            titleText.font = GameFonts.Main;
            titleText.fontSize = 38;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.black;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "게임 방법";
            titleText.raycastTarget = false;

            var bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
            bodyGO.transform.SetParent(cardGO.transform, false);
            var bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -110f);
            bodyRect.sizeDelta = new Vector2(780f, 1140f);
            var bodyText = bodyGO.GetComponent<Text>();
            bodyText.font = GameFonts.Main;
            bodyText.fontSize = 26;
            bodyText.color = Color.black;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.lineSpacing = 1.15f;
            bodyText.text = TutorialText;
            bodyText.raycastTarget = false;

            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(200f, 64f);
            var closeImage = closeGO.GetComponent<Image>();
            closeImage.sprite = cardImage.sprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = new Color(0.3f, 0.28f, 0.24f, 1f);
            var closeButton = closeGO.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;

            var closeLabelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            closeLabelGO.transform.SetParent(closeGO.transform, false);
            var closeLabelRect = closeLabelGO.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            var closeLabelText = closeLabelGO.GetComponent<Text>();
            closeLabelText.font = GameFonts.Main;
            closeLabelText.fontSize = 26;
            closeLabelText.fontStyle = FontStyle.Bold;
            closeLabelText.color = Color.white;
            closeLabelText.alignment = TextAnchor.MiddleCenter;
            closeLabelText.text = "닫기";
            closeLabelText.raycastTarget = false;

            var controller = panelGO.AddComponent<TutorialPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panelGO;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddVoidPersistentListener(closeButton.onClick, controller.Close);

            return controller;
        }

        private static void BuildButton(Transform canvasT, TutorialPanelController controller)
        {
            var old = canvasT.Find("TutorialButton");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var buttonGO = new GameObject("TutorialButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(canvasT, false);
            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 60f); // FixBottomButtonsLayout이 x를 다시 계산
            rect.sizeDelta = new Vector2(220f, 80f);
            var image = buttonGO.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.98f, 0.97f, 0.92f, 1f);
            var button = buttonGO.GetComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddVoidPersistentListener(button.onClick, controller.Open);

            var labelGO = new GameObject("TutorialButtonLabel", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGO.GetComponent<Text>();
            labelText.font = GameFonts.Main;
            labelText.fontSize = 34;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "설명";
            labelText.raycastTarget = false;
        }
    }
}
#endif
