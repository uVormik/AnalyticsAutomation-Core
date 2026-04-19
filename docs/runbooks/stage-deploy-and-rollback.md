# Stage deploy and rollback v1
Deploy:
git fetch origin
git checkout main
git pull --ff-only origin main
bash infra/deploy/stage-publish.sh
bash infra/deploy/stage-apply-migrations.sh
sudo systemctl restart analytics-api
sudo systemctl restart analytics-worker
BASE_URL=http://127.0.0.1:5000 bash infra/deploy/stage-smoke.sh
Rollback:
bash infra/deploy/stage-rollback.sh /opt/analytics-automation/releases/<previous-release-id>
BASE_URL=http://127.0.0.1:5000 bash infra/deploy/stage-smoke.sh
Rollback rehearsal:
See docs/runbooks/stage-rollback-rehearsal.md.
Smoke checklist:
See docs/runbooks/stage-smoke-checks.md.
DB rollback policy:
Migrations are additive-first. DB rollback is manual decision only. Default rollback is application artifact rollback.
Secrets:
Secrets are stored outside repo in /etc/analytics-automation/*.env.
