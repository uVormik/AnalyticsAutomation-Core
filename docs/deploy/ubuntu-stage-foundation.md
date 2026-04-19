# Ubuntu stage deployment v1
Layout:
/opt/analytics-automation/releases/<release-id>/api
/opt/analytics-automation/releases/<release-id>/worker
/opt/analytics-automation/current -> releases/<release-id>
/etc/analytics-automation/stage.api.env
/etc/analytics-automation/stage.worker.env
First install:
sudo mkdir -p /opt/analytics-automation/releases /etc/analytics-automation
sudo cp infra/deploy/examples/stage.api.env.example /etc/analytics-automation/stage.api.env
sudo cp infra/deploy/examples/stage.worker.env.example /etc/analytics-automation/stage.worker.env
Replace CHANGE_ME values outside git.
bash infra/deploy/stage-install-units.sh
Secrets are never stored in repo. Only .env.example files are tracked.
