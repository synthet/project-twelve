using UnityEngine;

namespace ProjectTwelve.Visual.Effects
{
    /// <summary>
    /// Short-lived sprite effect driven by an Animator clip name.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public sealed class SpriteEffectInstance : MonoBehaviour
    {
        private const float DefaultLifetime = 0.25f;

        /// <summary>
        /// Plays a named clip and destroys the instance after the default lifetime.
        /// </summary>
        public void Play(string clipName, int direction = 1)
        {
            transform.localScale = new Vector3(direction, 1f, 1f);
            GetComponent<Animator>().Play(clipName);
            Destroy(gameObject, DefaultLifetime);
        }
    }
}
