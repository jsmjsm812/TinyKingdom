using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// TinyKingdom 에셋의 모듈형 캐릭터 프리팹을 오프스크린 프리뷰 씬에서 애니메이션 클립을
    /// 샘플링해 idle/action(공격 또는 사망) 프레임을 PNG로 구워 GuardianData/InvaderData에
    /// 자동으로 연결한다.
    ///
    /// 이 프로젝트의 배틀필드는 UGUI Screen-Space-Overlay라 SpriteRenderer 기반 프리팹을
    /// 그대로 쓸 수 없다. 대신 에디터에서 한 번 "촬영"해서 PNG 프레임으로 만들고,
    /// 런타임에는 SpriteFrameAnimator(Image 스프라이트 코루틴 교체)로 재생한다.
    /// </summary>
    public static class SpumBattleSpriteBaker
    {
        private const string TKCreatureDir = "Assets/TinyKingdom/TowerDefense/Prefabs/Creatures";
        private const string TKAnimDir = "Assets/TinyKingdom/Common/Animation";

        private const int FrameSize = 256;
        private const int FrameCount = 4;

        private enum RigType { Humanoid, Troll, Wolf, Spider }

        private class CharConfig
        {
            public string name;
            public string dataAssetPath;
            public string prefabName;
            public RigType rig;
            public bool isGuardian;
            public string actionClip;
            public float visualScale = 1f;
            // TinyKingdom에 아군으로 쓸만한 "영웅형" 휴머노이드가 8종뿐이라 10명 전원을 서로 다른
            // 프리팹으로는 못 맞춘다 — 같은 프리팹을 쓰는 둘은 그 대신 색조를 다르게 입혀서
            // 실루엣이 같아도 한눈에 다른 캐릭터로 보이게 한다(곱연산 틴트라 흰색=원본 그대로).
            public Color tint = Color.white;
        }

        private static readonly CharConfig[] Guardians =
        {
            new CharConfig { name = "Lumi",    dataAssetPath = "Assets/02_Data/Guardians/Lumi.asset",    prefabName = "Warrior",      rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF" },
            new CharConfig { name = "Noa",     dataAssetPath = "Assets/02_Data/Guardians/Noa.asset",     prefabName = "BanditScout",  rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF" },
            new CharConfig { name = "Mira",    dataAssetPath = "Assets/02_Data/Guardians/Mira.asset",    prefabName = "Archer",       rig = RigType.Humanoid, isGuardian = true, actionClip = "ShootBowF" },
            new CharConfig { name = "Dori",    dataAssetPath = "Assets/02_Data/Guardians/Dori.asset",    prefabName = "BanditElder",  rig = RigType.Troll,    isGuardian = true, actionClip = "AttackF" },
            // 세라(연금술사)·클로에(마녀)는 둘 다 Witch 프리팹 — 세라는 연금약 느낌의 초록 틴트.
            new CharConfig { name = "Sera",    dataAssetPath = "Assets/02_Data/Guardians/Sera.asset",    prefabName = "Witch",        rig = RigType.Humanoid, isGuardian = true, actionClip = "SpellF", tint = new Color(0.55f, 0.95f, 0.55f) },
            new CharConfig { name = "Irene",   dataAssetPath = "Assets/02_Data/Guardians/Irene.asset",   prefabName = "BanditRanger", rig = RigType.Humanoid, isGuardian = true, actionClip = "ShootGunF" },
            new CharConfig { name = "Chloe",   dataAssetPath = "Assets/02_Data/Guardians/Chloe.asset",   prefabName = "Witch",        rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF" },
            new CharConfig { name = "Astel",   dataAssetPath = "Assets/02_Data/Guardians/Astel.asset",   prefabName = "Mage",         rig = RigType.Humanoid, isGuardian = true, actionClip = "SpellF" },
            // 아스텔(대마법사)·셀레네(빙결사)는 둘 다 Mage 프리팹 — 셀레네는 빙결 느낌의 파란 틴트.
            new CharConfig { name = "Selene",  dataAssetPath = "Assets/02_Data/Guardians/Selene.asset",  prefabName = "Mage",         rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF", tint = new Color(0.55f, 0.75f, 1f) },
            new CharConfig { name = "Ophelia", dataAssetPath = "Assets/02_Data/Guardians/Ophelia.asset", prefabName = "King",         rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF" },
        };

        private static readonly CharConfig[] Invaders =
        {
            new CharConfig { name = "WarpedCrow",      dataAssetPath = "Assets/02_Data/Invaders/WarpedCrow.asset",      prefabName = "Goblin",       rig = RigType.Humanoid, isGuardian = false, actionClip = "DeathF" },
            new CharConfig { name = "WarpedRat",        dataAssetPath = "Assets/02_Data/Invaders/WarpedRat.asset",       prefabName = "Goblin",       rig = RigType.Humanoid, isGuardian = false, actionClip = "DeathF" },
            new CharConfig { name = "WarpedSnake",      dataAssetPath = "Assets/02_Data/Invaders/WarpedSnake.asset",     prefabName = "Skeleton",     rig = RigType.Humanoid, isGuardian = false, actionClip = "DeathF" },
            new CharConfig { name = "WarpedBoar",       dataAssetPath = "Assets/02_Data/Invaders/WarpedBoar.asset",      prefabName = "Skeleton",     rig = RigType.Humanoid, isGuardian = false, actionClip = "DeathF" },
            new CharConfig { name = "RabidWolf",        dataAssetPath = "Assets/02_Data/Invaders/RabidWolf.asset",       prefabName = "Warg",         rig = RigType.Wolf,     isGuardian = false, actionClip = "DeathF", visualScale = 1.0f },
            new CharConfig { name = "PrimordialHawk",   dataAssetPath = "Assets/02_Data/Invaders/PrimordialHawk.asset",  prefabName = "BanditLeader", rig = RigType.Troll,    isGuardian = false, actionClip = "DeathF", visualScale = 1.3f },
            new CharConfig { name = "PrimordialBear",   dataAssetPath = "Assets/02_Data/Invaders/PrimordialBear.asset",  prefabName = "Cyclops",      rig = RigType.Troll,    isGuardian = false, actionClip = "DeathF", visualScale = 1.3f },
            new CharConfig { name = "PrimordialTiger",  dataAssetPath = "Assets/02_Data/Invaders/PrimordialTiger.asset", prefabName = "Troll",        rig = RigType.Troll,    isGuardian = false, actionClip = "DeathF", visualScale = 1.3f },
        };

        [MenuItem("WitchHour/Bake TinyKingdom Battle Sprites")]
        public static void BakeAll()
        {
            int done = 0;
            int total = Guardians.Length + Invaders.Length;
            try
            {
                foreach (var cfg in Guardians)
                {
                    EditorUtility.DisplayProgressBar("TinyKingdom 배틀 스프라이트 굽는 중", cfg.name + " (idle+attack)", (float)done / total);
                    BakeCharacter(cfg);
                    done++;
                }
                foreach (var cfg in Invaders)
                {
                    EditorUtility.DisplayProgressBar("TinyKingdom 배틀 스프라이트 굽는 중", cfg.name + " (idle+death)", (float)done / total);
                    BakeCharacter(cfg);
                    done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpumBattleSpriteBaker] 완료: 용사 {Guardians.Length}종 + 침입자 {Invaders.Length}종. " +
                      "Assets/03_Art/Sprites/Guardians(또는 Invaders)/Battle 폴더 확인.");
        }

        private static string GetClipPath(RigType rig, string clipName)
        {
            string rigFolder = rig == RigType.Humanoid ? "Humanoid/Front" : rig.ToString();
            return $"{TKAnimDir}/{rigFolder}/{clipName}.anim";
        }

        // 한때 용사 전원의 확대율(orthographicSize)을 가장 큰 캐릭터 기준으로 통일했었는데
        // (ComputeMaxOrthoSize), 발 위치 정렬은 어차피 카메라를 "발이 항상 프레임 맨 밑에 오도록"
        // 세로로만 재배치하는 방식이라 확대율 공유 여부와 무관하게 이미 보장돼 있었다 — 그런데
        // 확대율을 제일 큰 캐릭터(예: 덩치 큰 Troll 리그 야만전사)에 맞춰버리니 작은 캐릭터들이
        // 전부 그 큰 프레임 안에서 왜소하게 찍혀 "다른 애들이 다 작아 보인다"는 새 불만이 생겼다.
        // 그래서 그 통일 로직 자체를 걷어내고 캐릭터마다 자기 bounds에 맞춰 최대한 꽉 차게(=각자
        // 프레임을 최대한 채우는) 예전 방식으로 되돌렸다 — 발 정렬은 여전히 보장되고, 다들 자기
        // 프레임을 꽉 채우니 크기도 훨씬 커 보인다.
        private static void BakeCharacter(CharConfig cfg)
        {
            string prefabPath = $"{TKCreatureDir}/{cfg.prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SpumBattleSpriteBaker] {cfg.name}: 프리팹을 못 찾음 ({prefabPath})");
                return;
            }

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GetClipPath(cfg.rig, "IdleF"));
            var actionClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GetClipPath(cfg.rig, cfg.actionClip));
            if (idleClip == null || actionClip == null)
            {
                Debug.LogError($"[SpumBattleSpriteBaker] {cfg.name}: 애니메이션 클립을 못 찾음 " +
                               $"(idle={GetClipPath(cfg.rig, "IdleF")}, action={GetClipPath(cfg.rig, cfg.actionClip)})");
                return;
            }

            Scene scene = default;
            GameObject instance = null;
            GameObject camGO = null;
            RenderTexture rt = null;
            bool sceneOpened = false;
            try
            {
                scene = EditorSceneManager.NewPreviewScene();
                sceneOpened = true;

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                EditorSceneManager.MoveGameObjectToScene(instance, scene);
                instance.transform.position = Vector3.zero;
                instance.transform.localScale = Vector3.one * cfg.visualScale;

                // TinyKingdom 크리처는 Front/Back 자식을 가진다. 정면(Front)만 활성화.
                var frontT = instance.transform.Find("Front");
                var backT = instance.transform.Find("Back");
                if (frontT != null) frontT.gameObject.SetActive(true);
                if (backT != null) backT.gameObject.SetActive(false);

                // TinyKingdom 크리처는 자체 타워디펜스 데모용 체력바(Hud/Hp/Progress, 초록색
                // SpriteRenderer)를 Front/Back과 같은 레벨의 자식으로 기본 활성화 상태로 들고
                // 있다 — 우리 게임엔 이미 우리 자체 체력바(InvaderUnit.hpFillImage)가 있으니
                // 이건 꺼서 캡처에서 빼야 한다. 안 그러면 이 초록 바가 그대로 PNG에 구워져서
                // "캐릭터 머리 위에 원인 모를 초록 바가 있다"는 문제가 생긴다(전 세대 프레임에
                // 이미 이 문제가 박혀있으므로 재베이크 필요).
                var hudT = instance.transform.Find("Hud");
                if (hudT != null) hudT.gameObject.SetActive(false);

                var animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError($"[SpumBattleSpriteBaker] {cfg.name}: Animator를 못 찾음");
                    return;
                }

                var renderers = instance.GetComponentsInChildren<SpriteRenderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogError($"[SpumBattleSpriteBaker] {cfg.name}: SpriteRenderer가 없음");
                    return;
                }

                // 같은 프리팹을 재사용하는 캐릭터끼리 실루엣이 같아 보이는 문제를 색조로 구분한다
                // (곱연산이라 흰색 틴트는 원본 색 그대로, 사실상 no-op).
                if (cfg.tint != Color.white)
                {
                    foreach (var r in renderers)
                        r.color = r.color * cfg.tint;
                }

                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                {
                    if (r.gameObject.activeInHierarchy)
                        bounds.Encapsulate(r.bounds);
                }
                float pad = Mathf.Max(bounds.extents.x, bounds.extents.y) * 0.1f;
                bounds.Expand(pad);

                camGO = new GameObject("TKBakeCamera");
                EditorSceneManager.MoveGameObjectToScene(camGO, scene);
                var cam = camGO.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                // 캐릭터 자기 bounds 기준으로 확대 — 다들 자기 프레임을 최대한 꽉 채운다.
                cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;

                // 캐릭터마다 가로/세로 비율이 달라서(예: 옆으로 넓은 포즈 vs 위아래로 긴 포즈)
                // bounds.center를 그대로 카메라 중심으로 쓰면 정사각형 프레임 안에서 발 위치가
                // 캐릭터마다 서로 다른 높이에 찍힌다 — 같은 슬롯 높이에 배치해도 그리드에서
                // 캐릭터별로 키가 들쭉날쭉해 보이는 원인이었다("용사들 Y값이 안 맞는다" 피드백).
                // 대신 bounds 아래쪽(발)이 항상 프레임 맨 밑에 오도록 카메라를 세로로만 아래쪽에
                // 정렬해서, 어떤 캐릭터든 발 기준선이 구운 정사각형 이미지의 같은 위치에 찍히게 한다.
                camGO.transform.position = new Vector3(bounds.center.x, bounds.min.y + cam.orthographicSize, -10f);

                rt = new RenderTexture(FrameSize, FrameSize, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                cam.targetTexture = rt;

                string subFolder = cfg.isGuardian ? "Guardians" : "Invaders";
                var idleFrames = RenderClipFrames(animator.gameObject, cam, rt, idleClip, cfg.name, "Idle", subFolder);
                string actionStateName = cfg.isGuardian ? "Attack" : "Death";
                var actionFrames = RenderClipFrames(animator.gameObject, cam, rt, actionClip, cfg.name, actionStateName, subFolder);

                // 침입자만 걷기 모션이 필요하다(용사는 필드에 고정 배치라 안 움직임) — 통로를
                // 걸어가는 내내 idle이 아니라 실제로 다리를 움직이는 것처럼 보이게.
                Sprite[] walkFrames = null;
                if (!cfg.isGuardian)
                {
                    var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GetClipPath(cfg.rig, "RunF"));
                    if (runClip != null)
                        walkFrames = RenderClipFrames(animator.gameObject, cam, rt, runClip, cfg.name, "Walk", subFolder);
                    else
                        Debug.LogWarning($"[SpumBattleSpriteBaker] {cfg.name}: RunF 클립을 못 찾아 walkFrames를 못 구움 (idle로 대체됨) — {GetClipPath(cfg.rig, "RunF")}");
                }

                AssignToData(cfg.dataAssetPath, idleFrames, actionFrames, walkFrames);
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.active = null;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
                if (camGO != null) Object.DestroyImmediate(camGO);
                if (instance != null) Object.DestroyImmediate(instance);
                if (sceneOpened) EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Sprite[] RenderClipFrames(GameObject animatedRoot, Camera cam, RenderTexture rt,
            AnimationClip clip, string charName, string stateName, string subFolder)
        {
            string parentFolder = "Assets/03_Art/Sprites/" + subFolder;
            string folder = parentFolder + "/Battle";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(parentFolder, "Battle");

            var sprites = new Sprite[FrameCount];
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < FrameCount; i++)
                {
                    float t = clip.length * (i / (float)FrameCount);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animatedRoot, clip, t);
                    AnimationMode.EndSampling();
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, FrameSize, FrameSize), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;

                    string fileName = $"{charName}_{stateName}_{i}.png";
                    string filePath = $"{folder}/{fileName}";
                    File.WriteAllBytes(filePath, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);

                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.spritePixelsPerUnit = FrameSize;
                    importer.SaveAndReimport();

                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
            return sprites;
        }

        private static void AssignToData(string dataAssetPath, Sprite[] idleFrames, Sprite[] actionFrames, Sprite[] walkFrames = null)
        {
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dataAssetPath);
            if (so == null)
            {
                Debug.LogError($"[SpumBattleSpriteBaker] 데이터 에셋을 못 찾음: {dataAssetPath}");
                return;
            }
            var serialized = new SerializedObject(so);

            SetSpriteArray(serialized, "idleFrames", idleFrames);
            SetSpriteArray(serialized, "actionFrames", actionFrames);
            // walkFrames는 InvaderData에만 있는 필드라(GuardianData엔 없음) 프로퍼티가 없으면
            // SetSpriteArray가 경고만 찍고 조용히 넘어간다 — 용사 쪽은 null을 넘겨서 아예 스킵.
            if (walkFrames != null)
                SetSpriteArray(serialized, "walkFrames", walkFrames);

            var portraitProp = serialized.FindProperty("portrait") ?? serialized.FindProperty("sprite");
            if (portraitProp != null && idleFrames.Length > 0)
                portraitProp.objectReferenceValue = idleFrames[0];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
        }

        private static void SetSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[SpumBattleSpriteBaker] {so.targetObject.name}: '{propName}' 필드를 못 찾음");
                return;
            }
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }
}
