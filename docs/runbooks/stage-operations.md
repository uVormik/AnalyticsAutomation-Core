# Stage operations v1
Health:
curl -fsS http://127.0.0.1:5000/health/live
curl -fsS http://127.0.0.1:5000/health/ready
Smoke:
BASE_URL=http://127.0.0.1:5000 bash infra/deploy/stage-smoke.sh
Services:
sudo systemctl status analytics-api
sudo systemctl status analytics-worker
sudo systemctl restart analytics-api
sudo systemctl restart analytics-worker
Logs:
journalctl -u analytics-api -f
journalctl -u analytics-worker -f
Nginx:
sudo nginx -t
sudo systemctl reload nginx
Database:
bash infra/deploy/stage-apply-migrations.sh
Rollback:
bash infra/deploy/stage-rollback.sh /opt/analytics-automation/releases/<previous-release-id>
