#!/bin/bash
set -eo pipefail

CONSOLE_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$CONSOLE_DIR/BuildFiles"

# ── Usage ──────────────────────────────────────────────────────────────────────
# Single week:  ./compute-ratings.sh <week_number>
# All weeks:    ./compute-ratings.sh --all
# ──────────────────────────────────────────────────────────────────────────────
if [[ -z "$1" ]]; then
    echo "Usage: $0 <week_number>"
    echo "       $0 --all"
    exit 1
fi

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

# Expand tilde in paths
RATINGS_OUTPUT_DIR="${RATINGS_OUTPUT_DIR/#\~/$HOME}"
WEEK_SETTINGS_FILE="${WEEK_SETTINGS_FILE/#\~/$HOME}"

if [[ ! -f "$WEEK_SETTINGS_FILE" ]]; then
    echo "Error: WEEK_SETTINGS_FILE not found: $WEEK_SETTINGS_FILE"
    exit 1
fi

# ── Core pipeline for a single week ───────────────────────────────────────────
run_week() {
    local WEEK="$1"
    local CUTOFF_DATE="$2"

    export WEEK CUTOFF_DATE SEASON_YEAR

    echo "━━━ Week $WEEK (cutoff $CUTOFF_DATE) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    echo "  [1/4] Preparing game data through $CUTOFF_DATE..."
    cd "$BUILD_DIR"
    if [[ -n "${GAMES_DATA_FILE:-}" ]]; then
        # Pre-converted CSV supplied — filter by cutoff date using Python
        local EXPANDED_GAMES="${GAMES_DATA_FILE/#\~/$HOME}"
        python3 - "$EXPANDED_GAMES" "$CUTOFF_DATE" converted-games.csv << 'PYEOF'
import sys
from datetime import datetime
src, cutoff_str, dst = sys.argv[1], sys.argv[2], sys.argv[3]
cutoff = datetime.strptime(cutoff_str, '%Y-%m-%d').date()
kept = skipped = 0
with open(src) as f, open(dst, 'w') as out:
    for line in f:
        line = line.rstrip('\r\n')
        if not line:
            continue
        date_str = line.split(',')[0].strip()
        try:
            d = datetime.strptime(date_str, '%d-%b-%y').date()
            if d <= cutoff:
                out.write(line + '\n')
                kept += 1
            else:
                skipped += 1
        except ValueError:
            skipped += 1
print(f'Filtered {kept} games ({skipped} skipped) → {dst}')
PYEOF
    else
        # Raw fixed-width file — run DataConverter (CUTOFF_DATE env var filters output)
        dotnet run --project "$CONSOLE_DIR/DataConverter" --no-build 2>&1
    fi | grep -v "^$" | sed 's/^/        /'

    echo "  [2/4] Importing into database..."
    cd "$BUILD_DIR"
    dotnet run --project "$CONSOLE_DIR/DatabaseImport" --no-build 2>&1 \
        | grep -v "^$" | sed 's/^/        /'

    echo "  [3/4] Computing ratings..."
    cd "$BUILD_DIR"
    rm -rf Output && mkdir -p Output
    dotnet run --project "$CONSOLE_DIR/RatingSystem" --no-build 2>&1 \
        | grep -v "^$" | sed 's/^/        /'

    echo "  [4/4] Copying results..."
    DEST_DIR="${RATINGS_OUTPUT_DIR}/Week ${WEEK}"
    mkdir -p "$DEST_DIR"
    rm -f "$DEST_DIR"/*.csv
    cp "$BUILD_DIR/Output/"*.csv "$DEST_DIR/"
    echo "        → $DEST_DIR"
}

# ── Pre-build all projects once ───────────────────────────────────────────────
echo "Building projects..."
dotnet build "$CONSOLE_DIR/DataConverter"   --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/DatabaseImport"  --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/RatingSystem"    --no-restore -v q 2>&1 | tail -1
echo ""

# ── Dispatch ──────────────────────────────────────────────────────────────────
# Weeks 1-3 have too few games for meaningful connected components.
# Override by setting MIN_WEEK in the environment.
MIN_WEEK="${MIN_WEEK:-4}"

if [[ "$1" == "--all" ]]; then
    ELIGIBLE=$(awk -F',' -v m="$MIN_WEEK" '$1+0 >= m' "$WEEK_SETTINGS_FILE" | wc -l | tr -d ' ')
    echo "Computing weeks ≥ $MIN_WEEK for season $SEASON_YEAR ($ELIGIBLE weeks)"
    echo ""

    START_TIME=$SECONDS
    while IFS=',' read -r WEEK CUTOFF_DATE || [[ -n "$WEEK" ]]; do
        WEEK="${WEEK//[$'\r\n']}"
        CUTOFF_DATE="${CUTOFF_DATE//[$'\r\n']}"
        [[ -z "$WEEK" || -z "$CUTOFF_DATE" ]] && continue
        (( WEEK < MIN_WEEK )) && echo "  Skipping week $WEEK (< MIN_WEEK=$MIN_WEEK)" && continue
        run_week "$WEEK" "$CUTOFF_DATE"
        echo ""
    done < "$WEEK_SETTINGS_FILE"

    ELAPSED=$(( SECONDS - START_TIME ))
    echo "All $ELIGIBLE weeks complete in ${ELAPSED}s."

elif [[ "$1" =~ ^[0-9]+$ ]]; then
    WEEK="$1"
    if (( WEEK < MIN_WEEK )); then
        echo "Warning: week $WEEK is below MIN_WEEK=$MIN_WEEK — few teams will be connected."
        echo "Set MIN_WEEK=$WEEK to override."
        exit 1
    fi
    CUTOFF_DATE=$(awk -F',' -v w="$WEEK" '$1==w{print $2}' "$WEEK_SETTINGS_FILE" | tr -d $'\r\n')
    if [[ -z "$CUTOFF_DATE" ]]; then
        echo "Error: week $WEEK not found in $WEEK_SETTINGS_FILE"
        exit 1
    fi
    run_week "$WEEK" "$CUTOFF_DATE"
    echo ""
    echo "Done."

else
    echo "Error: argument must be a week number or --all"
    echo "Usage: $0 <week_number>"
    echo "       $0 --all"
    exit 1
fi
