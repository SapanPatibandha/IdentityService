using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using IdentityService.Infrastructure.Data;
using IdentityService.Core.Interfaces;
using IdentityService.Infrastructure.Repositories;
using IdentityService.Application.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/identityservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("IdentityService.Infrastructure")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IScopeRepository, ScopeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITwoFactorVerificationRepository, TwoFactorVerificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IScopeService, ScopeService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure JWT authentication with HS256 (HMAC)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = "IdentityServiceSecretKeyForDevelopmentAndTestingPurposes1234567890!@#$%";
var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddSingleton(signingKey);

var app = builder.Build();

// Create database and apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    try
    {
        // First, ensure the database exists
        dbContext.Database.EnsureCreated();
        Log.Information("Database ensured to exist");
        
        // Then try to apply migrations if they exist
        dbContext.Database.Migrate();
        Log.Information("Migrations applied successfully");

        // Create default admin client if it doesn't exist
        var adminClient = await dbContext.Set<IdentityService.Core.Entities.Client>()
            .FirstOrDefaultAsync(c => c.Name == "Admin Dashboard");
        
        if (adminClient == null)
        {
            var client = new IdentityService.Core.Entities.Client
            {
                Id = Guid.NewGuid(),
                ClientId = "admin-dashboard",
                ClientSecret = BCrypt.Net.BCrypt.HashPassword("admin-dashboard-secret"),
                Name = "Admin Dashboard",
                Description = "Built-in admin dashboard client",
                ClientType = "Public",
                IsActive = true,
                AccessTokenLifetime = 3600,
                RefreshTokenLifetime = 86400,
                AllowRefreshTokenRotation = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Set<IdentityService.Core.Entities.Client>().Add(client);
            await dbContext.SaveChangesAsync();
            Log.Information("Default admin client created: admin-dashboard");
        }
    }
    catch (Exception ex)
    {
        Log.Error($"Database initialization failed: {ex.Message}");
        throw;
    }
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

// Serve static files (admin dashboard)
app.UseDefaultFiles();
app.UseStaticFiles();

// Redirect root to admin dashboard
app.MapGet("/", context => 
{
    context.Response.Redirect("/admin/index.html", permanent: false);
    return Task.CompletedTask;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
