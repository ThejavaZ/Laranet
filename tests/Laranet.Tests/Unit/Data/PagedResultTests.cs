using Laranet.Data.Pagination;

namespace Laranet.Tests.Unit.Data;

public class PagedResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var items = new[] { "Keyboard", "Mouse" };

        var result = new PagedResult<string>(
            items,
            page: 1,
            pageSize: 2,
            totalCount: 5
        );

        Assert.Equal(items, result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ShouldRoundUp()
    {
        var result = new PagedResult<int>(
            [1, 2],
            page: 1,
            pageSize: 3,
            totalCount: 10
        );

        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ShouldBeZero_WhenCollectionIsEmpty()
    {
        var result = new PagedResult<int>(
            [],
            page: 1,
            pageSize: 15,
            totalCount: 0
        );

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void HasPreviousPage_ShouldReturnTrue_WhenPageIsGreaterThanOne()
    {
        var result = new PagedResult<int>(
            [1],
            page: 2,
            pageSize: 10,
            totalCount: 20
        );

        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_ShouldReturnTrue_WhenMorePagesExist()
    {
        var result = new PagedResult<int>(
            [1],
            page: 1,
            pageSize: 10,
            totalCount: 20
        );

        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenItemsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PagedResult<int>(
                null!,
                page: 1,
                pageSize: 10,
                totalCount: 10
            )
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenPageIsInvalid(int page)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResult<int>([], page, 10, 0)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenPageSizeIsInvalid(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResult<int>([], 1, pageSize, 0)
        );
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTotalCountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResult<int>([], 1, 10, -1)
        );
    }
}
