using UnityEngine;

namespace TinyKingdom.Common.Scripts.Scene
{
    public class TimeScale : MonoBehaviour
    {
        public float Scale = 1;
        public void Start()
        {
            Time.timeScale = Scale;
        }

        public void Set(float scale)
        {
            Time.timeScale = scale;
        }
    }
}