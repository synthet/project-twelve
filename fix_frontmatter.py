import os
import datetime

files = [
    'docs/terraria-like-unity-design-detailed.md',
    'docs/terraria-like-unity-design.md',
    'docs/unity-reference/README.md',
    'docs/unity-reference/unity-manual-index.md',
    'docs/unity-reference/unity-scriptreference-index.md',
    'docs/wiki/01-architecture.md',
    'docs/wiki/03-chunking.md',
    'docs/wiki/04-rendering.md',
    'docs/wiki/05-collision-physics.md',
    'docs/wiki/10-multiplayer.md',
    'docs/wiki/11-saving-loading.md',
    'docs/wiki/14-roadmap.md',
    'docs/wiki/architecture-blueprint.md',
    'docs/wiki/architecture-map.md',
    'docs/wiki/generation-and-saving.md',
    'docs/wiki/glossary.md',
    'docs/wiki/multiplayer-and-modding.md',
    'docs/wiki/project-brief.md',
    'docs/wiki/roadmap-and-tasks.md',
    'docs/wiki/spec-driven-development-tasks.md'
]

for f in files:
    filepath = os.path.join(r'd:\Projects\project-twelve', f)
    if os.path.exists(filepath):
        with open(filepath, 'r', encoding='utf-8') as file:
            lines = file.readlines()
        
        if lines[0] == '---\n' and lines[1] == 'type: Concept\n':
            end_idx = 0
            for i in range(1, len(lines)):
                if lines[i] == '---\n':
                    end_idx = i
                    break
            lines = lines[end_idx+1:]
            if len(lines) > 0 and lines[0] == '\n':
                lines = lines[1:]

        basename = os.path.basename(f)
        title = basename.replace('.md', '').replace('-', ' ').title()
        res = f.replace('\\\\', '/')
        
        frontmatter = f"---\ntype: Concept\ntitle: {title}\ndescription: Documentation for {title}.\nresource: {res}\ntags: [docs, wiki]\ntimestamp: {datetime.datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ')}\nokf_version: 0.1\n---\n\n"
        with open(filepath, 'w', encoding='utf-8') as file:
            file.write(frontmatter + "".join(lines))
