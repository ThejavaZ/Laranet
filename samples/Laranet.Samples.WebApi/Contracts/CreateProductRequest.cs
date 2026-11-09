namespace Laranet.Samples.WebApi.Contracts;

/// <summary>
/// The payload accepted when creating a product.
/// </summary>
/// <param name="Name">The display name.</param>
/// <param name="Price">The unit price.</param>
public sealed record CreateProductRequest(string Name, decimal Price);
