# Roadmap and LLM Task Prompts

## Milestone 1: Barebone Sandbox

- Keep only sandbox-relevant scripts and documentation.
- Maintain a minimal scene without unrelated demo objects.
- Verify chunk generation, rendering, collision, movement, and editing compile in Unity.

## Milestone 2: Prototype Hardening

- Add tests for world coordinate conversion, chunk dirtiness, and generation determinism.
- Merge chunk colliders or add manual collision to reduce runtime component counts.
- Add neighbor dirtying for border edits.

## Milestone 3: Content Foundation

- Introduce tile and item registries.
- Add texture atlas rendering.
- Add inventory-backed placement/removal.

## LLM Prompt Template

```text
Using docs/wiki and docs/terraria-like-unity-design.md as context, implement [feature].
Keep changes chunk-first, avoid demo-only artifacts, update wiki pages if architecture changes, and cite modified files in the final response.
```
