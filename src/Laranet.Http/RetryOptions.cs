namespace Laranet.Http;

/// <summary>
/// Configures safe retries for <see cref="LaranetHttp"/> operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MaxRetries"/> counts additional attempts after the first. Every attempt
/// rebuilds the request, so the same <see cref="HttpRequestMessage"/> is never sent
/// twice.
/// </para>
/// <para>
/// Retries apply to <see cref="HttpMethod.Get"/>, <see cref="HttpMethod.Put"/>, and
/// <see cref="HttpMethod.Delete"/> by default. <see cref="HttpMethod.Post"/> and
/// <see cref="HttpMethod.Patch"/> require <see cref="RetryNonIdempotentRequests"/>
/// because repeating them can duplicate writes.
/// </para>
/// </remarks>
public sealed record RetryOptions
{
    private int _maxRetries = 2;
    private TimeSpan _baseDelay = TimeSpan.FromSeconds(1);
    private TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the number of additional attempts after the first. The default is 2, which
    /// allows at most three attempts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative.
    /// </exception>
    public int MaxRetries
    {
        get => _maxRetries;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(MaxRetries));
            _maxRetries = value;
        }
    }

    /// <summary>
    /// Gets the delay used by the first retry. Later retries grow exponentially from
    /// this value. The default is one second.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative.
    /// </exception>
    public TimeSpan BaseDelay
    {
        get => _baseDelay;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                value,
                TimeSpan.Zero,
                nameof(BaseDelay));
            _baseDelay = value;
        }
    }

    /// <summary>
    /// Gets the maximum delay between attempts, including delays requested by
    /// <c>Retry-After</c>. The default is 30 seconds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is negative.
    /// </exception>
    public TimeSpan MaxDelay
    {
        get => _maxDelay;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                value,
                TimeSpan.Zero,
                nameof(MaxDelay));
            _maxDelay = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="HttpMethod.Post"/> and
    /// <see cref="HttpMethod.Patch"/> requests are retried. The default is
    /// <see langword="false"/>.
    /// </summary>
    public bool RetryNonIdempotentRequests { get; init; }
}
