using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.API.BackgroundServices;
using ModernizationPlatform.API.Logging;
using ModernizationPlatform.API.Middleware;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Handlers;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Application.Validators;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Infra.Messaging;
using ModernizationPlatform.Infra.Persistence;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Configuration["ServiceName"] ?? "modernization-api";
var serviceVersion = builder.Configuration["ServiceVersion"]
    ?? typeof(Program).Assembly.GetName().Version?.ToString()
    ?? "unknown";
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? builder.Configuration["SENTRY_DSN"];
var sentryRelease = builder.Configuration["Sentry:Release"] ?? builder.Configuration["SENTRY_RELEASE"];
var sentryTracesSampleRate = ResolveSampleRate(builder.Configuration["Sentry:TracesSampleRate"], 1.0);
var sentryEnabled = !string.IsNullOrWhiteSpace(sentryDsn);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console(new StructuredJsonFormatter(serviceName, serviceVersion));
});

if (sentryEnabled)
{
    builder.WebHost.UseSentry(options =>
        ConfigureSentry(options, sentryDsn, sentryRelease, sentryTracesSampleRate, builder.Environment));

    builder.Logging.AddSentry(options =>
    {
        ConfigureSentry(options, sentryDsn, sentryRelease, sentryTracesSampleRate, builder.Environment);
        options.MinimumEventLevel = LogLevel.Error;
        options.MinimumBreadcrumbLevel = LogLevel.Information;
    });
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (sentryEnabled)
        {
            tracing.AddSentry();
        }
    });

builder.Services.AddOptions<OrchestrationOptions>()
    .Bind(builder.Configuration.GetSection(OrchestrationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// JWT Configuration
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                
                if (authService.IsTokenRevoked(token))
                {
                    context.Fail("Token has been revoked.");
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptCatalogService, PromptCatalogService>();
builder.Services.AddScoped<ICommandHandler<CreateAnalysisCommand, AnalysisRequest>, CreateAnalysisCommandHandler>();
builder.Services.AddSingleton<OrchestrationStateStore>();
builder.Services.AddSingleton<IOrchestrationService, OrchestrationService>();
builder.Services.AddScoped<IConsolidationService, ConsolidationService>();
builder.Services.AddScoped<IConsolidatedResultService, ConsolidatedResultService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddHostedService<OrchestrationBackgroundService>();

// Discovery Services
builder.Services.AddScoped<IDiscoveryService, ModernizationPlatform.Infra.Discovery.DiscoveryService>();
builder.Services.AddScoped<ModernizationPlatform.Domain.Services.IGitCloneService, ModernizationPlatform.Infra.Discovery.GitCloneService>();
builder.Services.AddScoped<ModernizationPlatform.Domain.Services.ILanguageDetectorService, ModernizationPlatform.Infra.Discovery.LanguageDetectorService>();
builder.Services.AddScoped<ModernizationPlatform.Domain.Services.IDotNetProjectAnalyzer, ModernizationPlatform.Infra.Discovery.DotNetProjectAnalyzer>();
builder.Services.AddScoped<ModernizationPlatform.Domain.Services.IDependencyAnalyzer, ModernizationPlatform.Infra.Discovery.DependencyAnalyzer>();
builder.Services.AddScoped<ModernizationPlatform.Domain.Services.IDirectoryStructureMapper, ModernizationPlatform.Infra.Discovery.DirectoryStructureMapper>();

// Validators
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<CreatePromptRequest>, CreatePromptRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePromptRequest>, UpdatePromptRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAnalysisCommand>, CreateAnalysisCommandValidator>();

var rabbitOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
var rabbitConnectionString = $"amqp://{Uri.EscapeDataString(rabbitOptions.Username)}:{Uri.EscapeDataString(rabbitOptions.Password)}@{rabbitOptions.Host}:{rabbitOptions.Port}/";
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq");

builder.Services.AddSingleton<IAnalysisResultHandler, OrchestrationResultHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health").WithName("HealthCheck");

app.MapGet("/", () => Results.Ok(new { service = "ModernizationPlatform.API" }))
    .WithName("Root");

app.Run();

static void ConfigureSentry(
    SentryOptions options,
    string? dsn,
    string? release,
    double tracesSampleRate,
    IHostEnvironment environment)
{
    options.Dsn = string.IsNullOrWhiteSpace(dsn) ? string.Empty : dsn;
    options.Environment = environment.EnvironmentName;
    options.Release = release;
    options.TracesSampleRate = tracesSampleRate;
    options.SendDefaultPii = false;
    options.SetBeforeSend(@event =>
    {
        if (@event.Extra?.ContainsKey("accessToken") == true)
        {
            @event.SetExtra("accessToken", "[redacted]");
        }

        if (@event.Tags?.ContainsKey("accessToken") == true)
        {
            @event.SetTag("accessToken", "[redacted]");
        }

        return @event;
    });
}

static double ResolveSampleRate(string? value, double fallback)
{
    return double.TryParse(value, out var parsed) ? parsed : fallback;
}

// Make Program class accessible for integration tests
public partial class Program { }
