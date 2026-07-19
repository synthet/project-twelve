#!/usr/bin/env python3
"""Run Unity batch validation / EditMode / PlayMode tests for ProjectTwelve.

Deterministic harness for the unity-tests skill. Resolves UNITY_EDITOR, invokes
Unity with the correct Windows batchmode flags, and summarizes NUnit XML.

Examples:
  python scripts/run_unity_tests.py validate
  python scripts/run_unity_tests.py editmode
  python scripts/run_unity_tests.py editmode --filter "SandboxNavPathfinderTests"
  python scripts/run_unity_tests.py playmode
  python scripts/run_unity_tests.py editmode --parity --parity-print-only
"""

from __future__ import annotations

import argparse
import os
import platform
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
EXPECTED_UNITY_VERSION = "6000.5.1f1"


@dataclass
class TestSummary:
    total: int = 0
    passed: int = 0
    failed: int = 0
    result: str = ""
    failed_names: list[str] = field(default_factory=list)
    xml_path: Path | None = None

    @property
    def ok(self) -> bool:
        return self.failed == 0 and self.result.lower() in {"passed", "success", ""}


def load_dotenv_value(env_path: Path, key: str) -> str | None:
    if not env_path.is_file():
        return None
    for raw in env_path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        name, _, value = line.partition("=")
        if name.strip() == key:
            return value.strip().strip('"').strip("'")
    return None


def resolve_unity_editor(repo_root: Path | None = None, env: dict[str, str] | None = None) -> Path:
    root = repo_root or REPO_ROOT
    environ = env if env is not None else os.environ
    candidates: list[str] = []
    if environ.get("UNITY_EDITOR"):
        candidates.append(environ["UNITY_EDITOR"])
    dotenv = load_dotenv_value(root / ".env", "UNITY_EDITOR")
    if dotenv:
        candidates.append(dotenv)
    unity_root = environ.get("UNITY_ROOT") or load_dotenv_value(root / ".env", "UNITY_ROOT")
    if unity_root:
        hub = (
            Path(unity_root)
            / "Hub"
            / "Editor"
            / EXPECTED_UNITY_VERSION
            / "Editor"
            / ("Unity.exe" if platform.system() == "Windows" else "Unity")
        )
        candidates.append(str(hub))
    for raw in candidates:
        path = Path(raw)
        if path.is_file():
            return path
    raise FileNotFoundError(
        "UNITY_EDITOR not found. Set UNITY_EDITOR in the environment or repo-root .env "
        f"(expected Unity {EXPECTED_UNITY_VERSION})."
    )


def ensure_output_dirs(repo_root: Path | None = None) -> tuple[Path, Path]:
    root = repo_root or REPO_ROOT
    results = root / "TestResults"
    logs = root / "Logs"
    results.mkdir(parents=True, exist_ok=True)
    logs.mkdir(parents=True, exist_ok=True)
    return results, logs


def parse_nunit_xml(xml_path: Path) -> TestSummary:
    tree = ET.parse(xml_path)
    root = tree.getroot()
    # NUnit 3 Unity export uses <test-run ...>
    node = root if root.tag.endswith("test-run") else root.find(".//{*}test-run") or root
    total = int(node.attrib.get("total") or node.attrib.get("testcasecount") or 0)
    passed = int(node.attrib.get("passed") or 0)
    failed = int(node.attrib.get("failed") or 0)
    result = str(node.attrib.get("result") or "")
    failed_names: list[str] = []
    for case in root.iter():
        tag = case.tag.rsplit("}", 1)[-1]
        if tag != "test-case":
            continue
        if case.attrib.get("result") == "Failed":
            name = case.attrib.get("name") or case.attrib.get("fullname") or "(unnamed)"
            failed_names.append(name)
    if failed == 0 and failed_names:
        failed = len(failed_names)
    if total == 0:
        # Fallback: count test-case nodes
        cases = [
            c
            for c in root.iter()
            if c.tag.rsplit("}", 1)[-1] == "test-case"
        ]
        total = len(cases)
        if passed == 0 and failed == 0:
            passed = sum(1 for c in cases if c.attrib.get("result") == "Passed")
            failed = sum(1 for c in cases if c.attrib.get("result") == "Failed")
    return TestSummary(
        total=total,
        passed=passed,
        failed=failed,
        result=result or ("Failed" if failed else "Passed"),
        failed_names=failed_names,
        xml_path=xml_path,
    )


def format_summary(summary: TestSummary) -> str:
    lines = [
        f"total={summary.total} passed={summary.passed} failed={summary.failed} "
        f"result={summary.result}"
    ]
    if summary.xml_path:
        lines.append(f"xml={summary.xml_path}")
    for name in summary.failed_names:
        lines.append(f"FAILED {name}")
    return "\n".join(lines)


