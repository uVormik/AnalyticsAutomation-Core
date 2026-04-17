# Auth foundation

## Status
- Stage: Sprint 0 / S0-08
- Scope: auth, roles, permissions, session refresh baseline

## Auth approach
- opaque access token
- opaque refresh token
- both tokens are stored server side as hashes
- refresh is validated by session id + refresh token hash
- no JWT/public token verification baseline at this stage

## Auth model
- auth_users
- auth_roles
- auth_permissions
- auth_user_roles
- auth_role_permissions
- auth_sessions
- auth_last_active_device_accounts

## Last active account concept
- device id is the lookup key
- one last active account record exists per device
- the record stores:
  - user id
  - marked timestamp
  - offline restricted flag

## Offline policy hook
- session baseline already exposes IsOfflineRestricted
- auth_last_active_device_accounts gives the server side concept that later DeviceRegistration can rely on

## Development bootstrap
- only when App.Api runs in Development
- only when no auth users exist
- creates:
  - login: platform-owner
  - password: ChangeMe123!
  - role: platform_owner