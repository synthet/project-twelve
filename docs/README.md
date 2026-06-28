---
type: Documentation Hub
title: Documentation Hub
description: Entry point for ProjectTwelve documentation and agent-readable knowledge bundle.
resource: README.md
tags: [docs]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Documentation hub

ProjectTwelve's documentation is split into short operational guides, durable product/design references, and the implementation wiki. Use this page as the first stop when you need to find the right source of truth quickly.

## Start here

| Need | Read | Notes |
|------|------|-------|
| Current implementation map | [wiki/README.md](wiki/README.md) | Best entry point for subsystem work and prototype-aligned pages. |
| Source-of-truth rules | [CANONICAL_SOURCES.md](CANONICAL_SOURCES.md) | Explains which docs win when references disagree. |
| Full documentation list | [INDEX.md](INDEX.md) | Comprehensive index grouped by purpose. |
| Contributor/agent workflow | [ai-workflow/README.md](ai-workflow/README.md) | Spec → plan → implement → test → PR loop and asset map. |
| Backlog and tickets | [project/00-backlog-workflow.md](project/00-backlog-workflow.md) | How docs/wiki tickets map to implementation work. |
| Security expectations | [security.md](security.md) | Secrets, MCP, local configuration, and release checklist. |
| Paid/licensed assets | [PAID_ASSETS.md](PAID_ASSETS.md) | Rules for keeping licensed blobs out of this public repo. |

## Documentation sets

### Implementation wiki

The wiki is the day-to-day engineering reference. It includes:

- prototype-aligned pages that describe the code that exists now;
- deeper numbered subsystem pages for design intent and pitfalls;
- visual integration references for sprites, catalogs, and avatar presentation.

Start at [wiki/README.md](wiki/README.md), then follow the page set that matches your task.

### Product and architecture references

Use these when you need broader intent or are changing public conventions:

- [terraria-like-unity-design.md](terraria-like-unity-design.md) — canonical product-level architecture plan.
- [terraria-like-unity-design-detailed.md](terraria-like-unity-design-detailed.md) — expanded companion reference.
- [VISUAL_BEHAVIOR_SPEC.md](VISUAL_BEHAVIOR_SPEC.md) — rendering/avatar behavior contracts.
- [VISUAL_SETUP.md](VISUAL_SETUP.md) — local setup for visual source assets.

### Governance and maintenance

Use these before changing documentation structure or automation:

- [WIKI_SCHEMA.md](WIKI_SCHEMA.md) — required wiki structure and page expectations.
- [OKF_ADOPTION.md](OKF_ADOPTION.md) — Open Knowledge Format profile and linting notes.
- [EXTERNAL_CLI_REVIEWS.md](EXTERNAL_CLI_REVIEWS.md) — optional review-only external CLI workflow.
- [log.md](log.md) — append-only activity log for documentation work.

## Maintenance checklist

Before merging documentation changes:

1. Update [INDEX.md](INDEX.md) when adding, removing, or renaming documentation pages.
2. Update [CANONICAL_SOURCES.md](CANONICAL_SOURCES.md) if the change creates a new source of truth or changes precedence.
3. Keep wiki pages cross-linked through [wiki/README.md](wiki/README.md) and relevant “See also” sections.
4. Run the docs checks listed in the repository agent instructions when the environment supports them.
