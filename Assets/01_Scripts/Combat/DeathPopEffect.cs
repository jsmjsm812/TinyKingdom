using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 침입자가 죽을 때 잠깐 나타났다 커지면서 사라지는 원형 효과. FloatingTextEffect와 같은 패턴
    /// (프리팹 없이 즉석 생성, 수명이 짧아 풀링 없음). 원형 스프라이트는 에디터 전용 API 없이
    /// 런타임에서 한 번만 절차적으로 만들어서 캐싱해 재사용한다.
    /// </summary>
    public class DeathPopEffect : MonoBehaviour
    {
        private const float Duration = 0.25f;
        private const float StartSize = 50f;
        private const float EndSize = 110f;
        private static readonly Color PopColor = new Color(1f, 0.95f, 0.8f, 0.85f);

        private static Sprite _circleSprite;

        public static void Spawn(Vector2 position)
        {
            RectTransform parent = GridManager.BattlefieldRoot;
            if (parent == null) return;

            var go = new GameObject("DeathPopEffect", typeof(RectTransform), typeof(Image), typeof(DeathPopEffect));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(StartSize, StartSize);

            var image = go.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = PopColor;
            image.raycastTarget = false;

            go.GetComponent<DeathPopEffect>().StartCoroutine(Animate(rect, image));
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

        // 가장자리로 갈수록 옅어지는 원형 텍스처를 한 번만 만들어서 static으로 캐싱한다.
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
