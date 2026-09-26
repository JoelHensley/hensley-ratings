#!/bin/bash
# Reprocesses all available seasons into the database.
# Produces Hensley ratings only, starts at week 4, uses the largest connected
# group per week.  Does NOT copy output files back to the Dropbox data folder.
set -eo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONSOLE_DIR="$SCRIPT_DIR/../src/console"
DATA_DIR="$HOME/Dropbox/HensleyRatings/Data"
ENV_FILE="$CONSOLE_DIR/.env"

# Years that have complete data in the Dropbox folder (no 2020 — cancelled season)
YEARS=(
  2000 2001 2002 2003 2004 2005 2006 2007 2008 2009
  2010 2011 2012 2013 2014 2015 2016 2017 2018 2019
  2021 2022 2023 2024 2025
)

# ── Save original .env so we can restore it afterwards ───────────────────────
ORIGINAL_ENV=""
if [[ -f "$ENV_FILE" ]]; then
    ORIGINAL_ENV=$(cat "$ENV_FILE")
fi

restore_env() {
    if [[ -n "$ORIGINAL_ENV" ]]; then
        echo "$ORIGINAL_ENV" > "$ENV_FILE"
    else
        rm -f "$ENV_FILE"
    fi
}
trap restore_env EXIT

# ── Build all projects once upfront ──────────────────────────────────────────
echo "=== Building projects ==="
dotnet build "$CONSOLE_DIR/DataConverter"  --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/DatabaseImport" --no-restore -v q 2>&1 | tail -1
dotnet build "$CONSOLE_DIR/RatingSystem"   --no-restore -v q 2>&1 | tail -1
echo ""

# ── Process each year ─────────────────────────────────────────────────────────
FAILED_YEARS=()

for YEAR in "${YEARS[@]}"; do
    YEAR_DIR="$DATA_DIR/$YEAR"
    GAMES_FILE="$YEAR_DIR/cf${YEAR}games.csv"
    TEAMS_FILE="$YEAR_DIR/cf${YEAR}teams.csv"
    WEEK_SETTINGS_FILE="$YEAR_DIR/WeekSettings.txt"

    echo "════════════════════════════════════════════════════════════"
    echo "  Year: $YEAR"
    echo "════════════════════════════════════════════════════════════"

    # Validate that required input files exist
    MISSING=false
    for F in "$GAMES_FILE" "$TEAMS_FILE" "$WEEK_SETTINGS_FILE"; do
        if [[ ! -f "$F" ]]; then
            echo "  SKIP: missing $F"
            MISSING=true
        fi
    done
    if [[ "$MISSING" == "true" ]]; then
        FAILED_YEARS+=("$YEAR (missing files)")
        echo ""
        continue
    fi

    # Write a fresh .env for this year
    cat > "$ENV_FILE" <<EOF
SEASON_YEAR=$YEAR
TEAMS_DATA_FILE=$TEAMS_FILE
GAMES_DATA_FILE=$GAMES_FILE
WEEK_SETTINGS_FILE=$WEEK_SETTINGS_FILE
RATINGS_OUTPUT_DIR=$YEAR_DIR

# Only compute Hensley ratings
HENSLEY_RATING_ENABLED=true
STANDARD_RATING_ENABLED=false
MAX_POINT_DIFF_RATING_ENABLED=false
HOME_FIELD_ADV_RATING_ENABLED=false

# Start at week 4; use only the largest connected group each week
MIN_WEEK=4
LARGEST_GROUP_ONLY=true

# Full wipe of this year's data on each run; do not copy output to Dropbox
WIPE_DB_ON_START=true
COPY_TO_DATA_FOLDER=false
EOF

    if bash "$CONSOLE_DIR/compute-ratings.sh"; then
        echo "  ✓ $YEAR complete"
    else
        echo "  ✗ $YEAR FAILED"
        FAILED_YEARS+=("$YEAR (runtime error)")
    fi
    echo ""
done

# ── Summary ───────────────────────────────────────────────────────────────────
echo "════════════════════════════════════════════════════════════"
echo "  All years processed."
if [[ ${#FAILED_YEARS[@]} -gt 0 ]]; then
    echo "  Failed years:"
    for Y in "${FAILED_YEARS[@]}"; do
        echo "    - $Y"
    done
    exit 1
else
    echo "  No failures."
fi
