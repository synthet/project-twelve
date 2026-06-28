#!/usr/bin/env python3
"""Generate visual catalog ScriptableObject assets for the assets submodule."""

from __future__ import annotations

import re
import uuid
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
SUBMODULE_ROOT = REPO_ROOT / "Assets" / "_Licensed"
CONFIG_PATH = SUBMODULE_ROOT / "config" / "visual-import.txt"
OUTPUT_DIR = SUBMODULE_ROOT / "Settings" / "Visual"
SCRIPTS_ROOT = REPO_ROOT / "Assets" / "Scripts"

EXCLUDED_PREFABS_FOLDER = "Common/Prefabs"
GUID_PATTERN = re.compile(r"^[0-9a-f]{32}$")


def read_config() -> dict[str, str]:
    config: dict[str, str] = {}
    for line in CONFIG_PATH.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        key, value = stripped.split("=", 1)
        config[key.strip()] = value.strip().replace("\\", "/")
    return config


def read_guid(meta_path: Path) -> str:
    for line in meta_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            guid = line.split("guid: ", 1)[1].strip()
            validate_guid(guid, meta_path)
            return guid
    raise ValueError(f"guid not found in {meta_path}")


def validate_guid(guid: str, source: Path) -> None:
    if not GUID_PATTERN.match(guid):
        raise ValueError(
            f"Invalid Unity GUID in {source}: '{guid}' "
            f"(expected exactly 32 lowercase hex characters)"
        )


def read_script_guid(relative_script_path: str) -> str:
    meta_path = SCRIPTS_ROOT / relative_script_path
    meta_path = meta_path.with_suffix(meta_path.suffix + ".meta")
    if not meta_path.is_file():
        raise FileNotFoundError(f"Missing script meta: {meta_path}")
    return read_guid(meta_path)


def read_sprite_file_ids(meta_path: Path) -> list[int]:
    text = meta_path.read_text(encoding="utf-8")
    entries: list[tuple[int, int]] = []
    for match in re.finditer(
        r"- first:\s*\n\s*213:\s*(-?\d+)\s*\n\s*second:\s*(\d+)",
        text,
    ):
        file_id = int(match.group(1))
        index = int(match.group(2))
        entries.append((index, file_id))
    if entries:
        entries.sort(key=lambda item: item[0])
        return [file_id for _, file_id in entries]
    return [21300000]


def unity_ref(file_id: int, guid: str) -> str:
    return f"{{fileID: {file_id}, guid: {guid}, type: 3}}"


def write_meta(asset_path: Path, guid: str) -> None:
    validate_guid(guid, asset_path)
    meta = (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )
    asset_path.with_suffix(asset_path.suffix + ".meta").write_text(meta, encoding="utf-8")


def read_or_create_catalog_guid(asset_path: Path) -> str:
    meta_path = asset_path.with_suffix(asset_path.suffix + ".meta")
    if meta_path.is_file():
        return read_guid(meta_path)
    return uuid.uuid4().hex


def generate_autotile_catalog(tiles_root: str) -> None:
    ground: list[str] = []
    cover: list[str] = []
    for subfolder, bucket in (("Ground", ground), ("Cover", cover)):
        folder = resolve_asset_path(tiles_root, subfolder)
        if not folder.is_dir():
            continue
        for png in sorted(folder.glob("*.png")):
            asset_path = png.relative_to(REPO_ROOT).as_posix()
            meta_path = png.with_suffix(png.suffix + ".meta")
            if not meta_path.is_file():
                continue
            texture_guid = read_guid(meta_path)
            sprite_ids = read_sprite_file_ids(meta_path)
            sprite_lines = "\n".join(
                f"    - {unity_ref(sprite_id, texture_guid)}" for sprite_id in sprite_ids
            )
            tileset_name = png.stem
            bucket.append(
                f"  - tilesetName: {tileset_name}\n"
                f"    texture: {unity_ref(2800000, texture_guid)}\n"
                f"    sprites:\n{sprite_lines}\n"
                f"    customRules: []"
            )

    catalog_guid = read_or_create_catalog_guid(OUTPUT_DIR / "AutotileCatalog.asset")
    autotile_script_guid = read_script_guid("Visual/Tiles/AutotileCatalog.cs")
    asset_path = OUTPUT_DIR / "AutotileCatalog.asset"
    asset_path.parent.mkdir(parents=True, exist_ok=True)
    yaml = (
        "%YAML 1.1\n"
        "%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {autotile_script_guid}, type: 3}}\n"
        "  m_Name: AutotileCatalog\n"
        "  m_EditorClassIdentifier: ProjectTwelve.Runtime::ProjectTwelve.Visual.Tiles.AutotileCatalog\n"
        "  groundTilesets:\n"
        + ("\n".join(ground) if ground else "  []\n")
        + "  coverTilesets:\n"
        + ("\n".join(cover) if cover else "  []\n")
    )
    asset_path.write_text(yaml, encoding="utf-8")
    write_meta(asset_path, catalog_guid)
    print(f"Wrote {asset_path.relative_to(REPO_ROOT)} ({len(ground)} ground, {len(cover)} cover)")


