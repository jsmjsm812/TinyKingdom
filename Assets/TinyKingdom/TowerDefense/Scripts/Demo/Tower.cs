using System;
using System.Linq;
using TinyKingdom.Common.Scripts.Creature;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public abstract class Tower : MonoBehaviour
    {
        public Transform Source;

        [Header("Params")]
        public int Damage;
        public float Range;
        public float FireInterval;
        public float ReloadTime;

        [Header("Prefabs")]
        public Projectile ProjectilePrefab;

        private float _fireTime;
        protected State _state;

        protected enum State
        {
            Ready,
            Empty,
            Loaded
        }

        protected abstract void Rotate(Transform target);

        protected abstract void Fire(Action fire);

        protected abstract void Reload();
        
        public void Update()
        {
            var monster = Monster.Instances.Where(i => i.enabled && i.State < CreatureState.Dead).OrderBy(i => Vector2.Distance(i.transform.position, transform.position)).FirstOrDefault();

            if (monster != null && Vector2.Distance(monster.transform.position, transform.position) < Range)
            {
                Rotate(monster.transform);

                if (_state == State.Ready)
                {
                    Fire(monster);
                }
            }

            if (_state == State.Empty && Time.time - _fireTime > ReloadTime)
            {
                Reload();
                _state = State.Loaded;
            }

            if (_state == State.Loaded && Time.time - _fireTime > FireInterval)
            {
                _state = State.Ready;
            }
        }

        private void Fire(Monster target)
        {
            _state = State.Empty;
            _fireTime = Time.time;

            Fire(() => Instantiate(ProjectilePrefab, Source.position, Quaternion.identity, transform).Initialize(this, target));
        }
    }
}