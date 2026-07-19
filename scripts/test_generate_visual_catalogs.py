from __future__ import annotations

import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).with_name("generate_visual_catalogs.py")
SPEC = importlib.util.spec_from_file_location("generate_visual_catalogs", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
catalogs = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(catalogs)


class VisualCatalogConfigTests(unittest.TestCase):
    def test_local_override_replaces_submodule_config(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            local = root / "visual-import.local-only.txt"
            submodule = root / "visual-import.txt"
            local.write_text("tile_sprites_root=local/tiles\n", encoding="utf-8")
            submodule.write_text("tile_sprites_root=submodule/tiles\n", encoding="utf-8")

            with (
                mock.patch.object(catalogs, "LOCAL_OVERRIDE_PATH", local),
                mock.patch.object(catalogs, "SUBMODULE_CONFIG_PATH", submodule),
            ):
                self.assertEqual(local, catalogs.resolve_config_path())

    def test_submodule_config_is_fallback(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            local = root / "missing-local.txt"
            submodule = root / "visual-import.txt"
            submodule.write_text("tile_sprites_root=submodule/tiles\n", encoding="utf-8")

            with (
                mock.patch.object(catalogs, "LOCAL_OVERRIDE_PATH", local),
                mock.patch.object(catalogs, "SUBMODULE_CONFIG_PATH", submodule),
            ):
                self.assertEqual(submodule, catalogs.resolve_config_path())

    def test_read_config_normalizes_paths_and_keeps_numbered_values(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = Path(temp_dir) / "visual-import.txt"
            config_path.write_text(
                "# comment\n"
                "hero_sprites_root=Assets\\Heroes\\Sprites\n"
                "hero_extra_layer=Assets/Heroes/Hair\n"
                "hero_extra_layer.1=Assets/Heroes/Hats\n",
                encoding="utf-8",
            )

            self.assertEqual(
                {
                    "hero_sprites_root": "Assets/Heroes/Sprites",
                    "hero_extra_layer": "Assets/Heroes/Hair",
                    "hero_extra_layer.1": "Assets/Heroes/Hats",
                },
                catalogs.read_config(config_path),
            )

    def test_validation_rejects_missing_required_key(self) -> None:
        with self.assertRaisesRegex(SystemExit, "monster_prefabs_root"):
            catalogs.validate_catalog_inputs(
                {
                    "tile_sprites_root": "tiles",
                    "hero_sprites_root": "heroes",
                }
            )

    def test_source_asset_without_unity_metadata_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            source = Path(temp_dir) / "Ground.png"
            source.write_bytes(b"not-a-real-png")

            with self.assertRaisesRegex(FileNotFoundError, "Ground.png.meta"):
                catalogs.read_asset_guid(source)


if __name__ == "__main__":
    unittest.main()
