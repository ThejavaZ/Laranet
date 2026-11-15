using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Laranet.AspNetCore.Validation;

/// <summary>
/// Adds FluentValidation request validation to Minimal API endpoints.
/// </summary>
public static class FluentValidationEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds a filter that validates the first argument of <typeparamref name="TRequest"/>
    /// with the FluentValidation validators registered in dependency injection.
    /// </summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The endpoint builder.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static RouteHandlerBuilder WithFluentValidation<TRequest>(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddEndpointFilter<FluentValidationEndpointFilter<TRequest>>();
    }
}
