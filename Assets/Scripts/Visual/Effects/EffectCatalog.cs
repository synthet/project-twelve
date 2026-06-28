using ProjectTwelve.Visual.Creatures;
using UnityEngine;

namespace ProjectTwelve.Visual.Effects
{
    /// <summary>
    /// Catalog of sprite VFX prefabs and optional audio for creature presentation.
    /// </summary>
    [CreateAssetMenu(fileName = "EffectCatalog", menuName = "ProjectTwelve/Visual/Effect Catalog")]
    public sealed class EffectCatalog : ScriptableObject
    {
        [SerializeField] private SpriteEffectInstance spriteEffectPrefab;
        [SerializeField] private AudioClip fireAudioClip;

        private static EffectCatalog instance;

        public static EffectCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<EffectCatalog>("EffectCatalog");
                }

                return instance;
            }
        }

        public AudioClip FireAudioClip => fireAudioClip;

        /// <summary>
        /// Spawns a sprite effect at the creature position.
        /// </summary>
        public SpriteEffectInstance CreateSpriteEffect(
            CreatureVisual creature,
            string clipName,
            int direction = 0,
            Transform parent = null)
        {
            if (spriteEffectPrefab == null || creature == null)
            {
                return null;
            }

            SpriteEffectInstance effect = Instantiate(spriteEffectPrefab, creature.transform.position, Quaternion.identity, parent);
            effect.name = clipName;
            if (parent != null)
            {
                effect.transform.position = parent.position;
            }

            SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
            if (renderer != null && creature.Body != null)
            {
                renderer.sortingOrder = creature.Body.sortingOrder + 1;
            }

            int facing = direction == 0 ? (int)Mathf.Sign(creature.transform.localScale.x) : direction;
            effect.Play(clipName, facing);
            return effect;
        }
    }
}
