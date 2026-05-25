using IstiklalLorawanAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Load default appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Load Render secrets if available to override default settings
var secretPath = "/etc/secrets/appsettings.json";
if (File.Exists(secretPath))
{
    builder.Configuration.AddJsonFile(secretPath, optional: true, reloadOnChange: true);
}

// Add services to the container.
Console.WriteLine($"[DEBUG] Current Directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"[DEBUG] Content Root Path: {builder.Environment.ContentRootPath}");
Console.WriteLine($"[DEBUG] appsettings.json exists in root: {File.Exists(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"))}");
Console.WriteLine($"[DEBUG] /etc/secrets/appsettings.json exists: {File.Exists("/etc/secrets/appsettings.json")}");

var configRoot = (IConfigurationRoot)builder.Configuration;
foreach (var provider in configRoot.Providers)
{
    if (provider.TryGet("Firebase:AuthSecret", out var val))
    {
        Console.WriteLine($"[DEBUG] -> Provider: {provider.GetType().Name}, Key 'Firebase:AuthSecret' value length: {val?.Length ?? -1}");
    }
    if (provider.TryGet("Firebase:DatabaseSecret", out var dbSec))
    {
        Console.WriteLine($"[DEBUG] -> Provider: {provider.GetType().Name}, Key 'Firebase:DatabaseSecret' value length: {dbSec?.Length ?? -1}");
    }
    if (provider.TryGet("Firebase:DatabaseUrl", out var urlVal))
    {
        Console.WriteLine($"[DEBUG] -> Provider: {provider.GetType().Name}, Key 'Firebase:DatabaseUrl' value: '{urlVal}'");
    }
}
Console.WriteLine($"[DEBUG] Final Firebase:DatabaseUrl: '{builder.Configuration["Firebase:DatabaseUrl"]}'");
Console.WriteLine($"[DEBUG] Final Firebase:AuthSecret is null or empty: {string.IsNullOrEmpty(builder.Configuration["Firebase:AuthSecret"])}");
Console.WriteLine($"[DEBUG] Final Firebase:DatabaseSecret is null or empty: {string.IsNullOrEmpty(builder.Configuration["Firebase:DatabaseSecret"])}");



builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    });

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
builder.Services.AddHostedService<WeatherUpdateService>();

// CORS configuration (Adjust as needed for your UI)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseHttpsRedirection();

// CORS should be applied before routing and static files
app.UseCors("AllowAll");

// Support static file serving (for index.html, summary.html etc. inside wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong"));
app.MapControllers();

app.Run();
