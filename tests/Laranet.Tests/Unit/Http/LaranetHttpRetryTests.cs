using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Laranet.Http;
using Laranet.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Laranet.Tests.Unit.Http;

public class LaranetHttpRetryTests
{
    [Fact]
    public async Task GetAsync_ShouldRetryTransientStatus_WhenALaterAttemptSucceeds()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
        Assert.Equal(7, result.Value!.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTheLastAttemptFailure_WhenRetriesAreExhausted()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 2,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable, "down");
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable, "still down");
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable, "final");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(HttpErrorKind.Http, result.ErrorKind);
        Assert.Equal("final", result.Error);
        Assert.Equal(3, context.Handler.Requests.Count);
    }

    [Fact]
    public async Task GetAsync_ShouldNotRetry_WhenTheStatusIsNotTransient()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.NotFound, "missing");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task PostAsync_ShouldNotRetryTransientStatus_ByDefault()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);

        var result = await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"));

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task PostAsync_ShouldRetryTransientStatus_WhenTheRequestOptsIn()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueStatus(HttpStatusCode.Created);

        var result = await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"),
            new RequestOptions
            {
                Retry = new RetryOptions
                {
                    MaxRetries = 1,
                    BaseDelay = TimeSpan.Zero,
                    MaxDelay = TimeSpan.Zero,
                    RetryNonIdempotentRequests = true,
                },
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(2, context.Handler.Requests.Count);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRetryTransientStatus_ByDefault()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var result = await context.Client.DeleteAsync("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
    }

    [Fact]
    public async Task GetAsync_ShouldNotRetry_WhenMaxRetriesIsZero()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions { MaxRetries = 0 });
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldRetryNetworkFailures_WhenTheMethodIsIdempotent()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueException(new HttpRequestException("offline"));
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
    }

    [Fact]
    public async Task PostAsync_ShouldNotRetryNetworkFailures_ByDefault()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 2,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueException(new HttpRequestException("offline"));

        var result = await context.Client.PostAsync(
            "/users",
            new CreateUser("Ada Lovelace"));

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpErrorKind.Network, result.ErrorKind);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldNotRetryTimeouts()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 2,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage();
        });

        var result = await context.Client.GetAsync<TestUser>(
            "/users/7",
            new RequestOptions { Timeout = TimeSpan.FromMilliseconds(100) });

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpErrorKind.Timeout, result.ErrorKind);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldNotRetryDeserializationFailures()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueJson(HttpStatusCode.OK, "{not-json");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpErrorKind.Deserialization, result.ErrorKind);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldNotRetryUnexpectedResponses()
    {
        using var context = new HttpRetryTestContext();
        context.Handler.EnqueueStatus(HttpStatusCode.OK, body: string.Empty);

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpErrorKind.UnexpectedResponse, result.ErrorKind);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task PutAsync_ShouldBuildANewRequestAndContentForEveryAttempt()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueStatus(HttpStatusCode.NoContent);

        var result = await context.Client.PutAsync(
            "/users/7",
            new CreateUser("Ada Lovelace"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.MessageInstances.Count);
        Assert.NotSame(
            context.Handler.MessageInstances[0],
            context.Handler.MessageInstances[1]);
        Assert.NotSame(
            context.Handler.ContentInstances[0],
            context.Handler.ContentInstances[1]);
        Assert.Contains(
            "\"displayName\":\"Ada Lovelace\"",
            context.Handler.Requests[0].Content);
        Assert.Contains(
            "\"displayName\":\"Ada Lovelace\"",
            context.Handler.Requests[1].Content);
    }

    [Fact]
    public async Task GetAsync_ShouldStopRetrying_WhenTheCallerCancelsDuringTheDelay()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 2,
                BaseDelay = TimeSpan.FromSeconds(5),
                MaxDelay = TimeSpan.FromSeconds(5),
            });
        using var cancellationSource = new CancellationTokenSource();
        context.Handler.Enqueue((_, _) =>
        {
            cancellationSource.CancelAfter(TimeSpan.FromMilliseconds(50));
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.Client.GetAsync<TestUser>(
                "/users/7",
                cancellationToken: cancellationSource.Token));

        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldHonorRetryAfter_WhenTheServerProvidesIt()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.FromSeconds(5),
                MaxDelay = TimeSpan.FromSeconds(5),
            });
        context.Handler.EnqueueResponse(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
            return response;
        });
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var stopwatch = Stopwatch.StartNew();
        var result = await context.Client.GetAsync<TestUser>("/users/7");
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetAsync_ShouldCapRetryAfterAtTheMaximumDelay()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromMilliseconds(100),
            });
        context.Handler.EnqueueResponse(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(
                TimeSpan.FromSeconds(10));
            return response;
        });
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var stopwatch = Stopwatch.StartNew();
        var result = await context.Client.GetAsync<TestUser>("/users/7");
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetAsync_ShouldPreferTheRequestRetryPolicy_OverTheClientDefault()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 3,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);

        var result = await context.Client.GetAsync<TestUser>(
            "/users/7",
            new RequestOptions
            {
                Retry = new RetryOptions { MaxRetries = 0 },
            });

        Assert.False(result.IsSuccess);
        Assert.Single(context.Handler.Requests);
    }

    [Fact]
    public async Task GetAsync_ShouldUseTheClientRetryPolicy_WhenNoRequestOptionsAreProvided()
    {
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            });
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        var result = await context.Client.GetAsync<TestUser>("/users/7");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, context.Handler.Requests.Count);
    }

    [Fact]
    public async Task GetAsync_ShouldLogTheAttemptThatSucceededAndTheRetry()
    {
        var logger = new CapturingLogger<LaranetHttp>();
        using var context = new HttpRetryTestContext(
            new RetryOptions
            {
                MaxRetries = 1,
                BaseDelay = TimeSpan.Zero,
                MaxDelay = TimeSpan.Zero,
            },
            logger);
        context.Handler.EnqueueStatus(HttpStatusCode.ServiceUnavailable);
        context.Handler.EnqueueJson(
            HttpStatusCode.OK,
            """{"id":7,"name":"Ada"}""");

        await context.Client.GetAsync<TestUser>("/users/7");

        var retryLog = Assert.Single(
            logger.Entries,
            entry => entry.Message.Contains("retrying"));
        Assert.Equal(LogLevel.Warning, retryLog.Level);
        Assert.Equal(1, retryLog.Values["Attempt"]);
        Assert.Equal(503, retryLog.Values["StatusCode"]);

        var successLog = Assert.Single(
            logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("returned"));
        Assert.Equal(LogLevel.Information, successLog.Level);
        Assert.Equal(2, successLog.Values["Attempt"]);
        Assert.Equal(200, successLog.Values["StatusCode"]);
    }

    private sealed class HttpRetryTestContext : IDisposable
    {
        public HttpRetryTestContext(
            RetryOptions? retry = null,
            ILogger<LaranetHttp>? logger = null)
        {
            HttpClient = new HttpClient(Handler)
            {
                BaseAddress = new Uri("https://api.test/"),
            };

            var options = new LaranetHttpOptions
            {
                Retry = retry ?? new RetryOptions
                {
                    BaseDelay = TimeSpan.Zero,
                    MaxDelay = TimeSpan.Zero,
                },
            };

            Client = new LaranetHttp(
                HttpClient,
                logger ?? NullLogger<LaranetHttp>.Instance,
                Options.Create(options));
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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state is IReadOnlyList<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();

            Entries.Add(new LogEntry(
                logLevel,
                formatter(state, exception),
                values));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Values);
}
