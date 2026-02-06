using ModernizationPlatform.API.Handlers;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Infra.Messaging;
using ModernizationPlatform.Infra.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);

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

app.MapHealthChecks("/health").WithName("HealthCheck");

app.MapGet("/", () => Results.Ok(new { service = "ModernizationPlatform.API" }))
    .WithName("Root");

app.Run();
