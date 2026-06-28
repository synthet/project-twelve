using UnityEngine;

namespace ProjectTwelve.Visual.Creatures
{
    /// <summary>
    /// Base visual root for heroes and monsters: body renderer, animator, and optional audio.
    /// </summary>
    public class CreatureVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource audioSource;

        public SpriteRenderer Body
        {
            get => body;
            set => body = value;
        }

        public Animator Animator
        {
            get => animator;
            set => animator = value;
        }

        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (audioSource == null)
            {
                TryGetComponent(out audioSource);
            }
        }
    }
}
