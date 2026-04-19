# Stage operations v1
Health:
curl -fsS http://127.0.0.1:5000/health/live
curl -fsS http://127.0.0.1:5000/health/ready
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
