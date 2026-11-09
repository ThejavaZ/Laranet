using Laranet.Samples.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Samples.WebApi.Data;

/// <summary>
/// Entity Framework Core context for the sample catalog.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Gets the products in the catalog.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            // SQLite does not support ORDER BY on DateTimeOffset, and DateTime
            // loses its Kind when round-tripped. Store UTC and restore the Kind.
            entity.Property(product => product.CreatedAt)
                .HasConversion(
                    value => value.ToUniversalTime(),
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            entity.HasIndex(product => product.Name);
        });
    }
}
