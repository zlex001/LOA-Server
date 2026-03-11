#!/usr/bin/env bash
# Git commit and push (replaces .svn_auto/start_auto.sh after SVN->Git migration).
# Run from repo root. Reads .git_commit_msg.txt for commit message.

set -e
cd "$(git rev-parse --show-toplevel)"

if [ ! -f ".git_commit_msg.txt" ]; then
  echo "[Error] .git_commit_msg.txt not found"
  exit 1
fi

git add -A
git commit -F .git_commit_msg.txt
git push

echo "[OK] Committed and pushed."
rm -f .git_commit_msg.txt
