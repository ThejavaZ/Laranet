using Laranet.Cli;
using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class NameNormalizerTests
{
    [Theory]
    [InlineData("product", "Product")]
    [InlineData("Product", "Product")]
    [InlineData("productCategory", "ProductCategory")]
    [InlineData("product-category", "ProductCategory")]
    [InlineData("product_service", "ProductService")]
    [InlineData("product category", "ProductCategory")]
    [InlineData("Product.cs", "Product")]
    public void Normalize_ShouldProducePascalCaseIdentifier(
        string input,
        string expected)
    {
        Assert.Equal(expected, NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123Product")]
    [InlineData("---")]
    public void Normalize_ShouldThrow_WhenNameIsInvalid(string input)
    {
        Assert.Throws<CliException>(() => NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("Product", "Service", "ProductService")]
    [InlineData("ProductService", "Service", "ProductService")]
    [InlineData("Product", "Controller", "ProductController")]
    [InlineData("ProductController", "Controller", "ProductController")]
    public void EnsureSuffix_ShouldAddSuffixOnlyWhenMissing(
        string name,
        string suffix,
        string expected)
    {
        Assert.Equal(expected, NameNormalizer.EnsureSuffix(name, suffix));
    }
}
