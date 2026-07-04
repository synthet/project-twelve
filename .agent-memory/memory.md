# Project Memory


## Stable Project Facts

- Runtime MCP tools require Play Mode; endpoint is loopback-only on port 8765

## User Preferences

- (none yet)

## Working Rules

- Autotile changes require Unity EditMode tests and `cd tools/tile-viz && npm test` for C#/JS parity
- Terrain generation changes require Unity EditMode tests and `cd tools/world-viz && npm test`
- Licensed art lives in Assets/_Licensed submodule; never commit paid blobs to the public repo
- Autotile and sprite anchoring must use sprite bounds, not pivots (VISUAL_BEHAVIOR_SPEC)
- Edit .claude/ sources then run python scripts/sync_assistant_trees.py; do not hand-edit .cursor/

## Recurring Issues

- (none yet)

## Successful Patterns

- (none yet)

## Open Questions

- (none yet)

## Deprecated / Superseded

- (none yet)
