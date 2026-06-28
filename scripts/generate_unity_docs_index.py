#!/usr/bin/env python3
"""Generate wiki-style markdown indexes from Unity offline documentation TOC data."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

DOCS_ROOT = Path(r"D:\Soft\Unity\Hub\Editor\6000.5.1f1\Editor\Data\Documentation\en")
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "docs" / "unity-reference"
UNITY_VERSION = "6000.5.1f1 (Unity 6.5 Beta)"
MAX_SCRIPT_LEAF_DEPTH = 3  # collapse deep ScriptReference trees
SKIP_TITLES = {"toc", "root"}


def load_toc(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def sanitize_title(title: str | None) -> str:
    if not title or title == "null":
        return ""
    return title.strip()


def doc_href(relative_path: str) -> str:
    """Return a file:// URL for an HTML page under DOCS_ROOT."""
    target = (DOCS_ROOT / relative_path.replace("/", "\\")).resolve()
    return target.as_uri()


def manual_href(link: str | None) -> str | None:
    if not link or link == "null":
        return None
    filename = link if link.endswith(".html") else f"{link}.html"
    return doc_href(f"Manual/{filename}")


def script_href(link: str | None) -> str | None:
    if not link or link == "null":
        return None
    return doc_href(f"ScriptReference/{link}.html")


def count_leaves(node: dict[str, Any]) -> int:
    children = node.get("children") or []
    if not children:
        return 1 if node.get("link") not in (None, "null") else 0
    return sum(count_leaves(c) for c in children)


def render_manual_node(node: dict[str, Any], depth: int = 0) -> list[str]:
    lines: list[str] = []
    title = sanitize_title(node.get("title"))
    link = manual_href(node.get("link"))
    children = node.get("children") or []
    indent = "  " * depth

    if title and title.lower() not in SKIP_TITLES:
        if link:
            lines.append(f"{indent}- [{title}]({link})")
        else:
            if children:
                lines.append(f"{indent}- **{title}**")
            else:
                lines.append(f"{indent}- {title}")

    child_depth = depth
    if title and title.lower() not in SKIP_TITLES:
        child_depth = depth + 1

    for child in children:
        lines.extend(render_manual_node(child, child_depth))

    return lines


def render_script_node(
    node: dict[str, Any],
    depth: int = 0,
    *,
    max_depth: int = MAX_SCRIPT_LEAF_DEPTH,
) -> list[str]:
    lines: list[str] = []
    title = sanitize_title(node.get("title"))
    link = script_href(node.get("link"))
    children = node.get("children") or []
    indent = "  " * depth

    if title and title.lower() not in SKIP_TITLES:
        if link:
            lines.append(f"{indent}- [{title}]({link})")
        elif children:
            leaf_count = count_leaves(node)
            suffix = f" ({leaf_count} pages)" if leaf_count else ""
            lines.append(f"{indent}- **{title}**{suffix}")

    if not children:
        return lines

    if depth >= max_depth and not link:
        leaf_count = count_leaves(node)
        if leaf_count:
            lines.append(f"{indent}  - *{leaf_count} API pages; open subtree in ScriptReference index or search.*")
        return lines

    for child in children:
        lines.extend(render_script_node(child, depth + 1, max_depth=max_depth))

    return lines


def collect_manual_sections(root: dict[str, Any]) -> list[tuple[str, str | None, int]]:
    sections: list[tuple[str, str | None, int]] = []
    for child in root.get("children") or []:
        title = sanitize_title(child.get("title"))
        if not title:
            continue
        sections.append((title, manual_href(child.get("link")), count_leaves(child)))
    return sections


def collect_script_namespaces(root: dict[str, Any]) -> list[tuple[str, int]]:
    namespaces: list[tuple[str, int]] = []
    for child in root.get("children") or []:
        title = sanitize_title(child.get("title"))
        if not title:
            continue
        namespaces.append((title, count_leaves(child)))
    return sorted(namespaces, key=lambda x: x[0].lower())


