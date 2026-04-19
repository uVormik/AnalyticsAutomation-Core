# Stage rollback rehearsal v1
Purpose: verify that stage can move from current release to previous release.
Preconditions:
- At least two release folders exist under /opt/analytics-automation/releases.
- /opt/analytics-automation/current points to the active release.
- stage-smoke.sh passes before rehearsal.
Rehearsal:
1. Capture current target: readlink -f /opt/analytics-automation/current.
2. Select previous release folder.
3. Run: bash infra/deploy/stage-rollback.sh /opt/analytics-automation/releases/<previous-release-id>.
4. Run: BASE_URL=http://127.0.0.1:5000 bash infra/deploy/stage-smoke.sh.
5. Record rollback target and smoke result in release notes.
DB policy:
- Default rollback is application artifact rollback.
- DB rollback is manual decision only.
- Additive migrations are preferred and should not require immediate DB rollback.
Failure triage:
- If service restart fails, restore symlink to prior known-good release.
- If ready fails after rollback, inspect DB migration compatibility.
- If nginx fails, revert nginx config separately.
