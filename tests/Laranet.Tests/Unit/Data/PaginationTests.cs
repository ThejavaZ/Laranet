using Laranet.Data.Extensions;

namespace Laranet.Tests.Unit.Data;

public class PaginationTests
{
    private static IQueryable<int> CreateQuery()
    {
        return Enumerable.Range(1, 50).AsQueryable();
    }

    [Fact]
    public void Paginate_ShouldUseDefaultValues()
    {
        var result = CreateQuery().Paginate();

        Assert.Equal(1, result.Page);
        Assert.Equal(15, result.PageSize);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public void Paginate_ShouldReturnRequestedPage()
    {
        var result = CreateQuery().Paginate(page: 2, pageSize: 10);

        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(Enumerable.Range(11, 10), result.Items);
    }

    [Fact]
    public void Paginate_ShouldRespectMaximumPageSize()
    {
        var result = CreateQuery().Paginate(
            pageSize: 200,
            maxPageSize: 100
        );

        Assert.Equal(100, result.PageSize);
        Assert.Equal(50, result.Items.Count);
    }

    [Fact]
    public void Paginate_ShouldAllowCustomMaximumPageSize()
    {
        var result = CreateQuery().Paginate(
            pageSize: 40,
            maxPageSize: 50
        );

        Assert.Equal(40, result.PageSize);
        Assert.Equal(40, result.Items.Count);
    }

    [Fact]
    public void Paginate_ShouldReturnEmptyItems_WhenPageExceedsTotalPages()
    {
        var result = CreateQuery().Paginate(
            page: 10,
            pageSize: 10
        );

        Assert.Empty(result.Items);
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public void Paginate_ShouldHandleEmptyCollections()
    {
        var result = Enumerable.Empty<int>()
            .AsQueryable()
            .Paginate();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Paginate_ShouldThrow_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            QueryablePaginationExtensions.Paginate<int>(null!)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Paginate_ShouldThrow_WhenPageIsInvalid(int page)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateQuery().Paginate(page: page)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Paginate_ShouldThrow_WhenPageSizeIsInvalid(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateQuery().Paginate(pageSize: pageSize)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Paginate_ShouldThrow_WhenMaxPageSizeIsInvalid(int maxPageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateQuery().Paginate(maxPageSize: maxPageSize)
        );
    }

    [Fact]
    public void Paginate_ShouldThrow_WhenPageOffsetExceedsSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateQuery().Paginate(page: int.MaxValue)
        );
    }
}
