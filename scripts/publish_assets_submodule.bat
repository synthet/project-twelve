@echo off
REM Publish Assets/_Licensed: commit/push in project-twelve-assets, bump gitlink in project-twelve.
REM Usage: scripts\publish_assets_submodule.bat --status
REM        scripts\publish_assets_submodule.bat -m "docs: update licensed inventory"

setlocal
cd /d "%~dp0.."
python scripts/publish_assets_submodule.py %*
exit /b %ERRORLEVEL%
