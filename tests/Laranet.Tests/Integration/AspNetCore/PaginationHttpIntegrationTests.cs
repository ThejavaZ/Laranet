using System.Net;
using System.Text.Json;
using Laranet.AspNetCore.Extensions;
using Laranet.Data.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Laranet.Tests.Integration.AspNetCore;

public class PaginationHttpIntegrationTests
{
    private static async Task<IHost> CreateHostAsync()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();

                webHost.ConfigureServices(
                    services => services.AddRouting());

                webHost.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/paged", () =>
                        {
                            var result = new PagedResult<int>(
                                Enumerable.Range(11, 10).ToArray(),
                                page: 2,
                                pageSize: 10,
                                totalCount: 50);

                            return result.ToHttpResult();
                        });

                        endpoints.MapGet("/empty", () =>
                        {
                            var result = new PagedResult<int>(
                                [],
                                page: 2,
                                pageSize: 10,
                                totalCount: 0);

                            return result.ToHttpResult();
                        });
                    });
                });
            });

        return await builder.StartAsync();
    }

    [Fact]
    public async Task ToHttpResult_ShouldReturnJsonAndPaginationHeaders()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/paged");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        Assert.Equal("50", response.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal("2", response.Headers.GetValues("X-Page").Single());
        Assert.Equal("10", response.Headers.GetValues("X-Page-Size").Single());
        Assert.Equal("5", response.Headers.GetValues("X-Total-Pages").Single());

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(10, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(50, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(5, root.GetProperty("totalPages").GetInt32());
        Assert.True(root.GetProperty("hasPreviousPage").GetBoolean());
        Assert.True(root.GetProperty("hasNextPage").GetBoolean());
        Assert.Equal(10, root.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ToHttpResult_ShouldReturnEmptyPageAndZeroMetadata()
    {
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/empty");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("0", response.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal("2", response.Headers.GetValues("X-Page").Single());
        Assert.Equal("10", response.Headers.GetValues("X-Page-Size").Single());
        Assert.Equal("0", response.Headers.GetValues("X-Total-Pages").Single());

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(0, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, root.GetProperty("totalPages").GetInt32());
        Assert.Equal(0, root.GetProperty("items").GetArrayLength());
    }
}
