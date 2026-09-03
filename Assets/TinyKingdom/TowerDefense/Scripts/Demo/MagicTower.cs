using System;
using TinyKingdom.Common.Scripts.Common.Tween;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class MagicTower : Tower
    {
        public GameObject SourcePrefab;

        public void Start()
        {
            var source = Instantiate(SourcePrefab, Source.position, Quaternion.identity, Source);

            source.name = "Source";
        }

        protected override void Rotate(Transform target)
        {
        }

        protected override void Fire(Action fire)
        {
            fire();
            Source.GetComponent<ScaleSpring>().enabled = true;
        }

        protected override void Reload()
        {
        }
    }
}
