using backend.Models;
using backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

// Enable CORS to allow requests from the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000") // Replace with your frontend's URL if different
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add DbContext
builder.Services.AddDbContext<NotesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotesDb")));

// Add OpenTelemetry logging and tracing
// Temporarily disable Jaeger and console exporters to avoid potential delays during startup
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        // Enable Console Exporter for debugging OpenTelemetry logs
        .AddConsoleExporter(options =>
        {
            options.Targets = OpenTelemetry.Exporter.ConsoleExporterOutputTargets.Console;
        })
        // .AddJaegerExporter() // Commented out to disable Jaeger exporter
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TinyNotesService")));

// Correct the logging configuration to use OpenTelemetry's logging instrumentation
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.IncludeFormattedMessage = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapNotesEndpoints();

app.Run();
