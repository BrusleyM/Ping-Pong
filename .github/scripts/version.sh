#!/usr/bin/env bash
# Version name RELEASE.MAIN.DEVELOP, worked out from the git tags (nothing is committed back):
#   RELEASE  the major number in ProjectSettings bundleVersion. Bump it by hand (e.g. to 2.0.0) for a release;
#            MAIN and DEVELOP then start again at 0.
#   MAIN     one more than the last version promoted to BETA (main push) in this release.
#   DEVELOP  counts ALPHA uploads (develop pushes) since that promotion, starting at 0.
# Tags: develop uploads vR.M.D-alpha.<run>, main promotions v<promoted version>-beta.<run>.
#
# Usage: version.sh next      version for a new develop build
#        version.sh promoted  version of the latest ALPHA upload (what main promotes to BETA)
# Needs the tags fetched (git fetch --tags).
set -euo pipefail

mode=${1:?usage: version.sh next|promoted}
settings=${PROJECT_SETTINGS:-ProjectSettings/ProjectSettings.asset}

release=$(sed -n 's/^  bundleVersion: \([0-9]*\).*/\1/p' "$settings")
[ -n "$release" ] || { echo "::error::No bundleVersion in $settings" >&2; exit 1; }

# Highest number captured by regex $1 across all tags, or -1 when none.
highest() {
  git tag -l | sed -nE "s/$1/\\1/p" | sort -n | tail -1 | grep . || echo -1
}

case "$mode" in
  next)
    main=$(( $(highest "^v${release}\\.([0-9]+)\\.[0-9]+-beta\\.[0-9]+$") + 1 ))
    develop=$(( $(highest "^v${release}\\.${main}\\.([0-9]+)-alpha\\.[0-9]+$") + 1 ))
    echo "${release}.${main}.${develop}"
    ;;
  promoted)
    # The latest ALPHA upload is the one with the highest run number.
    latest=$(git tag -l 'v*-alpha.*' | sed -nE 's/^v([0-9]+\.[0-9]+\.[0-9]+)-alpha\.([0-9]+)$/\2 \1/p' | sort -n | tail -1)
    [ -n "$latest" ] || { echo "::error::No ALPHA upload tag to promote." >&2; exit 1; }
    echo "${latest#* }"
    ;;
  *) echo "usage: version.sh next|promoted" >&2; exit 2 ;;
esac
