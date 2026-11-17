using System.ComponentModel.DataAnnotations;
using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Tests.Unit.Admin;

public class AdminFieldInferenceTests
{
    [Fact]
    public void InferFields_ShouldMapDataAnnotationsToFieldMetadata()
    {
        var resource = new AnnotatedResource().Definition;

        var identifier = Assert.Single(resource.Fields, field => field.Name == "Identifier");
        Assert.True(identifier.IsKey);

        // [Key] wins over the "Id" name convention.
        var id = Assert.Single(resource.Fields, field => field.Name == "Id");
        Assert.False(id.IsKey);

        var name = Assert.Single(resource.Fields, field => field.Name == "Name");
        Assert.Equal("Product name", name.Label);
        Assert.True(name.IsRequired);
        Assert.Equal(50, name.MaxLength);
        Assert.Equal("Shown in lists", name.HelpText);

        var price = Assert.Single(resource.Fields, field => field.Name == "Price");
        Assert.Equal("0", price.Min);
        Assert.Equal("999.99", price.Max);
        Assert.Equal(AdminFieldType.Number, price.FieldType);

        var createdAt = Assert.Single(resource.Fields, field => field.Name == "CreatedAt");
        Assert.True(createdAt.IsReadOnly);

        var computed = Assert.Single(resource.Fields, field => field.Name == "Computed");
        Assert.True(computed.IsReadOnly);
    }

    [Fact]
    public void InferFields_ShouldExcludeScaffoldedOutAndNonInstanceProperties()
    {
        var resource = new AnnotatedResource().Definition;

        Assert.DoesNotContain(resource.Fields, field => field.Name == "InternalCode");
        Assert.DoesNotContain(resource.Fields, field => field.Name == "StaticValue");
        Assert.DoesNotContain(resource.Fields, field => field.Name == "Item");
    }

    [Fact]
    public void InferFields_PlainModel_ShouldProduceCompleteSurface()
    {
        var resource = new PlainResource().Definition;

        var id = Assert.Single(resource.Fields, field => field.Name == "Id");
        Assert.True(id.IsKey);
        Assert.Equal(3, resource.Fields.Count);
        Assert.Equal(AdminFieldType.Number, id.FieldType);
    }

    [Fact]
    public void Field_AfterInference_ShouldPreserveLabelAndTargetTheRightProperty()
    {
        var resource = new SearchableNameResource().Definition;

        // Merge semantics: the inferred [Display] label survives the explicit Field() call...
        var name = Assert.Single(resource.Fields, field => field.Name == "Name");
        Assert.Equal("Product name", name.Label);
        Assert.True(name.IsSearchable);

        // ...and the chained configuration lands on Name, not on the neighbouring field.
        var price = Assert.Single(resource.Fields, field => field.Name == "Price");
        Assert.False(price.IsSearchable);
    }

    [Fact]
    public void Field_ExplicitLabel_ShouldOverrideInferredLabel()
    {
        var resource = new RelabeledResource().Definition;

        var name = Assert.Single(resource.Fields, field => field.Name == "Name");
        Assert.Equal("Custom name", name.Label);
    }

    [Fact]
    public void AddResourceFromModel_ShouldRegisterInferredDefinition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options =>
            options.AddResourceFromModel<PlainModel>("Admin.Plains", resource =>
                resource.Field(model => model.Name).Searchable()));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAdminResourceRegistry>();
        var resource = Assert.Single(registry.Resources, item => item.Name == "PlainModels");

        Assert.Equal("Admin.Plains", resource.PermissionPolicy);
        Assert.True(resource.Fields.First(field => field.Name == "Id").IsKey);
        Assert.True(resource.Fields.First(field => field.Name == "Name").IsSearchable);
    }

    private sealed class AnnotatedModel
    {
        [Key]
        public int Identifier { get; set; }

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Product name", Description = "Shown in lists")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999.99)]
        public decimal Price { get; set; }

        [Editable(false)]
        public DateTime CreatedAt { get; set; }

        [ScaffoldColumn(false)]
        public string InternalCode { get; set; } = string.Empty;

        public string Computed { get; } = string.Empty;

        public static string StaticValue { get; set; } = string.Empty;

        public string this[int index] => string.Empty;
    }

    private sealed class AnnotatedResource : AdminResource<AnnotatedModel>
    {
        protected override void Configure(AdminResourceBuilder<AnnotatedModel> resource)
        {
        }
    }

    private sealed class PlainModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }

    private sealed class PlainResource : AdminResource<PlainModel>
    {
        protected override void Configure(AdminResourceBuilder<PlainModel> resource)
        {
        }
    }

    private sealed class SearchableNameResource : AdminResource<AnnotatedModel>
    {
        protected override void Configure(AdminResourceBuilder<AnnotatedModel> resource)
        {
            resource.Field(model => model.Name).Searchable();
        }
    }

    private sealed class RelabeledResource : AdminResource<AnnotatedModel>
    {
        protected override void Configure(AdminResourceBuilder<AnnotatedModel> resource)
        {
            resource.Field(model => model.Name, "Custom name");
        }
    }
}
