namespace ProjectTwelve.Sandbox.Inventory
{
    /// <summary>Adopted P2 inventory/edit constants, expressed in tiles and seconds.</summary>
    public static class SandboxInventoryConstants
    {
        public const int SlotCount = 40;
        public const int HotbarSlotCount = 10;
        public const int DefaultMaxStack = 999;
        public const float EditRange = 6f;
        public const float EditIntervalSeconds = 0.10f;
        public const float PickupRadius = 2.5f;
        public const float PickupCollectDistance = 0.35f;
        public const float PickupMoveSpeed = 8f;
        public const float DropLifetimeSeconds = 120f;
        public const int PrototypeStartingStack = 100;
    }
}
