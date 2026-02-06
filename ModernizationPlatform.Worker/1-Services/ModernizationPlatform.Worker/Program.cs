using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModernizationPlatform.Worker.Consumers;
using ModernizationPlatform.Worker.Infra.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddSingleton<AnalysisJobHandler>();
builder.Services.AddSingleton<ModernizationPlatform.Worker.Application.Interfaces.IAnalysisJobHandler>(sp => sp.GetRequiredService<AnalysisJobHandler>());

var host = builder.Build();
host.Run();
