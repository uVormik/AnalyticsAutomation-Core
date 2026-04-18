using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.WorkerPipeline;

public sealed class WorkerPipelineJobRecord
{
    public Guid Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? UploadReceiptId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset AvailableAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
}