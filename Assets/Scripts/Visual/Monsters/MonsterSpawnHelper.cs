using ProjectTwelve.Visual.Monsters;
using UnityEngine;

namespace ProjectTwelve.Visual.Monsters
{
    /// <summary>
    /// Sample helper for spawning catalog monsters at a world position.
    /// </summary>
    public static class MonsterSpawnHelper
    {
        /// <summary>
        /// Spawns a monster prefab from the catalog when the id is registered.
        /// </summary>
        public static MonsterVisual Spawn(MonsterVisualCatalog catalog, string monsterId, Vector3 position, Transform parent = null)
        {
            if (catalog == null || !catalog.TryGetPrefab(monsterId, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning($"MonsterSpawnHelper: unknown monster id '{monsterId}'.");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            instance.name = monsterId;
            if (!instance.TryGetComponent(out MonsterVisual visual))
            {
                visual = instance.AddComponent<MonsterVisual>();
            }

            if (!instance.TryGetComponent(out MonsterLocomotionDriver driver))
            {
                driver = instance.AddComponent<MonsterLocomotionDriver>();
            }

            driver.Idle();
            return visual;
        }
    }
}
