using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WitchHour.Core;

namespace WitchHour.EditorTools
{
    /// <summary>
    /// Boot 씬에 AudioManager를 만들고(BootLoader.cs 주석에 이미 자리를 비워뒀던 부분),
    /// Brackeys 2D Mega Pack의 무료 SFX를 클립 필드에 연결한다. BGM은 아직 프로젝트에
    /// 트랙이 없어서 자리만 비워둔다 — Assets/04_Audio/BGM에 트랙을 넣고 나면
    /// AudioManager 인스펙터에서 menuBgm/battleBgm에 직접 끌어넣으면 됨.
    /// </summary>
    public static class AudioBootstrap
    {
        private const string BootScenePath = "Assets/06_Scenes/Boot.unity";
        private const string SfxDir = "Assets/Brackeys/2D Mega Pack/Sounds";
        private const string BgmDir = "Assets/OldCartoonMusicFree";
        private const string EightBitDir = "Assets/8-bit SFX & UI Sounds";
        private const string ShootingDir = "Assets/ShootingSound";

        [MenuItem("TinyKingdom/Setup Audio Manager (Boot Scene)")]
        public static void SetupAudioManager()
        {
            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            var go = GameObject.Find("AudioManager");
            if (go == null)
                go = new GameObject("AudioManager", typeof(AudioManager));
            else if (go.GetComponent<AudioManager>() == null)
                go.AddComponent<AudioManager>();

            var so = new SerializedObject(go.GetComponent<AudioManager>());

            // BGM은 일단 뺀다("브금 없애고" 요청) — menuBgm/battleBgm을 비워두면
            // AudioManager.PlayBgm(null)이 그냥 무음 처리한다(죽지 않음).
            ClearClip(so, "menuBgm");
            ClearClip(so, "battleBgm");

            // 기존 Brackeys SFX 그대로 유지.
            SetClip(so, "sfxButtonClick", "ButtonPress.wav");
            SetClip(so, "sfxPurchase", "Bonus.wav");
            SetClip(so, "sfxReroll", "Whoosh.wav");
            SetClip(so, "sfxHit", "Hit.wav");
            SetClip(so, "sfxInvaderDeath", "Explosion.wav");
            SetClip(so, "sfxInvaderSpawn", "Spawn.wav");
            SetClip(so, "sfxCountdownTick", "RespawnCountdown.wav");
            // 웨이브 시작 전 효과음도 뺀다("웨이브 시작전 효과음 없애" 요청).
            ClearClip(so, "sfxWaveStart");

            // 새로 임포트된 팩 중 이 판타지 타워디펜스 톤에 더 잘 맞는 것만 골라 업그레이드.
            // (GameAppSFXPack002FuturisticUSERwet은 "미래적" 톤이라 이 중세 판타지 게임과 안 어울려서 일단 안 씀)
            SetClipAt(so, "sfxAttack", $"{ShootingDir}/crossbow.wav"); // 근거리 무기(석궁) 톤 — 마법 계열(magic_01)에서 교체
            SetClipAt(so, "sfxMerge", $"{EightBitDir}/Level_Up/5_LevelUp.wav"); // 합성 성급 상승과 의미가 정확히 맞음
            SetClipAt(so, "sfxVictory", $"{EightBitDir}/Victory/9_Victory.wav"); // 예전엔 구매(Bonus.wav)와 소리가 겹쳤음
            SetClipAt(so, "sfxDefeat", $"{EightBitDir}/Death_Screen/3_DeathScreen.wav");

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[AudioBootstrap] AudioManager에 BGM(OldCartoonMusicFree) 2트랙 + 효과음 일부(8-bit SFX, ShootingSound) 업그레이드 연결 완료.");
        }

        private static void SetClip(SerializedObject so, string fieldName, string fileName)
            => SetClipAt(so, fieldName, $"{SfxDir}/{fileName}");

        private static void ClearClip(SerializedObject so, string fieldName)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = null;
        }

        private static void SetClipAt(SerializedObject so, string fieldName, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioBootstrap] 클립을 못 찾음: {path} (필드 '{fieldName}'는 비워둠)");
                return;
            }
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[AudioBootstrap] AudioManager에 '{fieldName}' 필드가 없음");
                return;
            }
            prop.objectReferenceValue = clip;
        }
    }
}
