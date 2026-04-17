# S0-14 handoff for coder 2 (web / desktop / shared UI)

## You can now treat as stable
- auth/session baseline
- current user roles/permissions payload
- group tree nodes
- routing preview
- platform foundation endpoint
- observability endpoint
- website integration stub contracts

## Real backend endpoints available
- POST /api/auth/sign-in
- POST /api/auth/refresh
- GET /api/group-tree/nodes
- GET /api/group-tree/routing-preview
- GET /api/system/platform-foundation
- GET /api/system/observability
- GET /api/system/site-gateway
- POST /api/system/site-gateway/reconcile-preview

## What you should do now
- bind web shell to real auth/group/platform endpoints
- make backend-driven UI gating use platform-foundation
- keep upload/download UX behind adapter interfaces
- align upload/download adapter request/response shapes to SiteGatewayDtos
- use routing preview for admin/debug shell

## What you must not do yet
- do not build a fake production upload runtime
- do not invent PreUploadCheck/UploadReceipt payloads
- do not assume download control plane exists
- do not invent duplicate/fraud statuses outside frozen baseline

## Current known stub
- site gateway is stub-only
- use it for contract alignment, not for business-complete upload flow