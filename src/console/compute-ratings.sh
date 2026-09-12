#!/bin/bash
set -eo pipefail

CONSOLE_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$CONSOLE_DIR/BuildFiles"

# ── Load .env ─────────────────────────────────────────────────────────────────
if [[ ! -f "$CONSOLE_DIR/.env" ]]; then
    echo "Error: .env not found at $CONSOLE_DIR/.env"
    exit 1
fi
set -a
source "$CONSOLE_DIR/.env"
set +a

for VAR in RATINGS_OUTPUT_DIR TEAMS_DATA_FILE SEASON_YEAR WEEK_SETTINGS_FILE; do
    if [[ -z "${!VAR}" ]]; then
        echo "Error: $VAR not set in .env"
        exit 1
    fi
done

RATINGS_OUTPUT_DIR="${RATINGS_OUTPUT_DIR/#\~/$HOME}"
WEEK_SETTINGS_FILE="${WEEK_SETTINGS_FILE/#\~/$HOME}"

if [[ ! -f "$WEEK_SETTINGS_FILE" ]]; then
    echo "Error: WEEK_SETTINGS_FILE not found: $WEEK_SETTINGS_FILE"
    exit 1
fi

export WIPE_DB_ON_START SEASON_YEAR

# ── Pre-build all projects once ───────────────────────────────────────────────
echo "Building projects..."
dotnet build "$CONSOLE_DIR/DataConverter"   --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/DatabaseImport"  --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/RatingSystem"    --no-restore -v q 2>&1 | tail -1
echo ""

# ── Step 1: Convert raw game data ─────────────────────────────────────────────
cd "$BUILD_DIR"
if [[ -z "${GAMES_DATA_FILE:-}" ]]; then
    echo "[1/3] Converting game data..."
    dotnet run --project "$CONSOLE_DIR/DataConverter" --no-build 2>&1 \
        | grep -v "^$" | sed 's/^/  /'
else
    echo "[1/3] Using pre-converted game data: $GAMES_DATA_FILE"
    EXPANDED_GAMES="${GAMES_DATA_FILE/#\~/$HOME}"
    cp "$EXPANDED_GAMES" "$BUILD_DIR/converted-games.csv"
fi
echo ""

# ── Step 2: Import into database ──────────────────────────────────────────────
echo "[2/3] Importing into database..."
cd "$BUILD_DIR"
dotnet run --project "$CONSOLE_DIR/DatabaseImport" --no-build 2>&1 \
    | grep -v "^$" | sed 's/^/  /'
echo ""

# ── Step 3: Compute ratings ───────────────────────────────────────────────────
echo "[3/3] Computing ratings for all weeks..."
cd "$BUILD_DIR"
if [[ "${WIPE_DB_ON_START:-true}" == "true" ]]; then rm -rf Output; fi
mkdir -p Output
dotnet run --project "$CONSOLE_DIR/RatingSystem" --no-build 2>&1 \
    | grep -v "^$" | sed 's/^/  /'
echo ""

# ── Step 4: Copy results (optional) ──────────────────────────────────────────
COPY_TO_DATA_FOLDER="${COPY_TO_DATA_FOLDER:-false}"
if [[ "$COPY_TO_DATA_FOLDER" == "true" ]]; then
    echo "Copying results to $RATINGS_OUTPUT_DIR..."
    for weekDir in "$BUILD_DIR/Output/Week "*/; do
        [[ -d "$weekDir" ]] || continue
        weekName=$(basename "$weekDir")
        DEST="$RATINGS_OUTPUT_DIR/$weekName"
        mkdir -p "$DEST"
        rm -f "$DEST"/*.csv
        cp "$weekDir"*.csv "$DEST/" 2>/dev/null || true
        echo "  → $DEST"
    done
fi

echo "Done."
