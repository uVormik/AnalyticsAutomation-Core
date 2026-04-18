# DuplicateIncident routing v1

## Status
- Stage: Sprint 1 / S1-05
- Module: Incidents
- Endpoints:
  - `POST /api/incidents/duplicates`
  - `GET /api/incidents/duplicates/assigned/{assignedAdminUserId}`
  - `POST /api/incidents/duplicates/{incidentId}/decision`
- Kill switch: `Modules:Incidents:DuplicateIncidentRoutingEnabled`

## Routing baseline
- Regular uploader -> branch admin assignment.
- Branch admin uploader -> higher admin assignment.
- Root/supervisory fallback is represented by the supplied higher admin target list.

## Ownership
Incidents owns:
- `duplicate_incidents`
- `duplicate_incident_assignments`
- `duplicate_incident_decisions`
- `duplicate_incident_audit_records`

## Integration point
This v1 accepts routing context in the create request. Future worker integration can populate this from GroupTree routing query services after duplicate detection.

## Deferred
- Full UI admin review is coder 2 scope after handoff.
- Deep fraud/falsification incident scoring is later FraudSignals scope.
- Worker auto-consumption is deferred to S1-07.

## Rollback
Disable `Modules:Incidents:DuplicateIncidentRoutingEnabled` or revert the PR.
The migration is additive.