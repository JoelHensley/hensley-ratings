#!/bin/bash
set -e

CONSOLE_DIR="$(cd "$(dirname "$0")" && pwd)"

if [[ ! -f "$CONSOLE_DIR/.env" ]]; then
    echo "Error: .env not found at $CONSOLE_DIR/.env"
    exit 1
fi
set -a
source "$CONSOLE_DIR/.env"
set +a

if [[ -z "$PREVIOUS_TEAMS_DATA_FILE" ]]; then
    echo "Error: PREVIOUS_TEAMS_DATA_FILE not set in .env"
    exit 1
fi

cd "$CONSOLE_DIR"
dotnet run --project TeamsParser
