@echo off
title SVN Auto Commit (AI Agent)
cd /d "%~dp0"

if not exist "commit_msg.txt" (
    echo [Error] commit_msg.txt not found
    exit /b 1
)

python auto_commit.py --repo ".." --once ^
  --message-file "commit_msg.txt" ^
  --log-history "work_history.jsonl" ^
  --exclude "**/bin/**,**/obj/**,**/*.exe,**/*.dll,**/*.pdb,**/*.cache,**/*.log,**/~*,**/Release/**,**/Debug/**"

if %errorlevel%==0 (
    del commit_msg.txt
    echo.
    echo [OK] Committed and cleaned up!
) else (
    echo.
    echo [Error] SVN commit failed. commit_msg.txt preserved for manual handling.
)

pause >nul

