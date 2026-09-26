#!/bin/bash
# Runs on the EC2 server — called by deploy.sh via SSH
set -e

TARBALL="/tmp/hensley-ratings.tar.gz"
APP_DIR="/opt/hensley-ratings"
SERVICE="hensleyratings"

echo "==> Stopping service"
sudo systemctl stop "$SERVICE" || true

echo "==> Unpacking tarball"
sudo tar -xzf "$TARBALL" -C /tmp/deploy-unpack/ 2>/dev/null || {
    sudo mkdir -p /tmp/deploy-unpack
    sudo tar -xzf "$TARBALL" -C /tmp/deploy-unpack/
}

echo "==> Deploying files"
sudo rm -rf "$APP_DIR/frontend" "$APP_DIR/api"
sudo cp -r /tmp/deploy-unpack/frontend "$APP_DIR/frontend"
sudo cp -r /tmp/deploy-unpack/api "$APP_DIR/api"
sudo cp /tmp/deploy-unpack/collegefootball.db "$APP_DIR/db/collegefootball.db"

echo "==> Setting permissions"
sudo chown -R hrapi:hrapi "$APP_DIR"
sudo find "$APP_DIR/frontend" -type f -exec chmod 644 {} \;
sudo find "$APP_DIR/frontend" -type d -exec chmod 755 {} \;
sudo chmod +x "$APP_DIR/api/HensleyRatings.Api"

echo "==> Starting service"
sudo systemctl start "$SERVICE"

echo "==> Verifying service"
sleep 2
sudo systemctl is-active "$SERVICE"

echo "==> Cleaning up"
sudo rm -rf /tmp/deploy-unpack "$TARBALL" /tmp/swap-deploy.sh
