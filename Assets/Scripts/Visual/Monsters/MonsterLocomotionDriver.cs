using System;
using ProjectTwelve.Visual.Creatures;
using ProjectTwelve.Visual.Effects;
using UnityEngine;

namespace ProjectTwelve.Visual.Monsters
{
    /// <summary>
    /// Locomotion states for monster animator bool parameters.
    /// </summary>
    public enum MonsterLocomotionState
    {
        Idle,
        Ready,
        Walk,
        Run,
        Jump,
        Die
    }

    /// <summary>
    /// Drives monster locomotion animator parameters.
    /// </summary>
    [RequireComponent(typeof(MonsterVisual))]
    public sealed class MonsterLocomotionDriver : MonoBehaviour
    {
        private MonsterVisual monster;

        private void Awake()
        {
            monster = GetComponent<MonsterVisual>();
            Idle();
        }

        public void SetState(MonsterLocomotionState state)
        {
            foreach (string variable in new[] { "Idle", "Ready", "Walk", "Run", "Jump", "Die" })
            {
                monster.Animator.SetBool(variable, false);
            }

            switch (state)
            {
                case MonsterLocomotionState.Idle: monster.Animator.SetBool("Idle", true); break;
                case MonsterLocomotionState.Ready: monster.Animator.SetBool("Ready", true); break;
                case MonsterLocomotionState.Walk: monster.Animator.SetBool("Walk", true); break;
                case MonsterLocomotionState.Run: monster.Animator.SetBool("Run", true); break;
                case MonsterLocomotionState.Jump: monster.Animator.SetBool("Jump", true); break;
                case MonsterLocomotionState.Die: monster.Animator.SetBool("Die", true); break;
                default: throw new NotSupportedException(state.ToString());
            }
        }

        public MonsterLocomotionState GetState()
        {
            Animator animator = monster.Animator;
            if (animator.GetBool("Idle")) return MonsterLocomotionState.Idle;
            if (animator.GetBool("Ready")) return MonsterLocomotionState.Ready;
            if (animator.GetBool("Walk")) return MonsterLocomotionState.Walk;
            if (animator.GetBool("Run")) return MonsterLocomotionState.Run;
            if (animator.GetBool("Jump")) return MonsterLocomotionState.Jump;
            if (animator.GetBool("Die")) return MonsterLocomotionState.Die;
            return MonsterLocomotionState.Ready;
        }

        public void Idle() => SetState(MonsterLocomotionState.Idle);

        public void Ready()
        {
            if (GetState() == MonsterLocomotionState.Walk)
            {
                EffectCatalog.Instance?.CreateSpriteEffect(monster, "Brake");
            }
            else if (GetState() == MonsterLocomotionState.Idle)
            {
                return;
            }

            SetState(MonsterLocomotionState.Ready);
        }

        public void Run()
        {
            if (GetState() != MonsterLocomotionState.Walk)
            {
                EffectCatalog.Instance?.CreateSpriteEffect(monster, "Run");
            }

            SetState(MonsterLocomotionState.Walk);
        }

        public void Jump()
        {
            SetState(MonsterLocomotionState.Jump);
            EffectCatalog.Instance?.CreateSpriteEffect(monster, "Jump");
        }

        public void Die() => SetState(MonsterLocomotionState.Die);
        public void Attack() => monster.Animator.SetTrigger("Attack");
        public void Hit() => monster.Animator.SetTrigger("Hit");
        public void Fire() => monster.Animator.SetTrigger("Fire");
    }
}
