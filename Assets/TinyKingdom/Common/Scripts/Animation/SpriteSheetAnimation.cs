using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyKingdom.Common.Scripts.Animation
{
    public class SpriteSheetAnimation : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer;
        public List<Sprite> Sprites;
        public float FrameDelay;
        public bool Loop;
        public float LoopDelay;

        public IEnumerator Start()
        {
            var index = 0;

            while (true)
            {
                SpriteRenderer.sprite = Sprites[index];
                index++;

                if (index == Sprites.Count)
                {
                    if (!Loop)
                    {
                        SpriteRenderer.sprite = null;

                        yield break;
                    }

                    index = 0;

                    if (LoopDelay > 0)
                    {
                        SpriteRenderer.sprite = null;

                        yield return new WaitForSeconds(LoopDelay);
                    }
                }

                yield return new WaitForSeconds(FrameDelay);
            }
        }
    }
}