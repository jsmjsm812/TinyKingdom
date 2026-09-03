using System.Collections.Generic;
using System.Linq;
using TinyKingdom.Common.Scripts.Common.Tween;
using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class TowerDemo : MonoBehaviour
    {
        public GameObject ProjectilePrefab;
        public GameObject ExplosionPrefab;

        public float Interval;
        public Dictionary<GameObject, GameObject> Projectiles = new();
        public GameObject Target;
        public float Speed;

        public void Start()
        {
            InvokeRepeating(nameof(Fire), 0, Interval);
        }

        public void Fire()
        {
            var projectile = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity, transform);
            var p = projectile.GetComponent<Projectile>();

            if (p)
            {
                p.enabled = false;
            }

            Projectiles.Add(projectile, Target);

            GetComponentInChildren<TweenBase>().enabled = true;
        }

        public void Update()
        {
            foreach (var projectile in Projectiles.Keys.ToList())
            {
                var target = Projectiles[projectile];

                if (target == null)
                {
                    DestroyProjectile(projectile);
                }
                else
                {
                    var direction = Target.transform.position - projectile.transform.position;

                    if (direction.magnitude > 0.1f)
                    {
                        projectile.transform.position += direction.normalized * Speed * Time.deltaTime;
                        projectile.transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, target.transform.position - projectile.transform.position));
                    }
                    else
                    {
                        DestroyProjectile(projectile);
                    }
                }
            }
        }

        private void DestroyProjectile(GameObject projectile)
        {
            foreach (var sr in projectile.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }

            foreach (var ps in projectile.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true);
            }

            var explosion = Instantiate(ExplosionPrefab, projectile.transform.position, Quaternion.identity, projectile.transform);

            Destroy(projectile.gameObject, 1);
            Projectiles.Remove(projectile);
        }
    }
}