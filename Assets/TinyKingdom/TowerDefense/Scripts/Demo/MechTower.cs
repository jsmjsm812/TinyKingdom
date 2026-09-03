using System;
using System.Linq;
using TinyKingdom.Common.Scripts.Common.Tween;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class MechTower : Tower
    {
        public SpriteRenderer Weapon;
        public Sprite[] WeaponSprites;
        public float Recoil;
        public bool Reloading;
        
        private int _angle;
        private bool _loaded;

        protected override void Rotate(Transform target)
        {
            var direction = target.position - Source.transform.position;

            _angle = Mathf.RoundToInt(Vector2.SignedAngle(Vector2.up, direction) / 45) * 45;

            UpdateSprite();
        }

        protected override void Fire(Action fire)
        {
            fire();
            _loaded = false;
            UpdateSprite();
            Source.GetComponent<PositionSpring>().To = -Recoil * (Quaternion.AngleAxis(_angle, Vector3.forward) * Vector2.up).normalized;
            Source.GetComponent<PositionSpring>().enabled = true;
        }

        protected override void Reload()
        {
            _loaded = true;
            UpdateSprite();

            if (Reloading)
            {
                Source.GetComponent<PositionSpring>().To = -Recoil * (Quaternion.AngleAxis(_angle, Vector3.forward) * Vector2.up).normalized;
                Source.GetComponent<PositionSpring>().enabled = true;
            }
        }

        private void UpdateSprite()
        {
            var spriteName = $"Weapon{(_loaded || !Reloading ? "Loaded" : "Empty")}{Mathf.Abs(_angle)}";

            Weapon.sprite = WeaponSprites.Single(i => i.name == spriteName);
            Weapon.flipX = _angle > 0;
        }
    }
}