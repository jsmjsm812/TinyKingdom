using System.Collections.Generic;
using UnityEngine;
using WitchHour.Data;
using WitchHour.Field;

namespace WitchHour.Combat
{
    /// <summary>
    /// 배치된 수호자의 런타임 인스턴스. 사거리 안 InvaderUnit.ActiveUnits를 찾아 공속마다 공격한다.
    /// 등급(rarity)은 GuardianData에 고정, 성급(star)만 합성으로 바뀌므로 별도 필드로 들고 있는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GuardianUnit : MonoBehaviour
    {
        private static readonly float[] StarDamageMultiplier = { 1f, 1.8f, 3.0f };

        public GuardianData Data { get; private set; }
        public int StarLevel { get; private set; } = 1;
        public Vector2 FieldPosition { get; private set; }

        private float _attackTimer;

        public void Setup(GuardianData data, int starLevel = 1)
        {
            Data = data;
            StarLevel = starLevel;
            _attackTimer = 0f;
        }

        public void SetFieldPosition(Vector2 position)
        {
            FieldPosition = position;
        }

        public void SetStarLevel(int starLevel)
        {
            StarLevel = starLevel;
        }

        private void Update()
        {
            if (Data == null) return;

            _attackTimer += Time.deltaTime;
            float attackCooldown = 1f / Data.attackSpeed;
            if (_attackTimer < attackCooldown) return;

            if (TryAttack())
                _attackTimer = 0f;
        }

        private bool TryAttack()
        {
            float rangePixels = Data.range * FieldConstants.UnitSize;
            float damage = Data.attackPower * StarDamageMultiplier[StarLevel - 1];

            switch (Data.attackType)
            {
                case AttackType.Multi:
                    return AttackNearest(rangePixels, damage, maxTargets: 2);
                case AttackType.Pierce:
                    return AttackNearest(rangePixels, damage, maxTargets: int.MaxValue);
                case AttackType.Area:
                    return AttackAreaAroundNearest(rangePixels, damage);
                default:
                    // Single / DotSingle / BuffSingle: MVP는 즉발 단일 피해로 처리.
                    // 도트·버프 연출은 이 타겟팅 로직을 안 건드리고 이펙트 레이어만 나중에 얹으면 됨.
                    return AttackNearest(rangePixels, damage, maxTargets: 1);
            }
        }

        private bool AttackNearest(float rangePixels, float damage, int maxTargets)
        {
            var targets = FindTargetsInRange(rangePixels, maxTargets);
            if (targets.Count == 0) return false;

            foreach (var target in targets)
                target.TakeDamage(damage);
            return true;
        }

        private bool AttackAreaAroundNearest(float rangePixels, float damage)
        {
            var nearest = FindTargetsInRange(rangePixels, 1);
            if (nearest.Count == 0) return false;

            float areaRadiusPixels = Data.areaRadius * FieldConstants.UnitSize;
            Vector2 center = nearest[0].FieldPosition;

            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(invader.FieldPosition, center) <= areaRadiusPixels)
                    invader.TakeDamage(damage);
            }
            return true;
        }

        private List<InvaderUnit> FindTargetsInRange(float rangePixels, int maxCount)
        {
            var inRange = new List<InvaderUnit>();
            foreach (var invader in InvaderUnit.ActiveUnits)
            {
                if (Vector2.Distance(FieldPosition, invader.FieldPosition) <= rangePixels)
                    inRange.Add(invader);
            }

            // 가장 진행이 많이 된(=결계에 가까운) 적을 우선 타격 — 방치 시 결계 피해로 직결되는 개체부터 정리.
            inRange.Sort((a, b) => a.FieldPosition.y.CompareTo(b.FieldPosition.y));

            if (inRange.Count > maxCount)
                inRange.RemoveRange(maxCount, inRange.Count - maxCount);

            return inRange;
        }
    }
}
