using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data.TestSupport;

internal sealed class DataTestDbContext(
    DbContextOptions<DataTestDbContext> options) : DbContext(options)
{
    public DbSet<DataTestProduct> Products => Set<DataTestProduct>();
}
