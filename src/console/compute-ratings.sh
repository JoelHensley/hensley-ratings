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

# ── Step 1b: Append future scheduled games (full rebuild only) ────────────────
# In incremental mode (WIPE_DB_ON_START=false) the schedule already lives in the DB and
# raw-games only contains scored games, which are upserted over the existing schedule.
if [[ "${WIPE_DB_ON_START:-true}" == "true" && -n "${SCHEDULE_DATA_FILE:-}" ]]; then
    EXPANDED_SCHED="${SCHEDULE_DATA_FILE/#\~/$HOME}"
    if [[ -f "$EXPANDED_SCHED" ]]; then
        echo "[1b/3] Merging future games from schedule..."
        python3 - "$BUILD_DIR/converted-games.csv" "$EXPANDED_SCHED" <<'PYEOF'
import sys
from datetime import datetime

scored_file, schedule_file = sys.argv[1], sys.argv[2]

def parse(d):
    for fmt in ("%d-%b-%y", "%Y-%m-%d"):
        try: return datetime.strptime(d.strip(), fmt)
        except ValueError: pass
    return None

with open(scored_file) as f:
    scored_lines = f.read().splitlines()

max_date = max((parse(l.split(",")[0]) for l in scored_lines if l), default=None)

if max_date:
    future = []
    with open(schedule_file) as f:
        for line in f:
            line = line.strip()
            if not line: continue
            d = parse(line.split(",")[0])
            if d and d > max_date:
                future.append(line)
    with open(scored_file, "a") as f:
        for line in future:
            f.write(line + "\n")
    print(f"  Appended {len(future)} future games (cutoff: {max_date.strftime('%d-%b-%y')})")
else:
    print("  No scored games found; skipping merge")
PYEOF
        echo ""
    fi
fi

# ── Step 2: Import into database ──────────────────────────────────────────────
echo "[2/3] Importing into database..."
cd "$BUILD_DIR"
dotnet run --project "$CONSOLE_DIR/DatabaseImport" --no-build 2>&1 \
    | grep -v "^$" | sed 's/^/  /'
echo ""

# ── Step 3: Compute ratings ───────────────────────────────────────────────────
if [[ "${WIPE_DB_ON_START:-true}" == "true" ]]; then
    echo "[3/3] Computing ratings for all weeks..."
else
    echo "[3/3] Computing ratings (changed weeks only)..."
fi
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
