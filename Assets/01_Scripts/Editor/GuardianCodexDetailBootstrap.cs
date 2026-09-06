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
    /// 용사 도감 칸을 누르면 그 용사의 스탯(사거리·공속·공격방식·공격력·특수효과)을 보여주는
    /// 상세 팝업을 만들고, 도감 칸 10개 전부에 클릭 리스너를 배선한다.
    /// </summary>
    public static class GuardianCodexDetailBootstrap
    {
        private const string HomePath = "Assets/06_Scenes/Home.unity";

        [MenuItem("TinyKingdom/Add Guardian Codex Detail Popup")]
        public static void AddGuardianCodexDetailPopup()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            // GuardianCodexPanel은 평소엔 꺼져있는(m_IsActive: 0) 모달 패널이라 GameObject.Find는
            // 못 찾는다(비활성 오브젝트는 건너뜀) — 컴포넌트 타입으로 비활성 포함 검색한다.
            var controller = Object.FindObjectOfType<GuardianCodexController>(true);
            if (controller == null)
            {
                Debug.LogError("[GuardianCodexDetailBootstrap] GuardianCodexController를 못 찾았습니다.");
                return;
            }
            var panelGO = controller.gameObject;

            var detail = BuildDetailPanel(panelGO.transform, controller);
            WireControllerDetailFields(controller, detail);
            WireEntryButtons(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GuardianCodexDetailBootstrap] 도감 상세 팝업 생성 + 칸 10개 클릭 배선 완료.");
        }

        private class DetailRefs
        {
            public GameObject Root;
            public Image Portrait;
            public Text Name;
            public Text Rarity;
            public Text Stats;
            public Text Special;
        }

        private static DetailRefs BuildDetailPanel(Transform panelT, GuardianCodexController controller)
        {
            var old = panelT.Find("DetailPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 어두운 배경 — 탭하면 닫히고, 카드보다 뒤에서 전체 화면을 덮어 포커스를 잡아준다.
            var backdropGO = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropGO.transform.SetParent(panelT, false);
            backdropGO.transform.SetAsLastSibling();
            var backdropRect = backdropGO.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImage = backdropGO.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.55f);
            var backdropButton = backdropGO.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            UnityEventTools.AddVoidPersistentListener(backdropButton.onClick, controller.CloseDetail);
            backdropGO.SetActive(false);

            // 카드 — 배경 그림이 뭐든 항상 읽히도록 밝은 크림색 불투명 패널 + 검은 글씨.
            // 크기를 실제 콘텐츠(초상화+이름+등급+스탯4줄+특수효과)에 맞춰 꽉 채운다 —
            // 예전 780 높이는 아래쪽에 빈 여백이 많이 남아 "텍스트에 비해 크다"는 피드백이 있었음.
            const float cardW = 600f;
            const float cardH = 660f;
            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(backdropGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(cardW, cardH);
            var cardImage = cardGO.GetComponent<Image>();
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.97f, 0.93f, 0.84f, 1f);

            var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(cardGO.transform, false);
            var portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(0f, 220f);
            portraitRect.sizeDelta = new Vector2(190f, 190f);
            var portraitImage = portraitGO.GetComponent<Image>();
            portraitImage.preserveAspect = true;

            var nameText = CreateText(cardGO.transform, "NameText", new Vector2(0f, 100f), new Vector2(520f, 48f), 34, Color.black, FontStyle.Bold);
            var rarityText = CreateText(cardGO.transform, "RarityText", new Vector2(0f, 58f), new Vector2(520f, 32f), 22, new Color(0.75f, 0.55f, 0.08f, 1f), FontStyle.Bold);
            var statsText = CreateText(cardGO.transform, "StatsText", new Vector2(0f, -50f), new Vector2(520f, 160f), 24, Color.black, FontStyle.Normal);
            statsText.alignment = TextAnchor.UpperLeft;
            var specialText = CreateText(cardGO.transform, "SpecialText", new Vector2(0f, -195f), new Vector2(520f, 110f), 20, Color.black, FontStyle.Normal);
            specialText.alignment = TextAnchor.UpperLeft;

            // 닫기 버튼 — 카드 오른쪽 가장자리로부터의 가로 여백과 위쪽 가장자리로부터의 세로
            // 여백을 똑같이 맞춰서(20,20) 좌우 간격이 안 맞아 보이던 걸 고쳤다.
            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -20f);
            closeRect.sizeDelta = new Vector2(96f, 52f);
            var closeImage = closeGO.GetComponent<Image>();
            closeImage.sprite = cardImage.sprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = new Color(0.3f, 0.28f, 0.24f, 1f);
            var closeButton = closeGO.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;
            UnityEventTools.AddVoidPersistentListener(closeButton.onClick, controller.CloseDetail);

            var closeLabel = CreateText(closeGO.transform, "Label", Vector2.zero, new Vector2(96f, 52f), 24, Color.white, FontStyle.Bold);
            closeLabel.text = "닫기";
            closeLabel.raycastTarget = false;

            return new DetailRefs
            {
                Root = backdropGO,
                Portrait = portraitImage,
                Name = nameText,
                Rarity = rarityText,
                Stats = statsText,
                Special = specialText
            };
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            // 프로젝트 전역 픽셀 폰트(DNF BitBit)로 통일 — GameFonts.Main 참고.
            var text = go.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void WireControllerDetailFields(GuardianCodexController controller, DetailRefs refs)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("detailPanel").objectReferenceValue = refs.Root;
            so.FindProperty("detailPortrait").objectReferenceValue = refs.Portrait;
            so.FindProperty("detailName").objectReferenceValue = refs.Name;
            so.FindProperty("detailRarity").objectReferenceValue = refs.Rarity;
            so.FindProperty("detailStats").objectReferenceValue = refs.Stats;
            so.FindProperty("detailSpecial").objectReferenceValue = refs.Special;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // entryPortraits 배열을 그대로 따라가며(등록 순서 = registry.allGuardians 순서) 각
        // Portrait의 부모(=CodexEntry 칸 루트)에 Button을 달고 그 인덱스로 OnEntryClicked를
        // 호출하게 배선한다 — 오브젝트 그래프를 그대로 따라가므로 fileID를 손으로 맞출 필요가 없다.
        private static void WireEntryButtons(GuardianCodexController controller)
        {
            var so = new SerializedObject(controller);
            var portraits = so.FindProperty("entryPortraits");

            for (int i = 0; i < portraits.arraySize; i++)
            {
                var portraitImage = portraits.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                if (portraitImage == null)
                {
                    Debug.LogWarning($"[GuardianCodexDetailBootstrap] entryPortraits[{i}]가 비어있습니다.");
                    continue;
                }

                GameObject entryGO = portraitImage.transform.parent.gameObject;
                var entryImage = entryGO.GetComponent<Image>();
                var button = entryGO.GetComponent<Button>();
                if (button == null) button = entryGO.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = entryImage;

                // 재실행 시 중복 리스너가 쌓이지 않도록 기존 persistent 리스너를 지우고 다시 건다.
                for (int p = button.onClick.GetPersistentEventCount() - 1; p >= 0; p--)
                    UnityEventTools.RemovePersistentListener(button.onClick, p);
                UnityEventTools.AddIntPersistentListener(button.onClick, controller.OnEntryClicked, i);
            }
        }
    }
}
#endif
