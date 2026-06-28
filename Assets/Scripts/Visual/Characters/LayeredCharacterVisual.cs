using ProjectTwelve.Visual.Creatures;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Layered hero visual with body SpriteLibrary and optional detached firearm.
    /// </summary>
    public sealed class LayeredCharacterVisual : CreatureVisual
    {
        [SerializeField] private SpriteLibrary bodyLibrary;
        [SerializeField] private FirearmVisual firearm = new FirearmVisual();

        public SpriteLibrary BodyLibrary
        {
            get => bodyLibrary;
            set => bodyLibrary = value;
        }

        public FirearmVisual Firearm => firearm;

        private void Awake()
        {
            if (bodyLibrary == null && Body != null)
            {
                bodyLibrary = Body.GetComponent<SpriteLibrary>();
            }
        }

        /// <summary>
        /// Assigns a runtime-built sprite library to the body renderer.
        /// </summary>
        public void ApplySpriteLibrary(SpriteLibraryAsset asset)
        {
            if (bodyLibrary != null)
            {
                bodyLibrary.spriteLibraryAsset = asset;
            }
        }
    }
}
