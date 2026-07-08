---
type: Research
title: Transferable Game Mechanics — Terraria, Starbound, and Oxygen Not Included
description: Transferability assessment of sandbox, progression, settlement, automation, and simulation mechanics from Terraria, Starbound, and Oxygen Not Included for ProjectTwelve.
resource: project/game-mechanics-research.md
tags: [docs, project, design, research, mechanics]
timestamp: 2026-07-08T00:00:00Z
okf_version: 0.1
---

# Transferable Game Mechanics from Terraria, Starbound, and Oxygen Not Included

## Executive summary

Across **Terraria**, **Starbound**, and **Oxygen Not Included**, the most transferable mechanics are not necessarily the most iconic ones. The strongest ProjectTwelve candidates are mechanics that create compounding player agency without requiring a full simulation-heavy engine:

1. A **data-driven progression and crafting graph** inspired by Terraria and Starbound.
2. A **room-tagged settlement system** that turns housing into services, vendors, fast travel, or companions, inspired by Terraria's towns and Starbound's tenants and crew.
3. A **lightweight automation and overlay layer** inspired by Starbound wiring and Oxygen Not Included's sensors, automation, and management overlays.

These ideas provide large gameplay payoff, clear extensibility, and comparatively tractable implementation scope. By contrast, the least transferable mechanics under unspecified project conditions are those whose value depends on genre commitment or deep engine investment: Terraria-style boss-heavy combat progression, full Oxygen Not Included gas/liquid/thermodynamic simulation, and fully indirect worker-AI colony play. Those systems can be excellent, but they drag in substantial adjacent requirements: content volume, balancing effort, simulation performance budgets, pathfinding, scheduling, debugging tooling, and specialized UI.

The single most important design lesson from the source games is structural rather than cosmetic. Terraria succeeds by letting progression emerge from event flags, biomes, gear, towns, and equipment specialization without requiring formal character levels. Starbound is strongest when it treats authored story as a layer inside a sandbox rather than as a replacement for sandbox freedom. Oxygen Not Included succeeds by layering manageable subsystems until they become strategically entangled, then giving players overlays and automation to cope with that complexity.

Recommended implementation stance for ProjectTwelve:

| Recommendation | Stance | Rationale |
| --- | --- | --- |
| Progression + crafting graph | **Full feature** | Core to exploration, mining, building, and long-term goals. |
| Room-tagged settlements and NPC services | **Prototype → full feature if fun** | High payoff for base-building; can start without deep AI. |
| Lightweight automation + diagnostic overlays | **Prototype** | Powerful systemic expression; keep first pass deliberately small. |
| Environmental gear and temporary hazard workarounds | **Full feature candidate** | Turns preparation into progression and makes biomes distinct. |
| Journey Mode-style sandbox/accessibility controls | **Optional later feature** | Valuable for testing and player agency, but not core-loop first. |
| Boss-heavy progression | **Conditional** | Strong only if combat becomes a main pillar. |
| Full ONI-style fluids/gases/thermodynamics | **Defer** | High engine, UI, and performance risk unless simulation becomes the product. |
| Fully indirect worker colony AI | **Defer / narrow prototype** | Requires scheduling, priorities, pathfinding, and rich debug tooling. |

## Scope and assumptions

ProjectTwelve is currently a Unity 2D sandbox prototype, so this report evaluates mechanics against a hybrid sandbox case: exploration, mining, building, crafting, and possible settlement systems are relevant, but a full colony simulator or combat-first boss rush should not be assumed. All transferability ratings are conditional. A mechanic that is excellent in one of the reference games may be a poor first implementation if it requires a different primary genre.

Source priority for this report is: official game/store pages, official forums and release notes, official wikis, then community wikis or secondary sources only where they clarify mechanics. The goal is not to clone reference mechanics, but to extract reusable structural patterns while avoiding protected expression: names, art, story, UI layouts, exact values, item lists, recipes, formulas, and distinctive enemy or character designs.

## Catalog of source mechanics

### Terraria

