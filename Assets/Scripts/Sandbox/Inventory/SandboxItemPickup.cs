using UnityEngine;

namespace ProjectTwelve.Sandbox.Inventory
{
    /// <summary>Minimal world drop: waits when full, magnetizes in range, and despawns by lifetime.</summary>
    public sealed class SandboxItemPickup : MonoBehaviour
    {
        private static Sprite pickupSprite;
        private string itemId;
        private int count;
        private SandboxInventory inventory;
        private Transform target;
        private float spawnedAt;
        private float pickupRadiusWorld;
        private float pickupCollectDistanceWorld;
        private float pickupMoveSpeedWorld;

        public string ItemId => itemId;
        public int Count => count;

        public static SandboxItemPickup Spawn(
            string itemId,
            int count,
            Vector3 position,
            SandboxInventory inventory,
            Transform target,
            float tileSize)
        {
            GameObject go = new GameObject($"Pickup ({itemId} x{count})");
            go.transform.position = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPickupSprite();
            renderer.color = new Color(1f, 0.78f, 0.25f, 1f);
            renderer.sortingOrder = 20;
            SandboxItemPickup pickup = go.AddComponent<SandboxItemPickup>();
            pickup.Initialize(itemId, count, inventory, target, tileSize);
            return pickup;
        }

        public void Initialize(
            string newItemId,
            int newCount,
            SandboxInventory targetInventory,
            Transform playerTarget,
            float tileSize)
        {
            itemId = newItemId;
            count = Mathf.Max(0, newCount);
            inventory = targetInventory;
            target = playerTarget;
            spawnedAt = Time.time;
            float worldUnitsPerTile = Mathf.Max(0.0001f, tileSize);
            pickupRadiusWorld = SandboxInventoryConstants.PickupRadius * worldUnitsPerTile;
            pickupCollectDistanceWorld = SandboxInventoryConstants.PickupCollectDistance * worldUnitsPerTile;
            pickupMoveSpeedWorld = SandboxInventoryConstants.PickupMoveSpeed * worldUnitsPerTile;
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
            if (delta.sqrMagnitude > pickupRadiusWorld * pickupRadiusWorld)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                pickupMoveSpeedWorld * Time.deltaTime);

            if ((target.position - transform.position).sqrMagnitude
                > pickupCollectDistanceWorld * pickupCollectDistanceWorld)
            {
                return;
            }

            count = inventory.Add(itemId, count);
            if (count == 0)
            {
                Destroy(gameObject);
            }
        }

        private static Sprite GetPickupSprite()
        {
            if (pickupSprite != null)
            {
                return pickupSprite;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply(false, true);
            pickupSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 8f);
            pickupSprite.name = "RuntimePickup";
            pickupSprite.hideFlags = HideFlags.HideAndDontSave;
            return pickupSprite;
        }
    }
}
