using OrchardCore.Environment.Shell;

var builder = WebApplication.CreateBuilder(args);

// Seed the default tenant's Orchard media library once. Existing editor uploads are left untouched.
var seedMedia = Path.Combine(builder.Environment.ContentRootPath, "SeedMedia", "credit-plus");
var tenantMedia = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Sites", "Default", "Media", "credit-plus");
if (Directory.Exists(seedMedia))
{
    Directory.CreateDirectory(tenantMedia);
    foreach (var file in Directory.EnumerateFiles(seedMedia))
    {
        var destination = Path.Combine(tenantMedia, Path.GetFileName(file));
        if (!File.Exists(destination))
        {
            File.Copy(file, destination);
        }
    }
}

builder.Services.AddOrchardCms();
var app = builder.Build();
app.UseOrchardCore();
app.Run();