| Category | Core mechanic | What source material shows | Transferability to ProjectTwelve |
| --- | --- | --- | --- |
| Combat | Classless-but-specialized combat built around gear, damage types, and boss encounters | Terraria foregrounds digging, fighting, exploration, and building; progression is strongly equipment- and encounter-driven. | **Conditional.** Use equipment specialization and encounter milestones, but avoid boss-heavy gating unless combat becomes a main pillar. |
| Progression | Event-gated progression rather than XP levels | Bosses and world states unlock new hazards, resources, and tools; Journey Mode adds runtime customization controls. | **High.** Event flags and biome unlocks fit a sandbox better than formal levels. |
| Crafting | Large recipe graph with many stations and recipe discovery helpers | Crafting stations, recipes, and the Guide NPC make a huge material graph navigable. | **High.** Build a smaller data-driven graph with recipe discovery and station requirements. |
| World generation | Procedural worlds with seeds and biome composition | New worlds vary by seed, size, and biome placement. | **High.** Already aligned with a tile sandbox; pair generation with readable biome rules. |
| Base building | Functional housing and towns | Towns, NPC housing, happiness, and pylons make building affect services and travel. | **High.** Room validation plus service unlocks are a strong fit. |
| NPCs | Vendors and service NPCs tied to housing conditions | NPCs are useful without requiring deep simulation. | **High.** Start with condition-based services before complex schedules. |
| UI/UX | Guide, Bestiary, Journey powers | Informational helpers make large systems legible. | **High.** Add recipe, biome, tile, and creature catalogs as systemic density grows. |
| Mod support | tModLoader ecosystem | Terraria's long tail benefits from structured community extension. | **Later.** Data-driven content formats now; mod tooling only after schemas stabilize. |

**Transferable takeaway:** Terraria shows how a 2D sandbox can use **event flags, biome state, housing, equipment specialization, and crafting stations** to create progression without formal character levels. For ProjectTwelve, the safest adaptation is not copying boss cadence; it is using milestone flags to unlock new recipes, hazards, room types, tools, and biome interactions.

### Starbound

| Category | Core mechanic | What source material shows | Transferability to ProjectTwelve |
| --- | --- | --- | --- |
| Combat | Gear, energy, mobility tech, and authored mission bosses | Combat exists inside exploration and mission structure rather than replacing sandbox play. | **Medium.** Borrow mobility tech and gear utility more than boss structure. |
| Progression | Multi-track progression with soft gates | Starbound separated mission, armor, ship, and colony progression and reduced artificial gates in its 1.0 push. | **High.** Multiple independent tracks reduce player lock-in. |
| Crafting | More stations, tabs, meaningful recipe requirements, improved UI | The 1.0 update emphasized clearer crafting categories and more logical recipe advancement. | **High.** Use categories, search, station filters, and recipe provenance. |
| World generation | Procedural universe plus authored inserts | Procedural planets coexist with dungeons, villages, microdungeons, and missions. | **High.** Use authored pockets inside generated worlds for pacing and teaching. |
| Base building | Planet colonies plus expandable ship hub | Players can build, colonize, and upgrade a persistent mobile base. | **Medium-high.** A central hub is useful, but should not make local bases irrelevant. |
| NPCs | Tenant rooms and crew conversion | Colonies can produce crew with service roles; room contents affect tenant outcomes. | **High.** Room tags are a clean data model for ProjectTwelve settlements. |
| Resource management | Optional hunger, food rot, energy economy | Survival pressure can be difficulty-dependent. | **Medium.** Use optional or biome-specific pressures rather than universal chores. |
| UI/UX | Quest tracker, compass, tabbed crafting, ship console | Navigation and mission UX support a broad sandbox. | **High.** Surface goals without over-constraining play. |
| Multiplayer/mod support | Built for multiplayer and modding | Starbound highlights cooperative play and content extensibility. | **Later.** Useful north star, but not first-pass scope unless multiplayer is committed. |

**Transferable takeaway:** Starbound's strongest structural lesson is **story in a sandbox**: authored missions and tutorials can coexist with procedural exploration if they are layered as optional or milestone content rather than used to erase player freedom. Its tenant and crew systems are also a strong model for room-tagged settlement rewards.

### Oxygen Not Included

| Category | Core mechanic | What source material shows | Transferability to ProjectTwelve |
| --- | --- | --- | --- |
| Core loop | Survival-system management rather than combat | Official descriptions emphasize oxygen, temperature, plumbing, power, waste, food, and stress. | **Conditional.** Adopt selected pressures only if they support exploration/building. |
| Progression | Research plus worker skills and morale | Research unlocks systems; duplicant skill growth increases morale demands. | **Medium.** Research is useful; worker skill/morale should wait for NPC depth. |
| Building | Base layout is the main play surface | Pipes, wires, rooms, heat, gas, and power transform layout into strategy. | **High as inspiration; low as full clone.** Use layout-sensitive machines and overlays. |
| Simulation | Gas, liquid, temperature, phase, and power simulation | ONI's appeal depends on interlocking physical systems. | **Defer full scope.** Prototype coarse local fields before global simulation. |
| AI | Priority- and errand-based indirect labor | Workers choose errands from priorities and accessibility. | **Defer / narrow.** Start with simple assigned NPC services, not full indirect labor. |
| UI/UX | Overlays, range visualization, schedule tools, diagnostics | Overlays are mandatory because hidden simulation would otherwise be unreadable. | **High.** Any systemic field needs an overlay from day one. |
| Automation | Sensors, switches, automation buildings, ribbons | Automation increases player effectiveness and observability. | **High as lightweight prototype.** Sensors + signal wires + a few machines can add depth. |

