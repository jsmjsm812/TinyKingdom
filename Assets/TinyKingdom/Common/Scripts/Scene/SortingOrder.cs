using UnityEngine;
using UnityEngine.Rendering;

namespace TinyKingdom.Common.Scripts.Scene
{
    public class SortingOrder : MonoBehaviour
    {
        public bool Dynamic;
        public bool Overlay;

        public void OnValidate()
        {
            SetSorting();
        }

        public void Start()
        {
            SetSorting();
            enabled = Dynamic;
        }

        public void Update()
        {
            SetSorting();
        }

        private void SetSorting()
        {
            GetComponent<SortingGroup>().sortingOrder = 1000 - (int)(100 * transform.position.y) + (Overlay ? 250 : 0);
        }
    }
}