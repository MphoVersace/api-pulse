
using System.Reflection;
using ApiPulse.Api.Data;
using ApiPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Register the SQLite database.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

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
app.MapControllers();
app.Run();

// Expose Program so WebApplicationFactory can access the application.
public partial class Program { }
