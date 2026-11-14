using Laranet.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data.TestSupport;

internal sealed class DataTestDbContext(
    DbContextOptions<DataTestDbContext> options) : DbContext(options)
{
    public DbSet<DataTestProduct> Products => Set<DataTestProduct>();

    public DbSet<AuditedTestRecord> AuditedRecords => Set<AuditedTestRecord>();

    public DbSet<SoftDeleteTestRecord> SoftDeleteRecords =>
        Set<SoftDeleteTestRecord>();

    public DbSet<AuditedSoftDeleteTestRecord> AuditedSoftDeleteRecords =>
        Set<AuditedSoftDeleteTestRecord>();

    public DbSet<FilteredSoftDeleteTestRecord> FilteredSoftDeleteRecords =>
        Set<FilteredSoftDeleteTestRecord>();

    public DbSet<NamedFilteredSoftDeleteTestRecord>
        NamedFilteredSoftDeleteRecords =>
        Set<NamedFilteredSoftDeleteTestRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FilteredSoftDeleteTestRecord>()
            .HasQueryFilter(record => record.Name != "Hidden");

        modelBuilder.Entity<NamedFilteredSoftDeleteTestRecord>()
            .HasQueryFilter("VisibleOnly", record => record.Name != "Hidden");

        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
