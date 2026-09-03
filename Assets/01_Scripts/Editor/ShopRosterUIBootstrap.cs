#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WitchHour.Combat;
using WitchHour.Core;
using WitchHour.Data;
using WitchHour.Field;
using WitchHour.Merge;
using WitchHour.Shop;
using WitchHour.UI;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// 2주차 소환 상점 UI를 Battle 씬에 배선한다. GuardianUnit 프리팹을 만들고, BattleManager에
    /// Shop/Merge/Placement 매니저를 추가하고, 상점 4칸 + 아이템 탭(자리만, 기능은 나중에) 을
    /// Canvas 위에 생성해서 전부 연결한다.
    /// </summary>
    public static class ShopRosterUIBootstrap
    {
        private const string BattlePath = "Assets/06_Scenes/Battle.unity";
        private const string PrefabDir = "Assets/05_Prefabs";

        // 왼쪽에 상점/아이템 세로 탭을 붙이는 만큼 폭을 예약해둔다. 아이템 탭은 지금은 자리만
        // 있고("준비 중" 플레이스홀더) 실제 아이템 시스템은 나중에 별도로 설계해서 채운다.
        private const float PanelWidth = 1080f;
        // 카드가 180→195로 커졌을 때 패널 높이(220)를 안 늘려서, 카드 아래 여백이 20px에서
        // 5px로 확 줄어 이름 텍스트가 패널/화면 가장자리에 거의 닿아 "잘린 것처럼" 보였다.
        // 카드(195) + 위 여백(20) + 아래 여백(20) = 235 이상 필요해서 240으로 키움.
        private const float PanelHeight = 240f;
        private const float TabStripWidth = 140f;
        private const float PanelContentCenterX = (-PanelWidth / 2f + TabStripWidth + PanelWidth / 2f) / 2f;

        // 화면 위쪽 HUD(성벽 HP 바/금화/웨이브)가 차지하는 높이 — FixBattleLayout이 필드를
        // 위로 붙일 때 이 만큼은 항상 비워둬야 겹치지 않는다.
        private const float HudHeight = 140f;

        /// <summary>
        /// 상점/명부 패널이 필드 하단(성벽 포함)을 그대로 덮어버리던 문제 수정.
        /// FieldConstants 등 게임플레이 수치는 전혀 안 건드리고, BattlefieldRoot를
        /// 시각적으로만 축소·상단 정렬해서 화면 하단에 UI 전용 공간을 비워준다.
        /// (사거리/이동속도 등은 로컬 좌표 기준이라 시각적 스케일과 무관하게 그대로 동작함)
        /// </summary>
        [MenuItem("WitchHour/Fix Battle Layout (Field vs UI Overlap)")]
        public static void FixBattleLayout()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var fieldRootGO = GameObject.Find("BattlefieldRoot");
            var gridManagerGO = GameObject.Find("GridManager");
            if (fieldRootGO == null || gridManagerGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] BattlefieldRoot 또는 GridManager를 못 찾았습니다.");
                return;
            }

            // GridManager는 런타임 스크립트라 UnityEditor API(둥근 스프라이트)를 직접 못 써서,
            // 여기(에디터 전용 코드)서 대신 만들어 slotSprite로 주입해준다 — RebuildGrid보다 먼저 해야
            // 새로 생기는 슬롯들이 처음부터 이 스프라이트를 쓴다.
            var gridManager = gridManagerGO.GetComponent<GridManager>();
            var gridManagerSO = new SerializedObject(gridManager);
            gridManagerSO.FindProperty("slotSprite").objectReferenceValue = LoadPixelKitSprite("UI_Slot_Selected.png");
            gridManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // 씬에 저장된 슬롯은 예전 FieldConstants 값으로 만들어진 것일 수 있으니, 레이아웃을
            // 계산하기 전에 항상 최신 상수로 슬롯을 다시 생성한다(안 그러면 계산과 실제가 어긋남).
            gridManager.RebuildGrid();

            const float trueCanvasHalfHeight = 960f; // CanvasScaler 기준해상도 1920의 절반
            const float topPadding = 10f;
            const float reservedUiHeight = 240f; // ShopPanel/ItemPanel 높이(하단 고정, PanelHeight와 맞춰야 함)
            const float bottomPadding = 10f;

            // ㄹ자 선반/낙하 슬롯 전체가 차지하는 세로 범위(필드 로컬 좌표계, 스케일 적용 전).
            float slotHalfSize = FieldConstants.UnitSize / 2f;
            float contentTopY = FieldConstants.ShelfY.Max() + FieldConstants.SlotOffsetFromPath + slotHalfSize;
            float contentBottomY = FieldConstants.ShelfY.Min() - FieldConstants.SlotOffsetFromPath - slotHalfSize;

            float topTarget = trueCanvasHalfHeight - HudHeight - topPadding;
            float bottomTarget = -trueCanvasHalfHeight + reservedUiHeight + bottomPadding;

            float scale = (topTarget - bottomTarget) / (contentTopY - contentBottomY);
            float shiftUp = topTarget - contentTopY * scale;

            var rect = fieldRootGO.GetComponent<RectTransform>();
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = new Vector2(0f, shiftUp);

            // ShopPanel/ItemPanel은 이미 씬에 지어져 있으면 BuildShopPanel이 다시 안 건드리는데
            // (유저가 손으로 옮긴 배치를 보존하려고), PanelHeight 상수 자체가 바뀌었을 때는 그
            // "이미 지어짐" 가드 때문에 반영이 안 된다 — 여기서 패널 크기만 최신 PanelHeight로
            // 맞춰준다(카드 4장/리롤 버튼은 패널 위쪽 기준으로 붙어있어서 패널이 커지면 같이
            // 위로 밀려 올라가 아래쪽 여백이 자연히 생긴다).
            int panelsResized = 0;
            foreach (var panelName in new[] { "ShopPanel", "ItemPanel", "PanelShadow" })
            {
                // ItemPanel은 평소 꺼져있어서(SetActive(false)) GameObject.Find로는 못 찾는다.
                var panelGO = FindInScene(scene, panelName);
                if (panelGO == null) continue;
                var panelRect = panelGO.GetComponent<RectTransform>();
                if (panelRect.sizeDelta != new Vector2(PanelWidth, PanelHeight))
                {
                    panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                    panelsResized++;
                }
            }

            // 리롤/배속/탭 버튼 크기는 이제 유저가 씬에서 직접 손으로 맞춘 값이라(자동 크기
            // 강제 적용 피드백 이후) 이 메서드가 더 이상 안 건드린다 — 재실행해도 유지됨.

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ShopRosterUIBootstrap] 필드를 {scale:P0} 크기로 축소하고 위로 {shiftUp}px 올려서 하단 UI와 안 겹치게 했습니다. " +
                      $"패널 {panelsResized}개 높이를 {PanelHeight}로 맞췄습니다.");
        }

        // 유저가 직접 그려준 필드 배경 일러스트(초원+숲 테두리+풍차+깃발). 반복 타일이 아니라
        // 화면 전체를 한 장으로 채우는 단일 구성이라 Tiled가 아니라 Simple로 늘려서 쓴다.
        // 원본 768x1376 vs 화면 1080x1920 — 비율이 이미 거의 같아서(0.558 vs 0.5625) 늘려도 티 안 남.
        private const string FieldBackgroundIllustrationPath = "Assets/03_Art/Sprites/FieldBackground.png";
        // TinyKingdom TowerDefense 패키지의 Road.png(2016x1512, 자체 프로모 스크린샷과 같은
        // 그림체)에서 실측한 흙길 색 — 예전 (0.82,0.76,0.53)은 손으로 대충 고른 값이라
        // 새로 까는 테두리 장식(PathBorderTrim)의 흙 색과 이가 안 맞았다.
        private static readonly Color PathColor = new Color(0.851f, 0.725f, 0.4f);

        // 침입자가 지나가는 통로 시각적 폭 — 슬롯 배치 간격(FieldConstants)과는 무관하게 순수
        // 눈에 보이는 "길" 너비만 결정한다. 슬롯 박스 밑으로 살짝 깔려도 슬롯이 위에 그려져서
        // 안 보이니 슬롯 간격에 딱 맞출 필요는 없고, "길이 너무 좁다" 피드백 반영해서 넉넉하게 잡음.
        private const float PathWidth = 260f;

        // "길을 자연스럽고 이쁘게 만들고 싶다"는 요청으로 Road.png에서 파이썬(Pillow+scipy)으로
        // 알파를 뽑아 잘라온 장식들 — 자세한 추출 과정은 대화 기록 참고, 요약하면:
        // 1) 흙(R>G) vs 풀(G>=R) 픽셀을 색으로 분류하고, 2) 흙까지의 거리(distance_transform_edt)가
        // 가까운 풀 픽셀만 남기고 나머지는 투명하게(테두리 폭만큼 feather) — 그러면 "길 가장자리에
        // 붙은 삐죽삐죽한 덤불 테두리"만 도려낸 장식 조각이 나온다.
        //   PathBorderTrim: 길 위쪽 가장자리(풀 테두리+살짝의 흙)를 그대로 잘라온 것. 아래쪽
        //     가장자리엔 위아래로 뒤집어서, 세로 구간의 좌/우 가장자리엔 90도 돌려서 재사용한다.
        private const string PathBorderTrimPath = "Assets/03_Art/Sprites/PathBorderTrim.png";
        // 세로 구간(낙하 통로)용 — 런타임에 90도 Z회전으로 돌려 썼더니 안 보이는 버그가 있어서
        // (원인 불명 — Tiled+회전 조합 문제로 추정), 파이썬에서 미리 90도 돌려놓은 별도 텍스처를
        // 만들어 회전 없이 그대로 쓰는 쪽으로 바꿨다. 풀이 오른쪽을 향하게 돌려놨고, 왼쪽
        // 가장자리는 localScale.x=-1로 좌우만 뒤집어서 재사용한다.
        private const string PathBorderTrimVerticalPath = "Assets/03_Art/Sprites/PathBorderTrimVertical.png";
        private const float PathTrimHeight = 95f; // PathBorderTrim.png 원본 세로(95)와 동일하게 — Tiled 세로축은 안 늘리고 그대로 씀

        // 필드 배경 일러스트 전용 — 손그림 톤이라 Point(픽셀 각짐)보다 Bilinear로 부드럽게 늘리는
        // 쪽이 자연스럽다.
        private static Sprite EnsureIllustrationSprite(string path)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[ShopRosterUIBootstrap] {path}를 못 찾았습니다 — 파일이 실제로 있는지, .meta가 생겼는지 확인 필요.");
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
                Debug.LogError($"[ShopRosterUIBootstrap] {path} 재임포트는 됐는데 Sprite 로드에 실패했습니다 — textureType={importer.textureType}.");
            return sprite;
        }

        // Road.png에서 잘라온 장식 PNG 2종 전용 임포트 — 이미 파이썬에서 실제 알파 채널을 계산해
        // 구웠으니(EnsureIllustrationSprite와 달리 알파를 따로 계산할 필요 없음) 그대로 Single
        // 스프라이트로만 잡아준다. alphaIsTransparency를 켜서 투명 경계의 색 번짐을 막는다.
        private static Sprite EnsurePathDecorSprite(string path)
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[ShopRosterUIBootstrap] {path}를 못 찾았습니다.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[ShopRosterUIBootstrap] {path} 재임포트 후 Sprite 로드 실패.");
            return sprite;
        }

        // 통로 5구간(LanePath.Waypoints 순서대로) 각각에 단색 + 둥근 모서리 Image를 깐다.
        // 전부 축이 딱 맞는 가로/세로 직선 구간이라 회전 없이 폭/높이만 바꿔서 채우면 된다.
        // 그 위에 Road.png에서 뽑아온 덤불 테두리 장식을 양쪽 가장자리에 얹고, 꺾이는 지점마다
        // 둥근 덤불 장식을 하나씩 놓아 "평판한 사각형" 느낌을 없앤다.
        private static void BuildLanePathVisual(Transform battlefieldRootT)
        {
            var old = battlefieldRootT.Find("FieldPathVisual");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var containerGO = new GameObject("FieldPathVisual", typeof(RectTransform));
            containerGO.transform.SetParent(battlefieldRootT, false);
            containerGO.transform.SetSiblingIndex(1); // 배경(잔디) 바로 위, 슬롯/유닛보다는 아래

            Sprite trimSprite = EnsurePathDecorSprite(PathBorderTrimPath);
            Sprite trimVerticalSprite = EnsurePathDecorSprite(PathBorderTrimVerticalPath);

            var waypoints = LanePath.Waypoints;
            const float overlap = PathWidth / 2f;

            // 1단계: 채우기(단색 사각형)부터 전부 먼저 깐다. 테두리 장식을 같은 루프 안에서
            // 세그먼트별로 번갈아 지으면, 뒤쪽 세그먼트의 채우기가 형제 순서상 나중에 그려져서
            // 앞쪽 세그먼트가 이미 깔아둔 테두리 장식(특히 세로 구간)을 코너 겹침 구간에서
            // 위에서 덮어버리는 문제가 있었다 — 채우기를 모두 먼저, 장식은 전부 나중에 그려서
            // 항상 장식이 채우기보다 위에 오게 순서를 고정한다.
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                bool horizontal = Mathf.Abs(a.y - b.y) < 0.01f;
                float length = Vector2.Distance(a, b);
                Vector2 center = (a + b) / 2f;

                var segGO = new GameObject($"PathSegment_{i}", typeof(RectTransform), typeof(Image));
                segGO.transform.SetParent(containerGO.transform, false);
                var segRect = segGO.GetComponent<RectTransform>();
                segRect.anchorMin = segRect.anchorMax = new Vector2(0.5f, 0.5f);
                segRect.anchoredPosition = center;
                // 길이 쪽으로는 살짝 넉넉하게 늘려서 꺾이는 모서리끼리 서로 안 겹치는 부분(빈틈)이
                // 안 생기게 한다 — 세로/가로 구간이 딱 waypoint에서 맞닿기만 하면 둥근 모서리 때문에
                // 코너에 틈이 보일 수 있음.
                segRect.sizeDelta = horizontal
                    ? new Vector2(length + overlap, PathWidth)
                    : new Vector2(PathWidth, length + overlap);

                var segImage = segGO.GetComponent<Image>();
                ApplyRounded(segImage);
                segImage.color = PathColor;
                segImage.raycastTarget = false;
            }

            // 2단계: 테두리 장식 — 채우기보다 항상 나중(=위)에 그려진다.
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[i + 1];
                bool horizontal = Mathf.Abs(a.y - b.y) < 0.01f;
                float length = Vector2.Distance(a, b);
                Vector2 center = (a + b) / 2f;
                // 채우기(overlap 포함)와 달리 테두리 장식은 구간의 "진짜" 길이"만큼만"도 아니고
                // 그보다 더 짧게(양쪽에서 PathWidth/2씩 더 빼고) 그린다. 정확히 구간 끝까지
                // 채웠더니, 반복(Tiled)되는 덤불 무늬가 원래 들쭉날쭉해서 어쩌다 잘리는 지점에
                // 마침 돌기(bump)가 걸리면 그게 꺾이는 지점 쪽으로 삐죽 튀어나온 것처럼 보였다
                // (실제로 그렇게 보고됨). 꺾이는 지점 주변을 넉넉히(PathWidth만큼) 비워두면
                // 그 구간은 100% 채우기(흙)만 보이고, 어떤 돌기가 어디서 잘리든 꺾이는 지점 근처엔
                // 얼씬도 못 한다.
                float trimLength = Mathf.Max(length - PathWidth, 0f);

                if (horizontal)
                {
                    if (trimSprite == null) continue;
                    // 위쪽 가장자리: 원본 그대로(풀이 위, 흙이 아래 = 바깥쪽을 향함).
                    BuildPathTrimH(containerGO.transform, trimSprite, center, trimLength,
                        PathWidth / 2f, flipY: false);
                    // 아래쪽 가장자리: 위아래로 뒤집어서 풀이 바깥(아래)을 향하게.
                    BuildPathTrimH(containerGO.transform, trimSprite, center, trimLength,
                        -PathWidth / 2f, flipY: true);
                }
                else
                {
                    if (trimVerticalSprite == null) continue;
                    // 오른쪽 가장자리: 미리 90도 돌려둔 텍스처를 그대로(풀이 오른쪽을 향함).
                    BuildPathTrimV(containerGO.transform, trimVerticalSprite, center, trimLength,
                        PathWidth / 2f, flipX: false);
                    // 왼쪽 가장자리: 좌우로 뒤집어서 풀이 왼쪽을 향하게.
                    BuildPathTrimV(containerGO.transform, trimVerticalSprite, center, trimLength,
                        -PathWidth / 2f, flipX: true);
                }
            }

            // 코너 전용 장식은 안 쓴다 — 이 경로의 채우기는 가로/세로 사각형 2장이 꺾이는 지점
            // 주변 정사각형 전체를 서로 완전히 겹쳐서(overlap=PathWidth/2=130) 채우기 때문에
            // 흙 없는 빈 모서리 자체가 존재하지 않는다. "풀이 두 변을 감싸는" 코너 장식은
            // 어떻게 돌리고 뒤집어도 이미 흙으로 꽉 찬 자리를 풀로 덮어써서 길을 침범하는
            // 것처럼 보일 수밖에 없었다(실제로 그렇게 됐었음). 테두리 장식은 위에서 구간의
            // 진짜 길이만큼만(overlap 없이) 그려서 꺾이는 지점 바로 앞에서 멈추므로, 그 자리는
            // 채우기(overlap 있음)의 갈색만 깔끔하게 남고 초록 장식은 아예 안 겹친다.
        }

        /// <summary>가로 구간 테두리 한 줄. yOffset은 구간 중심에서 위/아래 가장자리로 옮기는 오프셋.</summary>
        private static void BuildPathTrimH(Transform parent, Sprite sprite, Vector2 segCenter,
            float length, float yOffset, bool flipY)
        {
            var go = new GameObject("PathTrim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = segCenter + new Vector2(0f, yOffset);
            rect.sizeDelta = new Vector2(length, PathTrimHeight);
            rect.localScale = new Vector3(1f, flipY ? -1f : 1f, 1f);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.raycastTarget = false;
        }

        /// <summary>세로 구간 테두리 한 줄(미리 90도 돌려둔 텍스처를 그대로 씀 — 런타임 회전 없음).</summary>
        private static void BuildPathTrimV(Transform parent, Sprite sprite, Vector2 segCenter,
            float length, float xOffset, bool flipX)
        {
            var go = new GameObject("PathTrim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = segCenter + new Vector2(xOffset, 0f);
            rect.sizeDelta = new Vector2(PathTrimHeight, length);
            rect.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.raycastTarget = false;
        }

        /// <summary>
        /// BattlefieldRoot 맨 뒤에 TinyKingdom 색감의 잔디 배경 + 모래색 통로를 깐다.
        /// 배경은 화면을 꽉 채우도록 FixBattleLayout이 이미 적용한 실제 scale/이동값을 읽어서
        /// 정확히 화면 전체(1080x1920)를 덮는 크기로 역산한다 — 반드시 Fix Battle Layout을
        /// 먼저 실행한 뒤 써야 함.
        /// </summary>
        [MenuItem("WitchHour/Add Field Background (TinyKingdom)")]
        public static void AddFieldBackground()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var fieldRootGO = GameObject.Find("BattlefieldRoot");
            if (fieldRootGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] BattlefieldRoot를 못 찾았습니다.");
                return;
            }

            Sprite bgSprite = EnsureIllustrationSprite(FieldBackgroundIllustrationPath);
            if (bgSprite == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] 필드 배경 스프라이트 준비에 실패했습니다.");
                return;
            }

            var old = fieldRootGO.transform.Find("FieldBackground");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var bgGO = new GameObject("FieldBackground", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(fieldRootGO.transform, false);
            bgGO.transform.SetAsFirstSibling(); // 슬롯/유닛보다 뒤에 그려지게

            // FixBattleLayout이 BattlefieldRoot에 적용해둔 scale/anchoredPosition을 역산해서,
            // 이 배경만은 화면 가장자리 끝까지(1080x1920) 정확히 채우게 만든다.
            var fieldRect = fieldRootGO.GetComponent<RectTransform>();
            float scale = fieldRect.localScale.x;
            if (scale <= 0f) scale = 1f;
            float shiftUp = fieldRect.anchoredPosition.y;

            const float screenWidth = 1080f;
            const float screenHeight = 1920f;

            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0f, -shiftUp / scale);
            bgRect.sizeDelta = new Vector2(screenWidth / scale, screenHeight / scale);

            var bgImage = bgGO.GetComponent<Image>();
            bgImage.sprite = bgSprite;
            // 반복 타일이 아니라 화면 전체를 채우는 한 장짜리 일러스트라 Tiled가 아니라 Simple로
            // 늘려서 채운다 — preserveAspect도 꺼서 1080x1920을 정확히 꽉 채움(원본 비율이 이미
            // 거의 같아서 늘려도 왜곡이 거의 안 보임).
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            BuildLanePathVisual(fieldRootGO.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopRosterUIBootstrap] 필드 배경(직접 그린 초원 일러스트) + 통로(모래색) 적용 완료.");
        }

        /// <summary>
        /// 웨이브 클리어/성벽 함락 시 구역 해금 + 결과창을 띄우도록 배선한다.
        /// </summary>
        [MenuItem("WitchHour/Build Result Screen and Zone Unlock")]
        public static void BuildResultScreenAndZoneUnlock()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var battleManagerGO = GameObject.Find("BattleManager");
            var canvasGO = GameObject.Find("Canvas");
            if (battleManagerGO == null || canvasGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] BattleManager 또는 Canvas를 못 찾았습니다.");
                return;
            }

            var waveSpawner = battleManagerGO.GetComponent<WitchHour.Core.WaveSpawner>();
            var ward = battleManagerGO.GetComponent<WitchHour.Core.WardHealth>();
            var zone1 = AssetDatabase.LoadAssetAtPath<ZoneData>("Assets/02_Data/Zones/Zone1_FrontGate.asset");

            var flowController = GetOrAddComponent<WitchHour.Core.BattleFlowController>(battleManagerGO);
            var flowSO = new SerializedObject(flowController);
            flowSO.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            flowSO.FindProperty("ward").objectReferenceValue = ward;
            flowSO.FindProperty("currentZone").objectReferenceValue = zone1;
            flowSO.ApplyModifiedPropertiesWithoutUndo();

            var old = GameObject.Find("ResultPanel");
            if (old != null) Object.DestroyImmediate(old);

            Font font = GameFonts.Main;
            GameObject resultPanel = BuildResultPanel(canvasGO.transform, flowController, font);
            resultPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopRosterUIBootstrap] 결과창/구역 해금 배선 완료.");
        }

        private static GameObject BuildResultPanel(Transform canvasT, WitchHour.Core.BattleFlowController flowController, Font font)
        {
            var panel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            // 텍스트/버튼이 배경 위에 그냥 떠 있지 않고 카드 하나에 담기게 — 모달 다이얼로그 느낌.
            var cardShadowGO = new GameObject("CardShadow", typeof(RectTransform), typeof(Image));
            cardShadowGO.transform.SetParent(panel.transform, false);
            var cardShadowRect = cardShadowGO.GetComponent<RectTransform>();
            cardShadowRect.anchorMin = cardShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardShadowRect.anchoredPosition = new Vector2(8, -8);
            cardShadowRect.sizeDelta = new Vector2(800, 480);
            var cardShadowImage = cardShadowGO.GetComponent<Image>();
            cardShadowImage.color = new Color(0f, 0f, 0f, 0.4f);
            ApplyRounded(cardShadowImage);

            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panel.transform, false);
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(800, 480);
            var cardImage = cardGO.GetComponent<Image>();
            cardImage.color = new Color(0.12f, 0.11f, 0.16f, 0.98f);
            ApplyRounded(cardImage);

            var titleText = AddLabel(cardGO.transform, "", font, 58, new Vector2(0, -80), new Vector2(720, 140));
            titleText.color = Color.white;

            var confirmGO = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGO.transform.SetParent(cardGO.transform, false);
            var confirmRect = confirmGO.GetComponent<RectTransform>();
            confirmRect.anchorMin = confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(0, 50);
            confirmRect.sizeDelta = new Vector2(310, 100);
            var confirmImage = confirmGO.GetComponent<Image>();
            confirmImage.color = new Color(0.4f, 0.35f, 0.75f);
            ApplyRounded(confirmImage);
            ApplyButtonColors(confirmGO.GetComponent<Button>(), confirmImage.color);
            AddLabel(confirmGO.transform, "확인", font, 34);

            var controllerGO = new GameObject("ResultScreenController", typeof(ResultScreenController));
            controllerGO.transform.SetParent(panel.transform, false);
            var controller = controllerGO.GetComponent<ResultScreenController>();
            var so = new SerializedObject(controller);
            so.FindProperty("battleFlow").objectReferenceValue = flowController;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                confirmGO.GetComponent<Button>().onClick, controller.OnClickConfirm);

            return panel;
        }

        [MenuItem("WitchHour/Build Shop and Roster UI (Week 2)")]
        public static void BuildShopAndRosterUI()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var battleManagerGO = GameObject.Find("BattleManager");
            var canvasGO = GameObject.Find("Canvas");
            var gridManagerGO = GameObject.Find("GridManager");
            if (battleManagerGO == null || canvasGO == null || gridManagerGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] BattleManager, Canvas 또는 GridManager를 씬에서 못 찾았습니다. 먼저 Build Battle Scene을 실행했는지 확인하세요.");
                return;
            }

            // 예전엔 다시 실행할 때마다 이전 UI를 싹 지우고 새로 지었는데, 그러면 유저가 씬에서
            // 손으로 옮겨놓은 위치가 매번 초기화됐다. 이제 ShopPanel이 이미 있으면(=한 번 지어진
            // 적 있으면) 레이아웃 쪽은 건드리지 않고, 아래 로직 배선(참조 연결)만 다시 한다.
            // 레이아웃을 완전히 새로 짜고 싶으면 씬에서 ShopPanel 등을 직접 지우고 이 메뉴를 다시 실행.
            bool uiAlreadyBuilt = GameObject.Find("ShopPanel") != null;

            GuardianUnit guardianPrefab = BuildGuardianPrefab();

            var runCurrency = battleManagerGO.GetComponent<WitchHour.Core.RunCurrency>();

            var summonPool = GetOrAddComponent<SummonPool>(battleManagerGO);
            var mergeSystem = GetOrAddComponent<MergeSystem>(battleManagerGO);
            var shopManager = GetOrAddComponent<ShopManager>(battleManagerGO);
            var placementManager = GetOrAddComponent<GuardianPlacementManager>(battleManagerGO);

            // ★1(가장 흔한 등급) 4종을 전부 시작부터 풀어둔다 — 원래 루미·노아 둘뿐이라 상점에
            // 뽑히는 게 계속 겹쳐서(합성이 너무 쉽게 터짐) 밸런스가 물렀고, 선택지도 없어서 재미없단
            // 피드백 반영. 등급 게이팅(진행으로 푸는 재미)은 ★2/★3에만 남긴다(Zone1/2 보상 참고).
            string[] defaultUnlockedNames = { "Lumi", "Noa", "Mira", "Dori" };
            var summonPoolSO = new SerializedObject(summonPool);
            var defaultUnlockedProp = summonPoolSO.FindProperty("defaultUnlocked");
            defaultUnlockedProp.ClearArray();
            for (int i = 0; i < defaultUnlockedNames.Length; i++)
            {
                var guardian = AssetDatabase.LoadAssetAtPath<GuardianData>($"Assets/02_Data/Guardians/{defaultUnlockedNames[i]}.asset");
                defaultUnlockedProp.InsertArrayElementAtIndex(i);
                defaultUnlockedProp.GetArrayElementAtIndex(i).objectReferenceValue = guardian;
            }
            summonPoolSO.ApplyModifiedPropertiesWithoutUndo();

            var placementSO = new SerializedObject(placementManager);
            placementSO.FindProperty("guardianPrefab").objectReferenceValue = guardianPrefab;
            placementSO.FindProperty("gridManager").objectReferenceValue = gridManagerGO.GetComponent<GridManager>();
            placementSO.FindProperty("mergeSystem").objectReferenceValue = mergeSystem;
            placementSO.ApplyModifiedPropertiesWithoutUndo();

            var shopSO = new SerializedObject(shopManager);
            shopSO.FindProperty("summonPool").objectReferenceValue = summonPool;
            shopSO.FindProperty("currency").objectReferenceValue = runCurrency;
            shopSO.FindProperty("placementManager").objectReferenceValue = placementManager;
            shopSO.ApplyModifiedPropertiesWithoutUndo();

            Font font = GameFonts.Main;
            Transform canvasT = canvasGO.transform;
            var waveSpawner = battleManagerGO.GetComponent<WitchHour.Core.WaveSpawner>();

            if (uiAlreadyBuilt)
            {
                Debug.Log("[ShopRosterUIBootstrap] ShopPanel이 이미 있어서 레이아웃은 그대로 두고 " +
                          "매니저 참조 배선만 다시 했습니다. (레이아웃을 새로 짜려면 씬에서 " +
                          "ShopPanel/ItemPanel/HudUI 등을 직접 지우고 다시 실행하세요)");
            }
            else
            {
                BuildPanelShadow(canvasT, new Vector2(PanelWidth, PanelHeight), new Vector2(6, -6));
                GameObject shopPanel = BuildShopPanel(canvasT, shopManager, font);
                GameObject itemPanel = BuildItemPlaceholderPanel(canvasT, font);
                BuildSideTabs(canvasT, shopPanel, itemPanel, font);
                BuildPrepTimer(canvasT, waveSpawner, shopManager, font);
                BuildHud(canvasT, battleManagerGO.GetComponent<WitchHour.Core.WardHealth>(), runCurrency, waveSpawner, font);
                BuildSpeedToggle(canvasT, font);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[ShopRosterUIBootstrap] 소환 상점/명부 UI 배선 완료.");
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        // 씬에 있는 인스턴스를 그대로 프리팹 에셋으로 저장하고 연결한다(SaveAsPrefabAssetAndConnect) —
        // 위치는 지금 그대로 씬에 남고, 대신 유저가 프리팹을 더블클릭해서 독립적으로 열어 고치거나
        // Apply/Revert로 다루거나, 인스펙터에서 자유롭게 옮길 수 있게 된다.
        private static GameObject ConnectAsPrefab(GameObject instance, string prefabPath)
        {
            string dir = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/');
                string leaf = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, leaf);
            }
            return PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
        }

        private static Sprite _roundedSprite;

        // 패널/카드/버튼에 공통으로 쓰는 둥근 사각형 스프라이트 — 새 텍스처 없이 유니티 기본 UI
        // 리소스를 재활용한다. Image.type을 Sliced로 같이 설정해야 모서리가 실제로 둥글게 나온다.
        private static Sprite RoundedSprite()
        {
            if (_roundedSprite == null)
                _roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return _roundedSprite;
        }

        // Pixel_HUD_UI_FreeKit 스프라이트들 — WitchHour > Apply Pixel HUD Kit Skin이 최초 1회
        // 9-slice 보더까지 설정해둠(배경류), 여기선 그냥 경로로 로드만 한다. internal이라
        // SceneBootstrap(같은 네임스페이스, 침입자 체력바 쪽)에서도 재사용한다.
        internal const string PixelKitDir = "Assets/Pixel_HUD_UI_FreeKit/Sprites/UI Elements";
        internal static Sprite LoadPixelKitSprite(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelKitDir}/{fileName}");

        private static void ApplyRounded(Image image)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
        }

        // 패널 뒤에 살짝 어긋난 어두운 사각형을 깔아 떠 있는 느낌(깊이감)을 준다.
        private static void BuildPanelShadow(Transform canvasT, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var go = new GameObject("PanelShadow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.35f);
            ApplyRounded(image);
        }

        // 버튼 눌림/하이라이트 색을 명시적으로 지정 — 기본값 그대로 두면 눌러도 티가 잘 안 난다.
        // 알파는 그대로 두고 RGB만 스케일해야 반투명 색상의 투명도가 안 바뀐다.
        private static void ApplyButtonColors(Button button, Color baseColor)
        {
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = ScaleRgb(baseColor, 1.15f);
            colors.pressedColor = ScaleRgb(baseColor, 0.7f);
            colors.selectedColor = baseColor;
            colors.disabledColor = ScaleRgb(baseColor, 0.5f);
            button.colors = colors;
        }

        private static Color ScaleRgb(Color c, float factor)
        {
            return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor), Mathf.Clamp01(c.b * factor), c.a);
        }

        private static GuardianUnit BuildGuardianPrefab()
        {
            string path = $"{PrefabDir}/GuardianUnit.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                EnsureCanvasGroup(path);
                EnsureFrameAnimator(path);
                EnsureStarText(path);
                EnsureAttackText(path);
                // 크기는 여기서 더 이상 강제로 맞추지 않는다 — 유저가 프리팹을 직접 열어 크기를
                // 바꿔놨을 수 있으니, 이 메서드를 다시 실행해도 그 값을 덮어쓰지 않고 그대로 둔다.
                return existing.GetComponent<GuardianUnit>();
            }

            var go = new GameObject("GuardianUnit", typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
                typeof(SpriteFrameAnimator), typeof(GuardianUnit));
            // 300x300은 Setup() 전(에디터 프리뷰 등)에만 쓰이는 기본값 — 실제 크기는
            // GuardianUnit.UpdateSpriteSize()가 캐릭터마다 베이크된 스프라이트의 실제 픽셀
            // 크기(NativeToUiScale 배율)로 매번 다시 잡는다. pivot을 바닥(0.5,0)으로 둬야
            // 슬롯에 놓일 때(anchoredPosition=(0,0)) 캐릭터 발밑이 슬롯 중심에 정확히 선다 —
            // "슬롯이랑 캐릭터 위치가 안 맞다" 피드백의 원인은 여기(예전엔 중앙 pivot이라
            // 캐릭터마다 다른 여백 때문에 발 위치가 제각각으로 어긋나 보였음). 자세한 경위는
            // 대화 기록 참고, 스프라이트 자체도 파이썬으로 캐릭터별 알파 바운딩 박스에 맞춰
            // 여백 없이 다시 잘라냈다(Assets/03_Art/Sprites/Guardians/Battle).
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 300f);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            go.GetComponent<Image>().color = Color.white;
            go.GetComponent<Image>().preserveAspect = true;

            Font font = GameFonts.Main;
            // 별 배지는 캐릭터 머리 위(박스 위쪽 바깥)에 뜨도록. 스프라이트를 여백 없이
            // 빡빡하게 잘라낸 뒤로는 박스 위 가장자리가 곧 머리 꼭대기라, 25px 정도만
            // 띄우면 딱 "머리 위에 떠 있는" 느낌이 난다(예전 여백 있는 스프라이트 기준으론
            // 35px, 그마저도 "너무 위에 떠 있다"는 피드백이 있었음 — 더 줄임).
            var starText = AddLabel(go.transform, "★", font, 46, new Vector2(0, 25), new Vector2(200, 60));
            starText.fontStyle = FontStyle.Bold;

            // 합성으로 성급이 오르면 공격력도 같이 오르는데(StarDamageMultiplier), 숫자로 안 보여주면
            // 유저가 강화 효과를 체감할 방법이 없었다 — 별 배지 바로 아래에 실공격력을 표시.
            // 공격력 숫자가 전투 중 가장 자주 확인하는 정보라 별 배지보다 살짝 작아도 볼드로.
            var attackText = AddLabel(go.transform, "", font, 40, new Vector2(0, -250), new Vector2(220, 56));
            attackText.fontStyle = FontStyle.Bold;
            attackText.color = new Color(1f, 0.55f, 0.25f);

            var so = new SerializedObject(go.GetComponent<GuardianUnit>());
            so.FindProperty("starText").objectReferenceValue = starText;
            so.FindProperty("attackText").objectReferenceValue = attackText;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<GuardianUnit>();
        }

        // GuardianUnit 프리팹이 별 배지(starText) 도입 이전에 이미 저장돼 있던 경우를 위한 마이그레이션.
        private static void EnsureStarText(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var so = new SerializedObject(prefab.GetComponent<GuardianUnit>());
            if (so.FindProperty("starText").objectReferenceValue != null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Font font = GameFonts.Main;
            var starText = AddLabel(instance.transform, "★", font, 46, new Vector2(0, 25), new Vector2(200, 60));
            starText.fontStyle = FontStyle.Bold;

            var instanceSo = new SerializedObject(instance.GetComponent<GuardianUnit>());
            instanceSo.FindProperty("starText").objectReferenceValue = starText;
            instanceSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        // GuardianUnit 프리팹이 공격력 표시(attackText) 도입 이전에 이미 저장돼 있던 경우를 위한 마이그레이션.
        private static void EnsureAttackText(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var so = new SerializedObject(prefab.GetComponent<GuardianUnit>());
            if (so.FindProperty("attackText").objectReferenceValue != null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Font font = GameFonts.Main;
            var attackText = AddLabel(instance.transform, "", font, 40, new Vector2(0, -250), new Vector2(220, 56));
            attackText.fontStyle = FontStyle.Bold;
            attackText.color = new Color(1f, 0.55f, 0.25f);

            var instanceSo = new SerializedObject(instance.GetComponent<GuardianUnit>());
            instanceSo.FindProperty("attackText").objectReferenceValue = attackText;
            instanceSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        // GuardianUnit 프리팹이 SpriteFrameAnimator 도입 이전에 이미 저장돼 있던 경우를 위한 마이그레이션.
        private static void EnsureFrameAnimator(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.GetComponent<SpriteFrameAnimator>() != null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.AddComponent<SpriteFrameAnimator>();
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        // GuardianUnit이 드래그(IBeginDragHandler 등)를 지원하려면 CanvasGroup이 필요한데,
        // 이 메서드가 처음 만들었던 예전 프리팹은 그 전에 저장된 거라 없을 수 있다 — 있으면 건너뛴다.
        private static void EnsureCanvasGroup(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root.GetComponent<CanvasGroup>() == null)
            {
                root.AddComponent<CanvasGroup>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static GameObject BuildShopPanel(Transform canvasT, ShopManager shopManager, Font font)
        {
            var panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            ApplyRounded(panelImage);

            // 리롤 버튼을 카드 아래 별도 줄에 두면 그만큼 패널 높이가 세로로 낭비돼서,
            // 카드 4장과 같은 줄 맨 오른쪽에 5번째 칸처럼 나란히 놓아 UI를 꽉 채운다
            // (덕분에 패널 높이도 카드 세로 한 줄만큼만 있으면 됨 — PanelHeight를 더 줄일 수 있었음).
            const float cardWidth = 147f;
            // 이름/가격 텍스트가 너무 작아 보인다는 피드백으로 폰트를 키우면서, 초상화(137) 밑에
            // 텍스트 두 줄이 들어갈 여유 공간도 180→195로 같이 늘렸다(패널 아래 여백에서 15px만 뺌).
            const float cardHeight = 195f;
            const float rerollWidth = 145f; // 120→145: "버튼이 너무 작다" 피드백으로 키움(FixBattleLayout에도 같은 값 반영)
            const float cardGap = 6f;
            const float rerollGap = 16f; // 리롤 버튼은 카드가 아니라는 걸 시각적으로 구분하는 여백

            // 카드 4장은 전부 같은 ShopSlot.prefab 인스턴스다 — 처음 한 장만 코드로 짓고 나머지
            // 셋은 그 프리팹을 복제한다. 유저가 나중에 프리팹 자체(모양)를 고치면 4장 다 같이
            // 바뀌고, 개별 인스턴스 위치는 씬에서 각자 자유롭게 옮길 수 있다.
            var slotViews = new ShopSlotView[4];
            float slotSpacing = cardWidth + cardGap;
            // 카드 4장 + 리롤 버튼을 한 그룹으로 보고, 그룹 전체를 PanelContentCenterX에 맞춘다.
            // (이건 "처음 지어질 때"의 기본 배치일 뿐 — 이후엔 유저가 씬에서 직접 옮기면 그게 유지된다.)
            float groupWidth = 4 * cardWidth + 3 * cardGap + rerollGap + rerollWidth;
            float leftEdge = PanelContentCenterX - groupWidth / 2f;
            float[] xPositions = new float[4];
            for (int i = 0; i < 4; i++)
                xPositions[i] = leftEdge + cardWidth / 2f + i * slotSpacing;

            const string shopSlotPrefabPath = PrefabDir + "/UI/ShopSlot.prefab";
            var firstSlotView = BuildShopSlot(panel.transform, xPositions[0], font);
            var shopSlotInstance = ConnectAsPrefab(firstSlotView.gameObject, shopSlotPrefabPath);
            slotViews[0] = shopSlotInstance.GetComponent<ShopSlotView>();

            // 복제는 씬 인스턴스가 아니라 저장된 에셋 쪽에서 해야 한다(InstantiatePrefab은 에셋을 기대함).
            var shopSlotAsset = AssetDatabase.LoadAssetAtPath<GameObject>(shopSlotPrefabPath);
            for (int i = 1; i < 4; i++)
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(shopSlotAsset, panel.transform);
                copy.name = "ShopSlot";
                var copyRect = copy.GetComponent<RectTransform>();
                copyRect.anchoredPosition = new Vector2(xPositions[i], copyRect.anchoredPosition.y);
                slotViews[i] = copy.GetComponent<ShopSlotView>();
            }

            float rerollX = xPositions[3] + cardWidth / 2f + rerollGap + rerollWidth / 2f;

            var rerollGO = new GameObject("RerollButton", typeof(RectTransform), typeof(Image), typeof(Button));
            rerollGO.transform.SetParent(panel.transform, false);
            var rerollRect = rerollGO.GetComponent<RectTransform>();
            rerollRect.anchorMin = rerollRect.anchorMax = new Vector2(0.5f, 1f);
            rerollRect.pivot = new Vector2(0.5f, 1f);
            rerollRect.anchoredPosition = new Vector2(rerollX, -20); // 카드와 같은 상단 기준(y=-20)
            rerollRect.sizeDelta = new Vector2(rerollWidth, cardHeight); // 카드와 높이를 맞춰 한 줄처럼 보이게
            var rerollImage = rerollGO.GetComponent<Image>();
            rerollImage.color = new Color(0.3f, 0.3f, 0.4f);
            ApplyRounded(rerollImage);
            ApplyButtonColors(rerollGO.GetComponent<Button>(), rerollImage.color);
            AddLabel(rerollGO.transform, "리롤\n(20)", font, 24);

            // 리롤 버튼을 독립 프리팹 에셋으로 저장한다 — 버튼 쪽에는 이 씬의 ShopUI를 가리키는
            // 값을 못 넣으니(프리팹→씬 참조 불가), ShopUI 쪽이 버튼을 들고 런타임에 리스너를 붙인다.
            var connectedReroll = ConnectAsPrefab(rerollGO, $"{PrefabDir}/UI/RerollButton.prefab");

            var shopUIGO = new GameObject("ShopUI", typeof(ShopUI));
            shopUIGO.transform.SetParent(panel.transform, false);
            var shopUI = shopUIGO.GetComponent<ShopUI>();
            var so = new SerializedObject(shopUI);
            so.FindProperty("shopManager").objectReferenceValue = shopManager;
            so.FindProperty("rerollButton").objectReferenceValue = connectedReroll.GetComponent<Button>();
            var slotsProp = so.FindProperty("slotViews");
            slotsProp.ClearArray();
            for (int i = 0; i < slotViews.Length; i++)
            {
                slotsProp.InsertArrayElementAtIndex(i);
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static ShopSlotView BuildShopSlot(Transform parent, float x, Font font)
        {
            // 카드 전체가 버튼 — 별도 구매 버튼 없이 카드 아무 곳이나 누르면 구매된다.
            var go = new GameObject("ShopSlot", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button), typeof(ShopSlotView));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -20);
            rect.sizeDelta = new Vector2(147, 195);
            var cardImage = go.GetComponent<Image>();
            cardImage.color = new Color(0.18f, 0.18f, 0.24f);
            ApplyRounded(cardImage);
            // 주의: 카드 색은 등급에 따라 ShopSlotView.SetData가 매번 새로 칠한다(RarityColors).
            // Button의 기본 ColorTint 트랜지션은 상태가 바뀔 때마다 Image.color를 colors.normalColor로
            // 덮어써버려서 등급 색이랑 충돌한다 — 그래서 여긴 색 트랜지션 자체를 꺼서 SetData가 항상 이긴다.
            go.GetComponent<Button>().transition = Selectable.Transition.None;

            var cardOutline = go.GetComponent<Outline>();
            cardOutline.effectDistance = new Vector2(3, -3);
            cardOutline.enabled = false; // 등급 색은 SetData에서 데이터가 있을 때만 켠다

            // 가격(금화 아이콘+숫자)을 캐릭터 위에, 이름을 캐릭터 아래에 두는 레이아웃 — 참고
            // 레퍼런스 스타일 반영. 초상화는 그만큼 높이를 줄여서(137→110) 위쪽 가격 줄 자리를 확보.
            Sprite goldIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Brackeys/2D Mega Pack/Items & Icons/Pixel Art/Diamond.png");
            if (goldIconSprite != null)
            {
                var iconGO = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(go.transform, false);
                var iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(-28, -19);
                iconRect.sizeDelta = new Vector2(22, 22);
                var iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = goldIconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            // 초상화는 카드 왼쪽에 고정 폭만 차지하고, 남은 오른쪽 폭은 스탯 세로 3줄이 쓴다
            // ("스탯이 상점 슬롯 오른쪽 중앙에 세로로" 피드백). "공격/속도/사거리" 풀 단어를
            // 쓰려니 한 글자 라벨(공/속/사) 때보다 폭이 더 필요해서 초상화를 105→88로 더 줄임.
            const float portraitWidth = 88f;
            var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(go.transform, false);
            var portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 1f); // 부모 왼쪽 위 모서리 기준
            portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.anchoredPosition = new Vector2(0, -40); // 가격 줄 밑으로 내림
            portraitRect.sizeDelta = new Vector2(portraitWidth, 110);
            var portraitImage = portraitGO.GetComponent<Image>();
            portraitImage.raycastTarget = false; // 클릭이 뒤의 카드 버튼으로 그대로 전달되게
            portraitImage.preserveAspect = true;

            // costText는 금화 아이콘 오른쪽에 붙도록 왼쪽 pivot으로, nameText는 카드 맨 아래.
            var costText = AddLabel(go.transform, "", font, 24, new Vector2(-2, -19), new Vector2(56, 26));
            costText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            costText.alignment = TextAnchor.MiddleLeft;
            costText.color = new Color(1f, 0.85f, 0.35f); // HUD 금화 색과 통일
            costText.raycastTarget = false;

            // 사기 전에 공격력/공속/사거리를 보고 판단할 수 있어야 한다는 피드백 — 초상화 오른쪽
            // 빈 폭에 세로 3줄로, 초상화 세로 중앙에 맞춘다(초상화 y:-40~-150, 중앙 -95).
            var statsGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            statsGO.transform.SetParent(go.transform, false);
            var statsRect = statsGO.GetComponent<RectTransform>();
            statsRect.anchorMin = statsRect.anchorMax = new Vector2(1f, 1f); // 부모 오른쪽 위 모서리 기준
            statsRect.pivot = new Vector2(1f, 0.5f);
            statsRect.anchoredPosition = new Vector2(-3, -95); // 초상화 세로 중앙과 같은 높이
            statsRect.sizeDelta = new Vector2(55, 90);
            var statsText = statsGO.GetComponent<Text>();
            statsText.font = font;
            statsText.fontSize = 15;
            statsText.alignment = TextAnchor.MiddleRight;
            statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
            statsText.lineSpacing = 1.1f;
            statsText.color = new Color(0.85f, 0.85f, 0.85f);
            statsText.raycastTarget = false;

            var nameText = AddLabel(go.transform, "", font, 20, new Vector2(0, -172), new Vector2(140, 24));
            nameText.raycastTarget = false;

            var slotView = go.GetComponent<ShopSlotView>();
            var so = new SerializedObject(slotView);
            so.FindProperty("cardBackground").objectReferenceValue = cardImage;
            so.FindProperty("cardOutline").objectReferenceValue = cardOutline;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImage;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("statsText").objectReferenceValue = statsText;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                go.GetComponent<Button>().onClick, slotView.OnClickBuy);

            return slotView;
        }

        /// <summary>
        /// 이미 만들어져 있는 ShopSlot.prefab을 최신 레이아웃(초상화 왼쪽 고정폭 + 스탯 3줄을
        /// 오른쪽 세로로)에 맞춘다. BuildShopSlot은 "처음 지을 때"만 실행되는 코드라, 프리팹이
        /// 이미 있으면 이 마이그레이션을 다시 돌려야 반영된다 — 재실행해도 항상 최신 위치/크기로
        /// 덮어쓰므로(존재 여부로 건너뛰지 않음) 레이아웃을 또 바꾸면 이 메뉴만 다시 실행하면 됨.
        /// 프리팹 하나만 고치면 씬의 4장 인스턴스에 전부 반영된다.
        /// </summary>
        [MenuItem("WitchHour/Fix Shop Slot Stats Layout")]
        public static void FixShopSlotStatsLayout()
        {
            const string shopSlotPrefabPath = PrefabDir + "/UI/ShopSlot.prefab";
            const float portraitWidth = 88f; // "공격/속도/사거리" 풀 단어 폭 확보용으로 105→88
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(shopSlotPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[ShopRosterUIBootstrap] {shopSlotPrefabPath}를 못 찾았습니다 — 먼저 상점 슬롯을 프리팹으로 만들어야 합니다.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(shopSlotPrefabPath);
            try
            {
                var slotView = contents.GetComponent<ShopSlotView>();
                var so = new SerializedObject(slotView);

                // 초상화를 왼쪽 고정폭(105)으로 — 예전엔 카드 전체 폭을 다 썼는데, 오른쪽에
                // 스탯 세로 3줄이 들어갈 자리를 내줘야 한다.
                var portraitT = contents.transform.Find("Portrait");
                if (portraitT != null)
                {
                    var portraitRect = (RectTransform)portraitT;
                    portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 1f);
                    portraitRect.pivot = new Vector2(0f, 1f);
                    portraitRect.anchoredPosition = new Vector2(0, -40);
                    portraitRect.sizeDelta = new Vector2(portraitWidth, 110);
                }

                // 기존 statsText(예전엔 초상화 밑 가로 한 줄이라 잘렸었음)를 지우고 새로 짓는다.
                var oldStatsProp = so.FindProperty("statsText");
                if (oldStatsProp.objectReferenceValue != null)
                {
                    var oldGO = ((Text)oldStatsProp.objectReferenceValue).gameObject;
                    if (oldGO.transform.parent == contents.transform) Object.DestroyImmediate(oldGO);
                }

                Font font = GameFonts.Main;
                var statsGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                statsGO.transform.SetParent(contents.transform, false);
                var statsRect = statsGO.GetComponent<RectTransform>();
                statsRect.anchorMin = statsRect.anchorMax = new Vector2(1f, 1f);
                statsRect.pivot = new Vector2(1f, 0.5f);
                statsRect.anchoredPosition = new Vector2(-3, -95); // 초상화(y:-40~-150) 세로 중앙
                statsRect.sizeDelta = new Vector2(55, 90);
                var statsText = statsGO.GetComponent<Text>();
                statsText.font = font;
                statsText.fontSize = 15;
                statsText.alignment = TextAnchor.MiddleRight;
                statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
                statsText.verticalOverflow = VerticalWrapMode.Overflow;
                statsText.lineSpacing = 1.1f;
                statsText.color = new Color(0.85f, 0.85f, 0.85f);
                statsText.raycastTarget = false;

                so.FindProperty("statsText").objectReferenceValue = statsText;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, shopSlotPrefabPath);
                Debug.Log("[ShopRosterUIBootstrap] ShopSlot.prefab 스탯 레이아웃을 오른쪽 세로 3줄로 맞췄습니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// 이미 씬에 지어진 상점 슬롯 4장이 실제로는 프리팹에 연결 안 된 일반 씬 오브젝트였던 걸
        /// 뒤늦게 바로잡는다(원래 BuildShopPanel이 ConnectAsPrefab으로 저장했어야 하는데, 씬에
        /// 있던 인스턴스들의 m_CorrespondingSourceObject가 비어있는 걸 확인함 — 프리팹 폴더 자체가
        /// 사라졌었거나 뭔가 어긋난 상태였던 듯). 이미 배치된 4장의 위치/데이터 배선은 그대로 두고,
        /// 첫 번째 칸만 프리팹 에셋으로 저장해 그 자리에서 연결하고, 나머지 세 칸은 지운 뒤 그
        /// 프리팹의 인스턴스로 다시 만들어 같은 위치에 놓는다 — 이제부터 ShopSlot.prefab 하나만
        /// 고치면 4장 전부(그리고 앞으로 씬을 새로 지어도) 같이 바뀐다.
        /// </summary>
        [MenuItem("WitchHour/Convert Shop Slot To Prefab")]
        public static void ConvertShopSlotToPrefab()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var shopUIGO = GameObject.Find("ShopUI");
            var shopUI = shopUIGO != null ? shopUIGO.GetComponent<ShopUI>() : null;
            if (shopUI == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] ShopUI를 못 찾았습니다 — 상점 UI가 먼저 지어져 있어야 합니다.");
                return;
            }

            var so = new SerializedObject(shopUI);
            var slotsProp = so.FindProperty("slotViews");
            int count = slotsProp.arraySize;
            if (count == 0)
            {
                Debug.LogError("[ShopRosterUIBootstrap] ShopUI.slotViews가 비어 있습니다.");
                return;
            }

            var slots = new ShopSlotView[count];
            var positions = new Vector2[count];
            Transform parent = null;
            for (int i = 0; i < count; i++)
            {
                var view = slotsProp.GetArrayElementAtIndex(i).objectReferenceValue as ShopSlotView;
                if (view == null)
                {
                    Debug.LogError($"[ShopRosterUIBootstrap] slotViews[{i}]가 비어 있습니다.");
                    return;
                }
                slots[i] = view;
                positions[i] = view.GetComponent<RectTransform>().anchoredPosition;
                parent = view.transform.parent;
            }

            if (PrefabUtility.GetPrefabAssetType(slots[0].gameObject) != PrefabAssetType.NotAPrefab)
            {
                Debug.Log("[ShopRosterUIBootstrap] 이미 프리팹으로 연결되어 있습니다 — 할 일 없음.");
                return;
            }

            string prefabPath = $"{PrefabDir}/UI/ShopSlot.prefab";

            // 첫 번째 칸은 지금 그 자리에서 그대로 프리팹으로 저장+연결한다(위치/배선 안 건드림).
            // 주의: SaveAsPrefabAssetAndConnect의 반환값은 "씬 인스턴스"가 아니라 방금 저장된
            // "프리팹 에셋"이다 — 예전에 이 반환값을 newSlotViews[0]에 그대로 넣었다가, ShopUI가
            // 실제 화면에 보이는(클릭되는) 씬 오브젝트가 아니라 에셋 쪽 컴포넌트를 참조하게 돼서
            // 그 칸만 SetData가 한 번도 안 불려 _shopManager가 null인 채로 남았다("상점 첫 번째
            // 칸 클릭이 안 먹힌다" NullReferenceException의 원인). 연결 후에도 씬 인스턴스 자체는
            // 그대로이니(프리팹 연결은 같은 오브젝트에 메타데이터만 붙는 것) slots[0](이미 위에서
            // 캡처해둔 씬 인스턴스 참조)을 그대로 쓴다.
            ConnectAsPrefab(slots[0].gameObject, prefabPath);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            var newSlotViews = new ShopSlotView[count];
            newSlotViews[0] = slots[0];

            // 나머지 칸은 SaveAsPrefabAssetAndConnect가 한 오브젝트만 연결할 수 있어서(이미 있는
            // 서로 다른 오브젝트 여러 개를 같은 프리팹에 소급 연결하는 API가 없음), 지운 뒤 그
            // 프리팹의 인스턴스로 다시 만들어 같은 자리에 놓는 방식으로 우회한다.
            for (int i = 1; i < count; i++)
            {
                Vector2 pos = positions[i];
                Object.DestroyImmediate(slots[i].gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                instance.name = "ShopSlot";
                instance.GetComponent<RectTransform>().anchoredPosition = pos;
                newSlotViews[i] = instance.GetComponent<ShopSlotView>();
            }

            slotsProp.ClearArray();
            for (int i = 0; i < newSlotViews.Length; i++)
            {
                slotsProp.InsertArrayElementAtIndex(i);
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = newSlotViews[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopRosterUIBootstrap] 상점 슬롯 {count}장을 {prefabPath} 프리팹으로 변환하고 다시 연결했습니다.");
        }

        // 아이템 시스템(종류/효과/획득 방법)은 아직 설계 전이라 자리만 만들어둔다 — 실제 슬롯/로직은
        // 아이템을 디자인한 뒤 이 메서드를 다시 채우면 된다.
        private static GameObject BuildItemPlaceholderPanel(Transform canvasT, Font font)
        {
            var panel = new GameObject("ItemPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasT, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var itemPanelImage = panel.GetComponent<Image>();
            itemPanelImage.color = new Color(0.1f, 0.08f, 0.08f, 0.95f);
            ApplyRounded(itemPanelImage);

            var label = AddLabel(panel.transform, "아이템 기능은 준비 중입니다", font, 18,
                new Vector2(PanelContentCenterX, -PanelHeight / 2f), new Vector2(PanelWidth - TabStripWidth - 40f, 60));
            label.color = new Color(0.7f, 0.7f, 0.7f);

            return panel;
        }

        // 예전엔 "소환상점"/"아이템" 고정 탭 2개였는데, 위 칸은 토글 버튼 1개(상점↔아이템,
        // 라벨이 상황에 맞게 바뀜)로, 아래 칸은 항상 고정인 설정 버튼으로 바꿨다.
        // (자세한 이유: BattleTabController.cs 클래스 주석 참고)
        private static void BuildSideTabs(Transform canvasT, GameObject shopPanel, GameObject itemPanel, Font font)
        {
            var controllerGO = new GameObject("BattleTabController", typeof(BattleTabController));
            controllerGO.transform.SetParent(canvasT, false);
            var controller = controllerGO.GetComponent<BattleTabController>();

            // 패널이 SetActive로 꺼졌다 켜졌다 하므로 탭은 패널의 자식이 아니라 별도 오브젝트로 두고
            // 왼쪽 가장자리에 같은 자리로 겹쳐 놓는다(둘 다 PanelWidth/PanelHeight가 같아서 자리가 맞음).
            // 둘이 똑같은 회색이라 구별이 안 간다는 피드백으로 색을 다르게 줬다 — 토글은 차가운
            // 청록(전환/이동 느낌), 설정은 따뜻한 황갈색(도구/톱니 느낌)으로 계열 자체를 다르게.
            var toggleGO = BuildSideTabButton(canvasT, "아이템", top: true, font, ToggleTabColor);
            var settingsGO = BuildSideTabButton(canvasT, "설정", top: false, font, SettingsTabColor);
            var toggleLabel = toggleGO.GetComponentInChildren<Text>();

            var so = new SerializedObject(controller);
            so.FindProperty("shopPanel").objectReferenceValue = shopPanel;
            so.FindProperty("itemPanel").objectReferenceValue = itemPanel;
            so.FindProperty("toggleLabel").objectReferenceValue = toggleLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                toggleGO.GetComponent<Button>().onClick, controller.ToggleShopItems);

            // 설정 버튼은 상점/아이템 전환과 무관한 별도 오버레이 — Home 씬과 같은
            // SettingsPanelController(BGM/효과음 슬라이더)를 그대로 재사용한다.
            var settingsController = SceneBootstrap.BuildSettingsPanelCard(canvasT, font);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                settingsGO.GetComponent<Button>().onClick, settingsController.Open);

            itemPanel.SetActive(false);
        }

        // 토글(아이템⇄소환상점) / 설정 버튼 색 — 회색 하나로 통일돼 있어서 구별이 안 간다는
        // 피드백으로 계열 자체를 다르게 나눴다. ColorizeSideTabs()에서도 같은 값을 씀.
        private static readonly Color ToggleTabColor = new Color(0.16f, 0.32f, 0.4f);   // 차가운 청록
        private static readonly Color SettingsTabColor = new Color(0.42f, 0.32f, 0.16f); // 따뜻한 황갈색

        private static GameObject BuildSideTabButton(Transform canvasT, string label, bool top, Font font, Color color)
        {
            var go = new GameObject($"Tab_{label.Replace('\n', ' ')}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            float tabHeight = PanelHeight / 2f;
            float x = -PanelWidth / 2f + TabStripWidth / 2f;
            float y = top ? tabHeight : 0f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(TabStripWidth, tabHeight);
            var tabImage = go.GetComponent<Image>();
            tabImage.color = color;
            ApplyButtonColors(go.GetComponent<Button>(), tabImage.color);
            var text = AddLabel(go.transform, label, font, 22);
            text.color = Color.white;
            return go;
        }

        /// <summary>
        /// 이미 지어진 씬에서 토글/설정 버튼 색만 다르게 바꾼다. 유저가 크기를 직접 손으로
        /// 맞춰놨을 수 있어서 RectTransform은 절대 안 건드리고 Image.color만 고친다
        /// (재실행해도 안전 — 항상 색만 다시 칠함).
        /// </summary>
        [MenuItem("WitchHour/Colorize Side Tabs (Item vs Settings)")]
        public static void ColorizeSideTabs()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            var toggleGO = FindInScene(scene, "Tab_아이템");
            var settingsGO = FindInScene(scene, "Tab_설정");
            if (toggleGO == null || settingsGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] Tab_아이템/Tab_설정을 못 찾았습니다 " +
                                "— 먼저 'Rebuild Side Tabs'로 새 탭 구조를 지어야 합니다.");
                return;
            }

            void Recolor(GameObject go, Color color)
            {
                var image = go.GetComponent<Image>();
                image.color = color;
                ApplyButtonColors(go.GetComponent<Button>(), color);
            }

            Recolor(toggleGO, ToggleTabColor);
            Recolor(settingsGO, SettingsTabColor);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopRosterUIBootstrap] 아이템/설정 탭 색을 구별되게 다시 칠했습니다.");
        }

        /// <summary>
        /// 이미 지어진 씬에서 예전 방식(고정 탭 2개)을 새 방식(토글 버튼 + 설정 버튼)으로
        /// 바꿔치기한다. BuildSideTabs는 ShopPanel이 이미 있으면 통째로 안 건드리게 가드가
        /// 걸려 있어서, 이미 상점 UI가 지어진 씬은 이 메뉴로 탭 부분만 따로 다시 지어야 한다.
        /// </summary>
        [MenuItem("WitchHour/Rebuild Side Tabs (Toggle + Settings)")]
        public static void RebuildSideTabs()
        {
            var scene = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);

            // ItemPanel은 기본적으로 꺼져있는(SetActive(false)) 상태라 GameObject.Find로는 못
            // 찾는다(비활성 오브젝트는 검색 안 됨) — 씬 루트부터 자식까지 직접 훑는 FindInScene을 씀.
            var canvasGO = FindInScene(scene, "Canvas");
            var shopPanelGO = FindInScene(scene, "ShopPanel");
            var itemPanelGO = FindInScene(scene, "ItemPanel");
            if (canvasGO == null || shopPanelGO == null || itemPanelGO == null)
            {
                Debug.LogError("[ShopRosterUIBootstrap] Canvas/ShopPanel/ItemPanel을 못 찾았습니다 " +
                                "— 먼저 상점 UI를 지어야 합니다.");
                return;
            }

            // 예전 고정 탭 2개, 예전 컨트롤러, (혹시 남아있으면) 예전 설정 패널까지 지우고 새로 짠다.
            // SettingsPanel도 평소엔 꺼져있으므로 같은 이유로 FindInScene을 씀.
            foreach (var name in new[] { "Tab_소환 상점", "Tab_아이템", "BattleTabController", "SettingsPanel" })
            {
                var old = FindInScene(scene, name);
                if (old != null) Object.DestroyImmediate(old);
            }

            BuildSideTabs(canvasGO.transform, shopPanelGO, itemPanelGO, GameFonts.Main);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopRosterUIBootstrap] 왼쪽 탭을 토글 버튼(아이템⇄소환상점) + 설정 버튼으로 다시 짰습니다.");
        }

        /// <summary>
        /// GameObject.Find는 비활성 오브젝트를 못 찾는다(ItemPanel/SettingsPanel처럼 평소엔
        /// SetActive(false)로 꺼둔 것들) — 씬 루트부터 자식까지 활성 여부 상관없이 이름으로 찾는다.
        /// </summary>
        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChildRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void BuildPrepTimer(
            Transform canvasT, WitchHour.Core.WaveSpawner waveSpawner, ShopManager shopManager, Font font)
        {
            // 텍스트만 둥실 떠 있지 않게 둥근 배경 알약(pill) 위에 올린다 — HUD와 같은 처리.
            var go = new GameObject("PrepTimerText", typeof(RectTransform), typeof(Image), typeof(PrepTimerUI));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, PanelHeight + 18f);
            rect.sizeDelta = new Vector2(400, 60);

            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.06f, 0.05f, 0.08f, 0.85f);
            ApplyRounded(bgImage);

            var text = AddLabel(go.transform, "", font, 32);
            text.color = new Color(1f, 0.85f, 0.2f);

            var so = new SerializedObject(go.GetComponent<PrepTimerUI>());
            so.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            so.FindProperty("shopManager").objectReferenceValue = shopManager;
            so.FindProperty("timerText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHud(
            Transform canvasT, WitchHour.Core.WardHealth ward, WitchHour.Core.RunCurrency currency,
            WitchHour.Core.WaveSpawner waveSpawner, Font font)
        {
            var hudGO = new GameObject("HudUI", typeof(RectTransform), typeof(HudUI));
            hudGO.transform.SetParent(canvasT, false);
            var hudRect = hudGO.GetComponent<RectTransform>();
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0.5f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = new Vector2(0, -12f); // 화면 맨 위 가장자리에 딱 붙지 않게 여유
            hudRect.sizeDelta = new Vector2(1040, HudHeight - 12f);

            // 텍스트만 떠 있으면 필드 배경(밝은 배경/캐릭터 등)에 따라 안 읽힐 수 있어 옅은 반투명
            // 바를 깔아 항상 대비가 나오게 한다.
            var bgGO = new GameObject("HudBackground", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(hudGO.transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.55f);
            ApplyRounded(bgImage);

            // 금화 텍스트가 박스보다 길어서 줄바꿈되다 잘려 보이던 문제 — 박스를 넉넉하게 잡고
            // AddLabel의 Overflow 설정과 함께 절대 안 잘리게 한다.
            var waveText = AddLabel(hudGO.transform, "", font, 29, new Vector2(-345, -18), new Vector2(320, 44));
            waveText.alignment = TextAnchor.MiddleLeft;
            waveText.color = new Color(0.8f, 0.88f, 1f);

            // Brackeys 2D Mega Pack 아이콘 — 다이아몬드는 금화, 성은 성벽 옆에 붙여서 숫자만
            // 덩그러니 있던 것보다 한눈에 무슨 값인지 알아보기 쉽게 한다.
            Sprite manaIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Brackeys/2D Mega Pack/Items & Icons/Pixel Art/Diamond.png");
            Sprite wardIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Brackeys/2D Mega Pack/Items & Icons/Pixel Art/Castle.png");

            if (manaIconSprite != null)
            {
                var iconGO = new GameObject("ManaIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(hudGO.transform, false);
                var iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.anchoredPosition = new Vector2(275, -18);
                iconRect.sizeDelta = new Vector2(36, 36);
                var iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = manaIconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            // 아이콘 폭만큼 오른쪽으로 밀고 왼쪽 정렬로 바꿔서 숫자가 길어져도 아이콘과 안 겹친다.
            var manaText = AddLabel(hudGO.transform, "", font, 29, new Vector2(405, -18), new Vector2(250, 44));
            manaText.alignment = TextAnchor.MiddleLeft;
            manaText.color = new Color(1f, 0.85f, 0.35f);

            if (wardIconSprite != null)
            {
                // 원본 아이콘이 어두운 톤이라 HUD의 어두운 반투명 배경 위에서 거의 안 보였다 —
                // 밝은 둥근 배지를 뒤에 깔아서 색이 뭐든 항상 도드라지게 한다.
                var badgeGO = new GameObject("WardIconBadge", typeof(RectTransform), typeof(Image));
                badgeGO.transform.SetParent(hudGO.transform, false);
                var badgeRect = badgeGO.GetComponent<RectTransform>();
                badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
                badgeRect.anchoredPosition = new Vector2(-430, -85);
                badgeRect.sizeDelta = new Vector2(60, 60);
                var badgeImage = badgeGO.GetComponent<Image>();
                ApplyRounded(badgeImage);
                badgeImage.color = new Color(0.95f, 0.9f, 0.75f, 0.9f);
                badgeImage.raycastTarget = false;

                var iconGO = new GameObject("WardIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(badgeGO.transform, false);
                var iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8, 8);
                iconRect.offsetMax = new Vector2(-8, -8);
                var iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = wardIconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            var barBgGO = new GameObject("WardHpBarBg", typeof(RectTransform), typeof(Image));
            barBgGO.transform.SetParent(hudGO.transform, false);
            var barBgRect = barBgGO.GetComponent<RectTransform>();
            barBgRect.anchorMin = barBgRect.anchorMax = new Vector2(0.5f, 1f);
            barBgRect.pivot = new Vector2(0.5f, 1f);
            // 웨이브/금화 텍스트 줄(-18)과의 간격을 줄여서 성벽 체력바를 더 위로 붙였다
            // ("체력바가 화면 아래쪽에 있어 눈에 안 띈다" 피드백 반영).
            barBgRect.anchoredPosition = new Vector2(0, -48);
            barBgRect.sizeDelta = new Vector2(860, 42);
            var barBgImage = barBgGO.GetComponent<Image>();
            Sprite wardBgSprite = LoadPixelKitSprite("UI_StatusBar_Bg.png");
            if (wardBgSprite != null)
            {
                barBgImage.sprite = wardBgSprite;
                barBgImage.type = Image.Type.Sliced;
                barBgImage.color = Color.white;
            }
            else
            {
                barBgImage.color = new Color(0.12f, 0.08f, 0.08f, 0.95f);
                ApplyRounded(barBgImage);
            }

            // Image.Type.Filled + fillAmount 조합이 이 프로젝트에서 실제로는 씬에 갱신이 반영되지
            // 않는(텍스트는 맞는데 바 자체는 항상 꽉 차 보이는) 문제가 있어서, 훨씬 더 기본적이고
            // 확실한 방식으로 바꿨다: 왼쪽 pivot에 고정된 채로 localScale.x만 줄이는 방식(막대
            // 너비 자체를 늘였다 줄였다 하는 가장 원시적인 방법이라 렌더링이 안 따라올 여지가 없음).
            var barFillGO = new GameObject("WardHpBarFill", typeof(RectTransform), typeof(Image));
            barFillGO.transform.SetParent(barBgGO.transform, false);
            var barFillRect = barFillGO.GetComponent<RectTransform>();
            barFillRect.anchorMin = barFillRect.anchorMax = new Vector2(0f, 0.5f); // 부모 왼쪽 가운데에 고정
            barFillRect.pivot = new Vector2(0f, 0.5f);
            barFillRect.anchoredPosition = new Vector2(4, 0);
            barFillRect.sizeDelta = new Vector2(852, 34); // 꽉 찼을 때(scale.x=1) 크기 — 배경보다 4px씩 안쪽
            var fillImage = barFillGO.GetComponent<Image>();
            Sprite wardFillSprite = LoadPixelKitSprite("UI_StatusBar_Fill_HP.png");
            if (wardFillSprite != null)
            {
                fillImage.sprite = wardFillSprite;
                fillImage.type = Image.Type.Simple;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = new Color(0.85f, 0.25f, 0.35f);
            }

            var hpText = AddLabel(barBgGO.transform, "", font, 25);
            hpText.color = Color.white;
            hpText.fontStyle = FontStyle.Bold;

            var so = new SerializedObject(hudGO.GetComponent<HudUI>());
            so.FindProperty("ward").objectReferenceValue = ward;
            so.FindProperty("currency").objectReferenceValue = currency;
            so.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            so.FindProperty("wardFillImage").objectReferenceValue = fillImage;
            so.FindProperty("wardHpText").objectReferenceValue = hpText;
            so.FindProperty("manaText").objectReferenceValue = manaText;
            so.FindProperty("waveText").objectReferenceValue = waveText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 화면 우상단 구석에 배치 — HUD 바가 이미 폭을 꽉 채우고 있어서 겹칠 수 있다.
        // 다른 UI들처럼 프리팹+재실행해도 안 지워지는 구조라, 실제로 보고 자리가 안 맞으면
        // 씬에서 그냥 드래그로 옮기면 됨(이 메서드를 다시 안 건드려도 위치가 유지된다).
        private static void BuildSpeedToggle(Transform canvasT, Font font)
        {
            var go = new GameObject("SpeedToggleButton", typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(WitchHour.Core.BattleSpeedController));
            go.transform.SetParent(canvasT, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-15, -15);
            rect.sizeDelta = new Vector2(110, 78); // 85x60→110x78: "버튼이 너무 작다" 피드백으로 키움

            var image = go.GetComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.4f);
            ApplyRounded(image);
            ApplyButtonColors(go.GetComponent<Button>(), image.color);

            var label = AddLabel(go.transform, "x1", font, 30);
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;

            var controller = go.GetComponent<WitchHour.Core.BattleSpeedController>();
            var so = new SerializedObject(controller);
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                go.GetComponent<Button>().onClick, controller.ToggleSpeed);

            ConnectAsPrefab(go, $"{PrefabDir}/UI/SpeedToggleButton.prefab");
        }

        private static Text AddLabel(Transform parent, string text, Font font, int fontSize,
            Vector2? anchoredPosition = null, Vector2? sizeDelta = null, bool withShadow = true)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();

            if (anchoredPosition.HasValue)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.anchoredPosition = anchoredPosition.Value;
                rect.sizeDelta = sizeDelta ?? new Vector2(200, 40);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            // 박스보다 글자가 살짝 길어져도 줄바꿈되다 잘려 보이는 일이 없게 — 금화 텍스트가
            // 박스 폭보다 조금 길어서 두 줄로 줄바꿈된 뒤 위쪽이 잘려 보이던 문제의 근본 원인이었다.
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            // 전투 필드나 다른 UI 위에 겹칠 수 있는 텍스트는 옅은 그림자를 깔아 배경이 밝든 어둡든
            // 항상 읽히게 한다(가독성 + 전체적인 마감 품질).
            if (withShadow)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return t;
        }
    }
}
#endif
