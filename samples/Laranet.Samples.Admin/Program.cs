using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Laranet.Samples.Admin.Data;
using Laranet.Samples.Admin.Models;
using Laranet.Samples.Admin.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite("Data Source=admin.db"));
builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AdminDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddLaranetAdmin();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin.Products", policy => policy.RequireAuthenticatedUser());
builder.Services.AddScoped<IAdminDataAdapter, ProductAdminAdapter>();
builder.Services.AddScoped<IAdminAuthorization, IdentityAdminAuthorization>();
builder.Services.AddSingleton<IAdminResourceRegistry>(services =>
{
    var registry = new AdminResourceRegistry();
    registry.Add(ProductAdminResource.Definition);

    return registry;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name = "Keyboard", Price = 99.99m },
            new Product { Name = "Monitor", Price = 249.50m },
            new Product { Name = "Mouse", Price = 39.90m });
        await db.SaveChangesAsync();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
