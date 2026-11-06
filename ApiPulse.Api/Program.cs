
using System.Reflection;
using System.Text;
using ApiPulse.Api.Data;
using ApiPulse.Api.Models;
using ApiPulse.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Register the SQLite database.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Register ASP.NET Core Identity.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // Configure password security requirements.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        // Configure account lockout protection.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// Read JWT configuration.
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

// Read the configured token expiration time.
var expirationMinutes = builder.Configuration.GetValue<int>(
    "Jwt:ExpirationInMinutes", 60);

if (expirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT expiration must be greater than zero.");
}

// Create the signing key used to validate JWT tokens.
var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSecretKey));

// Register JWT bearer authentication.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Register authorization services.
builder.Services.AddAuthorization();

// Register the JWT token generation service.
builder.Services.AddScoped<JwtTokenService>();

// Register HTTP client support with a 15-second timeout.
builder.Services
    .AddHttpClient("MonitorClient", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    });

// Register the background monitoring service.
builder.Services.AddHostedService<MonitorBackgroundService>();

// Register API services.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with API information and XML documentation.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Pulse",
        Version = "v1",
        Description = "A website monitoring API that tracks availability, uptime, and response-time performance."
    });

    // Locate the XML documentation file generated during compilation.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    // Include XML comments in the generated Swagger documentation.
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Pulse v1");
        options.DocumentTitle = "API Pulse - Swagger Documentation";
    });
}

app.UseHttpsRedirection();

// Authenticate incoming requests before checking their permissions.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

// Expose Program so WebApplicationFactory can access the application.
public partial class Program { }
