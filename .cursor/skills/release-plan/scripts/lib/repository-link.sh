# shellcheck shell=bash
# Resolve GitHub repository base URL from git remote (or default).
repository_link() {
  python3 - "${1:-}" <<'PY'
import re, sys
u = (sys.argv[1] or "").strip()
default = "https://github.com/denis-peshkov/Cross.Identity"
if not u:
    print(default)
    raise SystemExit
m = re.match(r"(?:git@github\.com:|ssh://git@github\.com/)(.+?)(?:\.git)?/?$", u)
if m:
    print(f"https://github.com/{m.group(1)}")
    raise SystemExit
m = re.match(r"https?://github\.com/(.+?)(?:\.git)?/?$", u)
if m:
    print(f"https://github.com/{m.group(1)}")
    raise SystemExit
print(default)
PY
}
