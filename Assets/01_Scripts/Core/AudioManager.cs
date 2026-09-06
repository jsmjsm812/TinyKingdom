using UnityEngine;
using UnityEngine.SceneManagement;

namespace WitchHour.Core
{
    /// <summary>
    /// 전역 BGM/SFX 재생기. Boot 씬에서 한 번만 만들어지고 DontDestroyOnLoad로 씬을 넘나들며
    /// 살아있는다(BootLoader.cs 주석에 이미 자리를 비워뒀던 부분). 씬 이름 기준으로 BGM을
    /// 자동 전환하고, 각 게임 이벤트는 정적 Instance를 통해 SFX 메서드를 직접 호출한다.
    ///
    /// SFX 클립은 주로 Brackeys 2D Mega Pack, 일부는 8-bit SFX & UI Sounds / ShootingSound
    /// 팩에서 가져온다(AudioBootstrap.SetupAudioManager 참고). BGM은 OldCartoonMusicFree 팩
    /// 2트랙(로비용/배틀용)을 쓴다. 클립이 비어있어도 죽지 않게(Play(null) 무시) 방어는 유지.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM (없으면 무음 — 나중에 트랙 추가)")]
        [SerializeField] private AudioClip menuBgm;
        [SerializeField] private AudioClip battleBgm;

        [Header("SFX (Brackeys 2D Mega Pack)")]
        [SerializeField] private AudioClip sfxButtonClick;
        [SerializeField] private AudioClip sfxPurchase;
        [SerializeField] private AudioClip sfxReroll;
        [SerializeField] private AudioClip sfxAttack;
        [SerializeField] private AudioClip sfxHit;
        [SerializeField] private AudioClip sfxInvaderDeath;
        [SerializeField] private AudioClip sfxInvaderSpawn;
        [SerializeField] private AudioClip sfxMerge;
        [SerializeField] private AudioClip sfxWaveStart;
        [SerializeField] private AudioClip sfxVictory;
        [SerializeField] private AudioClip sfxDefeat;
        [SerializeField] private AudioClip sfxCountdownTick;

        private const string BgmVolumeKey = "witchhour_bgm_volume";
        private const string SfxVolumeKey = "witchhour_sfx_volume";

        public float BgmVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        private void Awake()
        {
            // 씬을 재구성(Restructure Into Boot-Title-Home-Battle)하며 Boot를 다시 열 때마다
            // 중복 생성될 수 있어 방어— 이미 하나 살아있으면 새로 생긴 쪽을 정리한다.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 0.6f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            _bgmSource.volume = BgmVolume;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "Battle":
                    PlayBgm(battleBgm);
                    break;
                case "Title":
                case "Home":
                    PlayBgm(menuBgm);
                    break;
                // Boot는 순간적으로 지나가는 씬이라 BGM 전환 없음.
            }
        }

        private void PlayBgm(AudioClip clip)
        {
            if (clip == null)
            {
                _bgmSource.Stop();
                return;
            }
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            _bgmSource.volume = BgmVolume;
            PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, SfxVolume);
        }

        public void PlayButtonClick() => Play(sfxButtonClick);
        public void PlayPurchase() => Play(sfxPurchase);
        public void PlayReroll() => Play(sfxReroll);
        public void PlayAttack() => Play(sfxAttack);
        public void PlayHit() => Play(sfxHit);
        public void PlayInvaderDeath() => Play(sfxInvaderDeath);
        public void PlayInvaderSpawn() => Play(sfxInvaderSpawn);
        public void PlayMerge() => Play(sfxMerge);
        public void PlayWaveStart() => Play(sfxWaveStart);
        public void PlayVictory() => Play(sfxVictory);
        public void PlayDefeat() => Play(sfxDefeat);
        public void PlayCountdownTick() => Play(sfxCountdownTick);
    }
}
