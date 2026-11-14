using Laranet.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Unit.EntityFrameworkCore;

public class ConventionsExtensionsTests
{
    [Fact]
    public void AddLaranetAuditing_ShouldThrow_WhenOptionsBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => DbContextOptionsBuilderConventionsExtensions
                .AddLaranetAuditing(null!));
    }

    [Fact]
    public void AddLaranetAuditingGeneric_ShouldThrow_WhenOptionsBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => DbContextOptionsBuilderConventionsExtensions
                .AddLaranetAuditing<DbContext>(null!));
    }

    [Fact]
    public void AddLaranetSoftDelete_ShouldThrow_WhenOptionsBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => DbContextOptionsBuilderConventionsExtensions
                .AddLaranetSoftDelete(null!));
    }

    [Fact]
    public void AddLaranetSoftDeleteGeneric_ShouldThrow_WhenOptionsBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => DbContextOptionsBuilderConventionsExtensions
                .AddLaranetSoftDelete<DbContext>(null!));
    }

    [Fact]
    public void ApplySoftDeleteQueryFilters_ShouldThrow_WhenModelBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => ModelBuilderConventionsExtensions
                .ApplySoftDeleteQueryFilters(null!));
    }
}
