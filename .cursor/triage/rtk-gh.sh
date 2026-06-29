#!/usr/bin/env bash
# GitHub CLI wrapper: prefer RTK (Rust Token Killer) for compressed output.
set -euo pipefail

if command -v rtk &>/dev/null && rtk gain &>/dev/null 2>&1; then
  exec rtk gh "$@"
fi

exec gh "$@"
