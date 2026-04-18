# PostgreSQL backup and restore on stage

## Backup
Recommended path:
- `/var/backups/analytics-automation/postgresql/`

Example:
- `sudo -u postgres pg_dump -Fc analytics_automation_stage > /var/backups/analytics-automation/postgresql/analytics_automation_stage_YYYYMMDD_HHMMSS.dump`

## Restore drill
1. Create restore target DB:
   - `sudo -u postgres createdb analytics_automation_stage_restore`
2. Restore:
   - `sudo -u postgres pg_restore -d analytics_automation_stage_restore /var/backups/analytics-automation/postgresql/<file>.dump`
3. Run API against restore DB only for validation if needed.
4. Do not overwrite stage DB without explicit rollback decision.

## Policy
- backups are scheduled outside repo
- restore must be rehearsed on stage before production use
- migration flow remains additive-first