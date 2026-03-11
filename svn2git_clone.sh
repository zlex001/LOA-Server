#!/usr/bin/env bash
# One-time SVN-to-Git migration: clone LOA-Server/trunk with full history.
# Run from the parent of trunk (e.g. LOA-Server), or from anywhere with REPO_PARENT set.

set -e

SVN_ROOT="https://106.15.248.25/svn/LOA-Server"
OUT_DIR="LOA-Server-git"

# Resolve repo parent: directory that contains trunk (current SVN WC)
if [ -n "$REPO_PARENT" ]; then
  REPO_PARENT="$(cd "$REPO_PARENT" && pwd)"
else
  SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
  if [ -d "$SCRIPT_DIR/.svn" ]; then
    REPO_PARENT="$(dirname "$SCRIPT_DIR")"
  elif [ -d "$SCRIPT_DIR/trunk" ] && [ -d "$SCRIPT_DIR/trunk/.svn" ]; then
    REPO_PARENT="$SCRIPT_DIR"
  else
    REPO_PARENT="$(pwd)"
  fi
fi

cd "$REPO_PARENT"

if [ -d "$OUT_DIR" ]; then
  echo "[Error] $REPO_PARENT/$OUT_DIR already exists. Remove or rename it first."
  exit 1
fi

AUTHORS_FILE=""
if [ -f "trunk/authors.txt" ]; then
  AUTHORS_FILE="trunk/authors.txt"
  echo "[Info] Using authors file: $REPO_PARENT/$AUTHORS_FILE"
fi

echo "[Info] Cloning from $SVN_ROOT (trunk only) into $REPO_PARENT/$OUT_DIR ..."
if [ -n "$AUTHORS_FILE" ]; then
  git svn clone "$SVN_ROOT" \
    --trunk=trunk \
    --no-metadata \
    --authors-file="$AUTHORS_FILE" \
    "$OUT_DIR"
else
  git svn clone "$SVN_ROOT" \
    --trunk=trunk \
    --no-metadata \
    "$OUT_DIR"
fi

echo ""
echo "[OK] Done. Next steps:"
echo "  1. cd $REPO_PARENT/$OUT_DIR"
echo "  2. git branch -M main   # optional: use main as default branch"
echo "  3. Create an empty repo on GitHub/Gitee/GitLab, then:"
echo "     git remote add origin <YOUR_REMOTE_URL>"
echo "     git push -u origin main"
echo "  See Documents/SVN-to-Git-Migration.md for details."
