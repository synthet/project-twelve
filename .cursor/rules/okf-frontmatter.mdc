---
description: OKF frontmatter validation — required metadata fields for all wiki and documentation files.
alwaysApply: true
---

# OKF Frontmatter Requirements (always on)

All markdown files in `docs/` must include valid OKF (Open Knowledge Format) frontmatter. Under the CI command (`--fail-on error`), a **missing `type` is the only field that blocks merge**; missing `description`, `resource`, `tags`, or `timestamp` are reported as warnings. Treat all six as required anyway — the warnings exist to keep docs discoverable, and warnings can be promoted to errors later.

## Required fields for all wiki/doc files

Every markdown file in `docs/` should include these frontmatter fields (`type` is merge-blocking; the rest are warnings under `--fail-on error`):

```yaml
---
type: <PageType>
title: <Human-readable title>
description: <One-line summary of content and purpose>
resource: <wiki/path/to/file.md>
tags: [category, topic, phase]
timestamp: <ISO8601 date: YYYY-MM-DDTHH:MM:SSZ>
---
```

### Field definitions

| Field | Requirement | Example |
|-------|-------------|---------|
| **type** | Required. Describes content category. Valid values: Overview, Guide, Architecture, Reference, Risk Analysis, Task, Specification, Runbook, etc. | `type: Architecture` |
| **title** | Required. Human-readable title; same as `# Title` heading. | `title: World & Chunk Data` |
| **description** | Required. One-line summary of what this page documents and why it matters. | `description: Chunk data model, lifecycle, coordinate conversions.` |
| **resource** | Required. Relative path from repo root to this file (without leading `/`). | `resource: wiki/00-overview.md` |
| **tags** | Required. Array of 2–5 lowercase tags for categorization and discovery. Include phase (p0, p1, etc.) if applicable. | `tags: [docs, wiki, architecture, chunking]` |
| **timestamp** | Required. ISO 8601 UTC date when last updated. | `timestamp: 2026-06-29T02:13:18Z` |

## Type values by file category

Use these standardized type values:

- **System/architecture:** `Overview`, `Architecture`, `Reference`, `Design`
- **Process/workflow:** `Guide`, `Runbook`, `Specification`
- **Tracking:** `Task`, `Risk Analysis`, `Decision Log`
- **Reference:** `Glossary`, `FAQ`, `Index`
- **Other:** Use a descriptive singular noun (e.g., `Strategy`, `Proposal`)

## Tag guidelines

- **Always include:** At least one category tag (`docs`, `wiki`, `guide`, etc.).
- **Phase tags:** `p0`, `p1`, `p2`, `p3`, `p4`, `p5` if the document applies to a specific phase.
- **Domain tags:** Include subsystem or topic (e.g., `chunking`, `rendering`, `networking`).
- **Status tags (optional):** `draft`, `active`, `deprecated`, `archived` if relevant.

**Example tag set:**
```yaml
tags: [docs, wiki, architecture, p1, rendering, collision]
```

## Timestamps

Use the timestamp when the file was last meaningfully updated (not every edit, but when the content changed). Format: **ISO 8601 UTC only** (`YYYY-MM-DDTHH:MM:SSZ`).

When in doubt, use the current date in UTC:
```bash
# Get current UTC timestamp (Unix/Linux/macOS)
date -u +"%Y-%m-%dT%H:%M:%SZ"
```

## Before creating or editing wiki files

1. **Check the template** — Copy frontmatter from an existing file in the same directory (e.g., another `docs/wiki/*.md` file).
2. **Update fields** — Edit `type`, `title`, `description`, and `tags` to match the new content.
3. **Verify the resource path** — Must be relative to the repo root and match the actual file location.
4. **Set the timestamp** — Use the current UTC date or the date of last significant update.
5. **Run the lint check before push:**
   ```bash
   python3 scripts/check_markdown_links.py
   python scripts/sync_assistant_trees.py --check
   python scripts/ci/okf_lint_changed.py --base origin/master --head HEAD --profile project --fail-on error
   ```

## Common mistakes to avoid

❌ **Missing `type`** — CI fails with a `[missing_type]` error (the only merge-blocking field under `--fail-on error`). Missing `description`/`resource`/`tags`/`timestamp` are reported as warnings — add them anyway.

❌ **Incorrect resource path** — Path must be relative from repo root (e.g., `wiki/00-overview.md`, not `docs/wiki/00-overview.md` or `./00-overview.md`).

❌ **Invalid type value** — Use standard singular nouns (not plural; not abbreviations).

❌ **Malformed timestamp** — Must be ISO 8601 UTC; examples: `2026-06-29T14:30:00Z`, `2026-06-29T00:00:00Z`.

❌ **Empty or vague description** — Avoid "Documentation page"; explain *what* and *why* in one sentence.

## Frontmatter checklist

When creating a new wiki file, use this checklist:

- [ ] File has `---` YAML frontmatter block at the very top.
- [ ] `type:` is set to a valid value (singular noun, capitalized).
- [ ] `title:` matches the main `# Heading` in the file.
- [ ] `description:` is a clear one-line summary (20–80 chars).
- [ ] `resource:` path is relative from repo root and matches the file path.
- [ ] `tags:` includes 2–5 lowercase tags, at least one category tag.
- [ ] `timestamp:` is ISO 8601 UTC format (`YYYY-MM-DDTHH:MM:SSZ`).
- [ ] All field values are quoted strings (e.g., `type: "Task"` or `type: Task`).
- [ ] YAML syntax is valid (colons followed by space, no tabs).

## See also

- [`docs/wiki/quality-gates.md`](../../docs/wiki/quality-gates.md) — OKF linting is part of required CI checks.
- OKF specification (internal) — `scripts/ci/okf_lint_changed.py` defines the validation rules.
- Template files — Any file in `docs/wiki/` can serve as a reference for correct frontmatter.
