using Laranet.Http;

namespace Laranet.Tests.Unit.Http;

public class RetryOptionsTests
{
    [Fact]
    public void RetryOptions_ShouldUseSafeDefaults()
    {
        var options = new RetryOptions();

        Assert.Equal(2, options.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(1), options.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.MaxDelay);
        Assert.False(options.RetryNonIdempotentRequests);
    }

    [Fact]
    public void RetryOptions_ShouldRejectNegativeMaxRetries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryOptions { MaxRetries = -1 });
    }

    [Fact]
    public void RetryOptions_ShouldRejectNegativeBaseDelay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryOptions { BaseDelay = TimeSpan.FromMilliseconds(-1) });
    }

    [Fact]
    public void RetryOptions_ShouldRejectNegativeMaxDelay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RetryOptions { MaxDelay = TimeSpan.FromMilliseconds(-1) });
    }
}
