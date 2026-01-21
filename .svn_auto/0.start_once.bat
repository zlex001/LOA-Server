@echo off
title SVN Auto Commit (One-click Once)
cd /d "%~dp0"

python auto_commit.py --repo ".." --once ^
  --exclude "**/bin/**,**/obj/**,**/*.exe,**/*.dll,**/*.pdb,**/*.cache,**/*.log,**/~*,**/Release/**,**/Debug/**"

echo.
echo [OK] Completed!
pause >nul
