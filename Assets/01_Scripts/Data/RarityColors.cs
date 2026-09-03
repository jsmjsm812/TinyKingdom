using UnityEngine;

namespace WitchHour.Data
{
    /// <summary>등급(★1~3)에 대응하는 공용 색상 — 상점 카드 등 여러 UI가 같은 기준으로 색을 쓰도록 모아둔다.</summary>
    public static class RarityColors
    {
        public static Color GetCardColor(GuardianRarity rarity)
        {
            switch (rarity)
            {
                case GuardianRarity.Star1: return new Color(0.20f, 0.22f, 0.28f);
                case GuardianRarity.Star2: return new Color(0.28f, 0.20f, 0.34f);
                case GuardianRarity.Star3: return new Color(0.36f, 0.28f, 0.12f);
                default: return new Color(0.2f, 0.2f, 0.2f);
            }
        }

        public static Color GetAccentColor(GuardianRarity rarity)
        {
            switch (rarity)
            {
                case GuardianRarity.Star1: return new Color(0.6f, 0.65f, 0.75f);
                case GuardianRarity.Star2: return new Color(0.75f, 0.55f, 0.95f);
                case GuardianRarity.Star3: return new Color(1f, 0.8f, 0.3f);
                default: return Color.white;
            }
        }
    }
}
