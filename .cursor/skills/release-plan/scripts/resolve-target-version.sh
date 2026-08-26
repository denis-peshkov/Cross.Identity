#!/usr/bin/env bash
# Resolve target release version and related paths for the current branch.
#
# Usage:
#   bash .cursor/skills/release-plan/scripts/resolve-target-version.sh
#   bash .cursor/skills/release-plan/scripts/resolve-target-version.sh --json
#   bash .cursor/skills/release-plan/scripts/resolve-target-version.sh --version 2.3.0
#   eval "$(bash .cursor/skills/release-plan/scripts/resolve-target-version.sh --export)"
#
# Exit codes:
#   0 — target version resolved
#   2 — bump ambiguous (not release/* or hotfix/*); ask user for X.Y.Z
#   1 — error
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
cd "$ROOT"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=lib/repository-link.sh
source "$SCRIPT_DIR/lib/repository-link.sh"

BASE="origin/master"
VERSION=""
FORMAT="text"

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
    --json|--export)
      FORMAT="${1#--}"
      shift
      ;;
    -h|--help)
      sed -n '2,12p' "$0"
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
    BASE="master"
  else
    echo "error: base ref '$BASE' not found" >&2
    exit 1
  fi
fi

BRANCH="$(git branch --show-current 2>/dev/null || echo DETACHED)"
REPOSITORY_LINK="$(repository_link "$(git remote get-url origin 2>/dev/null || true)")"

latest_published() {
  git tag -l 'v*' 2>/dev/null \
    | sed 's/^v//' \
    | sort -t. -k1,1n -k2,2n -k3,3n \
    | tail -1
}

bump_version() {
  local ver="$1"
  local kind="$2"
  python3 - "$ver" "$kind" <<'PY'
import sys
parts = list(map(int, sys.argv[1].split(".")))
kind = sys.argv[2]
if kind == "minor":
    parts[1] += 1
    parts[2] = 0
elif kind == "patch":
    parts[2] += 1
else:
    raise SystemExit(f"unsupported bump: {kind}")
print(".".join(map(str, parts)))
PY
}

LATEST="$(latest_published)"
if [[ -z "$LATEST" ]]; then
  echo "error: no v* git tags found" >&2
  exit 1
fi

BUMP="ask"
if [[ "$BRANCH" == release/* ]]; then
  BUMP="minor"
elif [[ "$BRANCH" == hotfix/* ]]; then
  BUMP="patch"
fi

TARGET="$VERSION"
if [[ -z "$TARGET" ]]; then
  if [[ "$BUMP" == "ask" ]]; then
    if [[ "$FORMAT" == "json" ]]; then
      printf '{"branch":"%s","base":"%s","repository_link":"%s","latest_published":"%s","bump":"ask","target_version":null,"from_version":"%s","plan_path":null,"plan_exists":false,"breaking_from":"%s","breaking_to":null}\n' \
        "$BRANCH" "$BASE" "$REPOSITORY_LINK" "$LATEST" "$LATEST" "$LATEST"
    elif [[ "$FORMAT" == "export" ]]; then
      echo "RP_BRANCH=$(printf '%q' "$BRANCH")"
      echo "RP_BASE=$(printf '%q' "$BASE")"
      echo "RP_REPOSITORY_LINK=$(printf '%q' "$REPOSITORY_LINK")"
      echo "RP_LATEST_PUBLISHED=$(printf '%q' "$LATEST")"
      echo "RP_BUMP=ask"
      echo "RP_TARGET_VERSION="
      echo "RP_FROM_VERSION=$(printf '%q' "$LATEST")"
      echo "RP_PLAN_PATH="
      echo "RP_PLAN_EXISTS=0"
    else
      cat <<EOF
branch: $BRANCH
base: $BASE
repository_link: $REPOSITORY_LINK
latest_published: $LATEST
bump: ask (not release/* or hotfix/* — ask user for X.Y.Z)
target_version: _(unset)_
from_version: $LATEST
plan_path: _(unset)_
plan_exists: no
EOF
    fi
    exit 2
  fi
  TARGET="$(bump_version "$LATEST" "$BUMP")"
fi

FROM_VERSION="$LATEST"
PLAN_PATH="docs/RELEASE-PLAN-${TARGET}.md"
PLAN_EXISTS="false"
if [[ -f "$PLAN_PATH" ]]; then
  PLAN_EXISTS="true"
fi

BREAKING_PATH="docs/BREAKING.md"
ANCHOR="$(printf 'from-%s-to-%s' "${FROM_VERSION//./}" "${TARGET//./}" | tr '[:upper:]' '[:lower:]')"

if [[ "$FORMAT" == "json" ]]; then
  printf '{"branch":"%s","base":"%s","repository_link":"%s","latest_published":"%s","bump":"%s","target_version":"%s","from_version":"%s","plan_path":"%s","plan_exists":%s,"breaking_path":"%s","breaking_from":"%s","breaking_to":"%s","breaking_anchor":"%s"}\n' \
    "$BRANCH" "$BASE" "$REPOSITORY_LINK" "$LATEST" "$BUMP" "$TARGET" "$FROM_VERSION" "$PLAN_PATH" "$PLAN_EXISTS" \
    "$BREAKING_PATH" "$FROM_VERSION" "$TARGET" "$ANCHOR"
elif [[ "$FORMAT" == "export" ]]; then
  echo "RP_BRANCH=$(printf '%q' "$BRANCH")"
  echo "RP_BASE=$(printf '%q' "$BASE")"
  echo "RP_REPOSITORY_LINK=$(printf '%q' "$REPOSITORY_LINK")"
  echo "RP_LATEST_PUBLISHED=$(printf '%q' "$LATEST")"
  echo "RP_BUMP=$(printf '%q' "$BUMP")"
  echo "RP_TARGET_VERSION=$(printf '%q' "$TARGET")"
  echo "RP_FROM_VERSION=$(printf '%q' "$FROM_VERSION")"
  echo "RP_PLAN_PATH=$(printf '%q' "$PLAN_PATH")"
  echo "RP_PLAN_EXISTS=$([[ "$PLAN_EXISTS" == true ]] && echo 1 || echo 0)"
  echo "RP_BREAKING_PATH=$(printf '%q' "$BREAKING_PATH")"
  echo "RP_BREAKING_FROM=$(printf '%q' "$FROM_VERSION")"
  echo "RP_BREAKING_TO=$(printf '%q' "$TARGET")"
  echo "RP_BREAKING_ANCHOR=$(printf '%q' "$ANCHOR")"
else
  cat <<EOF
branch: $BRANCH
base: $BASE
repository_link: $REPOSITORY_LINK
latest_published: $LATEST
bump: $BUMP
target_version: $TARGET
from_version: $FROM_VERSION
plan_path: $PLAN_PATH
plan_exists: $PLAN_EXISTS
breaking_path: $BREAKING_PATH
breaking_from: $FROM_VERSION
breaking_to: $TARGET
breaking_anchor: $ANCHOR
EOF
fi
