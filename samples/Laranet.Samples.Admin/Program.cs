using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Laranet.Admin.EntityFrameworkCore;
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
builder.Services.AddLaranetAdmin(options => options
    .AddResource<ProductAdminResource>()
    .UseEfCore<AdminDbContext>());
builder.Services.AddLaranetAdminAuthentication(options =>
    options.AddUser(
        builder.Configuration["Admin:User"] ?? "admin",
        builder.Configuration["Admin:Password"] ?? "DevelopmentAdminPassword!"));

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
app.MapStaticAssets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/admin"));

app.Run();
