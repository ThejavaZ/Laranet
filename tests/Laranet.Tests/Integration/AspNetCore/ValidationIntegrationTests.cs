using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using Laranet.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Laranet.Tests.Integration.AspNetCore;

public class ValidationIntegrationTests
{
    private static async Task<IHost> CreateHostAsync(
        Action<IServiceCollection>? configureServices = null,
        Action<IEndpointRouteBuilder>? mapEndpoints = null)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();

                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    configureServices?.Invoke(services);
                });

                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => mapEndpoints?.Invoke(endpoints));
                });
            });

        return await builder.StartAsync();
    }

    [Fact]
    public async Task WithFluentValidation_ShouldAllowRequest_WhenRequestIsValid()
    {
        using var host = await CreateHostAsync(
            services => services.AddScoped<
                IValidator<CreateProductRequest>,
                CreateProductRequestValidator>(),
            endpoints => endpoints
                .MapPost("/products", (CreateProductRequest request) =>
                    Results.Ok(request.Name))
                .WithFluentValidation<CreateProductRequest>());

        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/products",
            new CreateProductRequest("Keyboard", 99.99m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Keyboard", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithFluentValidation_ShouldReturnValidationProblemDetails_WhenRequestIsInvalid()
    {
        using var host = await CreateHostAsync(
            services => services.AddScoped<
                IValidator<CreateProductRequest>,
                CreateProductRequestValidator>(),
            endpoints => endpoints
                .MapPost("/products", (CreateProductRequest request) =>
                    Results.Ok())
                .WithFluentValidation<CreateProductRequest>());

        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/products",
            new CreateProductRequest(string.Empty, -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));

        var errors = root.GetProperty("errors");

        Assert.Equal(
            "Name is required.",
            errors.GetProperty("Name")[0].GetString());

        Assert.Equal(
            "Price cannot be negative.",
            errors.GetProperty("Price")[0].GetString());
    }

    [Fact]
    public async Task WithFluentValidation_ShouldRunAllRegisteredValidators_WhenMultipleValidatorsAreRegistered()
    {
        using var host = await CreateHostAsync(
            services =>
            {
                services.AddScoped<
                    IValidator<CreateProductRequest>,
                    CreateProductRequestValidator>();
                services.AddScoped<
                    IValidator<CreateProductRequest>,
                    CreateProductPriceLimitValidator>();
            },
            endpoints => endpoints
                .MapPost("/products", (CreateProductRequest request) =>
                    Results.Ok())
                .WithFluentValidation<CreateProductRequest>());

        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/products",
            new CreateProductRequest(string.Empty, 2000m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var errors = document.RootElement.GetProperty("errors");

        Assert.Equal(
            "Name is required.",
            errors.GetProperty("Name")[0].GetString());

        Assert.Equal(
            "Price must be 1000 or less.",
            errors.GetProperty("Price")[0].GetString());
    }

    [Fact]
    public async Task WithFluentValidation_ShouldGroupErrorsByProperty_WhenMultipleRulesFail()
    {
        using var host = await CreateHostAsync(
            services =>
            {
                services.AddScoped<
                    IValidator<CreateProductRequest>,
                    CreateProductRequestValidator>();
                services.AddScoped<
                    IValidator<CreateProductRequest>,
                    CreateProductNameValidator>();
            },
            endpoints => endpoints
                .MapPost("/products", (CreateProductRequest request) =>
                    Results.Ok())
                .WithFluentValidation<CreateProductRequest>());

        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/products",
            new CreateProductRequest(string.Empty, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var messages = document.RootElement
            .GetProperty("errors")
            .GetProperty("Name")
            .EnumerateArray()
            .Select(error => error.GetString())
            .ToArray();

        Assert.Equal(2, messages.Length);
        Assert.Contains("Name is required.", messages);
        Assert.Contains("Name must be at least 3 characters.", messages);
    }

    [Fact]
    public async Task WithoutFilter_ShouldNotValidate_WhenRequestIsInvalid()
    {
        using var host = await CreateHostAsync(
            services => services.AddScoped<
                IValidator<CreateProductRequest>,
                CreateProductRequestValidator>(),
            endpoints => endpoints.MapPost(
                "/products",
                (CreateProductRequest request) => Results.Ok()));

        using var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/products",
            new CreateProductRequest(string.Empty, -1m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed record CreateProductRequest(string Name, decimal Price);

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative.");
    }
}

public sealed class CreateProductNameValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductNameValidator()
    {
        RuleFor(request => request.Name)
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters.");
    }
}

public sealed class CreateProductPriceLimitValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductPriceLimitValidator()
    {
        RuleFor(request => request.Price)
            .LessThanOrEqualTo(1000)
            .WithMessage("Price must be 1000 or less.");
    }
}