def build_unity_args(
    mode: str,
    project_path: Path,
    results_dir: Path,
    logs_dir: Path,
    *,
    test_filter: str | None = None,
    results_name: str | None = None,
    log_name: str | None = None,
    is_windows: bool | None = None,
) -> list[str]:
    windows = platform.system() == "Windows" if is_windows is None else is_windows
    project = str(project_path.resolve())
    if mode == "validate":
        log = logs_dir / (log_name or "unity-validate.log")
        return [
            "-batchmode",
            "-quit",
            "-projectPath",
            project,
            "-logFile",
            str(log.resolve()),
        ]

    platform_name = "EditMode" if mode == "editmode" else "PlayMode"
    default_xml = "editmode.xml" if mode == "editmode" else "playmode.xml"
    default_log = (
        "unity-editmode-tests.log" if mode == "editmode" else "unity-playmode-tests.log"
    )
    if test_filter and not results_name:
        slug = re.sub(r"[^A-Za-z0-9._-]+", "-", test_filter)[:48].strip("-") or "filtered"
        default_xml = f"{mode}-{slug}.xml"
        default_log = f"unity-{mode}-{slug}.log"
    xml_path = results_dir / (results_name or default_xml)
    log_path = logs_dir / (log_name or default_log)
    args = [
        "-batchmode",
        "-projectPath",
        project,
        "-runTests",
        "-testPlatform",
        platform_name,
        "-testResults",
        str(xml_path.resolve()),
        "-logFile",
        str(log_path.resolve()),
    ]
    # Windows Unity 6: -quit with -runTests can exit before tests run.
    if not windows:
        args.insert(1, "-quit")
    if test_filter:
        args.extend(["-testFilter", test_filter])
    return args


def run_unity(unity: Path, args: list[str], *, dry_run: bool = False) -> int:
    cmd = [str(unity), *args]
    print(" ".join(cmd), flush=True)
    if dry_run:
        return 0
    completed = subprocess.run(cmd, cwd=REPO_ROOT, check=False)
    return int(completed.returncode)


def parity_commands(repo_root: Path | None = None) -> list[list[str]]:
    root = repo_root or REPO_ROOT
    return [
        ["npm", "test", "--prefix", str(root / "tools" / "tile-viz")],
        ["npm", "test", "--prefix", str(root / "tools" / "world-viz")],
    ]


def run_parity(*, print_only: bool = False, repo_root: Path | None = None) -> int:
    root = repo_root or REPO_ROOT
    status = 0
    for cmd in parity_commands(root):
        print(" ".join(cmd), flush=True)
        if print_only:
            continue
        completed = subprocess.run(cmd, cwd=root, check=False)
        if completed.returncode != 0:
            status = completed.returncode
    return status


def xml_path_from_args(args: list[str]) -> Path | None:
    for i, token in enumerate(args):
        if token == "-testResults" and i + 1 < len(args):
            return Path(args[i + 1])
    return None


def log_path_from_args(args: list[str]) -> Path | None:
    for i, token in enumerate(args):
        if token == "-logFile" and i + 1 < len(args):
            return Path(args[i + 1])
    return None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "mode",
        choices=("validate", "editmode", "playmode"),
        help="validate = batch compile load; editmode/playmode = test suites",
    )
    parser.add_argument("--filter", dest="test_filter", default=None, help="NUnit -testFilter")
    parser.add_argument("--dry-run", action="store_true", help="Print Unity command only")
    parser.add_argument(
        "--parity",
        action="store_true",
        help="Also run (or print) tools/tile-viz and tools/world-viz npm tests",
    )
    parser.add_argument(
        "--parity-print-only",
        action="store_true",
        help="With --parity, print npm commands without executing them",
    )
    parser.add_argument(
        "--project-path",
        type=Path,
        default=REPO_ROOT,
        help="Unity -projectPath (default: repo root)",
    )
    args = parser.parse_args(argv)

    results_dir, logs_dir = ensure_output_dirs(args.project_path)
    try:
        unity = resolve_unity_editor(args.project_path)
    except FileNotFoundError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    unity_args = build_unity_args(
        args.mode,
        args.project_path,
        results_dir,
        logs_dir,
        test_filter=args.test_filter,
    )
    rc = run_unity(unity, unity_args, dry_run=args.dry_run)
    if args.dry_run:
        if args.parity:
            run_parity(print_only=True, repo_root=args.project_path)
        return 0

    if args.mode == "validate":
        if rc != 0:
            log = log_path_from_args(unity_args)
            print(f"validate failed (exit={rc}); inspect {log}", file=sys.stderr)
        else:
            print("validate ok")
        if args.parity:
            parity_rc = run_parity(
                print_only=args.parity_print_only, repo_root=args.project_path
            )
            return rc or parity_rc
        return rc

    xml_path = xml_path_from_args(unity_args)
    log = log_path_from_args(unity_args)
    if xml_path is None or not xml_path.is_file():
        print(
            f"error: missing test results XML ({xml_path}); inspect log {log}",
            file=sys.stderr,
        )
        return rc if rc != 0 else 1

    summary = parse_nunit_xml(xml_path)
    print(format_summary(summary))
    if not summary.ok:
        print(
            f"tests failed; log={log}",
            file=sys.stderr,
        )
        status = 1
    else:
        status = 0 if rc == 0 else rc

    if args.parity:
        parity_rc = run_parity(
            print_only=args.parity_print_only, repo_root=args.project_path
        )
        return status or parity_rc
    return status


if __name__ == "__main__":
    raise SystemExit(main())
