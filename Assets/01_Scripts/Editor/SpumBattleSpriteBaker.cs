using System.IO;
using System.Linq;
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
            // visualScale: 무기를 뺀 몸통 bounds 기준 자동 줌이 캐릭터마다 완벽히 균일하진 않아서
            // (무기-몸통 비율이 캐릭터마다 다름) 눈으로 확인된 편차만 여기서 수동 보정한다.
            // 민병(Lumi)은 자동 계산 결과가 다른 캐릭터보다 커서 슬롯을 넘어섰고, 야만전사(Dori)는
            // 반대로 작아 보였다는 피드백 반영 — 재굽기 후 다른 캐릭터도 안 맞으면 여기 값을 추가/조정.
            new CharConfig { name = "Lumi",    dataAssetPath = "Assets/02_Data/Guardians/Lumi.asset",    prefabName = "Warrior",      rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF", visualScale = 0.85f },
            new CharConfig { name = "Noa",     dataAssetPath = "Assets/02_Data/Guardians/Noa.asset",     prefabName = "BanditScout",  rig = RigType.Humanoid, isGuardian = true, actionClip = "AttackF" },
            new CharConfig { name = "Mira",    dataAssetPath = "Assets/02_Data/Guardians/Mira.asset",    prefabName = "Archer",       rig = RigType.Humanoid, isGuardian = true, actionClip = "ShootBowF" },
            new CharConfig { name = "Dori",    dataAssetPath = "Assets/02_Data/Guardians/Dori.asset",    prefabName = "BanditElder",  rig = RigType.Troll,    isGuardian = true, actionClip = "AttackF", visualScale = 1.15f },
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

        [MenuItem("TinyKingdom/Bake TinyKingdom Battle Sprites")]
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

        // 무기 자체를 던지니 어색하다는 피드백으로 클래스별 기성 투사체(전부 TinyKingdom 타워디펜스
        // 팩 안에 이미 있던 것, 원래는 타워 발사체용이라 캐릭터 그림체와 잘 어울림)로 바꿨었는데,
        // 근접 무기 클래스(검/도끼/단검)만은 어울리는 기성 에셋이 없어서 임팩트 이펙트로 대신했다가
        // "그냥 그 무기 날리는 게 나은 것 같다"는 피드백으로 다시 원래대로 — 근접 4명만 자기 무기
        // (weaponSprite, 이미 구워둔 값)를 투사체로 쓴다. spritePath가 null인 항목이 그 표시다.
        private const string ArrowPath = "Assets/TinyKingdom/TowerDefense/Sprites/Creatures/Weapons/Projectiles/Arrow.png";
        private const string FireballPath = "Assets/TinyKingdom/TowerDefense/Sprites/Fx/FiraballTower/Projectile.png";
        private const string IceBoltPath = "Assets/TinyKingdom/TowerDefense/Sprites/Fx/IceBoltTower/Projectile.png";

        // 화살(Arrow.png)은 원본이 오른쪽(0도)을 보고 그려져 있어 보정 없이 그대로 회전시키면 되지만,
        // 근접 무기 4종(검/단검/클럽/왕의 십자검) 원본은 전부 날 끝이 위쪽(90도)을 향해 그려져 있다
        // (직접 확인함, 넷 다 같은 방향) — 그대로 던지면 창처럼 끝이 목표를 향하는 게 아니라 옆으로
        // 누운 채 날아간다. 90도를 보정해서 끝이 항상 목표 쪽을 향하게 한다.
        private const float MeleeWeaponRotationOffset = 90f;

        private static readonly (string guardianName, string spritePath, float rotationOffset)[] ProjectileAssignments =
        {
            ("Mira", ArrowPath, 0f),                        // 궁수
            ("Irene", ArrowPath, 0f),                       // 명사수
            ("Astel", FireballPath, 0f),                    // 대마법사
            ("Selene", IceBoltPath, 0f),                    // 빙결사
            ("Sera", ArrowPath, 0f),                        // 연금술사 — 포션 투척에 딱 맞는 에셋이 없어 임시로 화살
            ("Chloe", ArrowPath, 0f),                       // 마녀 — 위와 동일
            ("Lumi", null, MeleeWeaponRotationOffset),      // 민병 — 근접 무기, 자기 weaponSprite를 던짐
            ("Noa", null, MeleeWeaponRotationOffset),       // 도적 — 근접 무기, 자기 weaponSprite를 던짐
            ("Dori", null, MeleeWeaponRotationOffset),      // 야만전사 — 근접 무기, 자기 weaponSprite를 던짐
            ("Ophelia", null, MeleeWeaponRotationOffset),   // 왕 — 근접 무기, 자기 weaponSprite를 던짐
        };

        [MenuItem("TinyKingdom/Assign Guardian Projectile Sprites")]
        public static void AssignProjectileSprites()
        {
            int done = 0;
            foreach (var (guardianName, spritePath, rotationOffset) in ProjectileAssignments)
            {
                var cfg = System.Array.Find(Guardians, c => c.name == guardianName);
                if (cfg == null)
                {
                    Debug.LogWarning($"[SpumBattleSpriteBaker] Guardians 목록에서 '{guardianName}'을 못 찾음.");
                    continue;
                }

                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(cfg.dataAssetPath);
                if (so == null)
                {
                    Debug.LogError($"[SpumBattleSpriteBaker] 데이터 에셋을 못 찾음: {cfg.dataAssetPath}");
                    continue;
                }
                var serialized = new SerializedObject(so);

                // spritePath가 null(근접 무기 클래스)이면 기성 투사체 대신 이미 구워둔 자기 무기
                // 스프라이트(weaponSprite)를 그대로 투사체로 쓴다.
                Sprite sprite = spritePath != null
                    ? LoadProjectileSprite(spritePath)
                    : serialized.FindProperty("weaponSprite")?.objectReferenceValue as Sprite;

                if (sprite == null)
                    Debug.LogWarning($"[SpumBattleSpriteBaker] {guardianName}: 투사체로 쓸 스프라이트를 못 찾음 " +
                                      (spritePath != null ? $"(경로: {spritePath})" : "(weaponSprite가 비어있음 — 먼저 Bake TinyKingdom Battle Sprites를 실행했는지 확인)"));

                var prop = serialized.FindProperty("projectileSprite");
                prop.objectReferenceValue = sprite;
                var offsetProp = serialized.FindProperty("projectileRotationOffsetDegrees");
                if (offsetProp != null) offsetProp.floatValue = rotationOffset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(so);
                done++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpumBattleSpriteBaker] 투사체 스프라이트 {done}명 지정 완료 " +
                      "(근접 무기 캐릭터 4명은 자기 무기를 투사체로 씀).");
        }

        // Fireball/IceBolt처럼 하나의 PNG 안에 여러 프레임이 슬라이스돼 있는 경우 첫 프레임("0")만
        // 골라 정적 스프라이트로 쓴다 — 투사체가 짧게 날아가는 동안 굳이 애니메이션까지 재생할
        // 필요는 없어서(화살처럼 정지 아이콘 한 장으로 충분).
        private static Sprite LoadProjectileSprite(string path)
        {
            var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            var namedZero = all.OfType<Sprite>().FirstOrDefault(s => s.name == "0" || s.name.EndsWith("_0"));
            if (namedZero != null) return namedZero;

            return all.OfType<Sprite>().FirstOrDefault();
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

                // TinyKingdom 리그는 무기를 "Weapon"이라는 이름의 별도 SpriteRenderer 자식으로
                // 들고 있다(활/검/지팡이 등 — 몸통과 분리된 독립 스프라이트). 프리팹 파일 안엔
                // Weapon 이름이 여러 개(포즈별) 있지만, Animator가 현재 포즈에서 보여줄 것만
                // GameObject.SetActive로 켜두는 방식이라 GetComponentsInChildren(기본값=비활성
                // 제외)로 걸러진 renderers 배열엔 지금 이 포즈에서 실제로 보이는 것 하나만 남는다
                // — 공격 투사체를 이 무기 자체로 날리는 데 쓴다(WeaponProjectile 참고).
                Sprite weaponSprite = null;
                foreach (var r in renderers)
                {
                    if (r.gameObject.name == "Weapon" && r.sprite != null)
                    {
                        weaponSprite = r.sprite;
                        break;
                    }
                }
                if (weaponSprite == null)
                    Debug.LogWarning($"[SpumBattleSpriteBaker] {cfg.name}: \"Weapon\" 스프라이트를 못 찾음 — " +
                                      "공격 투사체가 무기 대신 폴백(구 파이어볼트 이펙트)으로 표시됨.");

                // bounds를 Instantiate 직후(=Animator 기본/바인드 포즈, 실제로 굽는 Idle 포즈와
                // 다를 수 있음) 기준으로 재면 카메라 줌/센터가 실제 찍히는 실루엣과 어긋난다 —
                // "왕은 안 잡히는데 명사수는 잡힌다" 문제의 원인이었다. 실제로 아래에서 굽는
                // Idle 클립의 0프레임을 미리 한 번 샘플링해서 그 포즈 기준으로 bounds를 재면,
                // 캐릭터마다 바인드 포즈가 얼마나 다르든 항상 실제 찍히는 그림과 정렬이 맞는다.
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animator.gameObject, idleClip, 0f);
                AnimationMode.EndSampling();

                // 발 기준선(floorY)과 카메라 줌(orthographicSize) 모두 무기를 뺀 "몸통" 파츠
                // bounds로 잰다. 처음엔 무기까지 포함한 전체 bounds를 썼는데 두 가지 문제가 있었다:
                // (1) 활의 아래쪽 시위처럼 무기가 발보다 아래로 내려오면 그게 발 기준선이 되어
                //     캐릭터가 캔버스 안에서 붕 뜬 것처럼 찍히고,
                // (2) 활을 옆으로 넓게 벌린 궁수처럼 무기가 몸통보다 훨씬 넓게 뻗으면 그 폭에 맞춰
                //     카메라가 필요 이상으로 줌아웃되어, 같은 크기 박스 안에서 유독 작게 찍힌다
                //     ("궁수만 작고 위치도 안 맞는다" 피드백의 원인 — 발 기준선만 고쳤을 땐 (1)만
                //     해결되고 (2)가 남아있었다).
                // 몸통 기준으로 통일하면 전원 같은 기준으로 발 위치·크기가 맞는다. 대신 무기가
                // 프레임 가장자리에 살짝 걸릴 수 있는데(특히 옆으로 크게 뻗는 포즈), 패딩을 넉넉히
                // 둬서 웬만해선 안 잘리게 한다.
                bool hasBodyRenderer = false;
                Bounds bodyBounds = default;
                foreach (var r in renderers)
                {
                    if (!r.gameObject.activeInHierarchy || r.gameObject.name == "Weapon") continue;
                    if (!hasBodyRenderer) { bodyBounds = r.bounds; hasBodyRenderer = true; }
                    else bodyBounds.Encapsulate(r.bounds);
                }
                if (!hasBodyRenderer)
                {
                    // 안전장치: 몸통 파츠를 하나도 못 찾은 경우(예상 밖의 리그 구조)엔 전체 렌더러로 대체.
                    bodyBounds = renderers[0].bounds;
                    foreach (var r in renderers)
                        if (r.gameObject.activeInHierarchy) bodyBounds.Encapsulate(r.bounds);
                }
                // 패딩을 0.2로 키웠다가 몸통이 이미 큰 캐릭터(야만전사 등)가 필요 이상으로
                // 작아지는 부작용이 있어서 원래 비율(0.1)로 되돌림 — 무기 클리핑 여유보다
                // 캐릭터별 크기 일관성이 더 중요하다는 피드백.
                float pad = Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.y) * 0.1f;
                bodyBounds.Expand(pad);
                float floorY = bodyBounds.min.y;

                AnimationMode.StopAnimationMode();

                camGO = new GameObject("TKBakeCamera");
                EditorSceneManager.MoveGameObjectToScene(camGO, scene);
                var cam = camGO.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                // 캐릭터 자기 몸통 bounds 기준으로 확대 — 다들 자기 프레임을 최대한 꽉 채운다.
                cam.orthographicSize = Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.y);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;

                // 캐릭터마다 가로/세로 비율이 달라서(예: 옆으로 넓은 포즈 vs 위아래로 긴 포즈)
                // bounds.center를 그대로 카메라 중심으로 쓰면 정사각형 프레임 안에서 발 위치가
                // 캐릭터마다 서로 다른 높이에 찍힌다 — 같은 슬롯 높이에 배치해도 그리드에서
                // 캐릭터별로 키가 들쭉날쭉해 보이는 원인이었다("용사들 Y값이 안 맞는다" 피드백).
                // 대신 bounds 아래쪽(발)이 항상 프레임 맨 밑에 오도록 카메라를 세로로만 아래쪽에
                // 정렬해서, 어떤 캐릭터든 발 기준선이 구운 정사각형 이미지의 같은 위치에 찍히게 한다.
                //
                // 가로는 반대로 bounds.center.x를 쓰면 안 된다 — TinyKingdom 리그는 전부 오른쪽을
                // 보는 포즈로 구워지는데(무기를 오른쪽/앞으로 든 자세), 그러면 무기가 뻗은 만큼
                // bounds 중심이 몸통보다 오른쪽으로 쏠려서 몸통은 캔버스 왼쪽에 찍힌다. 그런데
                // 게임에서는 기본값을 왼쪽 보기로 좌우 반전시키므로(GuardianUnit.Setup) "왼쪽에
                // 치우친 몸통"이 뒤집혀서 "오른쪽에 치우친 몸통"이 되어버린다 — "슬롯 안에서
                // 캐릭터가 오른쪽으로 쏠려 보인다" 피드백의 원인. instance.transform.position.x는
                // 위에서 Vector3.zero로 고정해둔 리그 자체의 좌우 중심선이라 무기 방향과 무관하게
                // 항상 안정적이므로 이걸 가로 기준으로 쓴다.
                camGO.transform.position = new Vector3(instance.transform.position.x, floorY + cam.orthographicSize, -10f);

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

                AssignToData(cfg.dataAssetPath, idleFrames, actionFrames, walkFrames, weaponSprite);
            }
            finally
            {
                // 카메라가 아직 이 RenderTexture를 targetTexture로 물고 있는 상태에서 바로
                // Release/DestroyImmediate 하면 "Releasing render texture that is set as
                // Camera.targetTexture!" 경고가 뜬다 — 카메라 쪽 참조부터 먼저 끊어야 한다.
                if (camGO != null)
                {
                    var cam = camGO.GetComponent<Camera>();
                    if (cam != null) cam.targetTexture = null;
                }
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

        private static void AssignToData(string dataAssetPath, Sprite[] idleFrames, Sprite[] actionFrames,
            Sprite[] walkFrames = null, Sprite weaponSprite = null)
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

            // weaponSprite는 GuardianData에만 있는 필드 — InvaderData엔 없으니 FindProperty가
            // null을 반환하면 조용히 스킵한다(침입자는 무기 투사체를 안 씀).
            var weaponProp = serialized.FindProperty("weaponSprite");
            if (weaponProp != null)
                weaponProp.objectReferenceValue = weaponSprite;

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
