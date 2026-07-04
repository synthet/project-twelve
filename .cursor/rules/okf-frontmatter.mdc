---
description: OKF frontmatter validation — required metadata fields for docs/ concept files.
alwaysApply: true
---

# OKF Frontmatter Requirements (always on)

`scripts/okf_lint.py` validates OKF (Open Knowledge Format) frontmatter for **concept files** in the `docs/` bundle: Markdown files under `docs/` except `log.md` files and paths skipped by configured `--exclude-prefix` values (default: `archive/`). Under the CI command (`--fail-on error`), a **missing `type` is the only field that blocks merge**; missing `description`, `resource`, `tags`, or `timestamp` are reported as warnings. Treat all six as required anyway — the warnings exist to keep docs discoverable and may be promoted to errors later.

## Required fields for checked docs concept files

Every checked concept file in `docs/` should include these frontmatter fields (`type` is merge-blocking; the rest are warnings under `--fail-on error`). By default this means `docs/**/*.md` except `log.md` files and files under excluded prefixes such as `docs/archive/`:

```yaml
---
type: <PageType>
title: <Human-readable title>
description: <One-line summary of content and purpose>
resource: <path/relative/to/docs.md>
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
| **resource** | Required. Path to this file relative to the `docs/` bundle root (without leading `/`). The linter currently also accepts `docs/...` for compatibility, but prefer bundle-relative paths. | `resource: wiki/quality-gates.md` |
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
3. **Verify the resource path** — Must be relative to the `docs/` bundle root and match the actual file location, for example `wiki/quality-gates.md` for `docs/wiki/quality-gates.md`.
4. **Set the timestamp** — Use the current UTC date or the date of last significant update.
5. **Run the lint check before push:**
   ```bash
   python3 scripts/check_markdown_links.py
   python scripts/sync_assistant_trees.py --check
   python scripts/ci/okf_lint_changed.py --base origin/main --head HEAD --profile project --fail-on error
   ```

## Common mistakes to avoid

❌ **Missing `type`** — CI fails with a `[missing_type]` error (the only merge-blocking field under `--fail-on error`). Missing `description`/`resource`/`tags`/`timestamp` are reported as warnings — add them anyway.

❌ **Incorrect resource path** — Path must be relative to the `docs/` bundle (e.g., `wiki/quality-gates.md`, not `./quality-gates.md` or an unrelated repo path).

❌ **Invalid type value** — Use standard singular nouns (not plural; not abbreviations).

❌ **Malformed timestamp** — Must be ISO 8601 UTC; examples: `2026-06-29T14:30:00Z`, `2026-06-29T00:00:00Z`.

❌ **Empty or vague description** — Avoid "Documentation page"; explain *what* and *why* in one sentence.

## Frontmatter checklist

When creating a new wiki file, use this checklist:

- [ ] File has `---` YAML frontmatter block at the very top.
- [ ] `type:` is set to a valid value (singular noun, capitalized).
- [ ] `title:` matches the main `# Heading` in the file.
- [ ] `description:` is a clear one-line summary (20–80 chars).
- [ ] `resource:` path is relative to the `docs/` bundle and matches the file path.
- [ ] `tags:` includes 2–5 lowercase tags, at least one category tag.
- [ ] `timestamp:` is ISO 8601 UTC format (`YYYY-MM-DDTHH:MM:SSZ`).
- [ ] All field values are quoted strings (e.g., `type: "Task"` or `type: Task`).
- [ ] YAML syntax is valid (colons followed by space, no tabs).

## See also

- [`docs/wiki/quality-gates.md`](../../docs/wiki/quality-gates.md) — OKF linting is part of required CI checks.
- OKF specification (internal) — `scripts/okf_lint.py` and `scripts/ci/okf_lint_changed.py` define the validation rules.
- Template files — Any file in `docs/wiki/` can serve as a reference for correct frontmatter.