**Transferable takeaway:** Oxygen Not Included should influence ProjectTwelve's **observability and automation philosophy** more than its raw simulation fidelity. If ProjectTwelve adds heat, oxygen, pressure, pollution, or power fields, each field needs overlays, alerts, and a few player-authored control points before it becomes a large-scale simulation.

## Cross-game analysis: player impact and design intent

### 1. Progression without rigid linearity

All three games provide guidance without relying exclusively on a linear campaign. Terraria uses world flags, bosses, biomes, equipment, NPCs, and mode options. Starbound separates story progression from armor, ship, and colony progression. Oxygen Not Included uses research and player learning more than avatar power. The shared player impact is autonomy: players feel they are moving forward while still choosing the next problem to solve.

**ProjectTwelve implication:** build a milestone graph rather than a quest chain. A milestone can unlock recipes, NPC services, machine types, environmental resistance, map layers, or authored sites. Avoid requiring every player to clear the same combat sequence before interacting with core sandbox systems.

### 2. Bases as gameplay infrastructure

Terraria towns make housing matter through NPC services, happiness, and travel. Starbound colonies make furnishing matter through room tags, tenants, rent, quests, and crew conversion. Oxygen Not Included makes base layout the entire strategic surface through pipes, wires, airflow, temperature, and worker movement.

**ProjectTwelve implication:** make rooms and layouts functional early. A small room validator plus tags such as `sleep`, `workshop`, `medical`, `storage`, `power`, `farm`, `trade`, and `hazard-sealed` can support NPC services, machine bonuses, settlement ratings, and future automation.

### 3. Simulation visibility

As systems become denser, the UI must shift from ordinary menus to diagnostic layers. Terraria uses the Guide, Bestiary, and Journey controls to make a large sandbox searchable. Starbound uses quest tracking, crafting categories, and ship-console mission access. Oxygen Not Included relies on overlays for oxygen, power, temperature, materials, rooms, errands, and automation.

**ProjectTwelve implication:** never add a hidden systemic field without an overlay and a simple explanation. If players can fail because of heat, pressure, power, oxygen, morale, or contamination, they must be able to see it, filter it, and receive alerts before disaster.

### 4. Lightweight programmability

Starbound and Oxygen Not Included both show that even limited automation can produce high agency. Players enjoy building systems that respond to thresholds, switches, doors, pumps, lights, traps, alarms, and storage conditions.

**ProjectTwelve implication:** prototype a minimal signal system before attempting deep simulation. A first pass could include signal wire, manual switch, pressure plate, daylight sensor, storage-full sensor, door, lamp, pump/fan placeholder, and alarm. This enables player-authored contraptions without requiring a complete electronics sandbox.

## Transferability ranking

| Rank | Mechanic family | Transferability | Suggested ProjectTwelve treatment | Why |
| --- | --- | --- | --- | --- |
| 1 | Data-driven progression + crafting graph | Very high | **Full feature** | Fits mining/building/exploration and scales with content. |
| 2 | Room-tagged settlements and NPC services | Very high | **Prototype, then expand** | Makes building meaningful without heavy AI. |
| 3 | Diagnostic overlays | Very high | **Full support for any systemic field** | Prevents confusion and supports debugging. |
| 4 | Lightweight automation | High | **Prototype** | Adds emergent player agency at manageable scope. |
| 5 | Environmental gear gates | High | **Full feature candidate** | Makes biomes mechanically distinct and rewards preparation. |
| 6 | Authored sites inside procedural worlds | High | **Prototype** | Provides pacing, tutorials, and set-piece rewards without linearizing the sandbox. |
| 7 | Mobile or central hub | Medium-high | **Prototype carefully** | Useful anchor, but risks replacing local building. |
| 8 | Generated local tasks | Medium | **Small template set** | Adds texture only if tied to local state and consequences. |
| 9 | Boss/world-state phase changes | Medium | **Conditional** | Powerful if combat and spectacle are core; risky if not. |
| 10 | Worker priorities/schedules | Low-medium | **Defer** | Expensive UI/AI/pathing requirement. |
| 11 | Full gas/liquid/thermal simulation | Low first-pass | **Defer** | High technical and teaching cost. |

## ProjectTwelve implementation roadmap

### Phase 1: Foundational feature work

