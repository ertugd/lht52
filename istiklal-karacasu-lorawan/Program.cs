using Auth0.AspNetCore.Authentication;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using istiklal_karacasu_lorawan.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Render.com secret file path'i environment variable'dan al
var secretFilePath = Environment.GetEnvironmentVariable("APPSETTINGS_JSON");

// Dosya varsa configuration'a ekle
if (!string.IsNullOrEmpty(secretFilePath) && File.Exists(secretFilePath))
{
    builder.Configuration.AddJsonFile(secretFilePath, optional: false, reloadOnChange: true);
}
else
{
    Console.WriteLine("Warning: APPSETTINGS_JSON env var or file not found!");
}


builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .SetIsOriginAllowed(_ => true);
    });
});

// --- Auth0 ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Auth0Constants.AuthenticationScheme;
})
.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
});

builder.Services.AddSingleton<FirebaseService>();

builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Auth0ManagementService>();
var app = builder.Build();

// --- Firebase Admin ---
var firebaseKeyPath = builder.Configuration["Firebase:ServiceAccountKeyPath"];
if (!string.IsNullOrEmpty(firebaseKeyPath) && File.Exists(firebaseKeyPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseKeyPath)
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();
app.UseRouting();

// ✅ Auth0 middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
