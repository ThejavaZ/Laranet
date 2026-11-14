using System.Diagnostics;
using System.Net;
using Laranet.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Laranet.Tests.Integration.Http;

public class HttpResilienceIntegrationTests
{
    [Fact]
    public async Task GetAsync_ShouldRetryTransientResponses_AgainstARealServer()
    {
        var attempts = 0;

        using var host = await CreateHostAsync(endpoints =>
        {
            endpoints.MapGet("/flaky", () =>
            {
                var attempt = Interlocked.Increment(ref attempts);

                return attempt < 3
                    ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
                    : Results.Json(new UserDto(7, "Ada"));
            });
        });
        using var httpClient = host.GetTestClient();
        var client = CreateClient(
            httpClient,
            new RetryOptions
            {
                MaxRetries = 2,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });

        var result = await client.GetAsync<UserDto>("/flaky");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(7, result.Value!.Id);
        Assert.Equal("Ada", result.Value.Name);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task GetAsync_ShouldHonorRetryAfter_AgainstARealServer()
    {
        var attempts = 0;

        using var host = await CreateHostAsync(endpoints =>
        {
            endpoints.MapGet("/limited", (HttpContext context) =>
            {
                var attempt = Interlocked.Increment(ref attempts);

                if (attempt == 1)
                {
                    context.Response.Headers.RetryAfter = "0";
                    return Results.StatusCode(
                        StatusCodes.Status429TooManyRequests);
                }

                return Results.Json(new UserDto(7, "Ada"));
            });
        });
        using var httpClient = host.GetTestClient();
        var client = CreateClient(
            httpClient,
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.FromSeconds(5),
                MaxDelay = TimeSpan.FromSeconds(5),
            });

        var stopwatch = Stopwatch.StartNew();
        var result = await client.GetAsync<UserDto>("/limited");
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, attempts);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PostAsync_ShouldNotRetryTransientResponses_AgainstARealServer()
    {
        var attempts = 0;

        using var host = await CreateHostAsync(endpoints =>
        {
            endpoints.MapPost("/orders", () =>
            {
                Interlocked.Increment(ref attempts);
                return Results.StatusCode(
                    StatusCodes.Status503ServiceUnavailable);
            });
        });
        using var httpClient = host.GetTestClient();
        var client = CreateClient(httpClient, new RetryOptions());

        var result = await client.PostAsync(
            "/orders",
            new CreateOrder("book"));

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(1, attempts);
    }

    private static LaranetHttp CreateClient(
        HttpClient httpClient,
        RetryOptions retry)
    {
        return new LaranetHttp(
            httpClient,
            NullLogger<LaranetHttp>.Instance,
            Options.Create(new LaranetHttpOptions { Retry = retry }));
    }

    private static async Task<IHost> CreateHostAsync(
        Action<IEndpointRouteBuilder> mapEndpoints)
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

                    app.UseEndpoints(endpoints => mapEndpoints(endpoints));
                });
            });

        return await builder.StartAsync();
    }

    private sealed record UserDto(int Id, string Name);

    private sealed record CreateOrder(string Product);
}
