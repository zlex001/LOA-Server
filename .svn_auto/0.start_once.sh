#!/bin/bash
# SVN Auto Commit (One-click Once) - Mac/Linux version

cd "$(dirname "$0")"

python3 auto_commit.py --repo ".." --once \
  --exclude "**/bin/**,**/obj/**,**/*.exe,**/*.dll,**/*.pdb,**/*.cache,**/*.log,**/~*,**/Release/**,**/Debug/**"

echo ""
echo "[OK] Completed!"
