#!/usr/bin/env bash
set -euo pipefail

REPO_DIR=/tmp/Ion8600ClockSync
PUBLISH_DIR=/var/www/ion8600clocksync
SERVICE_NAME=ion8600clocksync
SERVICE_USER=ion8600clocksync

sudo systemctl stop "$SERVICE_NAME"

if [ -d "$REPO_DIR" ]; then
    git -C "$REPO_DIR" pull
else
    git clone <URL_DO_REPOSITORIO> "$REPO_DIR"
fi

dotnet publish "$REPO_DIR/Ion8600ClockSync/Ion8600ClockSync.csproj" -c Release -o "$PUBLISH_DIR"

sudo chown -R "$SERVICE_USER":"$SERVICE_USER" "$PUBLISH_DIR"

sudo systemctl start "$SERVICE_NAME"
sudo systemctl status "$SERVICE_NAME" --no-pager
