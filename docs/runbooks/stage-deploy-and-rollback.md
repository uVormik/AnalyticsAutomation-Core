# Stage deploy and rollback

## Deploy
1. Publish `App.Api` and `App.Worker`.
2. Copy artifacts to `/opt/analytics-automation/releases/<release>/`.
3. Update symlinks:
   - `/opt/analytics-automation/api/current`
   - `/opt/analytics-automation/worker/current`
4. Put real env files in:
   - `/etc/analytics-automation/api.env`
   - `/etc/analytics-automation/worker.env`
5. Install/update:
   - `infra/systemd/analytics-api.service`
   - `infra/systemd/analytics-worker.service`
   - `infra/nginx/analytics-automation-stage.conf`
6. Run:
   - `sudo systemctl daemon-reload`
   - `sudo systemctl restart analytics-api analytics-worker`
   - `sudo nginx -t && sudo systemctl reload nginx`
7. Smoke-check:
   - `curl -f https://stage.example.com/health/live`
   - `curl -f https://stage.example.com/health/ready`
   - `curl -f https://stage.example.com/api/system/version`

## Rollback
1. Point `api/current` and `worker/current` back to previous release.
2. Restart API and Worker.
3. Repeat smoke-check.
4. Keep failed release for investigation until root cause is clear.