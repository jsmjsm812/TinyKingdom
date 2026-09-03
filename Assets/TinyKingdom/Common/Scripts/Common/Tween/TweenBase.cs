using UnityEngine;

namespace TinyKingdom.Common.Scripts.Common.Tween
{
    /// <summary>
    /// Base class for all tweens.
    /// </summary>
    public abstract class TweenBase : MonoBehaviour
    {
        public float Speed;
        public float Period;
        public bool RandomPeriod;

        private float _time;

        public virtual void OnEnable()
        {
            _time = 0;

            if (RandomPeriod)
            {
                Period = Random.Range(0, 360);
            }
        }

        public void Update()
        {
            _time += Speed * Time.deltaTime;
            OnUpdate();
        }

	    public void Sync()
	    {
		    _time = 0;
	    }

		protected virtual void OnUpdate()
        {
        }

        protected float Sin()
        {
            return (Mathf.Sin(_time + Period * Mathf.Deg2Rad) + 1) / 2;
        }
    }
}