from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).with_name("run_unity_tests.py")
SPEC = importlib.util.spec_from_file_location("run_unity_tests", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
unity = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = unity
SPEC.loader.exec_module(unity)


SAMPLE_XML = """<?xml version="1.0" encoding="utf-8"?>
<test-run id="2" total="3" passed="2" failed="1" result="Failed">
  <test-suite>
    <test-case name="PassingOne" result="Passed" />
    <test-case name="PassingTwo" result="Passed" />
    <test-case name="BrokenThing" result="Failed" />
  </test-suite>
</test-run>
"""


class UnityHarnessTests(unittest.TestCase):
    def test_parse_nunit_xml_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            xml_path = Path(temp_dir) / "editmode.xml"
            xml_path.write_text(SAMPLE_XML, encoding="utf-8")
            summary = unity.parse_nunit_xml(xml_path)
            self.assertEqual(3, summary.total)
            self.assertEqual(2, summary.passed)
            self.assertEqual(1, summary.failed)
            self.assertEqual("Failed", summary.result)
            self.assertEqual(["BrokenThing"], summary.failed_names)
            self.assertFalse(summary.ok)
            text = unity.format_summary(summary)
            self.assertIn("failed=1", text)
            self.assertIn("FAILED BrokenThing", text)

    def test_load_dotenv_and_resolve_editor(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            fake = root / "Unity.exe"
            fake.write_text("", encoding="utf-8")
            (root / ".env").write_text(f"UNITY_EDITOR={fake}\n", encoding="utf-8")
            resolved = unity.resolve_unity_editor(root, env={})
            self.assertEqual(fake.resolve(), resolved.resolve())

    def test_build_args_omit_quit_on_windows_runtests(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            results, logs = unity.ensure_output_dirs(root)
            args = unity.build_unity_args(
                "editmode",
                root,
                results,
                logs,
                is_windows=True,
            )
            self.assertIn("-runTests", args)
            self.assertNotIn("-quit", args)
            self.assertIn("-testPlatform", args)
            self.assertEqual("EditMode", args[args.index("-testPlatform") + 1])

    def test_build_args_include_quit_on_non_windows_runtests(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            results, logs = unity.ensure_output_dirs(root)
            args = unity.build_unity_args(
                "editmode",
                root,
                results,
                logs,
                is_windows=False,
            )
            self.assertIn("-quit", args)
            self.assertIn("-runTests", args)

    def test_validate_args_always_quit(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            results, logs = unity.ensure_output_dirs(root)
            args = unity.build_unity_args(
                "validate",
                root,
                results,
                logs,
                is_windows=True,
            )
            self.assertIn("-quit", args)
            self.assertNotIn("-runTests", args)

    def test_parity_commands(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            cmds = unity.parity_commands(root)
            self.assertEqual(2, len(cmds))
            self.assertTrue(any("tile-viz" in " ".join(c) for c in cmds))
            self.assertTrue(any("world-viz" in " ".join(c) for c in cmds))


if __name__ == "__main__":
    unittest.main()
