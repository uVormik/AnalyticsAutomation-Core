# Contract map

## BuildingBlocks.Contracts
- Common:
  - ApiErrorDto
  - ValidationIssueDto
  - PageRequestDto
  - PagedResultDto<T>

- Auth:
  - CurrentUserSummaryDto
  - SessionInfoDto
  - SignInRequestDto
  - SignInResponseDto
  - RefreshSessionRequestDto
  - RefreshSessionResponseDto

- Groups:
  - GroupNodeDto
  - GroupTreeNodeDto

- Devices:
  - DevicePlatform
  - DeviceRegistrationRequestDto
  - DeviceRegistrationResponseDto
  - DeviceSummaryDto

- VideoUpload:
  - UploadPrecheckDecision
  - DuplicateMatchKind
  - UploadPrecheckRequestDto
  - UploadPrecheckResponseDto
  - UploadReceiptDto
  - UploadReceiptAcceptedDto

- VideoDownload:
  - CreateDownloadIntentRequestDto
  - CreateDownloadIntentResponseDto
  - DownloadReceiptDto
  - DownloadReceiptAcceptedDto

- VideoSync:
  - PendingSyncItemType
  - PendingSyncItemStatus
  - PendingSyncItemDto
  - PendingSyncSummaryDto
  - SyncPushRequestDto
  - SyncPushResponseDto

- Incidents:
  - IncidentType
  - IncidentStatus
  - IncidentSeverity
  - DuplicateIncidentSummaryDto
  - FraudSuspicionIncidentSummaryDto

## Contract freeze rule
- additive-first
- no silent rename/removal
- any shared change must be announced as a shared contract change