- Define a **data-driven recipe schema** with ingredients, station requirements, unlock conditions, source hints, tags, and output categories.
- Add a **recipe discovery UI contract** before the graph becomes large: search, station filters, unavailable-reason text, and "show uses" links.
- Add **milestone flags** for world events, biome discoveries, machine unlocks, NPC arrivals, and environmental access.
- Define a small set of **room tags** and a validator that can detect enclosure, size, door/access, light, furniture/machine anchors, and hazard exposure.

### Phase 2: High-payoff prototypes

- Prototype **NPC service rooms**: trader, engineer, medic, courier, researcher, cartographer, and transport operator as original ProjectTwelve archetypes.
- Prototype **environmental hazards** with temporary workarounds: heat, cold, toxic air, pressure, darkness, corrosive fluid, or unstable terrain.
- Prototype **diagnostic overlays** for at least power, room tags, hazard exposure, and tile/material categories.
- Prototype **lightweight automation** with switches, sensors, signal wire, doors, lamps, alarms, and one mover machine such as a fan or pump placeholder.

### Phase 3: Conditional expansions

- Add authored challenge sites inside procedural terrain to teach mobility, automation, room sealing, hazard prep, and extraction logistics.
- Expand settlements into faction or reputation systems only after basic rooms and services are fun.
- Add boss/anomaly milestones only if combat testing shows it can carry progression.
- Add worker priorities, schedules, morale, and pathing only if NPC labor becomes a core fantasy.
- Add fluid/gas/thermal simulation only after overlays and small-field prototypes are performant and readable.

## Legal and creative boundaries

Use:

- Abstract mechanics and genre conventions.
- Original implementations of crafting, building, simulation, NPC services, progression, and procedural generation.
- New names, visuals, UI, lore, recipes, numbers, enemy archetypes, and balance curves.
- Source games as comparative references in internal design notes, not as content templates.

Do not use:

- Terraria, Starbound, or Oxygen Not Included names, characters, sprites, music, UI layouts, exact item lists, exact recipes, exact map structures, copied text, or distinctive story elements.
- Exact formulas or hidden mechanics copied from wikis, guides, or decompiled data.
- Marketing claims that imply affiliation, compatibility, endorsement, or sequel/spiritual-successor status.

## Source notes

- Terraria Steam page: product framing for digging, fighting, exploring, building, single-player/co-op/PvP, crafting, machinery, and city construction. <https://store.steampowered.com/app/105600/Terraria/>
- Official Terraria Wiki, `NPCs`: town NPC, housing, and services context. <https://terraria.wiki.gg/wiki/NPCs>
- Official Terraria Wiki, `Bosses`: boss progression and reward context. <https://terraria.wiki.gg/wiki/Bosses>
- Official Terraria Wiki, `Guide:NPC Happiness`: NPC happiness and town logistics context. <https://terraria.wiki.gg/wiki/Guide:NPC_Happiness>
- Terraria Community Forums, `Pylons, Town Building, and NPC Happiness`: official Journey's End-era town/pylon framing. <https://forums.terraria.org/index.php?threads/expand-your-terraria-empire-pylons-town-building-and-npc-happiness.88128/>
- tModLoader site: Terraria modding ecosystem context. <https://www.tmodloader.net/>
- Starbound Steam page: procedural universe, building, colonization, multiplayer, and modding framing. <https://store.steampowered.com/app/211820/Starbound/>
- Chucklefish Forums, `Final Approach to 1.0`: story, generated quests, crew, ship upgrades, crafting progression, hunger, multiplayer, and Workshop context. <https://community.playstarbound.com/threads/final-approach-to-1-0.112500/>
- Starbounder, `Tier`: progression, threat tier, gear, and planet context. <https://starbounder.org/Tier>
- Starbounder, `Colony Deed`: tenant room-tag context. <https://starbounder.org/Colony_Deed>
- Starbounder, `Crew`: crew role context. <https://starbounder.org/Crew>
- Oxygen Not Included Steam page: base-building, oxygen, temperature, plumbing, power, recycling, overlays, stress, and procedural world framing. <https://store.steampowered.com/app/457140/Oxygen_Not_Included/>
- Klei Forums, `Thermal Upgrade`: temperature mechanics, burst pipes, circuits, overheating, stress, and feedback context. <https://forums.kleientertainment.com/forums/topic/77053-game-update-thermal-upgrade-live/>
- Klei Forums, `Automation Innovation Upgrade`: automation, sensors, ribbons, routing, and notification context. <https://forums.kleientertainment.com/forums/topic/85036-game-update-automation-innovation-upgrade/>
- Oxygen Not Included Wiki, `Overlays`: oxygen, power, temperature, material, and diagnostic overlay context. <https://oxygennotincluded.wiki.gg/wiki/Overlay>
- Oxygen Not Included Wiki, `Errand`: indirect labor and priority context. <https://oxygennotincluded.wiki.gg/wiki/Errand>
