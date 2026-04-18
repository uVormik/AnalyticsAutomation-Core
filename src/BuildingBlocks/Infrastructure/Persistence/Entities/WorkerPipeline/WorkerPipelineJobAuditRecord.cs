using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.WorkerPipeline;

public sealed class WorkerPipelineJobAuditRecord
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}