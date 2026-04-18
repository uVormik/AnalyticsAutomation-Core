# Worker pipeline v1

## Status
- Stage: Sprint 1 / S1-07
- Module: WorkerPipeline
- Hosted service: `WorkerPipelineHostedService`
- Kill switch: `Modules:WorkerPipeline:DeepAnalysisEnabled`

## Job types
- `AnalyzeUploadedVideo`

## Status vocabulary
- `queued`
- `running`
- `completed`
- `failed`

## API
- `POST /api/worker-pipeline/jobs/analyze-uploaded-video`
- `POST /api/worker-pipeline/jobs/process-one`
- `GET /api/worker-pipeline/jobs/{jobId}`
- `GET /api/worker-pipeline/jobs?take=25`

## Tables
- `worker_pipeline_jobs`
- `worker_pipeline_job_audit_records`

## Deferred
- ffprobe / ffmpeg / Chromaprint adapters.
- Deep media fingerprint extraction.
- Automatic duplicate/fraud event fan-out from worker result.

## Rollback
Disable `Modules:WorkerPipeline:DeepAnalysisEnabled` or revert the PR.
Migration is additive and keeps job history.