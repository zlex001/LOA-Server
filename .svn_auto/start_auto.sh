#!/bin/bash
# SVN Auto Commit (AI Agent) - Mac/Linux version

cd "$(dirname "$0")"

if [ ! -f "commit_msg.txt" ]; then
    echo "[Error] commit_msg.txt not found"
    exit 1
fi

python3 auto_commit.py --repo ".." --once \
  --message-file "commit_msg.txt" \
  --log-history "work_history.jsonl" \
  --exclude "**/bin/**,**/obj/**,**/*.exe,**/*.dll,**/*.pdb,**/*.cache,**/*.log,**/~*,**/Release/**,**/Debug/**"

if [ $? -eq 0 ]; then
    rm commit_msg.txt
    echo ""
    echo "[OK] Committed and cleaned up!"
else
    echo ""
    echo "[Error] SVN commit failed. commit_msg.txt preserved for manual handling."
fi
