using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.WorkerPipeline;

public static class WorkerPipelineJobTypesV1
{
    public const string AnalyzeUploadedVideo = "AnalyzeUploadedVideo";
}

public static class WorkerPipelineJobStatusesV1
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public sealed record AnalyzeUploadedVideoJobRequestV1Dto(
    Guid UploadReceiptId,
    Guid? VideoAssetId,
    Guid? DuplicateCandidateId,
    Guid? FraudSignalId,
    string? ExternalVideoId,
    string? BusinessObjectKey,
    DateTimeOffset RequestedAtUtc);

public sealed record WorkerPipelineJobV1Dto(
    Guid JobId,
    string JobType,
    string Status,
    Guid? UploadReceiptId,
    int Attempts,
    int MaxAttempts,
    string? LastError,
    string? PayloadJson,
    string? ResultJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? FailedAtUtc);

public sealed record WorkerPipelineProcessResultV1Dto(
    bool Processed,
    WorkerPipelineJobV1Dto? Job,
    string? Message);

public sealed record WorkerPipelineJobListResponseV1Dto(
    IReadOnlyCollection<WorkerPipelineJobV1Dto> Jobs);