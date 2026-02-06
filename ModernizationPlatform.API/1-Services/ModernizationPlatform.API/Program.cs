using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModernizationPlatform.API.Handlers;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Application.Validators;
using ModernizationPlatform.Infra.Messaging;
using ModernizationPlatform.Infra.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);

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

// Validators
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

var rabbitOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
var rabbitConnectionString = $"amqp://{Uri.EscapeDataString(rabbitOptions.Username)}:{Uri.EscapeDataString(rabbitOptions.Password)}@{rabbitOptions.Host}:{rabbitOptions.Port}/";
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq");

builder.Services.AddSingleton<IAnalysisResultHandler, DefaultAnalysisResultHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health").WithName("HealthCheck");

app.MapGet("/", () => Results.Ok(new { service = "ModernizationPlatform.API" }))
    .WithName("Root");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
