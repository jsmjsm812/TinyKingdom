using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 수호자 공격 시 날아가는 불덩이. Pegasus Studios의 무료 "RPG Essentials - Pixel Fire" 팩에서
    /// 가져온 프레임을 쓴다 — 원본 프리팹은 SpriteRenderer+Animator라 월드스페이스 전용이고 우리
    /// 필드는 전부 UGUI라 그대로 못 쓰므로, 개별 프레임 PNG만 Resources.Load로 꺼내 UI Image로
    /// 직접 재생한다(에셋 자체 폴더 구조에 "Resources"가 포함돼 있어 런타임 로드가 가능함).
    /// </summary>
    public class FireBoltProjectile : MonoBehaviour
    {
        private const string ResourceFolder = "Fire Bolts/Fire bolt base/Fire bolt base";
        private const int FrameCount = 18;
        private const float FrameRate = 60f; // 원본 .anim의 샘플레이트(60fps)와 맞춤
        private const float FlightDuration = 0.3f;
        // 소스 프레임이 92x32로 작은데 필드 전체가 FixBattleLayout에서 다시 축소되니(약 0.6배),
        // 화면에서 눈에 띄게 하려고 슬롯 크기(180)에 가깝게 크게 잡는다.
        private const float Size = 150f;

        private static Sprite[] _frames;

        public static void Spawn(Vector2 from, Vector2 to)
        {
            Sprite[] frames = GetFrames();
            if (frames == null || frames.Length == 0) return;

            RectTransform parent = GridManager.BattlefieldRoot;
            if (parent == null) return;

            var go = new GameObject("FireBoltProjectile", typeof(RectTransform), typeof(Image), typeof(FireBoltProjectile));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = from;
            rect.sizeDelta = new Vector2(Size, Size);

            Vector2 delta = to - from;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = go.GetComponent<Image>();
            image.sprite = frames[0];
            image.preserveAspect = true;
            image.raycastTarget = false;

            go.GetComponent<FireBoltProjectile>().StartCoroutine(Fly(rect, image, frames, from, to));
        }

        private static IEnumerator Fly(RectTransform rect, Image image, Sprite[] frames, Vector2 from, Vector2 to)
        {
            float elapsed = 0f;
            while (elapsed < FlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FlightDuration);
                rect.anchoredPosition = Vector2.Lerp(from, to, t);
                image.sprite = frames[Mathf.FloorToInt(elapsed * FrameRate) % frames.Length];
                yield return null;
            }
            Destroy(image.gameObject);
        }

        // 프레임 18장을 한 번만 로드해서 static으로 캐싱 — 공격마다 다시 로드하지 않는다.
        private static Sprite[] GetFrames()
        {
            if (_frames != null) return _frames;

            var frames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                string path = ResourceFolder + (i + 1);
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite == null)
                {
                    var all = Resources.LoadAll<Sprite>(path);
                    if (all.Length > 0) sprite = all[0];
                }
                frames[i] = sprite;
            }
            _frames = frames;
            return _frames;
        }
    }
}
