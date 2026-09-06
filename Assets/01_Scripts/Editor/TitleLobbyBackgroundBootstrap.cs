#if UNITY_EDITOR
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
    /// 타이틀/로비(홈) 화면에 픽셀아트 배경 일러스트를 깔고, 로비 배경 속 탁자 지도에 그려진
    /// 3개 구역 아이콘 위에 구역 카드와 연결된 은은한 빛(MapGlow)을 겹쳐 놓는다. 카드를 누르면
    /// 해당 구역 아이콘이 반짝여서 "이 카드 = 지도의 저 지역"이라는 게 시각적으로 이어지게 한다.
    /// </summary>
    public static class TitleLobbyBackgroundBootstrap
    {
        private const string TitlePath = "Assets/06_Scenes/Title.unity";
        private const string HomePath = "Assets/06_Scenes/Home.unity";

        private const string TitleBackgroundPath = "Assets/03_Art/Sprites/TitleBackground.png";
        private const string LobbyBackgroundPath = "Assets/03_Art/Sprites/LobbyBackground.png";
        private const string MapGlowPath = "Assets/03_Art/Sprites/MapGlow.png";

        // 로비 배경 그림(768x1376 원본, 1080x1920 캔버스에 꽉 채워 늘림) 속 탁자 지도 위 3개
        // 아이콘의 중심 좌표 — 그림을 파이썬으로 크롭해서 픽셀 좌표를 실측한 뒤, 캔버스 기준
        // anchoredPosition(중앙 원점, Y-up)으로 환산한 값이다. 배경 그림을 다시 그리면 이 좌표도
        // 다시 맞춰야 한다.
        private static readonly Vector2 Zone1GlowPos = new Vector2(-132f, -454f); // 초원 아이콘
        private static readonly Vector2 Zone2GlowPos = new Vector2(11f, -447f);   // 폐허 탑(성) 아이콘
        private static readonly Vector2 Zone3GlowPos = new Vector2(159f, -454f);  // 화산 아이콘
        private const float GlowSize = 170f;

        // 세로 목록 카드 3장 — 원래 방식(전체 폭 바)으로 되돌리되, 로고와 지도 사이 공간에만
        // 들어가도록 높이를 줄이고 위로 당겨서 아래 지도(원탁)를 가리지 않게 한다.
        private const float CardWidth = 720f;
        private const float CardHeight = 170f;
        private const float CardGap = 20f;
        private const float TopCardY = 460f; // 로고 아래 첫 카드 중심 Y

        // 주의: 이 메뉴는 더 이상 LayoutZoneCardsAboveMap()을 부르지 않는다 — 그 함수는 카드의
        // NameText/StateText를 매번 완전히 새로 만드는 파괴적 동작이라, 사용자가 씬 뷰에서 직접
        // 손본 카드가 있으면 그 작업이 다 날아간다. 구역 카드를 처음부터 다시 배치해야 할 때만
        // "TinyKingdom/Layout Zone Cards Above Map"을 따로 실행할 것 — 손으로 수정한 게 있다면
        // 절대 누르지 말 것.
        [MenuItem("TinyKingdom/Add Title and Lobby Backgrounds")]
        public static void AddTitleAndLobbyBackgrounds()
        {
            AddTitleBackground();
            AddLobbyBackgroundAndMapGlow();
            FixBottomButtonsLayout();
            Debug.Log("[TitleLobbyBackgroundBootstrap] 타이틀/로비 배경 + 지도 하이라이트 + 하단 버튼 배치 완료(구역 카드 레이아웃은 별도 메뉴).");
        }

        // 구역 카드는 사용자가 씬 뷰에서 직접 다듬은 상태를 그대로 두고, 위치/크기/텍스트는
        // 절대 건드리지 않는다 — 그림자만 얹어서 입체감만 살짝 더한다(순수 추가, 파괴적이지 않음).
        [MenuItem("TinyKingdom/Add Zone Card Shadow Polish")]
        public static void AddZoneCardShadowPolish()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            AddPanelShadow("ZoneCard_1");
            AddPanelShadow("ZoneCard_2");
            AddPanelShadow("ZoneCard_3");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TitleLobbyBackgroundBootstrap] 구역 카드에 그림자 추가 완료(위치/텍스트는 그대로 유지).");
        }

        // 화면 맨 위에 "로비"라는 화면 제목을 큰 글씨로 추가한다 — 순수 추가만 하고 기존
        // 오브젝트(사용자가 씬 뷰에서 직접 손본 구역 카드 등)는 전혀 건드리지 않는다.
        [MenuItem("TinyKingdom/Add Lobby Title Label")]
        public static void AddLobbyTitleLabel()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TitleLobbyBackgroundBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            var old = canvasGO.transform.Find("LobbyTitleLabel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 글자 배경 패널 — 배경 그림이 뭐든 항상 또렷하게 보이도록 살짝 어두운 판을 깐다.
            var titleGO = new GameObject("LobbyTitleLabel", typeof(RectTransform), typeof(Image));
            titleGO.transform.SetParent(canvasGO.transform, false);
            titleGO.transform.SetAsLastSibling(); // 다른 UI보다 위에 보이도록 맨 앞으로

            var rect = titleGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(320f, 84f);

            var panelImage = titleGO.GetComponent<Image>();
            panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.1f, 0.08f, 0.06f, 0.75f);

            // 폰트 크기 64는 DNF BitBit(픽셀 폰트)가 미리 구워둔 범위를 벗어나서 글리프가 아예 안
            // 그려졌던 것으로 보인다(이 프로젝트에서 지금까지 성공적으로 쓴 크기는 전부 44 이하) —
            // 검증된 범위 안(44)으로 낮추고, 대신 배경 패널로 존재감을 키운다.
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(titleGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = 44;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "로비";
            text.raycastTarget = false;

            var shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TitleLobbyBackgroundBootstrap] 로비 제목 라벨 추가 완료.");
        }

        private static void AddPanelShadow(string cardName)
        {
            var cardGO = GameObject.Find(cardName);
            if (cardGO == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}을 못 찾았습니다.");
                return;
            }

            var shadow = cardGO.GetComponent<Shadow>();
            if (shadow == null) shadow = cardGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        // 도감/설정 버튼이 화면 중앙 기준으로 짝이 안 맞게(-240 / 0) 떨어져 있어서 전체가 왼쪽으로
        // 쏠려 보였다("좌우 간격 다 맞춰" 피드백) — 두 버튼을 화면 중앙 기준 대칭으로 재배치하고,
        // 흰 글씨(원래 배경이 단색 버튼이라 괜찮았지만 지금은 라벨이 검게 보여야 통일감 있음) 라벨도
        // 검은색으로 맞춘다.
        private const float BottomButtonGap = 24f;
        private const float BottomButtonWidth = 220f;

        [MenuItem("TinyKingdom/Fix Bottom Buttons Layout")]
        public static void FixBottomButtonsLayout()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            // 도감/설정 2개일 땐 ±half로 대칭이었는데, 튜토리얼(설명) 버튼이 추가되면서 3개가
            // 됐다 — 가운데(설정)를 0에 두고 양옆에 하나씩 step만큼 띄운다. TutorialButton이
            // 아직 없는 씬(튜토리얼 패널을 먼저 안 만든 경우)에서도 안전하게 건너뛴다.
            float step = BottomButtonWidth + BottomButtonGap;
            bool hasTutorial = GameObject.Find("TutorialButton") != null;
            if (hasTutorial)
            {
                CenterBottomButton("GuardianCodexButton", -step, "GuardianCodexButtonLabel");
                CenterBottomButton("SettingsButton", 0f, "SettingsButtonLabel");
                CenterBottomButton("TutorialButton", step, "TutorialButtonLabel");
            }
            else
            {
                float half = BottomButtonWidth / 2f + BottomButtonGap / 2f;
                CenterBottomButton("GuardianCodexButton", -half, "GuardianCodexButtonLabel");
                CenterBottomButton("SettingsButton", half, "SettingsButtonLabel");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TitleLobbyBackgroundBootstrap] 하단 버튼 좌우 대칭 배치 + 검은 글씨 적용 완료.");
        }

        private static void CenterBottomButton(string buttonName, float x, string labelName)
        {
            var buttonGO = GameObject.Find(buttonName);
            if (buttonGO == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {buttonName}을 못 찾았습니다.");
                return;
            }

            var rect = buttonGO.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            pos.x = x;
            rect.anchoredPosition = pos;

            // 둥근 모서리 패널 + 옅은 그림자로 눌러볼 만한 버튼 느낌을 준다(예전 스킨은 톤이
            // 섞여 라벨이 묻힐 수 있어 피함).
            var buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                buttonImage.type = Image.Type.Sliced;
                buttonImage.color = new Color(0.98f, 0.97f, 0.92f, 1f);
            }
            var buttonShadow = buttonGO.GetComponent<Shadow>();
            if (buttonShadow == null) buttonShadow = buttonGO.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            buttonShadow.effectDistance = new Vector2(0f, -3f);

            var labelGO = GameObject.Find(labelName);
            var labelText = labelGO?.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.color = Color.black;
                labelText.fontStyle = FontStyle.Bold;
            }
        }

        [MenuItem("TinyKingdom/Add Title Background Only")]
        public static void AddTitleBackground()
        {
            var scene = EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TitleLobbyBackgroundBootstrap] Title 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            Sprite sprite = EnsureIllustrationSprite(TitleBackgroundPath);
            if (sprite == null) return;

            CreateFullScreenBackground(canvasGO.transform, sprite);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("TinyKingdom/Add Lobby Background Only")]
        public static void AddLobbyBackgroundAndMapGlow()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TitleLobbyBackgroundBootstrap] Home 씬에서 Canvas를 못 찾았습니다.");
                return;
            }

            Sprite bgSprite = EnsureIllustrationSprite(LobbyBackgroundPath);
            Sprite glowSprite = EnsureGlowSprite(MapGlowPath);
            if (bgSprite == null || glowSprite == null) return;

            CreateFullScreenBackground(canvasGO.transform, bgSprite);

            var zone1Glow = CreateMapGlow(canvasGO.transform, glowSprite, "MapGlow_Zone1", Zone1GlowPos);
            var zone2Glow = CreateMapGlow(canvasGO.transform, glowSprite, "MapGlow_Zone2", Zone2GlowPos);
            var zone3Glow = CreateMapGlow(canvasGO.transform, glowSprite, "MapGlow_Zone3", Zone3GlowPos);

            WireZoneCardGlow("ZoneCard_1", zone1Glow, zone2Glow, zone3Glow);
            WireZoneCardGlow("ZoneCard_2", zone1Glow, zone2Glow, zone3Glow);
            WireZoneCardGlow("ZoneCard_3", zone1Glow, zone2Glow, zone3Glow);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // 전체 폭 세로 목록 바 3장 — 높이를 줄이고 로고 바로 아래로 당겨서 아래쪽 지도(원탁)를
        // 안 가리게 배치한다. 이름(흰색 굵게, 그림자 있음)/상태(짙은 금색) 텍스트는 매번 완전히
        // 새로 만들어서, 여러 차례 수정하는 동안 한쪽만 원인 불명으로 안 보이던 문제가 남아있을
        // 여지를 없앤다.
        [MenuItem("TinyKingdom/Layout Zone Cards Above Map")]
        public static void LayoutZoneCardsAboveMap()
        {
            var scene = EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);

            LayoutOneCard("ZoneCard_1");
            LayoutOneCard("ZoneCard_2");
            LayoutOneCard("ZoneCard_3");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TitleLobbyBackgroundBootstrap] 구역 카드를 지도 위쪽 공간에 재배치 완료.");
        }

        private static void LayoutOneCard(string cardName)
        {
            var cardGO = GameObject.Find(cardName);
            if (cardGO == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}을 못 찾았습니다.");
                return;
            }

            var view = cardGO.GetComponent<ZoneButtonView>();
            var so = new SerializedObject(view);
            var zoneData = so.FindProperty("zone").objectReferenceValue as ZoneData;
            if (zoneData == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}에 연결된 ZoneData가 없습니다.");
                return;
            }

            // zoneIndex(1~3) 순서 그대로 위에서부터 쌓는다 — 이름 순서가 아니라 실제 구역 순서로
            // 정렬해서, 씬에서 카드 오브젝트를 어떤 순서로 만들었든 항상 1→2→3구역이 위→아래로.
            float y = zoneData.zoneIndex switch
            {
                1 => TopCardY,
                2 => TopCardY - (CardHeight + CardGap),
                3 => TopCardY - (CardHeight + CardGap) * 2f,
                _ => 0f
            };

            // 이전에 지도 아이콘 히트박스용으로 만들었던 Label 서브패널이 남아있으면 정리.
            var oldLabel = cardGO.transform.Find("Label");
            if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);

            var rootRect = cardGO.GetComponent<RectTransform>();
            rootRect.anchoredPosition = new Vector2(0f, y);
            rootRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            // 사용자가 마음에 들어했던 모습(이름 흰색 굵게 위, 상태 금색 아래)을 재현하되,
            // 원래 쓰던 픽셀 UI 스킨(9-slice, 부분적으로 다른 톤이 섞여 있어 텍스트가 묻힐 수
            // 있었던 원인으로 의심됨) 대신 균일한 밝은 패널 + 이름 텍스트에 그림자를 넣는
            // 방식으로 만든다 — 어떤 배경이든 흰 글씨가 항상 또렷하게 보이도록 보장된다.
            var rootImage = cardGO.GetComponent<Image>();
            rootImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            rootImage.type = Image.Type.Sliced;
            rootImage.color = Color.white;

            // NameText/StateText를 기존 오브젝트를 재활용하지 않고 완전히 새로 만든다 — 여러 번
            // 고쳐 쓰는 동안 한쪽만 원인 불명으로 안 보이는 문제가 있었어서, 남아있을지 모를
            // 이상한 상태를 깔끔하게 걷어내고 처음부터 다시 굽는 쪽이 안전하다.
            var oldName = cardGO.transform.Find("NameText");
            if (oldName != null) Object.DestroyImmediate(oldName.gameObject);
            var oldState = cardGO.transform.Find("StateText");
            if (oldState != null) Object.DestroyImmediate(oldState.gameObject);

            var nameT = CreateCardText(cardGO.transform, "NameText", new Vector2(0f, 38f),
                new Vector2(CardWidth - 40f, 56f), 40, Color.black, FontStyle.Bold);

            // 상태 텍스트 초기 색은 ZoneButtonView.OnEnable이 해금 여부에 따라 매번 다시
            // 칠한다(출전 가능=초록, 잠김=회색) — 여기 값은 에디터 미리보기용 기본값일 뿐.
            var stateT = CreateCardText(cardGO.transform, "StateText", new Vector2(0f, -36f),
                new Vector2(CardWidth - 40f, 42f), 28, new Color(0.15f, 0.55f, 0.25f, 1f), FontStyle.Bold);

            so.FindProperty("nameText").objectReferenceValue = nameT;
            so.FindProperty("stateText").objectReferenceValue = stateT;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateCardText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, int fontSize, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = GameFonts.Main;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // 캔버스 전체(1080x1920)를 꽉 채우는 배경 Image를 첫 번째 자식으로 깐다(다른 UI보다
        // 항상 뒤에 그려지게). AddFieldBackground(ShopRosterUIBootstrap)와 같은 방식: Simple +
        // preserveAspect 끔 — 원본 비율이 거의 같아 늘려도 왜곡이 거의 안 보인다.
        private static void CreateFullScreenBackground(Transform canvasT, Sprite sprite)
        {
            var old = canvasT.Find("Background");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvasT, false);
            bgGO.transform.SetAsFirstSibling();

            var rect = bgGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = bgGO.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        // 구역 카드를 누르면 잠깐 켜지는 지도 위 하이라이트. 기본은 꺼진 채로 시작(카드 쪽
        // ZoneButtonView.OnEnable에서도 다시 한 번 꺼주지만, 씬 저장 시점에도 꺼둔 상태로
        // 남기는 게 안전).
        private static GameObject CreateMapGlow(Transform canvasT, Sprite sprite, string name, Vector2 pos)
        {
            var old = canvasT.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var glowGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            glowGO.transform.SetParent(canvasT, false);
            // Background 바로 위, 나머지 UI(카드 등)보다는 아래에 그려지게 두 번째 자리에 둔다.
            glowGO.transform.SetSiblingIndex(1);

            var rect = glowGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(GlowSize, GlowSize);

            var image = glowGO.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;

            glowGO.SetActive(false);
            return glowGO;
        }

        // ZoneCard_N의 ZoneButtonView가 들고 있는 zone(ZoneData)의 zoneIndex를 읽어서, 이름
        // 순서에 기대지 않고 실제 어느 구역인지로 올바른 MapGlow를 골라 연결한다.
        private static void WireZoneCardGlow(string cardName, GameObject zone1Glow, GameObject zone2Glow, GameObject zone3Glow)
        {
            var cardGO = GameObject.Find(cardName);
            if (cardGO == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}을 못 찾았습니다.");
                return;
            }

            var view = cardGO.GetComponent<ZoneButtonView>();
            if (view == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}에 ZoneButtonView가 없습니다.");
                return;
            }

            var so = new SerializedObject(view);
            var zoneProp = so.FindProperty("zone");
            var zoneData = zoneProp.objectReferenceValue as ZoneData;
            if (zoneData == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}에 연결된 ZoneData가 없습니다.");
                return;
            }

            // ZoneData.zoneIndex는 1부터 시작한다(GameProgress.IsZoneUnlocked가 "1구역은 항상
            // 해금"을 zoneIndex<=1로 판정) — 0-based로 착각해서 한 칸씩 밀렸던 걸 바로잡음.
            GameObject glow = zoneData.zoneIndex switch
            {
                1 => zone1Glow,
                2 => zone2Glow,
                3 => zone3Glow,
                _ => null
            };
            if (glow == null)
            {
                Debug.LogWarning($"[TitleLobbyBackgroundBootstrap] {cardName}의 zoneIndex({zoneData.zoneIndex})에 대응하는 MapGlow가 없습니다.");
                return;
            }

            so.FindProperty("mapGlow").objectReferenceValue = glow;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite EnsureIllustrationSprite(string path)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[TitleLobbyBackgroundBootstrap] {path}를 못 찾았습니다 — 파일이 실제로 있는지 확인 필요.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[TitleLobbyBackgroundBootstrap] {path} 재임포트는 됐는데 Sprite 로드에 실패했습니다.");
            return sprite;
        }

        // MapGlow.png는 파이썬으로 실제 알파 채널(방사형 그라데이션)을 계산해서 구운 PNG라
        // alphaIsTransparency를 켜서 투명 경계의 색 번짐을 막는다.
        private static Sprite EnsureGlowSprite(string path)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[TitleLobbyBackgroundBootstrap] {path}를 못 찾았습니다.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[TitleLobbyBackgroundBootstrap] {path} 재임포트 후 Sprite 로드 실패.");
            return sprite;
        }
    }
}
#endif
