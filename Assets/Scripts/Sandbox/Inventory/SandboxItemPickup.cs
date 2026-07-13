using UnityEngine;

namespace ProjectTwelve.Sandbox.Inventory
{
    /// <summary>Minimal world drop: waits when full, magnetizes in range, and despawns by lifetime.</summary>
    public sealed class SandboxItemPickup : MonoBehaviour
    {
        private string itemId;
        private int count;
        private SandboxInventory inventory;
        private Transform target;
        private float spawnedAt;

        public string ItemId => itemId;
        public int Count => count;

        public static SandboxItemPickup Spawn(
            string itemId,
            int count,
            Vector3 position,
            SandboxInventory inventory,
            Transform target)
        {
            GameObject go = new GameObject($"Pickup ({itemId} x{count})");
            go.transform.position = position;
            SandboxItemPickup pickup = go.AddComponent<SandboxItemPickup>();
            pickup.Initialize(itemId, count, inventory, target);
            return pickup;
        }

        public void Initialize(string newItemId, int newCount, SandboxInventory targetInventory, Transform playerTarget)
        {
            itemId = newItemId;
            count = Mathf.Max(0, newCount);
            inventory = targetInventory;
            target = playerTarget;
            spawnedAt = Time.time;
        }

        private void Update()
        {
            if (count <= 0 || Time.time - spawnedAt >= SandboxInventoryConstants.DropLifetimeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            if (inventory == null || target == null)
            {
                return;
            }

            Vector3 delta = target.position - transform.position;
            if (delta.sqrMagnitude > SandboxInventoryConstants.PickupRadius * SandboxInventoryConstants.PickupRadius)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                SandboxInventoryConstants.PickupMoveSpeed * Time.deltaTime);

            if ((target.position - transform.position).sqrMagnitude
                > SandboxInventoryConstants.PickupCollectDistance * SandboxInventoryConstants.PickupCollectDistance)
            {
                return;
            }

            count = inventory.Add(itemId, count);
            if (count == 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
