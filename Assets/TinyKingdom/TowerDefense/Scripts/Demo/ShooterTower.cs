using System;
using System.Collections;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class ShooterTower : Tower
    {
        public Monster Shooter;
        public float AnimationDuration;

        private static readonly int ShotHash = Animator.StringToHash("BowShot");

        protected override void Rotate(Transform target)
        {
            if (_state != State.Ready) return;

            var direction = target.position - Source.transform.position;

            Shooter.RotateTo(direction);
            Source = Shooter.Front.activeSelf ? Shooter.FrontWeapons[0] : Shooter.BackWeapons[0];
        }

        protected override void Fire(Action fire)
        {
            Shooter.Animator.SetTrigger(ShotHash);
            StartCoroutine(nameof(FireDelayed), fire);
        }

        protected override void Reload()
        {
        }

        private IEnumerator FireDelayed(Action fire)
        {
            yield return new WaitForSeconds(AnimationDuration);

            fire();
        }
    }
}