#!/usr/bin/env bash
# Collect branch delta vs a base ref for RELEASE-PLAN drafting.
# Usage:
#   bash .cursor/skills/release-plan/scripts/collect-release-delta.sh \
#     [--base origin/master] [--version 2.2.0] [--out PATH]
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
cd "$ROOT"

BASE="origin/master"
VERSION=""
OUT=""

require_value() {
  local flag="$1"
  local value="${2-}"
  if [[ -z "$value" || "$value" == -* ]]; then
    echo "error: $flag requires a non-option value" >&2
    exit 1
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base)
      require_value "$1" "${2-}"
      BASE="$2"
      shift 2
      ;;
    --version)
      require_value "$1" "${2-}"
      VERSION="$2"
      shift 2
      ;;
    --out)
      require_value "$1" "${2-}"
      OUT="$2"
      shift 2
      ;;
    -h|--help)
      sed -n '2,6p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown arg: $1" >&2
      exit 1
      ;;
  esac
done

if ! git rev-parse --verify "$BASE" >/dev/null 2>&1; then
  if git rev-parse --verify master >/dev/null 2>&1; then
    echo "warn: '$BASE' missing; falling back to 'master'" >&2
    BASE="master"
  else
    echo "error: base ref '$BASE' not found" >&2
    exit 1
  fi
fi

BRANCH="$(git branch --show-current 2>/dev/null || echo DETACHED)"
DATE="$(date +%Y-%m-%d)"
CACHE_DIR="$ROOT/.cursor/skills/release-plan/.cache"
mkdir -p "$CACHE_DIR"
SAFE_BRANCH="${BRANCH//\//-}"
if [[ -z "$OUT" ]]; then
  OUT="$CACHE_DIR/delta-${VERSION:-unknown}-${SAFE_BRANCH}.md"
fi

MB="$(git merge-base "$BASE" HEAD)"
COMMITS="$(git rev-list --count "${BASE}..HEAD")"
FILES="$(git diff --name-only "${BASE}...HEAD" | wc -l | tr -d ' ')"

{
  echo "# Release delta cache"
  echo
  echo "- **version:** ${VERSION:-_(unset)_}"
  echo "- **branch:** \`$BRANCH\`"
  echo "- **base:** \`$BASE\`"
  echo "- **merge-base:** \`$MB\`"
  echo "- **date:** $DATE"
  echo "- **commits:** $COMMITS · **files:** $FILES"
  echo
  echo "## Commits (\`${BASE}..HEAD\`)"
  echo
  echo '```'
  git log --oneline "${BASE}..HEAD"
  echo '```'
  echo
  echo "## Diffstat"
  echo
  echo '```'
  git diff --stat "${BASE}...HEAD"
  echo '```'
  echo
  echo "## Name-status"
  echo
  echo '```'
  git diff --name-status "${BASE}...HEAD"
  echo '```'
  echo
  echo "## Focus: public service interfaces"
  echo
  echo '```diff'
  git diff "${BASE}...HEAD" -- \
    'Cross.Identity/Services/I*.cs' \
    'Cross.Identity/Services/**/I*.cs' \
    2>/dev/null || true
  echo '```'
  echo
  echo "## Focus: stock flows JSON"
  echo
  echo '```diff'
  git diff "${BASE}...HEAD" -- 'Cross.Identity/ProcessEngine/Definitions/Flows/*.json'
  echo '```'
  echo
  echo "## Focus: FLOWS.md"
  echo
  echo '```diff'
  git diff "${BASE}...HEAD" -- 'Cross.Identity/FLOWS.md'
  echo '```'
  echo
  echo "## Focus: BREAKING.md"
  echo
  echo '```diff'
  git diff "${BASE}...HEAD" -- 'docs/BREAKING.md'
  echo '```'
  echo
  echo "## Focus: steps / factories / services (paths only)"
  echo
  echo '```'
  git diff --name-only "${BASE}...HEAD" -- \
    'Cross.Identity/ProcessEngine/Steps/' \
    'Cross.Identity/ProcessEngine/Factories/' \
    'Cross.Identity/Services/' \
    'Cross.Identity.Tests/'
  echo '```'
} > "$OUT"

echo "$OUT"
