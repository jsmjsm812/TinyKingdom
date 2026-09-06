#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 로비에도 아이템 상점을 만든다 — 배틀씬 아이템 탭과 같은 카탈로그(같은 ItemData 4종)와
    /// 같은 지갑(RunSession.Gold)을 쓰지만, 카드 자체는 배틀의 비좁은 240높이 패널용
    /// (ItemSlot.prefab)이 아니라 화면 전체를 쓰는 별도의 큰 프리팹(ItemSlotLarge.prefab)을
    /// 새로 찍어서 쓴다 — "로비 아이템 UI가 너무 작다" 피드백으로 카드 자체 크기를 키움.
    /// </summary>
    public static class LobbyItemShopBootstrap
    {
        private const string HomePath = "Assets/06_Scenes/Home.unity";
        private const string ItemDataDir = "Assets/02_Data/Items";
        internal const string LargeItemPrefabPath = "Assets/05_Prefabs/UI/ItemSlotLarge.prefab";
        private static readonly string[] ItemNames = { "공격력 강화", "속공의 물약", "축복의 유물", "둔화의 저주" };

        internal static readonly ItemShopBootstrap.ItemSlotSizeProfile LargeProfile = new ItemShopBootstrap.ItemSlotSizeProfile
        {
            CardWidth = 440f, CardHeight = 320f,
            TopMargin = 14f, Gap = 6f,
            // 배틀 패널과 같은 이유로 아이콘을 살짝 줄이고(160→145) 설명 칸을 넓혀서(56→68)
            // 설명 글자를 더 키웠다.
            IconSize = 145f, NameHeight = 28f, DescHeight = 68f, CostHeight = 34f,
            NameFontSize = 22, DescFontSize = 17, CostFontSize = 24,
        };

        [MenuItem("TinyKingdom/Add Lobby Item Shop")]
        public static void AddLobbyItemShop()
        {
            // 이 메뉴는 LobbyGoldText/LobbyItemShopButton/LobbyItemPanel을 통째로 지우고 다시
            // 짓는다 — 로비 씬을 손으로 옮겨놓고 아직 저장 안 했으면 그 수정이 통째로 날아간다.
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(HomePath)) return;

            var itemAssets = new ItemData[ItemNames.Length];
            for (int i = 0; i < ItemNames.Length; i++)
                itemAssets[i] = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemDataDir}/{ItemNames[i]}.asset");
            if (System.Array.IndexOf(itemAssets, null) >= 0)
            {
                Debug.LogError("[LobbyItemShopBootstrap] 아이템 데이터를 못 찾았습니다 — 먼저 " +
                                "'TinyKingdom > Build Item Shop'(배틀씬)을 한 번 실행해서 아이템 4종을 만드세요.");
                return;
            }

            // 로비 전용 큰 카드 프리팹 — 재실행할 때마다 최신 크기로 새로 찍는다.
            var prefab = ItemShopBootstrap.BuildItemSlotPrefab(LargeItemPrefabPath, LargeProfile);

            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[LobbyItemShopBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            // 재실행해도 안전하게 예전 것들을 지우고 새로 짠다.
            foreach (var name in new[] { "LobbyGoldText", "LobbyItemShopButton", "LobbyItemPanel" })
            {
                var old = canvasGO.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            Font font = GameFonts.Main;

            // "로비" 제목 라벨(LobbyTitleLabel, 화면 맨 위 anchoredPosition y=0, 높이 216)과
            // 안 겹치게 그 아래(y=-250)에 금화 표시 + 아이템 상점 버튼을 나란히 배치한다.
            const float rowY = -250f;
            BuildGoldText(canvasGO.transform, font, rowY);
            var controller = BuildPanel(canvasGO.transform, font, prefab, itemAssets);
            BuildOpenButton(canvasGO.transform, font, controller, rowY);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LobbyItemShopBootstrap] 로비 아이템 상점 + 금화 표시를 추가했습니다(카드 확대 + 위치 조정).");
        }

        private static void BuildGoldText(Transform canvasT, Font font, float rowY)
        {
            var go = new GameObject("LobbyGoldText", typeof(RectTransform), typeof(Text), typeof(GoldDisplayText));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, rowY);
            rect.sizeDelta = new Vector2(320f, 84f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.85f, 0.3f);
            text.alignment = TextAnchor.MiddleRight;
            text.raycastTarget = false;
            // GoldDisplayText.OnEnable()이 실제 값을 채우는 건 Play 모드에서만 실행되니,
            // 에디터 씬 화면(Play 전)에서도 뭔가 보이도록 시작값을 미리 박아둔다 — 안 그러면
            // 빈 문자열이라 "로비에 금화가 안 보인다"로 보였음(실제로는 플레이하면 정상 표시됨).
            text.text = $"금화 {RunSession.StartingGold}";

            var display = go.GetComponent<GoldDisplayText>();
            var so = new SerializedObject(display);
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildOpenButton(Transform canvasT, Font font, ItemPanelController controller, float rowY)
        {
            var go = new GameObject("LobbyItemShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, rowY);
            rect.sizeDelta = new Vector2(360f, 84f);
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.16f, 0.32f, 0.4f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddVoidPersistentListener(button.onClick, controller.Open);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = label.GetComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 32;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "아이템 상점";
            labelText.raycastTarget = false;
        }

        private static ItemPanelController BuildPanel(Transform canvasT, Font font, GameObject slotPrefab, ItemData[] itemAssets)
        {
            var panelGO = new GameObject("LobbyItemPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasT, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var backdrop = panelGO.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.65f);
            panelGO.SetActive(false);

            const float cardW = 1000f, cardH = 960f;
            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelGO.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(cardW, cardH);
            var cardImage = cardGO.GetComponent<Image>();
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.97f, 0.93f, 0.84f, 1f);

            var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(cardGO.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);
            titleRect.sizeDelta = new Vector2(760f, 64f);
            var titleText = titleGO.GetComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.black;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "아이템 상점";
            titleText.raycastTarget = false;

            var subLabel = ShopRosterUIBootstrap.AddLabel(cardGO.transform,
                "이번 출전 한정 버프/디버프 — 같은 아이템은 3개까지 중복 구매 가능",
                font, 22, new Vector2(0f, -112f), new Vector2(880f, 36f));
            subLabel.color = new Color(0.35f, 0.32f, 0.28f);

            // 2x2 격자 — 카드 4개를 한 줄로 늘어놓기엔 폭이 좁아서(배틀 패널과 달리 화면
            // 전체가 아니라 카드 하나 안에서 배치해야 함) 세로 여유를 살려 2x2로 배치한다.
            float cellW = LargeProfile.CardWidth, cellH = LargeProfile.CardHeight;
            const float gridGap = 30f;
            float gridTop = -170f;
            float gridWidth = cellW * 2 + gridGap;
            float startX = -gridWidth / 2f + cellW / 2f;

            var slotViews = new ItemSlotView[itemAssets.Length];
            for (int i = 0; i < itemAssets.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, cardGO.transform);
                instance.name = $"ItemSlot_{i}";
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(
                    startX + col * (cellW + gridGap),
                    gridTop - row * (cellH + gridGap));
                rect.sizeDelta = new Vector2(cellW, cellH);

                var view = instance.GetComponent<ItemSlotView>();
                view.EditorAssignItem(itemAssets[i]);
                var so = new SerializedObject(view);
                ItemShopBootstrap.BakeItemDisplay(so, itemAssets[i]);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                slotViews[i] = view;
            }

            var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-24f, -24f);
            closeRect.sizeDelta = new Vector2(110f, 60f);
            var closeImage = closeGO.GetComponent<Image>();
            closeImage.sprite = cardImage.sprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = new Color(0.3f, 0.28f, 0.24f, 1f);
            var closeButton = closeGO.GetComponent<Button>();
            closeButton.targetGraphic = closeImage;

            var closeLabel = ShopRosterUIBootstrap.AddLabel(closeGO.transform, "닫기", font, 26);
            closeLabel.fontStyle = FontStyle.Bold;

            var controllerGO = new GameObject("ItemPanelController", typeof(ItemPanelController));
            controllerGO.transform.SetParent(panelGO.transform, false);
            var controller = controllerGO.GetComponent<ItemPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panel").objectReferenceValue = panelGO;
            var slotsProp = controllerSO.FindProperty("slots");
            slotsProp.arraySize = slotViews.Length;
            for (int i = 0; i < slotViews.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddVoidPersistentListener(closeButton.onClick, controller.Close);

            return controller;
        }
    }
}
#endif
