# GroupTree and Devices foundation

## Status
- Stage: Sprint 0 / S0-09
- Scope: GroupTree, branch admin routing and device registration baseline

## Group tree model
- group_nodes
- group_admin_assignments

## Device model
- device_registrations

## Routing helper
Preview routing resolves:
- requested group node
- resolved escalation target node
- admin users on the resolved node
- whether escalation to a higher node happened

## Device registration baseline
- upsert by device id
- trusted flag baseline
- last known user link