using UnityEngine;

namespace TinyKingdom.Common.Scripts.Creature
{
    /// <summary>
    /// Used to block animation transitions.
    /// </summary>
    public class SoloAnimationState : StateMachineBehaviour
    {
        private float? _time;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _time = Time.time;
            animator.SetBool("Solo", true);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.normalizedTime >= 1)
            {
                Exit(animator, stateInfo);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Exit(animator, stateInfo);
        }

        private void Exit(Animator animator, AnimatorStateInfo stateInfo)
        {
            if (_time == null || Time.time - _time < stateInfo.length) return;

            _time = null;
            animator.SetBool("Solo", false);
        }
    }
}