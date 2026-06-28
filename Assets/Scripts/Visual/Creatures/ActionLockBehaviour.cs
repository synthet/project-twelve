using UnityEngine;

namespace ProjectTwelve.Visual.Creatures
{
    /// <summary>
    /// Blocks animator transitions while an action clip plays by holding the Action bool.
    /// </summary>
    public sealed class ActionLockBehaviour : StateMachineBehaviour
    {
        [SerializeField] private bool continuous;

        private float enterTime;
        private bool active;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            enterTime = Time.time;
            animator.SetBool("Action", true);
            active = true;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.normalizedTime >= 1f && !continuous)
            {
                TryExit(animator, stateInfo);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            TryExit(animator, stateInfo);
        }

        private void TryExit(Animator animator, AnimatorStateInfo stateInfo)
        {
            if (!active || Time.time - enterTime < stateInfo.length)
            {
                return;
            }

            active = false;
            animator.SetBool("Action", false);
        }
    }
}
