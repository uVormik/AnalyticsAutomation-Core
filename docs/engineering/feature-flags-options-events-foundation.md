# Feature flags, options and internal events foundation

## Status
- Stage: Sprint 0 / S0-10
- Scope: platform runtime foundation

## Feature flag naming convention
Use:
- Modules.<ModuleName>.<CapabilityName>Enabled

Current baseline flags:
- Modules.Auth.DevelopmentBootstrapEnabled
- Modules.Devices.DeviceRegistrationEnabled

## Options rules
- every module has its own configuration section
- every module has its own options class
- validation runs on startup
- invalid configuration must fail startup before runtime traffic

## Internal event rules
- use internal events for cross-module reactions instead of hidden direct coupling
- event names stay explicit and business-readable

Current baseline event:
- DeviceRegistrationUpsertedInternalEvent

## First working path
- device registration publishes DeviceRegistrationUpsertedInternalEvent
- logging handler consumes it
- device registration can be disabled by feature flag