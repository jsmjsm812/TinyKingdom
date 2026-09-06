using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 근접 무기(검·도끼·단검 등)를 든 수호자의 공격 연출. 이 클래스는 무기를 던지지 않는 게
    /// 더 자연스러워서(WeaponProjectile 참고 — GuardianData.projectileSprite가 null인 캐릭터),
    /// 날아가는 오브젝트 없이 타격 순간 대상 위치에 즉시 나타났다 사라지는 짧은 임팩트 섬광으로
    /// 대신한다. DeathPopEffect(사망 시 커지는 원)와 같은 절차적 원형 스프라이트를 재사용하지만,
    /// 훨씬 짧고(투사체 비행시간과 맞춤) 밝은 흰색이라 "베였다/찔렸다"는 느낌에 가깝다.
    /// </summary>
    public class MeleeImpactEffect : MonoBehaviour
    {
        // 0.12초·EndSize 140이었을 땐 너무 짧고 작아서 거의 안 보인다는 피드백 — 눈에 확실히
        // 띄도록 지속시간과 최대 크기를 키움(침입자 피격 플래시 때와 같은 이유의 같은 조정).
        private const float Duration = 0.22f;
        private const float StartSize = 40f;
        private const float EndSize = 220f;
        private static readonly Color FlashColor = new Color(1f, 0.95f, 0.6f, 0.95f);

        private static Sprite _circleSprite;

        public static void Spawn(Vector2 position)
        {
            RectTransform parent = GridManager.BattlefieldRoot;
            if (parent == null) return;

            var go = new GameObject("MeleeImpactEffect", typeof(RectTransform), typeof(Image), typeof(MeleeImpactEffect));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(StartSize, StartSize);

            var image = go.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = FlashColor;
            image.raycastTarget = false;

            go.GetComponent<MeleeImpactEffect>().StartCoroutine(Animate(rect, image));
        }

        private static IEnumerator Animate(RectTransform rect, Image image)
        {
            Color startColor = image.color;
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;
                float size = Mathf.Lerp(StartSize, EndSize, t);
                rect.sizeDelta = new Vector2(size, size);
                image.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }
            Destroy(image.gameObject);
        }

        // DeathPopEffect와 동일한 방식으로 만드는 가장자리가 옅어지는 원형 텍스처 — 용도가 달라서
        // (색·크기·수명이 다름) 텍스처를 공유하지 않고 이 클래스 전용으로 한 번만 만들어 캐싱한다.
        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Clamp01(1f - dist);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }
    }
}
