---
type: Maintenance Schema
title: Wiki Maintenance Schema
description: Operating rules for maintaining this OKF bundle as an LLM-writable project wiki.
tags: [okf, llm, maintenance, schema]
timestamp: 2026-06-27T00:00:00Z
---
# Purpose

This schema tells future agents how to update `docs/wiki` without turning it into a loose folder of notes. The bundle should act as the maintained wiki layer between raw project sources and future implementation work.

# Layers

1. **Raw sources** are authoritative project materials such as [`../terraria-like-unity-design.md`](../terraria-like-unity-design.md), `README.md`, and source files under `Assets/Scripts`.
2. **Wiki concepts** are the markdown files in this directory. They synthesize source material into stable concepts, ownership boundaries, playbooks, and roadmap guidance.
3. **Maintenance schema** is this file. It defines conventions for adding, updating, and cross-linking concepts.

# Required Workflow

When adding or updating a concept document:

1. Add YAML frontmatter with a non-empty `type` field.
2. Prefer `title`, `description`, `resource`, `tags`, and `timestamp` when useful for routing and search.
3. Use standard markdown headings, lists, tables, and fenced code blocks.
4. Link related concepts with normal markdown links, preferably bundle-relative absolute-style paths when a viewer supports them or stable relative links otherwise.
5. Keep claims traceable to raw sources, source files, or external references.
6. Update [`index.md`](index.md) when adding a new major concept.
7. Update [`log.md`](log.md) with the date and reason for meaningful structural changes.

# Concept Types Used Here

| Type | Purpose |
| --- | --- |
| `Project Brief` | Product and prototype scope. |
| `Architecture Map` | Runtime ownership and system relationships. |
| `System Concept` | A focused subsystem or future feature area. |
| `Playbook` | Repeatable task, milestone, or prompt guidance. |
| `Maintenance Schema` | Rules for the knowledge bundle itself. |
| `Reference` | External or internal source material used by the bundle. |

# Guardrails

- Do not duplicate the full design document. Link to it and summarize implementation consequences.
- Do not invent production systems that are not grounded in the design document or codebase.
- Keep this wiki useful to both humans and agents: short pages, explicit links, concrete ownership boundaries.
- Treat missing future pages as acceptable broken links while the bundle evolves.
