# Stage deploy and rollback v1
Deploy:
git fetch origin
git checkout main
git pull --ff-only origin main
bash infra/deploy/stage-publish.sh
bash infra/deploy/stage-apply-migrations.sh
sudo systemctl restart analytics-api
sudo systemctl restart analytics-worker
bash infra/deploy/stage-smoke.sh
Rollback:
bash infra/deploy/stage-rollback.sh /opt/analytics-automation/releases/<previous-release-id>
bash infra/deploy/stage-smoke.sh
DB rollback policy:
Migrations are additive-first. DB rollback is manual decision only. Default rollback is application artifact rollback.
