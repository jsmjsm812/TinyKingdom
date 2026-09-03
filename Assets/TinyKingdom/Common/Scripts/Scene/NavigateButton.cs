using UnityEngine;

namespace TinyKingdom.Common.Scripts.Scene
{
    public class NavigateButton : MonoBehaviour
    {
        public void OpenURL(string url)
        {
            Application.OpenURL(url);
        }
    }
}