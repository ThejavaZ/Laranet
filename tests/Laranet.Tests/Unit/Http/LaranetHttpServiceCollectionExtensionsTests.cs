using System.Net;
using Laranet.Http;
using Laranet.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Tests.Unit.Http;

public class LaranetHttpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLaranetHttp_ShouldThrow_WhenServicesAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LaranetHttpServiceCollectionExtensions.AddLaranetHttp(null!));
    }

    [Fact]
    public async Task AddLaranetHttp_ShouldRegisterATypedClientAndApplyTheBaseAddress()
    {
        using var handler = new RecordingHttpMessageHandler();
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":1,"name":"Ada"}""");

        var services = new ServiceCollection();
        services.AddLogging();

        var builder = services.AddLaranetHttp(options =>
        {
            options.BaseAddress = new Uri("https://api.test/");
            options.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LaranetHttp>();

        var result = await client.GetAsync<TestUser>("/users/1");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal(
            "/users/1",
            handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AddLaranetHttp_ShouldWorkWithoutConfiguration()
    {
        using var handler = new RecordingHttpMessageHandler();
        handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":1,"name":"Ada"}""");

        var services = new ServiceCollection();
        services.AddLogging();

        var builder = services.AddLaranetHttp();
        builder.ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<LaranetHttp>();

        var result = await client.GetAsync<TestUser>("https://api.test/users/1");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
    }

    [Fact]
    public void AddLaranetHttp_ShouldRejectARelativeBaseAddress()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetHttp(options =>
            options.BaseAddress = new Uri("/api", UriKind.Relative));

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<LaranetHttp>());
    }

    [Fact]
    public void AddLaranetHttp_ShouldRejectANonPositiveTimeout()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetHttp(options => options.Timeout = TimeSpan.Zero);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<LaranetHttp>());
    }
}
