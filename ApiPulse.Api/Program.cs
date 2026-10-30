using ApiPulse.Api.Data;
using ApiPulse.Api.Services;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();