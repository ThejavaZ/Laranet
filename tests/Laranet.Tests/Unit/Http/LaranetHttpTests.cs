using System.Net;
using System.Text;
using System.Text.Json;
using Laranet.Http;
using Laranet.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Laranet.Tests.Unit.Http;

public class LaranetHttpTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnTypedValue_WhenResponseIsSuccessful()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(HttpErrorKind.None, result.ErrorKind);
        Assert.Null(result.Error);
        Assert.Equal(7, result.Value!.Id);
        Assert.Equal("Ada", result.Value.Name);
        Assert.Equal(HttpMethod.Get, context.Handler.Requests[0].Method);
        Assert.Equal("/users/7", context.Handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnHttpFailure_WhenStatusIsNotSuccess()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(
            HttpStatusCode.NotFound,
            """{"error":"missing"}""");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal(HttpErrorKind.Http, result.ErrorKind);
        Assert.Contains("missing", result.Error!);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldBoundTheErrorBody()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(
            HttpStatusCode.InternalServerError,
            new string('x', 5000));

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(4096, result.Error!.Length);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNetworkFailure_WhenTheRequestThrows()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueException(
            new HttpRequestException("Name resolution failed."));

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Null(result.StatusCode);
        Assert.Equal(HttpErrorKind.Network, result.ErrorKind);
        Assert.Contains("Name resolution failed.", result.Error!);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTimeout_WhenTheRequestExceedsItsTimeout()
    {
        using var context = new HttpTestContext();
        context.Handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage();
        });

        var result = await context.Client.GetAsync<TestUser>(
            "/users/7",
            new RequestOptions { Timeout = TimeSpan.FromMilliseconds(100) });

        Assert.False(result.IsSuccess);
        Assert.Null(result.StatusCode);
        Assert.Equal(HttpErrorKind.Timeout, result.ErrorKind);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowOperationCanceledException_WhenTheCallerCancels()
    {
        using var context = new HttpTestContext();
        context.Handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage();
        });

        using var cancellationSource = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                cancellationToken: cancellationSource.Token));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDeserializationFailure_WhenTheJsonIsInvalid()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueJson(HttpStatusCode.OK, "{not-json");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(HttpErrorKind.Deserialization, result.ErrorKind);
        Assert.Contains("could not be deserialized", result.Error!);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnUnexpectedResponse_WhenTheBodyIsEmpty()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.OK, body: string.Empty);

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(HttpErrorKind.UnexpectedResponse, result.ErrorKind);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnUnexpectedResponse_WhenTheStatusHasNoContent()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueResponse(() => new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(HttpErrorKind.UnexpectedResponse, result.ErrorKind);
    }

    [Fact]
    public async Task GetAsync_ShouldApplyRequestHeaders()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueJson(HttpStatusCode.OK, """{"id":7,"name":"Ada"}""");

        var options = new RequestOptions
        {
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer token",
                ["X-Trace-Id"] = "trace-1",
            },
        };

        await context.Client.GetAsync<TestUser>("/users/7", options);

        Assert.Equal(
            "Bearer token",
            context.Handler.Requests[0].Headers["Authorization"].Single());
        Assert.Equal(
            "trace-1",
            context.Handler.Requests[0].Headers["X-Trace-Id"].Single());
    }

    [Fact]
    public async Task GetAsync_ShouldExposeResponseAndContentHeaders()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueResponse(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":7,"name":"Ada"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
            response.Headers.Add("X-Request-Id", "request-1");

            return response;
        });

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.Equal(
            "request-1",
            result.Headers["X-Request-Id"].Single());
        Assert.StartsWith(
            "application/json",
            result.Headers["Content-Type"].Single());
    }

    [Fact]
    public async Task PostAsync_ShouldSendTheJsonBodyAndReturnTheResult()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.Created);

        var result = await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(HttpMethod.Post, context.Handler.Requests[0].Method);
        Assert.Contains(
            "\"displayName\":\"Ada Lovelace\"",
            context.Handler.Requests[0].Content);
    }

    [Fact]
    public async Task PostAsyncOfT_ShouldDeserializeTheResponseBody()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueJson(
            HttpStatusCode.Created,
            """{"id":8,"name":"Grace"}""");

        var result = await context.Client.PostAsync<CreateUser, TestUser>(
            "/users",
            new CreateUser("Grace Hopper"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(8, result.Value!.Id);
        Assert.Equal("Grace", result.Value.Name);
    }

    [Fact]
    public async Task PutAsync_ShouldSendAPutRequest()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var result = await context.Client.PutAsync(
            "/users/7",
            new CreateUser("Ada Lovelace"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Put, context.Handler.Requests[0].Method);
    }

    [Fact]
    public async Task PatchAsync_ShouldSendAPatchRequest()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var result = await context.Client.PatchAsync(
            "/users/7",
            new CreateUser("Ada Lovelace"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Patch, context.Handler.Requests[0].Method);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSendADeleteRequest()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var result = await context.Client.DeleteAsync("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Delete, context.Handler.Requests[0].Method);
    }

    [Fact]
    public async Task PostAsync_ShouldApplyContentHeaders()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var options = new RequestOptions
        {
            ContentHeaders = new Dictionary<string, string>
            {
                ["X-Checksum"] = "123",
            },
        };

        await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"),
            options);

        Assert.Equal(
            "123",
            context.Handler.Requests[0].ContentHeaders["X-Checksum"].Single());
    }

    [Fact]
    public async Task DeleteAsync_ShouldRejectContentHeaders()
    {
        using var context = new HttpTestContext();
        var options = new RequestOptions
        {
            ContentHeaders = new Dictionary<string, string>
            {
                ["X-Checksum"] = "123",
            },
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Client.DeleteAsync("/users/7", options));
    }

    [Fact]
    public async Task PostAsync_ShouldUseTheRequestJsonOptions()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var options = new RequestOptions
        {
            JsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            },
        };

        await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"),
            options);

        Assert.Contains(
            "\"display_name\":\"Ada Lovelace\"",
            context.Handler.Requests[0].Content);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowInvalidOperationException_WhenARelativeUrlHasNoBaseAddress()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var client = new LaranetHttp(
            httpClient,
            NullLogger<LaranetHttp>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetAsync<TestUser>("/users/7"));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenTheUrlIsNull()
    {
        using var context = new HttpTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.Client.GetAsync<TestUser>(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_ShouldThrowArgumentException_WhenTheUrlIsBlank(
        string url)
    {
        using var context = new HttpTestContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Client.GetAsync<TestUser>(url));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentOutOfRangeException_WhenTheTimeoutIsNotPositive()
    {
        using var context = new HttpTestContext();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions { Timeout = TimeSpan.Zero }));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowHttpRequestException_WhenThrowOnErrorIsEnabled()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.BadRequest, "invalid");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions { ThrowOnError = true }));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ShouldRethrowTheNetworkException_WhenThrowOnErrorIsEnabled()
    {
        using var context = new HttpTestContext();
        var networkException = new HttpRequestException("offline");
        context.Handler.EnqueueException(networkException);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions { ThrowOnError = true }));

        Assert.Same(networkException, exception);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowJsonException_WhenThrowOnErrorIsEnabled()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueJson(HttpStatusCode.OK, "{not-json");

        await Assert.ThrowsAsync<JsonException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions { ThrowOnError = true }));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowInvalidOperationException_WhenThrowOnErrorIsEnabled()
    {
        using var context = new HttpTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.OK, body: string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions { ThrowOnError = true }));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowTimeoutException_WhenThrowOnErrorIsEnabled()
    {
        using var context = new HttpTestContext();
        context.Handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage();
        });

        await Assert.ThrowsAsync<TimeoutException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                new RequestOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(100),
                    ThrowOnError = true,
                }));
    }

    private sealed class HttpTestContext : IDisposable
    {
        public HttpTestContext(LaranetHttpOptions? options = null)
        {
            HttpClient = new HttpClient(Handler)
            {
                BaseAddress = new Uri("https://api.test/"),
            };

            var clientOptions = options ?? new LaranetHttpOptions
            {
                Retry = new RetryOptions { MaxRetries = 0 },
            };

            Client = new LaranetHttp(
                HttpClient,
                NullLogger<LaranetHttp>.Instance,
                Options.Create(clientOptions));
        }

        public RecordingHttpMessageHandler Handler { get; } = new();

        public HttpClient HttpClient { get; }

        public LaranetHttp Client { get; }

        public void Dispose()
        {
            HttpClient.Dispose();
            Handler.Dispose();
        }
    }
}

public sealed record TestUser(int Id, string Name);

public sealed record CreateUser(string DisplayName);
