using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
// Configure Swagger/OpenAPI using Swashbuckle

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
// Configure API behavior
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = false; });

// CORS for Scalar UI testing (allow any origin in Development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowScalar", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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

// Configure JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? builder.Configuration["JWT_SECRET"] ?? "CHANGE_THIS_SECRET";
var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey);
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
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PortfolioApi",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes)
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
