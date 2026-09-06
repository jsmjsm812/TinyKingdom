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

        /// <summary>배경색 밝기(휘도)에 맞춰 항상 읽히는 글자색을 골라준다 — 어두운 카드엔 흰색,
        /// 밝은 카드엔 검은색("배경이 어두운 카드는 흰색, 밝은 카드는 검은색" 피드백). 등급별
        /// 카드 배경(GetCardColor)처럼 코드에서 동적으로 색을 입히는 곳은 배경만 바꾸고 글자색을
        /// 고정해두면, 나중에 등급 색이 바뀌었을 때 글자가 안 보이게 될 수 있다 — 매번 이 함수로
        /// 다시 계산하면 그럴 일이 없다.</summary>
        public static Color GetReadableTextColor(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.5f ? Color.black : Color.white;
        }
    }
}
