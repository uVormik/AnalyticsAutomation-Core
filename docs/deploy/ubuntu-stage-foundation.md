# Ubuntu stage foundation

## Runtime layout
- `/opt/analytics-automation/releases/<release>/api`
- `/opt/analytics-automation/releases/<release>/worker`
- `/opt/analytics-automation/api/current`
- `/opt/analytics-automation/worker/current`
- `/etc/analytics-automation/api.env`
- `/etc/analytics-automation/worker.env`
- `/etc/analytics-automation/secrets/`
- `/var/log/analytics-automation/`
- `/var/backups/analytics-automation/postgresql/`

## Services and ports
- `analytics-api.service`
- `analytics-worker.service`
- Kestrel: `127.0.0.1:18080`
- public entrypoint: Nginx with TLS

## Secrets policy
- secrets are never stored in repo
- repo keeps only `*.example` env files
- runtime secrets live under `/etc/analytics-automation/secrets`
- DB password is referenced through `Database__PasswordFilePath`

## Smoke endpoints
- `GET /health/live`
- `GET /health/ready`
- `GET /api/system/version`

## Stage baseline
- provider for website integration remains `Stub`
- stage deploy is manual and reproducible
- rollback is symlink-based