def count_html_files(folder: Path) -> int:
    if not folder.is_dir():
        return 0
    return sum(1 for _ in folder.rglob("*.html"))


def write_file(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def build_root_index(
    manual_sections: list[tuple[str, str | None, int]],
    script_namespaces: list[tuple[str, int]],
    manual_count: int,
    script_count: int,
) -> str:
    docs_rel = DOCS_ROOT.as_posix()
    lines = [
        "# Unity 6.5 Offline Documentation — Wiki Index",
        "",
        f"Local mirror of Unity **{UNITY_VERSION}** English documentation.",
        "",
        "## Location",
        "",
        f"| Item | Path |",
        f"|------|------|",
        f"| Documentation root | `{docs_rel}` |",
        f"| User Manual | `{docs_rel}/Manual` ({manual_count:,} HTML pages) |",
        f"| Scripting API | `{docs_rel}/ScriptReference` ({script_count:,} HTML pages) |",
            f"| Manual search | [30_search.html]({doc_href('Manual/30_search.html')}) |",
            f"| API search | [30_search.html]({doc_href('ScriptReference/30_search.html')}) |",
        "",
        "Open pages with a browser (`file://` URLs) or from Unity **Help → Unity Manual** when the Editor version matches.",
        "",
        "## Index pages",
        "",
        "| Index | Description |",
        "|-------|-------------|",
        "| [Manual TOC](unity-manual-index.md) | Full User Manual table of contents |",
        "| [Scripting API TOC](unity-scriptreference-index.md) | ScriptReference namespace tree (summarized below depth 3) |",
        "",
        "## Manual — top-level sections",
        "",
    ]

    for title, href, leaf_count in manual_sections:
        if href:
            lines.append(f"- [{title}]({href}) — {leaf_count:,} pages")
        else:
            lines.append(f"- **{title}** — {leaf_count:,} pages")

    lines.extend(
        [
            "",
            "## Scripting API — namespaces",
            "",
            "Full tree: [unity-scriptreference-index.md](unity-scriptreference-index.md).",
            "",
        ]
    )

    for title, leaf_count in script_namespaces[:40]:
        lines.append(f"- **{title}** — {leaf_count:,} pages")

    if len(script_namespaces) > 40:
        remaining = len(script_namespaces) - 40
        lines.append(f"- *…and {remaining} more namespaces in the ScriptReference index.*")

    lines.extend(
        [
            "",
            "## Project-relevant shortcuts",
            "",
            "Links for ProjectTwelve (2D sandbox, URP, tilemaps, physics, saving).",
            "",
            "| Topic | Manual | Scripting API |",
            "|-------|--------|---------------|",
            f"| 2D development hub | [2d-game-development-landing]({doc_href('Manual/2d-game-development-landing.html')}) | — |",
            f"| 2D URP | [2d-urp-landing]({doc_href('Manual/2d-urp-landing.html')}) | [Universal RP]({doc_href('ScriptReference/UnityEngine.Rendering.Universal.html')}) |",
            f"| Tilemaps | [Tilemap]({doc_href('Manual/class-Tilemap.html')}) | [Tilemaps]({doc_href('ScriptReference/UnityEngine.Tilemaps.html')}) |",
            f"| Sprites | [Sprites]({doc_href('Manual/Sprites.html')}) | [Sprite]({doc_href('ScriptReference/UnityEngine.Sprite.html')}) |",
            f"| 2D physics | [Physics2DReference]({doc_href('Manual/Physics2DReference.html')}) | [Physics2D]({doc_href('ScriptReference/UnityEngine.Physics2D.html')}) |",
            f"| Input System | [Input System]({doc_href('Manual/com.unity.inputsystem.html')}) | [InputSystem]({doc_href('ScriptReference/UnityEngine.InputSystem.html')}) |",
            f"| Test Framework | [Edit Mode tests]({doc_href('Manual/testing-editortestsrunner.html')}) | [TestTools]({doc_href('ScriptReference/TestTools.html')}) |",
            f"| Unity AI / MCP | [AI menu]({doc_href('Manual/ai-menu.html')}) | — |",
            f"| ScriptableObjects | [ScriptableObject]({doc_href('Manual/class-ScriptableObject.html')}) | [ScriptableObject]({doc_href('ScriptReference/ScriptableObject.html')}) |",
            f"| Serialization | [Serialization]({doc_href('Manual/script-Serialization.html')}) | [Serialization]({doc_href('ScriptReference/UnityEngine.Serialization.html')}) |",
            "",
            "## Subfolders with bundled package docs",
            "",
            "| Folder | Package / topic |",
            "|--------|-----------------|",
            "| `Manual/2d-physics/` | 2D physics manual pages |",
            "| `Manual/2d-physics-api/` | 2D physics scripting reference |",
            "| `Manual/tilemaps/` | Tilemap package manual |",
            "| `Manual/sprite/` | Sprite package manual |",
            "| `Manual/urp/` | Universal Render Pipeline manual |",
            "| `Manual/ui-systems/` | uGUI and UI Toolkit |",
            "| `Manual/test-framework/` | Unity Test Framework |",
            "| `Manual/accessibility/` | Accessibility package |",
            "| `Manual/adaptive-performance/` | Adaptive Performance |",
            "| `Manual/best-practice-guides/` | Best practice guides |",
            "| `Manual/project-auditor/` | Project Auditor |",
            "",
            "## Maintenance",
            "",
            "Regenerate after upgrading the Unity Editor:",
            "",
            "```bash",
            "python3 scripts/generate_unity_docs_index.py",
            "```",
            "",
            f"*Generated from `{DOCS_ROOT}` TOC metadata.*",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> None:
    if not DOCS_ROOT.is_dir():
        raise SystemExit(f"Documentation root not found: {DOCS_ROOT}")

    manual_toc = load_toc(DOCS_ROOT / "Manual" / "docdata" / "toc.json")
    script_toc = load_toc(DOCS_ROOT / "ScriptReference" / "docdata" / "toc.json")

    manual_count = count_html_files(DOCS_ROOT / "Manual")
    script_count = count_html_files(DOCS_ROOT / "ScriptReference")

    manual_sections = collect_manual_sections(manual_toc)
    script_namespaces = collect_script_namespaces(script_toc)

    root_md = build_root_index(manual_sections, script_namespaces, manual_count, script_count)

    manual_lines = [
        "# Unity User Manual — Table of Contents",
        "",
        f"Unity **{UNITY_VERSION}** offline manual. Documentation root: `{DOCS_ROOT.as_posix()}`.",
        "",
        f"**{manual_count:,}** HTML pages · [Search]({doc_href('Manual/30_search.html')}) · [Back to index](README.md)",
        "",
    ]
    manual_lines.extend(render_manual_node(manual_toc))

    script_lines = [
        "# Unity Scripting API — Table of Contents",
        "",
        f"Unity **{UNITY_VERSION}** ScriptReference. Documentation root: `{DOCS_ROOT.as_posix()}`.",
        "",
        f"**{script_count:,}** HTML pages · [Search]({doc_href('ScriptReference/30_search.html')}) · [Back to index](README.md)",
        "",
        f"> Deep subtrees collapse after depth {MAX_SCRIPT_LEAF_DEPTH}; use search for specific members.",
        "",
    ]
    script_lines.extend(render_script_node(script_toc))

    write_file(OUTPUT_DIR / "README.md", root_md)
    write_file(OUTPUT_DIR / "unity-manual-index.md", "\n".join(manual_lines) + "\n")
    write_file(OUTPUT_DIR / "unity-scriptreference-index.md", "\n".join(script_lines) + "\n")

    print(f"Wrote {OUTPUT_DIR / 'README.md'}")
    print(f"  Manual sections: {len(manual_sections)}")
    print(f"  Script namespaces: {len(script_namespaces)}")
    print(f"  Manual pages: {manual_count:,}")
    print(f"  Script pages: {script_count:,}")


if __name__ == "__main__":
    main()
