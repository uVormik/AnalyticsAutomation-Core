using BuildingBlocks.Infrastructure.Persistence.Entities;
using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.Devices;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.EntityFrameworkCore;

using BuildingBlocks.Infrastructure.Persistence.Configurations.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence.Configurations.VideoDuplicates;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;
using BuildingBlocks.Infrastructure.Persistence.Configurations.Incidents;
using BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;
using BuildingBlocks.Infrastructure.Persistence.Configurations.FraudSignals;
using BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;
namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<DatabaseBootstrapMarker> DatabaseBootstrapMarkers => Set<DatabaseBootstrapMarker>();

    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<AuthRole> AuthRoles => Set<AuthRole>();
    public DbSet<AuthPermission> AuthPermissions => Set<AuthPermission>();
    public DbSet<AuthUserRole> AuthUserRoles => Set<AuthUserRole>();
    public DbSet<AuthRolePermission> AuthRolePermissions => Set<AuthRolePermission>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<AuthLastActiveDeviceAccount> AuthLastActiveDeviceAccounts => Set<AuthLastActiveDeviceAccount>();

    public DbSet<GroupNode> GroupNodes => Set<GroupNode>();
    public DbSet<GroupAdminAssignment> GroupAdminAssignments => Set<GroupAdminAssignment>();

    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<VideoUploadPreUploadCheck> VideoUploadPreUploadChecks => Set<VideoUploadPreUploadCheck>();

    public DbSet<VideoUploadReceipt> VideoUploadReceipts => Set<VideoUploadReceipt>();
    public DbSet<VideoUploadReceiptAnalysisJob> VideoUploadReceiptAnalysisJobs => Set<VideoUploadReceiptAnalysisJob>();
    public DbSet<VideoUploadReceiptAuditRecord> VideoUploadReceiptAuditRecords => Set<VideoUploadReceiptAuditRecord>();

    public DbSet<VideoDuplicateAsset> VideoDuplicateAssets => Set<VideoDuplicateAsset>();
    public DbSet<VideoDuplicateFingerprint> VideoDuplicateFingerprints => Set<VideoDuplicateFingerprint>();
    public DbSet<VideoDuplicateCandidate> VideoDuplicateCandidates => Set<VideoDuplicateCandidate>();
    public DbSet<VideoDuplicateRegistryAuditRecord> VideoDuplicateRegistryAuditRecords => Set<VideoDuplicateRegistryAuditRecord>();

    public DbSet<DuplicateIncidentRecord> DuplicateIncidentRecords => Set<DuplicateIncidentRecord>();
    public DbSet<DuplicateIncidentAssignmentRecord> DuplicateIncidentAssignmentRecords => Set<DuplicateIncidentAssignmentRecord>();
    public DbSet<DuplicateIncidentDecisionRecord> DuplicateIncidentDecisionRecords => Set<DuplicateIncidentDecisionRecord>();
    public DbSet<DuplicateIncidentAuditRecord> DuplicateIncidentAuditRecords => Set<DuplicateIncidentAuditRecord>();

    public DbSet<FraudSignalRecord> FraudSignalRecords => Set<FraudSignalRecord>();
    public DbSet<FraudSuspicionIncidentRecord> FraudSuspicionIncidentRecords => Set<FraudSuspicionIncidentRecord>();
    public DbSet<FraudSuspicionIncidentAssignmentRecord> FraudSuspicionIncidentAssignmentRecords => Set<FraudSuspicionIncidentAssignmentRecord>();
    public DbSet<FraudSuspicionIncidentDecisionRecord> FraudSuspicionIncidentDecisionRecords => Set<FraudSuspicionIncidentDecisionRecord>();
    public DbSet<FraudSignalAuditRecord> FraudSignalAuditRecords => Set<FraudSignalAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfiguration(new VideoUploadPreUploadCheckConfiguration());
        modelBuilder.ApplyConfiguration(new VideoUploadReceiptConfiguration());
        modelBuilder.ApplyConfiguration(new VideoUploadReceiptAnalysisJobConfiguration());
        modelBuilder.ApplyConfiguration(new VideoUploadReceiptAuditRecordConfiguration());
        modelBuilder.ApplyConfiguration(new VideoDuplicateAssetConfiguration());
        modelBuilder.ApplyConfiguration(new VideoDuplicateFingerprintConfiguration());
        modelBuilder.ApplyConfiguration(new VideoDuplicateCandidateConfiguration());
        modelBuilder.ApplyConfiguration(new VideoDuplicateRegistryAuditRecordConfiguration());
        modelBuilder.ApplyConfiguration(new DuplicateIncidentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new DuplicateIncidentAssignmentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new DuplicateIncidentDecisionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new DuplicateIncidentAuditRecordConfiguration());
        modelBuilder.ApplyConfiguration(new FraudSignalRecordConfiguration());
        modelBuilder.ApplyConfiguration(new FraudSuspicionIncidentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new FraudSuspicionIncidentAssignmentRecordConfiguration());
        modelBuilder.ApplyConfiguration(new FraudSuspicionIncidentDecisionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new FraudSignalAuditRecordConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}