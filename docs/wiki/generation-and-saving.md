# Generation and Saving

## Generation Pipeline

The prototype currently generates terrain height from noise, then fills grass, dirt, stone, or air. The full generator should become pass-based:

1. Surface height.
2. Terrain layers.
3. Cave carving.
4. Biome assignment.
5. Ore and resource placement.
6. Structures and points of interest.
7. Validation and spawn safety.

## Saving Strategy

Save chunks independently with a format version, seed, generation settings, dirty chunk data, entity state, inventories, time of day, and global events. Untouched chunks can be regenerated from the seed; edited chunks need persisted diffs or complete chunk payloads.
