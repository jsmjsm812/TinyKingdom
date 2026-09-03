using UnityEngine;

namespace TinyKingdom.Common.Scripts.Common.Tween
{
    /// <summary>
    /// Tween scale
    /// </summary>
    public class ScaleSpring : TweenBase
    {
        public float From;
        public float To;
        public float Dumping;

        private float _amplitude = 1;

        protected override void OnUpdate()
        {
            _amplitude = Mathf.Max(0, _amplitude - Dumping * Time.deltaTime);

            transform.localScale = Vector3.one * (From + (To - From) * Sin() * _amplitude);
     
            if (_amplitude <= 0)
            {
                enabled = false;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Reset();
        }

        public void OnDisable()
        {
            transform.localScale = Vector3.one * From;
        }

        public void Reset()
        {
            _amplitude = 1;
        }
    }
}