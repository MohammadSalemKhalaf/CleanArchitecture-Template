#!/usr/bin/env bash
# Manual rollback on the server: redeploys the image that was running before the last successful deployment.
# Usage: ./rollback.sh            (uses ./previous-image)
#        ./rollback.sh <image>    (any earlier image reference)
# Database migrations are never reverted; see README > "Rollback".
set -euo pipefail
cd "$(dirname "$0")"

TARGET="${1:-$(cat previous-image 2>/dev/null || true)}"
[[ -n "$TARGET" ]] || { echo "No previous image recorded; pass an image reference." >&2; exit 1; }

exec ./deploy.sh "$TARGET"
