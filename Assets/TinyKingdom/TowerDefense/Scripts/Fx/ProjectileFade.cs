using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Fx
{
    public class ProjectileFade : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer;

        private float _state;

        public void Start()
        {
            transform.localScale = Vector3.one / 2;
            SpriteRenderer.color = Color.clear;
        }
        
        public void Update()
        {
            _state = Mathf.Clamp(_state + 5 * Time.deltaTime, 0, 1);

            transform.localScale = (0.5f + _state / 2f) * Vector3.one;
            SpriteRenderer.color = new Color(1, 1, 1, _state);
        }
    }
}