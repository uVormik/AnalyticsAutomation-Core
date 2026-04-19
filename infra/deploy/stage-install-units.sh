#!/usr/bin/env bash
set -euo pipefail
SRC_ROOT="${SRC_ROOT:-$(pwd)}"
sudo install -m 0644 "$SRC_ROOT/infra/systemd/analytics-api.service" /etc/systemd/system/analytics-api.service
sudo install -m 0644 "$SRC_ROOT/infra/systemd/analytics-worker.service" /etc/systemd/system/analytics-worker.service
sudo install -m 0644 "$SRC_ROOT/infra/nginx/analytics-automation-stage.conf" /etc/nginx/sites-available/analytics-automation-stage.conf
sudo ln -sfn /etc/nginx/sites-available/analytics-automation-stage.conf /etc/nginx/sites-enabled/analytics-automation-stage.conf
sudo systemctl daemon-reload
sudo nginx -t
sudo systemctl reload nginx
