using UnityEngine;

namespace TinyKingdom.Common.Scripts.Common.Tween
{
    /// <summary>
    /// Tween position
    /// </summary>
    public class PositionSpring : TweenBase
    {
        public Vector3 From;
        public Vector3 To;
        public float Dumping;
        
        private float _amplitude = 1;
        private Vector3 _pos;

        public void Awake()
        {
            _pos = transform.localPosition;
        }

        protected override void OnUpdate()
        {
            _amplitude = Mathf.Max(0, _amplitude - Dumping * Time.deltaTime);

            transform.localPosition = _pos + (From + (To - From) * Sin()) * _amplitude;
            
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
            transform.localPosition = _pos;
        }

        public void Reset()
        {
            _amplitude = 1;
        }
    }
}