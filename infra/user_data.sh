#!/bin/bash
set -e

# Install .NET 8 runtime
dnf install -y aspnetcore-runtime-8.0

# Install Nginx
dnf install -y nginx
systemctl enable nginx
systemctl start nginx

# Install Certbot
dnf install -y python3-certbot-nginx

# Create hrapi system user
useradd --system --no-create-home --shell /sbin/nologin hrapi

# Create application directories
mkdir -p /opt/hensley-ratings/api
mkdir -p /opt/hensley-ratings/frontend
mkdir -p /opt/hensley-ratings/db

chown -R hrapi:hrapi /opt/hensley-ratings

# Install systemd service unit (to be copied on first deploy)
# Service file will be deployed via scripts/deploy.sh