def resolve_asset_path(root: str, sub: str = "") -> Path:
    path = Path(root.replace("\\", "/"))
    if not path.is_absolute():
        path = REPO_ROOT / path
    return path / sub if sub else path


def generate_character_layer_catalog(sprites_root: str, extra_roots: list[str]) -> None:
    layers: list[str] = []
    seen: set[str] = set()

    def add_layer_dir(layer_dir: Path) -> None:
        layer_name = layer_dir.name
        if layer_name in seen:
            return
        textures: list[str] = []
        for png in sorted(layer_dir.glob("*.png")):
            meta_path = png.with_suffix(png.suffix + ".meta")
            if not meta_path.is_file():
                continue
            texture_guid = read_guid(meta_path)
            textures.append(f"    - {unity_ref(2800000, texture_guid)}")
        if not textures:
            return
        seen.add(layer_name)
        layers.append(
            f"  - layerName: {layer_name}\n"
            f"    textures:\n"
            + "\n".join(textures)
        )

    root_path = resolve_asset_path(sprites_root)
    if root_path.is_dir():
        for layer_dir in sorted(root_path.iterdir()):
            if layer_dir.is_dir():
                add_layer_dir(layer_dir)

    for extra in extra_roots:
        extra_path = resolve_asset_path(extra)
        if extra_path.is_dir():
            add_layer_dir(extra_path)

    catalog_guid = read_or_create_catalog_guid(OUTPUT_DIR / "CharacterLayerCatalog.asset")
    character_layer_script_guid = read_script_guid("Visual/Characters/CharacterLayerCatalog.cs")
    asset_path = OUTPUT_DIR / "CharacterLayerCatalog.asset"
    yaml = (
        "%YAML 1.1\n"
        "%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {character_layer_script_guid}, type: 3}}\n"
        "  m_Name: CharacterLayerCatalog\n"
        "  m_EditorClassIdentifier: ProjectTwelve.Runtime::ProjectTwelve.Visual.Characters.CharacterLayerCatalog\n"
        "  layers:\n"
        + ("\n".join(layers) if layers else "  []\n")
    )
    asset_path.write_text(yaml, encoding="utf-8")
    write_meta(asset_path, catalog_guid)
    print(f"Wrote {asset_path.relative_to(REPO_ROOT)} ({len(layers)} layers)")


def generate_monster_catalog(monsters_root: str) -> None:
    root_path = resolve_asset_path(monsters_root)
    entries: list[str] = []
    seen: set[str] = set()
    for prefab in sorted(root_path.rglob("*.prefab")):
        rel = prefab.relative_to(REPO_ROOT).as_posix()
        if EXCLUDED_PREFABS_FOLDER in rel.replace("\\", "/"):
            continue
        meta_path = prefab.with_suffix(prefab.suffix + ".meta")
        if not meta_path.is_file():
            continue
        monster_id = prefab.stem
        if monster_id in seen:
            continue
        seen.add(monster_id)
        prefab_guid = read_guid(meta_path)
        entries.append(
            f"  - monsterId: {monster_id}\n"
            f"    prefab: {unity_ref(100100000, prefab_guid)}"
        )

    catalog_guid = read_or_create_catalog_guid(OUTPUT_DIR / "MonsterVisualCatalog.asset")
    monster_visual_script_guid = read_script_guid("Visual/Monsters/MonsterVisualCatalog.cs")
    asset_path = OUTPUT_DIR / "MonsterVisualCatalog.asset"
    yaml = (
        "%YAML 1.1\n"
        "%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {monster_visual_script_guid}, type: 3}}\n"
        "  m_Name: MonsterVisualCatalog\n"
        "  m_EditorClassIdentifier: ProjectTwelve.Runtime::ProjectTwelve.Visual.Monsters.MonsterVisualCatalog\n"
        "  entries:\n"
        + ("\n".join(entries) if entries else "  []\n")
    )
    asset_path.write_text(yaml, encoding="utf-8")
    write_meta(asset_path, catalog_guid)
    print(f"Wrote {asset_path.relative_to(REPO_ROOT)} ({len(entries)} monsters)")


def main() -> None:
    if not CONFIG_PATH.is_file():
        raise SystemExit(f"Missing config: {CONFIG_PATH}")
    config = read_config()
    generate_autotile_catalog(config["tile_sprites_root"])
    extra_layers = [value for key, value in config.items() if key.startswith("hero_extra_layer")]
    generate_character_layer_catalog(config["hero_sprites_root"], extra_layers)
    generate_monster_catalog(config["monster_prefabs_root"])


if __name__ == "__main__":
    main()
