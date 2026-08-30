#!/bin/bash
set -e

if [[ -z "$1" || ! "$1" =~ ^[0-9]+$ ]]; then
    echo "Usage: $0 <week_number>"
    exit 1
fi
WEEK="$1"

CONSOLE_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$CONSOLE_DIR/BuildFiles"

if [[ ! -f "$CONSOLE_DIR/.env" ]]; then
    echo "Error: .env not found at $CONSOLE_DIR/.env"
    exit 1
fi
set -a
source "$CONSOLE_DIR/.env"
set +a

if [[ -z "$RATINGS_OUTPUT_DIR" ]]; then
    echo "Error: RATINGS_OUTPUT_DIR not set in .env"
    exit 1
fi
if [[ -z "$TEAMS_DATA_FILE" ]]; then
    echo "Error: TEAMS_DATA_FILE not set in .env"
    exit 1
fi

echo "Step 1: Converting raw game data..."
cd "$BUILD_DIR"
dotnet run --project "$CONSOLE_DIR/DataConverter"

echo ""
echo "Step 2: Importing data into database..."
cd "$BUILD_DIR"
rm -f collegefootball.db
dotnet run --project "$CONSOLE_DIR/DatabaseImport"

echo ""
echo "Step 3: Computing ratings..."
cd "$BUILD_DIR"
rm -rf Output
mkdir -p Output
dotnet run --project "$CONSOLE_DIR/RatingSystem"

echo ""
echo "Step 4: Copying results to Week $WEEK..."
DEST_DIR="${RATINGS_OUTPUT_DIR}/Week ${WEEK}"
if [[ -d "$DEST_DIR" ]]; then
    rm -f "$DEST_DIR"/*
else
    mkdir -p "$DEST_DIR"
fi
cp "$BUILD_DIR/Output/"*.csv "$DEST_DIR/"

echo ""
echo "Done. Results in $DEST_DIR"
