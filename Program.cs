using IstiklalLorawanAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Load Render secrets if available, otherwise fall back to appsettings.json
var secretPath = "/etc/secrets/appsettings.json";
if (File.Exists(secretPath))
{
    builder.Configuration.AddJsonFile(secretPath, optional: false, reloadOnChange: true);
}
else
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
}

// Add services to the container.
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
app.MapControllers();

app.Run();
