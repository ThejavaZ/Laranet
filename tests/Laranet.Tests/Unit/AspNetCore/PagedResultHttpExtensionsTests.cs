using Laranet.AspNetCore.Extensions;
using Laranet.Data.Pagination;
using Microsoft.AspNetCore.Http;

namespace Laranet.Tests.Unit.AspNetCore;

public class PagedResultHttpExtensionsTests
{
    private static PagedResult<int> CreateResult()
    {
        return new PagedResult<int>(
            Enumerable.Range(11, 10).ToArray(),
            page: 2,
            pageSize: 10,
            totalCount: 50);
    }

    [Fact]
    public void WritePaginationHeaders_ShouldSetPaginationHeaders()
    {
        var context = new DefaultHttpContext();

        CreateResult().WritePaginationHeaders(context.Response);

        Assert.Equal("50", context.Response.Headers["X-Total-Count"]);
        Assert.Equal("2", context.Response.Headers["X-Page"]);
        Assert.Equal("10", context.Response.Headers["X-Page-Size"]);
        Assert.Equal("5", context.Response.Headers["X-Total-Pages"]);
    }

    [Fact]
    public void WritePaginationHeaders_ShouldSetZeroMetadata_WhenResultIsEmpty()
    {
        var context = new DefaultHttpContext();

        new PagedResult<int>([], page: 1, pageSize: 15, totalCount: 0)
            .WritePaginationHeaders(context.Response);

        Assert.Equal("0", context.Response.Headers["X-Total-Count"]);
        Assert.Equal("1", context.Response.Headers["X-Page"]);
        Assert.Equal("15", context.Response.Headers["X-Page-Size"]);
        Assert.Equal("0", context.Response.Headers["X-Total-Pages"]);
    }

    [Fact]
    public void WritePaginationHeaders_ShouldThrow_WhenResultIsNull()
    {
        var context = new DefaultHttpContext();

        Assert.Throws<ArgumentNullException>(() =>
            PagedResultHttpExtensions.WritePaginationHeaders<int>(
                null!,
                context.Response));
    }

    [Fact]
    public void WritePaginationHeaders_ShouldThrow_WhenResponseIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CreateResult().WritePaginationHeaders(null!));
    }

    [Fact]
    public void ToHttpResult_ShouldThrow_WhenResultIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PagedResultHttpExtensions.ToHttpResult<int>(null!));
    }
}
