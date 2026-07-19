using System;
using ProjectTwelve.Visual.Creatures;
using ProjectTwelve.Visual.Effects;
using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Locomotion states for hero animator bool parameters.
    /// </summary>
    public enum CharacterLocomotionState
    {
        Idle,
        Ready,
        Walk,
        Run,
        Crouch,
        Crawl,
        Jump,
        Fall,
        Land,
        Block,
        Climb,
        Die
    }

    /// <summary>
    /// Drives hero locomotion and action animator parameters from gameplay state.
    /// </summary>
    [RequireComponent(typeof(CreatureVisual))]
    public sealed class CharacterLocomotionDriver : MonoBehaviour, ISandboxPlayerLocomotion
    {
        private static readonly string[] LocomotionBools =
        {
            "Idle", "Ready", "Walk", "Run", "Crouch", "Crawl", "Jump", "Fall", "Land", "Block", "Climb", "Die"
        };

        private CreatureVisual creature;
        
        public EffectCatalog Catalog { get; set; }

        private void Awake()
        {
            creature = GetComponent<CreatureVisual>();
            Idle();
        }

        public void Idle() => SetState(CharacterLocomotionState.Idle);
        public void Walk() => SetState(CharacterLocomotionState.Walk);

        public void Ready()
        {
            if (GetState() == CharacterLocomotionState.Run)
            {
                SpawnEffect("Brake");
            }

            SetState(CharacterLocomotionState.Ready);
        }

        public void Run()
        {
            if (GetState() != CharacterLocomotionState.Run)
            {
                SpawnEffect("Run");
            }

            SetState(CharacterLocomotionState.Run);
        }

        public void Jump()
        {
            SetState(CharacterLocomotionState.Jump);
            SpawnEffect("Jump");
        }

        public void Fall() => SetState(CharacterLocomotionState.Fall);

        public void Land()
        {
            SetState(CharacterLocomotionState.Land);
            SpawnEffect("Fall");
        }

        public void Block() => SetState(CharacterLocomotionState.Block);
        public void Climb() => SetState(CharacterLocomotionState.Climb);
        public void Die() => SetState(CharacterLocomotionState.Die);
        public void Crawl() => SetState(CharacterLocomotionState.Crawl);
        public void Crouch() => SetState(CharacterLocomotionState.Crouch);

        public void Roll()
        {
            creature.Animator.SetTrigger("Roll");
            SpawnEffect("Dash");
        }

        public void Slash() => creature.Animator.SetTrigger("Slash");
        public void Jab() => creature.Animator.SetTrigger("Jab");
        public void Push() => creature.Animator.SetTrigger("Push");
        public void Shot() => creature.Animator.SetTrigger("Shot");
        public void Hit() => creature.Animator.SetTrigger("Hit");

        public void SetState(CharacterLocomotionState state)
        {
            for (int i = 0; i < LocomotionBools.Length; i++)
            {
                creature.Animator.SetBool(LocomotionBools[i], false);
            }

            switch (state)
            {
                case CharacterLocomotionState.Idle: creature.Animator.SetBool("Idle", true); break;
                case CharacterLocomotionState.Ready: creature.Animator.SetBool("Ready", true); break;
                case CharacterLocomotionState.Walk: creature.Animator.SetBool("Walk", true); break;
                case CharacterLocomotionState.Run: creature.Animator.SetBool("Run", true); break;
                case CharacterLocomotionState.Crouch: creature.Animator.SetBool("Crouch", true); break;
                case CharacterLocomotionState.Crawl: creature.Animator.SetBool("Crawl", true); break;
                case CharacterLocomotionState.Jump: creature.Animator.SetBool("Jump", true); break;
                case CharacterLocomotionState.Fall: creature.Animator.SetBool("Fall", true); break;
                case CharacterLocomotionState.Land: creature.Animator.SetBool("Land", true); break;
                case CharacterLocomotionState.Block: creature.Animator.SetBool("Block", true); break;
                case CharacterLocomotionState.Climb: creature.Animator.SetBool("Climb", true); break;
                case CharacterLocomotionState.Die: creature.Animator.SetBool("Die", true); break;
                default: throw new NotSupportedException(state.ToString());
            }
        }

        public CharacterLocomotionState GetState()
        {
            Animator animator = creature.Animator;
            if (animator.GetBool("Idle")) return CharacterLocomotionState.Idle;
            if (animator.GetBool("Ready")) return CharacterLocomotionState.Ready;
            if (animator.GetBool("Walk")) return CharacterLocomotionState.Walk;
            if (animator.GetBool("Run")) return CharacterLocomotionState.Run;
            if (animator.GetBool("Crawl")) return CharacterLocomotionState.Crawl;
            if (animator.GetBool("Crouch")) return CharacterLocomotionState.Crouch;
            if (animator.GetBool("Jump")) return CharacterLocomotionState.Jump;
            if (animator.GetBool("Fall")) return CharacterLocomotionState.Fall;
            if (animator.GetBool("Land")) return CharacterLocomotionState.Land;
            if (animator.GetBool("Block")) return CharacterLocomotionState.Block;
            if (animator.GetBool("Climb")) return CharacterLocomotionState.Climb;
            if (animator.GetBool("Die")) return CharacterLocomotionState.Die;
            return CharacterLocomotionState.Ready;
        }

        private void SpawnEffect(string clipName)
        {
            if (Catalog != null)
            {
                Catalog.CreateSpriteEffect(creature, clipName);
            }
            else
            {
                EffectCatalog.Instance?.CreateSpriteEffect(creature, clipName);
            }
        }
    }
}
