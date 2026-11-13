using Laranet.Cli;
using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class CrudFieldParserTests
{
    [Fact]
    public void Parse_ShouldReturnDefaultField_WhenNoFieldsAreProvided()
    {
        var field = Assert.Single(CrudFieldParser.Parse(null));

        Assert.Equal("Name", field.Name);
        Assert.Equal("string", field.Type);
        Assert.False(field.IsNullable);
        Assert.True(field.RequiresDefault);
    }

    [Fact]
    public void Parse_ShouldReturnEveryField_WhenFieldsAreValid()
    {
        var fields = CrudFieldParser.Parse(
            "Name:string,Price:decimal,Description:string?,Stock:int, CreatedAt:DateTime? ");

        Assert.Collection(
            fields,
            field =>
            {
                Assert.Equal("Name", field.Name);
                Assert.Equal("string", field.Type);
                Assert.False(field.IsNullable);
            },
            field =>
            {
                Assert.Equal("Price", field.Name);
                Assert.Equal("decimal", field.Type);
            },
            field =>
            {
                Assert.Equal("Description", field.Name);
                Assert.Equal("string", field.Type);
                Assert.True(field.IsNullable);
                Assert.Equal("string?", field.Declaration);
            },
            field =>
            {
                Assert.Equal("Stock", field.Name);
                Assert.Equal("int", field.Type);
            },
            field =>
            {
                Assert.Equal("CreatedAt", field.Name);
                Assert.Equal("DateTime", field.Type);
                Assert.True(field.IsNullable);
            });
    }

    [Fact]
    public void Parse_ShouldNormalizeFieldNames()
    {
        var field = Assert.Single(CrudFieldParser.Parse("product-code:string"));

        Assert.Equal("ProductCode", field.Name);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Name:")]
    [InlineData(":string")]
    public void Parse_ShouldThrow_WhenFieldDoesNotUseNameAndType(string value)
    {
        Assert.Throws<CliException>(() => CrudFieldParser.Parse(value));
    }

    [Fact]
    public void Parse_ShouldThrow_WhenFieldListContainsAnEmptyEntry()
    {
        Assert.Throws<CliException>(() => CrudFieldParser.Parse("Name:string,"));
    }

    [Fact]
    public void Parse_ShouldThrow_WhenTypeIsNotSupported()
    {
        Assert.Throws<CliException>(() => CrudFieldParser.Parse("Price:money"));
    }

    [Fact]
    public void Parse_ShouldThrow_WhenFieldIsDeclaredTwice()
    {
        Assert.Throws<CliException>(
            () => CrudFieldParser.Parse("Name:string,name:string"));
    }

    [Fact]
    public void Parse_ShouldThrow_WhenFieldIsTheGeneratedId()
    {
        Assert.Throws<CliException>(() => CrudFieldParser.Parse("Id:int"));
    }
}
