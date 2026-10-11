using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.RateLimiting;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
// Configure Swagger/OpenAPI using Swashbuckle

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
// In-memory cache for lightweight rate-limiting in Login
// Rate limiting (named policies)
builder.Services.AddRateLimiter(options =>
{
    // Partition by remote IP address for single-host deployments. Do NOT trust forwarded headers here.
    options.AddPolicy("LoginPolicy", context => RateLimitPartition.GetFixedWindowLimiter<string>(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 10,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        Window = TimeSpan.FromMinutes(5)
    }));

    // Contact submission policy: stricter limits for public submissions
    options.AddPolicy("ContactPolicy", _ => RateLimitPartition.GetFixedWindowLimiter<string>("contact-key", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 5,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        Window = TimeSpan.FromMinutes(10)
    }));

    options.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        return new ValueTask();
    };
});
// Configure API behavior
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = false; });

// CORS for Scalar UI testing (allow any origin in Development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowScalar", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

    // Default CORS policy: read allowed origins from configuration (comma-separated). If none configured,
    // the policy will allow no origins in production to avoid accidental exposure.
    options.AddPolicy("DefaultCors", p =>
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(origins))
        {
            var list = origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            p.WithOrigins(list).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // No origins configured: do not allow any cross-origin requests by default
            p.DisallowCredentials();
        }
    });
});
// Configure EF Core DbContext
builder.Services.AddDbContext<PortfolioApi.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=PortfolioApiDb;Trusted_Connection=True;MultipleActiveResultSets=true")); // no-op patch to keep Program.cs in sync

// Repositories
builder.Services.AddScoped<PortfolioApi.Repositories.IContactRepository, PortfolioApi.Repositories.ContactRepository>();
// Admin repository and services
builder.Services.AddScoped<PortfolioApi.Repositories.IAdminRepository, PortfolioApi.Repositories.AdminRepository>();
builder.Services.AddScoped<PortfolioApi.Services.IJwtService, PortfolioApi.Services.JwtService>();
// Portfolio repository
builder.Services.AddScoped<PortfolioApi.Repositories.IPortfolioRepository, PortfolioApi.Repositories.PortfolioRepository>();

// Password hasher
builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>));

// Configure Azure Key Vault (optional)
// If KeyVault:Name or KeyVault:Uri is present in configuration, add the Key Vault configuration
// provider so secrets become available through builder.Configuration. Local and production
// authentication will be handled by DefaultAzureCredential (CLI, Visual Studio, Managed Identity, etc.).
var keyVaultName = builder.Configuration["KeyVault:Name"];
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultName))
{
    var kvUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
}
else if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

// Configure JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? builder.Configuration["JWT_SECRET"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Validate required JWT settings at startup — fail fast and avoid unsafe defaults
if (string.IsNullOrWhiteSpace(jwtIssuer)) throw new InvalidOperationException("JWT issuer is not configured. Set configuration value 'Jwt:Issuer'.");
if (string.IsNullOrWhiteSpace(jwtAudience)) throw new InvalidOperationException("JWT audience is not configured. Set configuration value 'Jwt:Audience'.");

// If no signing key is provided, allow a development fallback that generates a temporary key
// to make local debugging easier. Do NOT use this in production — require a configured secret.
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        // Generate a 64-byte random key and use its base64 representation as the signing secret.
        var devKeyBytes = RandomNumberGenerator.GetBytes(64);
        jwtKey = Convert.ToBase64String(devKeyBytes);
        // Inform developer via console; this avoids adding logging dependencies at this point.
        Console.WriteLine("WARNING: No JWT signing key configured. A temporary development key was generated. Do NOT use this in production.");
    }
    else
    {
        throw new InvalidOperationException("JWT signing key is not configured. Set configuration value 'Jwt:Key' (do not store secrets in source control).");
    }
}

var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32) throw new InvalidOperationException("JWT signing key is too short. Provide a key with at least 32 bytes (recommended) for HMAC-SHA256.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes)
    };

    // Server-side checks on validated tokens (e.g., account active)
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            try
            {
                var idClaim = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(idClaim))
                {
                    ctx.Fail("Missing name identifier");
                    return;
                }

                if (!Guid.TryParse(idClaim, out var adminId))
                {
                    ctx.Fail("Invalid name identifier");
                    return;
                }

                // Resolve repository from DI and check admin status
                var repo = ctx.HttpContext.RequestServices.GetService<PortfolioApi.Repositories.IAdminRepository>();
                if (repo == null)
                {
                    ctx.Fail("Admin repository unavailable");
                    return;
                }

                var admin = await repo.GetByIdAsync(adminId);
                if (admin == null || !admin.IsActive)
                {
                    ctx.Fail("Administrator not found or inactive");
                    return;
                }
            }
            catch (Exception ex)
            {
                var logger = ctx.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("JwtValidation");
                logger?.LogWarning(ex, "Error during token validation");
                ctx.Fail("Token validation error");
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireClaim(System.Security.Claims.ClaimTypes.Role, "super-admin"));
});

// File storage
builder.Services.AddSingleton<PortfolioApi.Services.IFileStorage, PortfolioApi.Services.LocalFileStorage>();

// FluentValidation registration
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PortfolioApi.Validators.ContactCreateDtoValidator>();
// Register new validators
builder.Services.AddValidatorsFromAssemblyContaining<PortfolioApi.Validators.ProfileUpdateDtoValidator>();

var app = builder.Build();

// Enable rate limiter middleware so attribute-based policies are enforced
app.UseRateLimiter();

// Attach global exception handlers to capture unobserved errors during startup/runtime
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    var loggerFactory = app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
    var logger = loggerFactory?.CreateLogger("GlobalException") ;
    if (e.ExceptionObject is Exception ex)
        logger?.LogCritical(ex, "Unhandled exception (AppDomain.CurrentDomain)");
    else
        logger?.LogCritical("Unhandled exception object: {Obj}", e.ExceptionObject);
};
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    var loggerFactory = app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
    var logger = loggerFactory?.CreateLogger("TaskScheduler");
    logger?.LogCritical(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowScalar");
    // Enable Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI();
    // Automatically open Swagger UI in the default browser when the app starts
    try
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                var server = app.Services.GetService<IServer>();
                var addressesFeature = server?.Features.Get<IServerAddressesFeature>();
                var address = addressesFeature?.Addresses?.FirstOrDefault() ?? app.Urls.FirstOrDefault() ?? "http://localhost:5075";
                var swaggerUrl = address.TrimEnd('/') + "/swagger/index.html";
                Process.Start(new ProcessStartInfo { FileName = swaggerUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("Startup");
                logger?.LogWarning(ex, "Failed to open Swagger UI automatically.");
            }
        });
    }
    catch { }
}
else
{
    // In non-development environments, apply the stricter default CORS policy
    app.UseCors("DefaultCors");

    // Do not enable Swagger or developer exception pages in production
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<PortfolioApi.Middleware.ApiExceptionMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Seed database (create default admin if configured)
// Patch placeholder to ensure patch format consistency - no functional change
try
{
    PortfolioApi.Data.DbInitializer.SeedAsync(app.Services).GetAwaiter().GetResult();
}
catch (Exception ex)
{
    var logger = app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("Startup");
    logger?.LogError(ex, "An error occurred while seeding the database.");
}

try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetService<Microsoft.Extensions.Logging.ILogger<Program>>();
    logger?.LogCritical(ex, "Host terminated unexpectedly");
    throw;
}
