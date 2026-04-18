# FraudSignals v1

## Status
- Stage: Sprint 1 / S1-06
- Module: FraudSignals
- Endpoints:
  - `POST /api/fraud-signals/evaluate-upload`
  - `GET /api/fraud-signals/incidents/assigned/{assignedAdminUserId}`
  - `POST /api/fraud-signals/incidents/{incidentId}/decision`
- Kill switch: `Modules:FraudSignals:Enabled`

## Scope
FraudSignals v1 detects possible falsification through synthetic behavioral signals:
- repeated upload attempts in a short window
- duplicate candidates in the same behavioral context
- branch admin uploader escalation

## Routing baseline
- Regular uploader -> branch admin assignment.
- Branch admin uploader -> higher admin assignment.

## Ownership
FraudSignals owns:
- `fraud_signal_records`
- `fraud_suspicion_incidents`
- `fraud_suspicion_incident_assignments`
- `fraud_suspicion_incident_decisions`
- `fraud_signal_audit_records`

## Deferred
- Worker auto-consumption is deferred to S1-07.
- Deep media fingerprint scoring is deferred to deep dedupe/fraud phases.
- Full web admin review is coder 2 scope after handoff.

## Rollback
Disable `Modules:FraudSignals:Enabled` or revert the PR.
The migration is additive.