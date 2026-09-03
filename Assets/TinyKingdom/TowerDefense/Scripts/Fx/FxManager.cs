using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Fx
{
    public class FxManager : MonoBehaviour
    {
        public List<GameObject> EffectPrefabs;

        public static FxManager Instance;

        public void Awake()
        {
            Instance = this;
        }

        public void CreateDeath(Transform parent)
        {
            var prefab = EffectPrefabs.SingleOrDefault(i => i.name == "Death");
            var instance = Instantiate(prefab, parent);

            instance.transform.localPosition = Vector3.zero;
        }
    }
}