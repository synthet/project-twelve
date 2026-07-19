using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Detached or attached firearm presentation on a layered character.
    /// </summary>
    [System.Serializable]
    public sealed class FirearmVisual
    {
        [SerializeField] private SpriteRenderer renderer;
        [SerializeField] private Transform fireMuzzle;
        [SerializeField] private bool detached;

        public SpriteRenderer Renderer => renderer;
        public Transform FireMuzzle => fireMuzzle;
        public bool Detached => detached;
        public Vector2 FireMuzzlePosition { get; set; }

        public void Wire(SpriteRenderer r, Transform muzzle, bool isDetached)
        {
            renderer = r;
            fireMuzzle = muzzle;
            detached = isDetached;
        }

        public void SetDetachedActive(bool active)
        {
            if (renderer != null)
            {
                renderer.gameObject.SetActive(detached && active);
            }
        }
    }
}
