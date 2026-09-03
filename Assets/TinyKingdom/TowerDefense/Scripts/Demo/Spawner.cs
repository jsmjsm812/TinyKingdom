using System.Collections.Generic;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class Spawner : MonoBehaviour
    {
        public List<Monster> MonsterPrefabs;
        public List<Transform> Checkpoints;

        public void Start()
        {
            InvokeRepeating("CreateMonster", 0, 2);
        }

        private void CreateMonster()
        {
            var monster = Instantiate(MonsterPrefabs[Random.Range(0, MonsterPrefabs.Count)], transform);

            monster.Initialize(Checkpoints);
        }
    }
}