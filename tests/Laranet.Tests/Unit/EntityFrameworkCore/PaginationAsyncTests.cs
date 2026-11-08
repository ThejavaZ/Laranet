using Laranet.EntityFrameworkCore.Extensions;

namespace Laranet.Tests.Unit.EntityFrameworkCore;

public class PaginationAsyncTests
{
    private static IQueryable<int> CreateQuery()
    {
        return Enumerable.Range(1, 50).AsQueryable();
    }

    [Fact]
    public async Task PaginateAsync_ShouldThrow_WhenSourceIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            EntityFrameworkQueryablePaginationExtensions.PaginateAsync<int>(null!)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PaginateAsync_ShouldThrow_WhenPageIsInvalid(int page)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateQuery().PaginateAsync(page: page)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PaginateAsync_ShouldThrow_WhenPageSizeIsInvalid(
        int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateQuery().PaginateAsync(pageSize: pageSize)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PaginateAsync_ShouldThrow_WhenMaxPageSizeIsInvalid(
        int maxPageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateQuery().PaginateAsync(maxPageSize: maxPageSize)
        );
    }

    [Fact]
    public async Task PaginateAsync_ShouldThrow_WhenPageOffsetExceedsSupportedRange()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateQuery().PaginateAsync(page: int.MaxValue)
        );
    }
}
