using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Core;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>금화 획득 등을 짧게 띄워 보여주는 텍스트 — 위로 떠오르며 사라진다.</summary>
    public class FloatingTextEffect : MonoBehaviour
    {
        private const float Duration = 0.6f;
        private const float RiseDistance = 50f;

        public static void Spawn(Vector2 position, string text, Color color)
        {
            RectTransform parent = GridManager.BattlefieldRoot;
            if (parent == null) return;

            var go = new GameObject("FloatingTextEffect", typeof(RectTransform), typeof(Text), typeof(FloatingTextEffect));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120, 40);

            var uiText = go.GetComponent<Text>();
            uiText.text = text;
            uiText.font = GameFonts.Main;
            uiText.fontSize = 24;
            uiText.fontStyle = FontStyle.Bold;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = color;
            uiText.raycastTarget = false;

            go.GetComponent<FloatingTextEffect>().StartCoroutine(Animate(rect, uiText, position));
        }

        private static IEnumerator Animate(RectTransform rect, Text text, Vector2 startPos)
        {
            Color startColor = text.color;
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;
                rect.anchoredPosition = startPos + Vector2.up * (RiseDistance * t);
                text.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                yield return null;
            }
            Destroy(text.gameObject);
        }
    }
}
