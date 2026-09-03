using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinyKingdom.Common.Scripts.Scene
{
    public class GridSnap : MonoBehaviour
    {
        public void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            var grid = GetComponentInParent<Grid>();

            foreach (Transform child in transform)
            {
                if (child.GetComponent<Tilemap>()) continue;

                var cell = grid.WorldToCell(child.position);

                child.position = grid.GetCellCenterWorld(cell);
            }
        }
    }
}