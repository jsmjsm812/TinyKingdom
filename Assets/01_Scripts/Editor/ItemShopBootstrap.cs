#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 배틀 씬 "아이템" 탭(지금까지 "아이템 기능은 준비 중입니다" 플레이스홀더였던 ItemPanel)을
    /// 출전 한정 버프/디버프 상점으로 채운다. 상점 카드처럼 리롤이 도는 구조가 아니라, 4종
    /// 아이템이 항상 고정으로 떠 있고 살 때마다 즉시 효과가 적용된다(같은 아이템도 최대
    /// RunItemEffects.MaxStackPerItem개까지 중복 구매 가능). 로비의 아이템 상점(같은
    /// ItemData·같은 프리팹)과 지갑(RunSession.Gold)을 공유한다.
    /// </summary>
    public static class ItemShopBootstrap
    {
        private const string BattlePath = "Assets/06_Scenes/Battle.unity";
        private const string ItemDataDir = "Assets/02_Data/Items";
        private const string ItemPrefabPath = "Assets/05_Prefabs/UI/ItemSlot.prefab";

        // tint(아이콘 색)는 전부 흰색(원본 그대로) — 처음엔 아이콘이 색깔 원 placeholder라
        // 효과별로 구분하려고 곱연산 색을 입혔었는데, 지금은 실제 아이콘 아트가 있어서 색을
        // 곱히면 원본 색이 달라져 보인다("이미지 색 원본이랑 똑같게" 피드백). EnsureItemAsset이
        // 매번 이 값으로 덮어쓰므로 여기서 흰색으로 고정해야 Build Item Shop을 다시 돌려도
        // 색이 안 틀어진다. accent(카드 배경색)는 그 옛날 아이콘 색으로 재사용 — "카드가 다
        // 똑같아서 구별이 안 간다" 피드백에 맞춰 아이콘이 아니라 배경에 옅게 입힌다.
        private static readonly (string name, string desc, ItemEffectType type, float value, int cost, Color tint, Color accent)[] Items =
        {
            ("공격력 강화", "이번 출전 동안\n모든 수호자 공격력 +25%",
                ItemEffectType.GuardianAttackPowerBuff, 0.25f, 50, Color.white, new Color(0.9f, 0.3f, 0.25f)),
            ("속공의 물약", "이번 출전 동안\n모든 수호자 공격속도 +25%",
                ItemEffectType.GuardianAttackSpeedBuff, 0.25f, 50, Color.white, new Color(0.95f, 0.8f, 0.2f)),
            ("축복의 유물", "이번 출전 동안\n모든 수호자 등급 일시 +1성",
                ItemEffectType.GuardianStarLevelBuff, 1f, 150, Color.white, new Color(0.65f, 0.35f, 0.9f)),
            ("둔화의 저주", "이번 출전 동안\n침입자 이동속도 -20%",
                ItemEffectType.InvaderSlowDebuff, 0.2f, 60, Color.white, new Color(0.3f, 0.55f, 0.95f)),
        };

        [MenuItem("TinyKingdom/Build Item Shop")]
        public static void BuildItemShop()
        {
            // 이 메뉴는 ItemPanel을 통째로 지우고 다시 짓는다 — 배틀 씬을 손으로 옮겨놓고
            // 아직 저장 안 했으면 그 수정이 통째로 날아간다.
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(BattlePath)) return;

            EnsureFolder(ItemDataDir);
            var itemAssets = new ItemData[Items.Length];
            for (int i = 0; i < Items.Length; i++)
                itemAssets[i] = EnsureItemAsset(Items[i]);

            // 레이아웃을 고칠 때마다 반영되도록 항상 새로 짓는다 — 씬 쪽 인스턴스도 이 메서드
            // 시작부에서 어차피 매번 지우고 다시 놓으므로, 프리팹만 예전 걸 남겨두면 간격 수정이
            // 안 먹는 불일치가 생긴다.
            AssetDatabase.DeleteAsset(ItemPrefabPath);
            var prefab = EnsureItemSlotPrefab();

            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);
            var itemPanelGO = ShopRosterUIBootstrap.FindInScene(scene, "ItemPanel");
            var canvasGO = ShopRosterUIBootstrap.FindInScene(scene, "Canvas");
            if (itemPanelGO == null || canvasGO == null)
            {
                Debug.LogError("[ItemShopBootstrap] ItemPanel/Canvas를 못 찾았습니다 — 먼저 상점 UI를 지어야 합니다.");
                return;
            }

            // 예전 "준비 중입니다" 플레이스홀더 라벨과 예전 슬롯들을 지우고 새로 짠다(재실행해도 안전).
            for (int i = itemPanelGO.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(itemPanelGO.transform.GetChild(i).gameObject);

            float cardWidth = CompactProfile.CardWidth;
            float cardHeight = CompactProfile.CardHeight;
            const float gap = 14f;
            // 카드 4장을 콘텐츠 영역(패널폭-탭폭) 안에서 그냥 가운데 정렬하면 왼쪽 여백이 겨우
            // 40밖에 안 남아서, 카드 안 텍스트가 살짝만 넘쳐도 왼쪽 탭 버튼(소환상점/설정)에
            // 가려 잘려 보였다("공격력 강화는 여전히 잘려있다" 반복 피드백) — 왼쪽 여백을
            // 넉넉하게 고정으로 주고 남는 공간만 오른쪽에 몰아준다(좌우 대칭 안 맞아도 안전이 우선).
            const float leftMargin = 70f;
            float totalWidth = itemAssets.Length * cardWidth + (itemAssets.Length - 1) * gap;
            float contentLeftEdge = -ShopRosterUIBootstrap.PanelWidth / 2f + ShopRosterUIBootstrap.TabStripWidth;
            float startX = contentLeftEdge + leftMargin + cardWidth / 2f;
            float cardY = -(ShopRosterUIBootstrap.PanelHeight - cardHeight) / 2f;

            var slotViews = new ItemSlotView[itemAssets.Length];
            for (int i = 0; i < itemAssets.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, itemPanelGO.transform);
                instance.name = $"ItemSlot_{i}";
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(startX + i * (cardWidth + gap), cardY);
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);

                var view = instance.GetComponent<ItemSlotView>();
                view.EditorAssignItem(itemAssets[i]);
                var so = new SerializedObject(view);
                BakeItemDisplay(so, itemAssets[i]);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                slotViews[i] = view;
            }

            var controllerGO = new GameObject("ItemPanelController", typeof(ItemPanelController));
            controllerGO.transform.SetParent(itemPanelGO.transform, false);
            var controllerSO = new SerializedObject(controllerGO.GetComponent<ItemPanelController>());
            // panel 필드는 비워둔다 — 배틀씬에서는 BattleTabController가 ItemPanel 자체를
            // SetActive로 토글하므로 이 컨트롤러는 OnEnable에서 슬롯만 새로고침하면 된다.
            var slotsProp = controllerSO.FindProperty("slots");
            slotsProp.arraySize = slotViews.Length;
            for (int i = 0; i < slotViews.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ItemShopBootstrap] 아이템 상점 4종을 ItemPanel에 배치했습니다.");
        }

        /// <summary>
        /// 슬롯 인스턴스의 아이콘/이름/설명/가격을 에디터 스크립트 시점에 바로 채워 넣는다.
        /// 원래는 ItemSlotView.Init()이 런타임(Play)에 알아서 채우는 구조였는데, 로비 팝업처럼
        /// 부모 활성화 경로가 복잡한 씬에서 그 호출이 왜 안 먹는지 원인을 못 찾아서(직접 재현
        /// 못 함) — 씬 저장 시점에 이미 정답을 박아 넣어 런타임 로직이 실패하더라도 최소한
        /// 화면엔 항상 맞는 내용이 보이도록 이중 안전장치를 걸었다.
        /// </summary>
        internal static void BakeItemDisplay(SerializedObject slotSO, ItemData item)
        {
            if (slotSO.FindProperty("cardBackground").objectReferenceValue is Image bg)
                bg.color = Color.Lerp(Color.white, item.cardAccent, 0.6f);
            if (slotSO.FindProperty("iconImage").objectReferenceValue is Image icon)
            {
                icon.sprite = item.icon;
                icon.color = item.iconTint;
            }
            if (slotSO.FindProperty("nameText").objectReferenceValue is Text nameText)
                nameText.text = item.itemName;
            if (slotSO.FindProperty("descText").objectReferenceValue is Text descText)
                descText.text = item.description;
            if (slotSO.FindProperty("costText").objectReferenceValue is Text costText)
                costText.text = $"{item.cost}G (0/{RunItemEffects.MaxStackPerItem})";
        }

        private static ItemData EnsureItemAsset((string name, string desc, ItemEffectType type, float value, int cost, Color tint, Color accent) def)
        {
            string path = $"{ItemDataDir}/{def.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(existing, path);
            }

            existing.itemName = def.name;
            existing.description = def.desc;
            existing.effectType = def.type;
            existing.effectValue = def.value;
            existing.cost = def.cost;
            existing.iconTint = def.tint;
            existing.cardAccent = def.accent;
            if (existing.icon == null)
                existing.icon = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>카드 내부 세로 배치를 결정하는 크기 값 묶음 — 배틀 씬(좁은 240높이 패널)과
        /// 로비 씬(화면 전체를 쓰는 팝업)이 서로 다른 크기의 카드를 원해서, 프리팹 자체를
        /// 둘로 나누고(ItemSlot.prefab / ItemSlotLarge.prefab) 이 값만 바꿔 찍어낸다.</summary>
        internal struct ItemSlotSizeProfile
        {
            public float CardWidth, CardHeight;
            public float TopMargin, Gap;
            public float IconSize, NameHeight, DescHeight, CostHeight;
            public int NameFontSize, DescFontSize, CostFontSize;
        }

        internal static readonly ItemSlotSizeProfile CompactProfile = new ItemSlotSizeProfile
        {
            // 200→185: 카드를 살짝 좁혀서 왼쪽 탭 버튼과의 여백을 더 벌릴 공간을 만들었다
            // (BuildItemShop의 leftMargin 참고).
            CardWidth = 185f, CardHeight = 195f,
            TopMargin = 6f, Gap = 3f,
            // 아이콘을 95까지 키웠더니 설명 칸이 34밖에 안 남아서 글자가 10pt까지 눌렸다 —
            // 아이콘을 80으로 살짝 줄이고 그만큼을 설명(48)에 돌려줘서 둘 다 읽을 만하게.
            IconSize = 80f, NameHeight = 20f, DescHeight = 48f, CostHeight = 22f,
            NameFontSize = 15, DescFontSize = 13, CostFontSize = 17,
        };

        private static GameObject EnsureItemSlotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath);
            if (existing != null) return existing;
            return BuildItemSlotPrefab(ItemPrefabPath, CompactProfile);
        }

        /// <summary>세로 배치: 위 여백 + 아이콘 + 간격 + 이름 + 간격 + 설명 + 간격 + 가격 +
        /// 아래여백 = 카드 높이가 되도록 y좌표를 계산해서 찍는다(감으로 찍으면 텍스트끼리
        /// 겹치거나 빈틈이 들쭉날쭉해지는 문제가 있었음 — 항상 이 계산을 거칠 것).</summary>
        internal static GameObject BuildItemSlotPrefab(string prefabPath, ItemSlotSizeProfile p)
        {
            Font font = GameFonts.Main;
            var root = new GameObject("ItemSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemSlotView));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(p.CardWidth, p.CardHeight);
            var cardImage = root.GetComponent<Image>();
            cardImage.color = new Color(0.16f, 0.15f, 0.2f);
            ShopRosterUIBootstrap.ApplyRounded(cardImage);
            ShopRosterUIBootstrap.ApplyButtonColors(root.GetComponent<Button>(), cardImage.color);

            float iconTop = -p.TopMargin;
            float iconY = iconTop - p.IconSize / 2f;
            float nameTop = iconTop - p.IconSize - p.Gap;
            float nameY = nameTop - p.NameHeight / 2f;
            float descTop = nameTop - p.NameHeight - p.Gap;
            float descY = descTop - p.DescHeight / 2f;
            float costTop = descTop - p.DescHeight - p.Gap;
            float costY = costTop - p.CostHeight / 2f;
            float labelWidth = p.CardWidth - 20f;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(root.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, iconY);
            iconRect.sizeDelta = new Vector2(p.IconSize, p.IconSize);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            iconImage.preserveAspect = true; // 실제 아이콘 아트가 정사각형이 아니어도 안 찌그러지게

            var nameText = ShopRosterUIBootstrap.AddLabel(root.transform, "아이템", font, p.NameFontSize,
                new Vector2(0f, nameY), new Vector2(labelWidth, p.NameHeight));
            nameText.fontStyle = FontStyle.Bold;
            // 이름이 좁은 카드 폭(특히 배틀 패널)보다 길면 Overflow 기본값 때문에 옆으로
            // 삐져나가 첫 번째 카드는 왼쪽 탭 버튼에 가려 잘려 보였다 — Best Fit으로 항상
            // 박스 안에 들어오게 한다(가격 텍스트와 같은 이유).
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 10;
            nameText.resizeTextMaxSize = p.NameFontSize;

            var descText = ShopRosterUIBootstrap.AddLabel(root.transform, "설명", font, p.DescFontSize,
                new Vector2(0f, descY), new Vector2(labelWidth, p.DescHeight));
            descText.color = new Color(0.25f, 0.22f, 0.18f);
            // 설명은 이름/가격과 달리 지금까지 Wrap이 빠져 있었다 — "이번 출전 동안 / 모든
            // 수호자 XXX" 같은 긴 줄이 박스 밖으로 삐져나가, 왼쪽 탭 버튼과 가장 가까운 첫 번째
            // 카드에서만 그 옆으로 넘친 부분이 탭 버튼에 가려 잘려 보였다. Best Fit은 일부러 안
            // 쓴다 — 아이템마다 설명 길이가 달라서 카드마다 다른 크기로 보여 "카드 크기가 다
            // 르다"는 착시(실제 카드는 다 200x195로 동일)를 만들었다. 고정 크기 + Wrap이면
            // 모든 카드가 항상 같은 글자 크기로 보인다.
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var costText = ShopRosterUIBootstrap.AddLabel(root.transform, "0", font, p.CostFontSize,
                new Vector2(0f, costY), new Vector2(labelWidth, p.CostHeight));
            costText.color = new Color(1f, 0.85f, 0.3f);
            costText.fontStyle = FontStyle.Bold;
            // "150G (0/3)"처럼 긴 문자열이 좁은 카드(배틀 패널)에서 AddLabel 기본값인
            // Overflow 때문에 박스 밖으로(특히 옆 카드/좌측 탭 쪽으로) 삐져나가 잘려 보이던
            // 문제 — 이 라벨만 Wrap으로 바꿔서 항상 박스 안에 접히게 한다.
            costText.horizontalOverflow = HorizontalWrapMode.Wrap;
            costText.resizeTextForBestFit = true;
            costText.resizeTextMinSize = 10;
            costText.resizeTextMaxSize = p.CostFontSize;

            var view = root.GetComponent<ItemSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("cardBackground").objectReferenceValue = cardImage;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("descText").objectReferenceValue = descText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("buyButton").objectReferenceValue = root.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(root.GetComponent<Button>().onClick, view.OnClickBuy);

            EnsureFolder("Assets/05_Prefabs/UI");
            AssetDatabase.DeleteAsset(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// 씬 레이아웃(패널/버튼 위치 — 사용자가 로비에서 직접 손댔을 수 있음)은 절대 안 건드리고,
        /// 프리팹 두 개(아이콘 크기 등)만 "같은 자산 그대로" 내부만 고치고(GUID 유지 — 삭제 후
        /// 재생성하면 이미 배치된 인스턴스와 프리팹 연결이 끊어짐) 이미 배치된 카드들의 아이콘
        /// 색만 최신 ItemData 기준으로 바로잡는다. "이미지 색이 원본과 다르다" / "이미지가 너무
        /// 작다" 피드백을 로비 쪽 사람 손댄 배치는 안 건드리면서 고치기 위한 안전한 경로.
        /// </summary>
        [MenuItem("TinyKingdom/Refresh Item Icons (Safe, No Layout Changes)")]
        public static void RefreshItemIconsSafe()
        {
            UpdateItemSlotPrefabLayout(ItemPrefabPath, CompactProfile);
            UpdateItemSlotPrefabLayout(LobbyItemShopBootstrap.LargeItemPrefabPath, LobbyItemShopBootstrap.LargeProfile);

            RefreshIconsInScene(BattlePath);
            RefreshIconsInScene("Assets/06_Scenes/Home.unity");

            AssetDatabase.SaveAssets();
            Debug.Log("[ItemShopBootstrap] 프리팹 갱신 + 씬 내 아이콘 색 보정 완료(레이아웃은 안 건드림).");
        }

        /// <summary>기존 프리팹 자산(같은 GUID)을 그대로 열어서 내부 자식들의 크기/위치/글자
        /// 크기만 새 프로필 값으로 고쳐 다시 저장한다 — 삭제 후 BuildItemSlotPrefab으로 새로
        /// 만들면 GUID가 바뀌어서 이미 씬에 배치된 인스턴스들이 프리팹과의 연결을 잃는다.</summary>
        private static void UpdateItemSlotPrefabLayout(string prefabPath, ItemSlotSizeProfile p)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                // 프리팹이 아직 없으면(첫 실행) 새로 만드는 수밖에 없다 — 이 경우는 씬에도
                // 아직 아무 인스턴스가 없을 것이므로 GUID가 새로 생겨도 문제없음.
                BuildItemSlotPrefab(prefabPath, p);
                return;
            }

            var view = root.GetComponent<ItemSlotView>();
            var so = new SerializedObject(view);
            var iconImage = so.FindProperty("iconImage").objectReferenceValue as Image;
            var nameText = so.FindProperty("nameText").objectReferenceValue as Text;
            var descText = so.FindProperty("descText").objectReferenceValue as Text;
            var costText = so.FindProperty("costText").objectReferenceValue as Text;

            // cardBackground는 나중에 추가된 필드라 예전 프리팹엔 안 물려 있을 수 있다 —
            // 비어있으면 루트 자신의 Image로 지금 물려준다. SerializedObject로 쓴 값은
            // ApplyModifiedProperties를 호출해야 실제 컴포넌트에 반영된다(아래 다른 필드들은
            // 컴포넌트 참조를 직접 수정하는 방식이라 이 호출이 필요 없었음).
            var cardBgProp = so.FindProperty("cardBackground");
            if (cardBgProp.objectReferenceValue == null)
                cardBgProp.objectReferenceValue = root.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(p.CardWidth, p.CardHeight);

            float iconTop = -p.TopMargin;
            float iconY = iconTop - p.IconSize / 2f;
            float nameTop = iconTop - p.IconSize - p.Gap;
            float nameY = nameTop - p.NameHeight / 2f;
            float descTop = nameTop - p.NameHeight - p.Gap;
            float descY = descTop - p.DescHeight / 2f;
            float costTop = descTop - p.DescHeight - p.Gap;
            float costY = costTop - p.CostHeight / 2f;
            float labelWidth = p.CardWidth - 20f;

            if (iconImage != null)
            {
                iconImage.rectTransform.anchoredPosition = new Vector2(0f, iconY);
                iconImage.rectTransform.sizeDelta = new Vector2(p.IconSize, p.IconSize);
                iconImage.preserveAspect = true;
            }
            if (nameText != null)
            {
                nameText.rectTransform.anchoredPosition = new Vector2(0f, nameY);
                nameText.rectTransform.sizeDelta = new Vector2(labelWidth, p.NameHeight);
                // 이 값들을 매번 다시 강제해야 한다 — 예전 프리팹이 Best Fit 없이 만들어졌던
                // 시절 자산이면 이 갱신 경로에서라도 확실히 켜줘야 "긴 이름이 옆 카드/탭 쪽으로
                // 삐져나가 잘려 보이는" 문제가 재발하지 않는다.
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 8;
                nameText.resizeTextMaxSize = p.NameFontSize;
            }
            if (descText != null)
            {
                descText.rectTransform.anchoredPosition = new Vector2(0f, descY);
                descText.rectTransform.sizeDelta = new Vector2(labelWidth, p.DescHeight);
                descText.horizontalOverflow = HorizontalWrapMode.Wrap;
                // 설명은 Best Fit을 끈다 — 아이템마다 설명 길이가 달라 카드마다 다른 크기로
                // 보이는("카드 크기가 다르다"는 착시) 원인이었다. 고정 크기로 통일.
                descText.resizeTextForBestFit = false;
                descText.fontSize = p.DescFontSize;
                descText.color = new Color(0.25f, 0.22f, 0.18f);
            }
            if (costText != null)
            {
                costText.rectTransform.anchoredPosition = new Vector2(0f, costY);
                costText.rectTransform.sizeDelta = new Vector2(labelWidth, p.CostHeight);
                costText.horizontalOverflow = HorizontalWrapMode.Wrap;
                costText.resizeTextForBestFit = true;
                costText.resizeTextMinSize = 8;
                costText.resizeTextMaxSize = p.CostFontSize;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void RefreshIconsInScene(string scenePath)
        {
            if (!ShopRosterUIBootstrap.CanSafelyOpenScene(scenePath)) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var views = Object.FindObjectsOfType<ItemSlotView>(true);
            int changed = 0;
            foreach (var view in views)
            {
                var so = new SerializedObject(view);
                var item = so.FindProperty("item").objectReferenceValue as ItemData;
                var iconImage = so.FindProperty("iconImage").objectReferenceValue as Image;
                var cardBackground = so.FindProperty("cardBackground").objectReferenceValue as Image;
                if (item == null || iconImage == null) continue;

                iconImage.sprite = item.icon;
                iconImage.color = item.iconTint;
                EditorUtility.SetDirty(iconImage);
                if (cardBackground != null)
                {
                    cardBackground.color = Color.Lerp(Color.white, item.cardAccent, 0.6f);
                    EditorUtility.SetDirty(cardBackground);
                }
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[ItemShopBootstrap] {scenePath}: 아이콘 {changed}개 색 보정.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
