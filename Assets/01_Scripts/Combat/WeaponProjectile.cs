using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 수호자 공격 시 날아가는 투사체. 처음엔 무료 에셋의 "파이어볼트" 이펙트를 전원 공통으로
    /// 썼다가, 캐릭터가 든 무기 자체를 날리는 방식도 써봤는데 둘 다 어색했다(전자는 그림체가
    /// 안 맞고, 후자는 무기가 회전하는 게 부자연스러움). 지금은 GuardianData.projectileSprite에
    /// 클래스별로 어울리는 기성 투사체(화살/파이어볼/아이스볼트 — TinyKingdom 타워디펜스 팩의
    /// 원래 타워 발사체 스프라이트, SpumBattleSpriteBaker.AssignProjectileSprites가 지정)를
    /// 미리 정해두고 그걸 날린다. 근접 무기 클래스는 projectileSprite가 일부러 null이라, 그 경우
    /// 날아가는 오브젝트 없이 MeleeImpactEffect(타격 순간 즉시 섬광)로 대신한다.
    /// </summary>
    public class WeaponProjectile : MonoBehaviour
    {
        private const float WeaponSize = 190f;
        private const float FlightDuration = 0.3f;

        public static void Spawn(Vector2 from, Vector2 to, Sprite projectileSprite, float rotationOffsetDegrees = 0f)
        {
            if (projectileSprite == null)
            {
                MeleeImpactEffect.Spawn(to);
                return;
            }

            RectTransform parent = GridManager.BattlefieldRoot;
            if (parent == null) return;

            var go = new GameObject("WeaponProjectile", typeof(RectTransform), typeof(Image), typeof(WeaponProjectile));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = from;
            rect.sizeDelta = new Vector2(WeaponSize, WeaponSize);

            var image = go.GetComponent<Image>();
            image.sprite = projectileSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // 화살처럼 방향성이 뚜렷한 스프라이트는 날아가는 방향을 보고 있어야 자연스럽다.
            // 파이어볼/아이스볼트처럼 방향이 없는 대칭 스프라이트는 회전해도 티가 안 나 무해함.
            // rotationOffsetDegrees는 원본 그림이 "오른쪽(0도)"이 아닌 다른 기본 방향으로 그려진
            // 경우의 보정(예: 근접 무기 원본은 날 끝이 위쪽을 향해 그려져 있어 90도 필요) — 이 값을
            // 빼줘야 끝이 실제로 목표 쪽을 향한 채(창처럼) 날아간다.
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle - rotationOffsetDegrees);

            go.GetComponent<WeaponProjectile>().StartCoroutine(Fly(rect, image, from, to));
        }

        private static IEnumerator Fly(RectTransform rect, Image image, Vector2 from, Vector2 to)
        {
            float elapsed = 0f;
            while (elapsed < FlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FlightDuration);
                rect.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            Destroy(image.gameObject);
        }
    }
}
