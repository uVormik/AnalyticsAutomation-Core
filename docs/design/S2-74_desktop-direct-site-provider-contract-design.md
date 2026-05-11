# S2-74 Desktop Direct Site Provider Request / Response Contract Design

Status: design proposal.

This document proposes the request/response boundary for a future direct-site provider upload.

It does not implement DTO code, shared contracts, App.Api endpoints, provider client code, or byte transfer.

## Background

S2-71 found no approved direct-site provider/config/contract.

S2-72 defined the direct-site provider boundary design.

S2-73 added the App.Desktop provider config/options boundary and kept the live provider disabled/unavailable.

S2-74 defines the proposed contract shape that owner/platform/backend must review before implementation.

## Architecture rule

App.Api remains control plane.

Video bytes must not go through App.Api.

The desktop client may upload bytes only to an approved direct-site provider endpoint in a later task.

PreUploadCheck remains the gate before upload.

UploadReceipt remains the server-side authoritative post-upload control-plane record.

## Contract levels

There are three distinct levels and they must not be confused:

1. Desktop internal request model

Used by App.Desktop to describe a direct-site upload attempt before calling a provider client.

2. Provider wire request

The provider-specific HTTP request sent to the direct-site provider.

3. UploadReceipt request

The control-plane request sent to App.Api after a provider upload succeeds.

S2-74 does not add any of these as code.

## Proposed desktop internal upload request fields

A future desktop internal request should contain only safe, necessary fields:

- PreUploadCheckId
- UserId or user context reference if already available from desktop session
- DeviceId if required by control plane
- GroupNodeId
- BusinessObjectKey
- Local file handle or stream abstraction
- File display name without full local path
- SizeBytes
- ByteSha256
- ContentType
- IdempotencyKey
- ProviderKey
- ProviderUploadTarget reference
- ExternalVideoId if assigned by PreUploadCheck / SitePlan
- StorageKey if assigned by PreUploadCheck / SitePlan
- CorrelationId

Fields that must not be present:

- password
- accessToken value
- refreshToken value
- Authorization header value
- provider credential value
- signed URL query secret
- raw local file path in UI/log/debug output
- raw request body
- raw response body

## Proposed provider upload target

A future provider target should be designed as a redacted value object.

Possible fields:

- ProviderKey
- EndpointBaseAddress or UploadUri
- Method
- RequiredHeaders metadata
- CredentialKind
- ExpiresAtUtc if temporary
- MaxSizeBytes if provider-limited
- AllowedContentTypes
- CorrelationId
- RedactedDisplayUri

Sensitive fields must be separated from display/diagnostic fields.

## Proposed provider wire request rules

A future provider wire request must define:

- HTTP method
- upload URL shape
- required headers
- allowed credential kind
- stream behavior
- timeout
- cancellation
- retry policy
- checksum requirement
- content-type behavior
- size validation
- idempotency propagation
- correlation propagation

The provider wire request must never be logged as raw text.

## Proposed provider response fields

A future provider response should contain:

- ProviderKey
- ProviderUploadId or provider receipt id
- ExternalVideoId
- StorageKey
- ProviderStatus
- UploadedAtUtc
- SizeBytes
- ByteSha256 if provider confirms it
- ETag or checksum echo if provider returns one
- CorrelationId
- Retryable flag for failures
- FailureCode
- FailureMessageRedacted
- RawBodyRedacted indicator

Provider response must not expose raw response body in logs or UI.

## Provider status normalization

A future implementation must define provider status mapping.

Minimum proposed normalized statuses:

- Accepted
- Completed
- Rejected
- DuplicateSuspected
- FailedRetryable
- FailedTerminal
- Unknown

Status normalization must be reviewed before UploadReceipt linkage.

## Error and retry classification

A future implementation must define:

- retryable network error
- retryable provider temporary error
- non-retryable validation error
- auth/credential error
- provider unavailable
- checksum mismatch
- size mismatch
- unsupported media type
- unknown provider response

Retry behavior must not create duplicate uploads without idempotency guard.

## Idempotency requirements

A future implementation must define:

- IdempotencyKey source
- whether key is generated before provider upload
- whether key is reused after app restart
- whether key is scoped to PreUploadCheckId
- whether key is scoped to file hash
- whether key is sent to provider
- whether key is sent to UploadReceipt
- how duplicate provider response is handled

Idempotency behavior must be approved before real upload.

## UploadReceipt linkage

A future provider response may be used for UploadReceipt only after approved mapping.

Required linkage decisions:

- PreUploadCheckId source
- GroupNodeId source
- BusinessObjectKey source
- ExternalVideoId source
- StorageKey source
- UploadedAtUtc source
- SiteStatus mapping
- SizeBytes verification
- ByteSha256 verification
- IdempotencyKey propagation
- Provider correlation id propagation
- behavior when provider upload succeeds but UploadReceipt fails
- behavior when UploadReceipt succeeds but later reconcile disagrees

Fake/dev site upload must not produce a live UploadReceipt in configured live control-plane mode.

## Security and redaction

Required redaction rules:

- no password
- no accessToken
- no refreshToken
- no Authorization header
- no provider credential
- no signed URL query secret
- no raw local file path
- no raw request body
- no raw response body
- no screenshots containing secrets
- no full local path in request preview
- no provider credential in ToString

Allowed diagnostic examples:

- provider key
- status enum
- retry classification
- correlation id
- size bucket
- elapsed time
- redacted host if approved
- redacted display URI if approved

## Offline behavior

S2-74 does not implement offline behavior.

Future real upload should default to online-only unless a separate offline/late-sync design is approved.

Offline mode must not claim absolute duplicate prevention while the server is unavailable.

## Proposed review questions

Owner/platform/backend should answer:

1. Is provider upload target issued by App.Api or configured locally?
2. Is provider credential required?
3. If credential is required, who issues it and how long does it live?
4. Is provider upload single-use?
5. Is IdempotencyKey required by provider?
6. Which field is authoritative for ExternalVideoId?
7. Which field is authoritative for StorageKey?
8. Does provider confirm checksum?
9. What statuses can provider return?
10. What response fields can be safely used for UploadReceipt?
11. What fields must be redacted in diagnostics and UI?
12. What is the retry policy?
13. What is the terminal failure policy?
14. What is the reconcile policy after UploadReceipt?

## NO-GO before implementation

Do not implement real direct-site upload while any of these are unresolved:

- provider target source not approved
- credential boundary not approved
- request shape not approved
- response shape not approved
- UploadReceipt mapping not approved
- idempotency not approved
- retry policy not approved
- redaction not approved
- provider status mapping not approved
- security owner/platform owner has not approved
- implementation would send video bytes through App.Api

## Recommended next slices after approval

Possible sequence:

1. S2-75 App.Desktop provider upload target value objects and redacted diagnostics.
2. S2-76 provider request/response model implementation if approved.
3. S2-77 provider client boundary with no live secrets in tests.
4. S2-78 UploadReceipt linkage after provider response.
5. S2-79 controlled live smoke with approved provider and redaction.

## Review checklist

Reviewer should verify:

- this doc does not sneak in runtime implementation
- App.Api remains control plane
- video bytes never go through App.Api
- UploadReceipt remains server-authoritative
- fake/dev path remains isolated
- secrets are explicitly prohibited
- provider credential behavior is not assumed without approval
- next runtime slices remain separate