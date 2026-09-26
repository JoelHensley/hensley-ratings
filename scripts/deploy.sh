#!/bin/bash
set -e

EC2_HOST="${EC2_HOST:?EC2_HOST must be set}"
SSH_KEY="${SSH_KEY:-$HOME/.ssh/hensley-ratings}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="$REPO_ROOT/.deploy-tmp"
TARBALL="$BUILD_DIR/hensley-ratings.tar.gz"
DB_PATH="$REPO_ROOT/src/console/BuildFiles/collegefootball.db"

echo "==> Cleaning build dir"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/frontend" "$BUILD_DIR/api"

echo "==> Building frontend"
cd "$REPO_ROOT/src/web-frontend"
npm ci --silent
npm run build
cp -r dist/. "$BUILD_DIR/frontend/"

echo "==> Publishing backend"
cd "$REPO_ROOT/src/web-backend/HensleyRatings.Api"
dotnet publish -c Release -r linux-x64 --self-contained true -o "$BUILD_DIR/api" --nologo -v quiet

echo "==> Copying database"
cp "$DB_PATH" "$BUILD_DIR/collegefootball.db"

echo "==> Packaging tarball"
cd "$BUILD_DIR"
tar -czf hensley-ratings.tar.gz frontend/ api/ collegefootball.db

echo "==> Uploading to $EC2_HOST"
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$TARBALL" \
    "$REPO_ROOT/scripts/swap-deploy.sh" \
    "ec2-user@$EC2_HOST:/tmp/"

echo "==> Running deploy on server"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "ec2-user@$EC2_HOST" \
    "bash /tmp/swap-deploy.sh"

echo "==> Deploy complete"
rm -rf "$BUILD_DIR"
