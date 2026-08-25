using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Data
{
    [CreateAssetMenu(fileName = "New Zone", menuName = "WitchHour/Zone Data")]
    public class ZoneData : ScriptableObject
    {
        [Header("기본 정보")]
        public string zoneName;
        [Tooltip("1, 2, 3")]
        public int zoneIndex;

        [Header("배율 (GDD.md 14번 표 그대로)")]
        public float hpMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float manaMultiplier = 1f;

        [Header("구역 전용 보스 (10웨이브)")]
        public InvaderData bossInvader;

        [TextArea]
        public string specialRuleDescription;

        [Header("클리어 시 해금되는 수호자")]
        public List<GuardianData> unlockedGuardians;
    }
}
