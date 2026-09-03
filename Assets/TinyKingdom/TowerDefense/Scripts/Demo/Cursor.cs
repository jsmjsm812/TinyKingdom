using UnityEngine;

namespace TinyKingdom.TowerDefense.Scripts.Demo
{
    public class Cursor : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        public Grid Grid;

        private Camera _camera;

        public static Cursor Instance;

        public void Awake()
        {
            Instance = this;
            _camera = Camera.main;
        }

        public void Update()
        {
            var mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            var cell = Grid.WorldToCell(mousePosition);

            Renderer.transform.position = Grid.GetCellCenterWorld(cell);
        }

        public Vector2 GetPosition()
        {
            return Renderer.transform.position;
        }
    }
}