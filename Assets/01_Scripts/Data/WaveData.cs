using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Data
{
    [CreateAssetMenu(fileName = "New Wave", menuName = "WitchHour/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Serializable]
        public struct SpawnEntry
        {
            public InvaderData invader;
            public int count;
        }

        [Header("웨이브 번호 (1~10)")]
        public int waveNumber;

        [Header("스폰 구성 (구역 전용 보스 제외, GDD.md 13번 표 그대로)")]
        public List<SpawnEntry> spawnEntries;
        [Tooltip("스폰 간격(초)")]
        public float spawnInterval;

        [Tooltip("체크 시 현재 ZoneData.bossInvader를 1마리 추가 스폰 (10웨이브용)")]
        public bool isBossWave;
    }
}
