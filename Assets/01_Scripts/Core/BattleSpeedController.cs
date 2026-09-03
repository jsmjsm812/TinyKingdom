using UnityEngine;
using UnityEngine.UI;

namespace WitchHour.Core
{
    /// <summary>
    /// x1/x2 배속 토글(GDD.md 3번). 버튼 자체에 컴포넌트를 붙여서 클릭 리스너가 자기 자신을
    /// 가리키게 했다 — ShopUI/RerollButton 때처럼 씬 바깥 오브젝트를 참조할 필요가 없어서
    /// 이 GameObject 통째로 프리팹 에셋으로 저장해도 안전하다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BattleSpeedController : MonoBehaviour
    {
        private const float NormalSpeed = 1f;
        private const float FastSpeed = 2f;

        [SerializeField] private Text label;

        private bool _isFast;

        private void Awake()
        {
            // 이전 전투에서 배속을 올린 채로 씬을 나갔더라도 새 전투는 항상 1배속으로 시작.
            Time.timeScale = NormalSpeed;
            _isFast = false;
            UpdateLabel();
        }

        private void OnDestroy()
        {
            // Battle 씬을 벗어날 때 배속이 다른 씬(상점/홈 등)까지 새어나가지 않게 원상복구.
            Time.timeScale = NormalSpeed;
        }

        public void ToggleSpeed()
        {
            _isFast = !_isFast;
            Time.timeScale = _isFast ? FastSpeed : NormalSpeed;
            UpdateLabel();
            AudioManager.Instance?.PlayButtonClick();
        }

        private void UpdateLabel()
        {
            if (label != null)
                label.text = _isFast ? "x2" : "x1";
        }
    }
}
