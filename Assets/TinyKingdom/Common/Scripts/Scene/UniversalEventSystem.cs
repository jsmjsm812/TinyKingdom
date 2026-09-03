using UnityEngine;

namespace TinyKingdom.Common.Scripts.Scene
{
    public class UniversalEventSystem : MonoBehaviour
    {
        public void Awake()
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM
        
            gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            #elif ENABLE_LEGACY_INPUT_MANAGER
        
            gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            #endif
        }
    }
}