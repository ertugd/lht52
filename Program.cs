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
Console.WriteLine($"[DEBUG] Firebase:DatabaseUrl: {builder.Configuration["Firebase:DatabaseUrl"]}");
Console.WriteLine($"[DEBUG] Firebase:AuthSecret is null or empty: {string.IsNullOrEmpty(builder.Configuration["Firebase:AuthSecret"])}");
if (!string.IsNullOrEmpty(builder.Configuration["Firebase:AuthSecret"]))
{
    Console.WriteLine($"[DEBUG] Firebase:AuthSecret starts with: {builder.Configuration["Firebase:AuthSecret"].Substring(0, Math.Min(5, builder.Configuration["Firebase:AuthSecret"].Length))}...");
}

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
