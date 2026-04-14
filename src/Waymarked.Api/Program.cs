using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using Waymarked.Api;
using Waymarked.Api.Data;
using Waymarked.Api.Email;
using Waymarked.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<WaymarkedDbContext>("waymarked");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<WaymarkedDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(24));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

// Rate limiting disabled in Test environment to avoid cross-test interference
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("forgot-password", limiter =>
        {
            limiter.PermitLimit = 3;
            limiter.Window = TimeSpan.FromMinutes(15);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
}

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
builder.Services.AddTransient<IWaymarkedEmailSender, SmtpEmailSender>();

builder.Services.AddOpenApi();
builder.Services.AddGraphHopperClient();

// In CI, elevation is disabled for faster graph builds (no SRTM downloads)
var elevationEnabled = builder.Configuration.GetValue<bool>("GRAPHHOPPER:ELEVATIONENABLED", true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WaymarkedDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
if (!app.Environment.IsEnvironment("Test"))
    app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Waymarked API is running. POST to /api/routes to plan a route.");
app.MapRouteEndpoints(elevationEnabled);
app.MapDefaultEndpoints();
app.MapAuthEndpoints();

app.Run();
