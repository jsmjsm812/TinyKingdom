using System.Collections.Generic;
using TinyKingdom.Common.Scripts.Scene;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class Builder : MonoBehaviour
    {
        public List<GameObject> TowerPrefabs;
        public Transform TowersNode;

        public void Update()
        {
            if (UniversalInput.GetMouseButtonDown(0))
            {
                var tower = Instantiate(TowerPrefabs[Random.Range(0, TowerPrefabs.Count)], TowersNode);

                tower.transform.position = Cursor.Instance.GetPosition();
            }
        }
    }
}