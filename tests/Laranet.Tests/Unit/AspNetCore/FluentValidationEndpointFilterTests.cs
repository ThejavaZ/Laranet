using Laranet.AspNetCore.Validation;

namespace Laranet.Tests.Unit.AspNetCore;

public class FluentValidationEndpointFilterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenNoValidatorsAreRegistered()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new FluentValidationEndpointFilter<TestRequest>([]));

        Assert.Contains("No IValidator", exception.Message);
    }

    public sealed record TestRequest(string Name);
}
