#!/usr/bin/env bash
# Deploys one image to this host: migrate, start, wait for health, roll back the API on failure.
# Usage: ./deploy.sh <registry>/<image>:<tag>
# Runs on the server, from the directory that holds docker-compose.prod.yml and .env.
set -euo pipefail

IMAGE="${1:?usage: deploy.sh <image-reference>}"
cd "$(dirname "$0")"

[[ -f .env ]] || { echo "Missing $(pwd)/.env (see server.env.example)." >&2; exit 1; }

compose() { docker compose -f docker-compose.prod.yml --env-file .env --env-file release.env "$@"; }

PREVIOUS_IMAGE="$(sed -n 's/^API_IMAGE=//p' release.env 2>/dev/null || true)"

echo "Deploying ${IMAGE} (previous: ${PREVIOUS_IMAGE:-none})"
printf 'API_IMAGE=%s\n' "$IMAGE" > release.env

compose pull api

# Migrations run once, before the new version takes traffic. A failure stops the deployment
# with the previous version still running.
if ! compose --profile migrate run --rm migrator; then
  echo "Migration failed; the running version was not changed." >&2
  [[ -n "$PREVIOUS_IMAGE" ]] && printf 'API_IMAGE=%s\n' "$PREVIOUS_IMAGE" > release.env
  exit 1
fi

compose up -d --remove-orphans

status="starting"
for _ in $(seq 1 30); do
  status="$(docker inspect --format '{{.State.Health.Status}}' "$(compose ps -q api)")"
  [[ "$status" == "healthy" ]] && break
  sleep 5
done

if [[ "$status" != "healthy" ]]; then
  echo "New version is ${status}." >&2
  if [[ -n "$PREVIOUS_IMAGE" ]]; then
    echo "Rolling back the API to ${PREVIOUS_IMAGE}. Database migrations are not reverted." >&2
    printf 'API_IMAGE=%s\n' "$PREVIOUS_IMAGE" > release.env
    compose up -d api
  fi
  exit 1
fi

[[ -n "$PREVIOUS_IMAGE" && "$PREVIOUS_IMAGE" != "$IMAGE" ]] && printf '%s\n' "$PREVIOUS_IMAGE" > previous-image
docker image prune --force --filter "until=168h" > /dev/null || true

echo "Deployed ${IMAGE}"
