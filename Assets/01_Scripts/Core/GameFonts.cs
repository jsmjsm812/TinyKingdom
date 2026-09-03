using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>
    /// 게임 전역 폰트를 한 곳에서 통일한다. 예전엔 UI를 지을 때마다 유니티 기본 내장 폰트
    /// (Resources.GetBuiltinResource&lt;Font&gt;("LegacyRuntime.ttf"))를 따로 불러 썼는데, 이제
    /// 픽셀 폰트(DNF BitBit)로 통일하기로 해서 여기 한 곳만 바꾸면 전체가 같이 바뀌게 정리했다.
    /// Resources 폴더 안에 둬서(Assets/02_Data/Resources/Fonts) 에디터 툴(빌드 스크립트)과
    /// 런타임 코드(FloatingTextEffect 등) 양쪽에서 동일하게 Resources.Load로 불러올 수 있다.
    /// </summary>
    public static class GameFonts
    {
        private const string ResourcePath = "Fonts/DNFBitBitv2";

        private static Font _main;

        public static Font Main
        {
            get
            {
                if (_main == null)
                {
                    _main = Resources.Load<Font>(ResourcePath);
                    if (_main == null)
                        Debug.LogError($"[GameFonts] '{ResourcePath}' 폰트를 못 찾았습니다 — " +
                                        $"Assets/02_Data/Resources/{ResourcePath}.ttf가 있는지 확인하세요.");
                }
                return _main;
            }
        }
    }
}
