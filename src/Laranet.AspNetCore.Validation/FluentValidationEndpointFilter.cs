using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Laranet.AspNetCore.Validation;

/// <summary>
/// Validates the first endpoint argument of <typeparamref name="TRequest"/> with the
/// FluentValidation validators registered in dependency injection.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public sealed class FluentValidationEndpointFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest>[] validators;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="FluentValidationEndpointFilter{TRequest}"/> class.
    /// </summary>
    /// <param name="validators">The validators registered for <typeparamref name="TRequest"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// No validator is registered for <typeparamref name="TRequest"/>.
    /// </exception>
    public FluentValidationEndpointFilter(IEnumerable<IValidator<TRequest>> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);

        this.validators = [.. validators];

        if (this.validators.Length == 0)
        {
            throw new InvalidOperationException(
                $"No IValidator<{typeof(TRequest)}> is registered. " +
                "Register a validator before adding the validation filter.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var argument = context.Arguments.FirstOrDefault(value => value is TRequest);

        if (argument is null)
        {
            return await next(context);
        }

        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(
                (TRequest)argument,
                context.HttpContext.RequestAborted)));

        var validationResult = new ValidationResult(results);

        return validationResult.IsValid
            ? await next(context)
            : Results.ValidationProblem(validationResult.ToDictionary());
    }
}
