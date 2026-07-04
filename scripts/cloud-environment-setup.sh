#!/bin/bash
# Claude Code cloud environment setup script (Ubuntu 24.04).
# Paste this into claude.ai/code → environment → Setup script, or run manually in a cloud VM.
# See .agent/CLOUD_ENVIRONMENT.md for field-by-field configuration.
set -euo pipefail

# Python: OKF lint, paid-assets guard, agent-memory scripts (AGENTS.md)
python3 -m pip install --quiet pyyaml

# Node >=18 offline parity tools (tile-viz needs pngjs)
cd tools/tile-viz && npm install --no-fund --no-audit
cd ../world-viz && npm install --no-fund --no-audit
cd ../..

# Smoke-check offline tests (fail fast if resolver/terrain drift)
cd tools/tile-viz && npm test
cd ../world-viz && npm test
cd ../..

# Output dirs for any future Unity runs (harmless no-op in cloud)
mkdir -p TestResults Logs

# Optional: gh CLI for commands not covered by built-in GitHub tools
apt-get update -qq && apt-get install -y -qq gh || true
