using System.Collections.Generic;
using UnityEngine;

namespace TinyKingdom.Common.Scripts.Creature
{
    public class FaceExpressions : MonoBehaviour
    {
        public SpriteRenderer Face;
        public SpriteRenderer FaceBack;
        public List<Sprite> Sprites;
        public List<string> Expressions;

        public void SetFace(string expression)
        {
            if (Face == null || Sprites.Count == 0) return;

            var index = Expressions.IndexOf(expression);

            if (Face.gameObject.activeInHierarchy)
            {
                Face.sprite = index >= 0 ? Sprites[index] : null;
            }

            if (FaceBack && FaceBack.gameObject.activeInHierarchy)
            {
                FaceBack.sprite = index >= 0 ? Sprites[index] : null;
            }
        }
    }
}