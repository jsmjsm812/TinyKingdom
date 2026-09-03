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

        [MenuItem("WitchHour/Setup Audio Manager (Boot Scene)")]
        public static void SetupAudioManager()
        {
            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            var go = GameObject.Find("AudioManager");
            if (go == null)
                go = new GameObject("AudioManager", typeof(AudioManager));
            else if (go.GetComponent<AudioManager>() == null)
                go.AddComponent<AudioManager>();

            var so = new SerializedObject(go.GetComponent<AudioManager>());
            SetClip(so, "sfxButtonClick", "ButtonPress.wav");
            SetClip(so, "sfxPurchase", "Bonus.wav");
            SetClip(so, "sfxReroll", "Whoosh.wav");
            SetClip(so, "sfxAttack", "Shot.wav");
            SetClip(so, "sfxHit", "Hit.wav");
            SetClip(so, "sfxInvaderDeath", "Explosion.wav");
            SetClip(so, "sfxInvaderSpawn", "Spawn.wav");
            SetClip(so, "sfxMerge", "Bonus.wav");
            SetClip(so, "sfxWaveStart", "Whoosh.wav");
            SetClip(so, "sfxVictory", "Bonus.wav");
            SetClip(so, "sfxDefeat", "GameOver.wav");
            SetClip(so, "sfxCountdownTick", "RespawnCountdown.wav");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[AudioBootstrap] AudioManager를 Boot 씬에 배치하고 Brackeys 2D Mega Pack SFX 11개를 연결했습니다. " +
                       "BGM(menuBgm/battleBgm)은 아직 트랙이 없어서 비워뒀습니다 — 트랙을 구해서 " +
                       "Assets/04_Audio/BGM에 넣고 인스펙터에서 직접 연결해주세요.");
        }

        private static void SetClip(SerializedObject so, string fieldName, string fileName)
        {
            string path = $"{SfxDir}/{fileName}";
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
