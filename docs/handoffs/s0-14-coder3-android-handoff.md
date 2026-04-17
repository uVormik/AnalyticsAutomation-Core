# S0-14 handoff for coder 3 (android / mobile)

## You can now treat as stable
- auth/session baseline
- current user roles/permissions payload
- device registration endpoint
- group tree nodes baseline
- platform foundation endpoint
- observability endpoint
- website integration stub contracts

## Real backend endpoints available
- POST /api/auth/sign-in
- POST /api/auth/refresh
- POST /api/devices/register
- GET /api/group-tree/nodes
- GET /api/system/platform-foundation
- GET /api/system/observability
- GET /api/system/site-gateway
- POST /api/system/site-gateway/reconcile-preview

## What you should do now
- bind mobile shell to real auth/device/platform endpoints
- persist last active account/session locally
- use backend-driven flags from platform-foundation
- keep camera/file upload behind adapter interfaces
- align future site-facing adapter shapes to SiteGatewayDtos

## What you must not do yet
- do not build fake production upload runtime
- do not invent UploadReceipt/PreUploadCheck payloads
- do not invent sync statuses or incident payloads
- do not move duplicate/fraud business logic into mobile

## Current known stub
- site gateway is stub-only
- use it for contract alignment, not for business-complete upload flow