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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}