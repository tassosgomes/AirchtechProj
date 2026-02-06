using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Application.Services;
using ModernizationPlatform.Worker.Consumers;
using ModernizationPlatform.Worker.Infra.CopilotSdk;
using ModernizationPlatform.Worker.Infra.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddCopilotSdk(builder.Configuration);

builder.Services.AddOptions<AnalysisTimeoutOptions>()
	.BindConfiguration(AnalysisTimeoutOptions.SectionName)
	.ValidateDataAnnotations();

builder.Services.AddSingleton<IRepositorySnapshotBuilder, RepositorySnapshotBuilder>();
builder.Services.AddSingleton<IAnalysisOutputParser, AnalysisOutputParser>();
builder.Services.AddSingleton<IAnalysisExecutor, AnalysisExecutor>();
builder.Services.AddSingleton<AnalysisJobHandler>();
builder.Services.AddSingleton<ModernizationPlatform.Worker.Application.Interfaces.IAnalysisJobHandler>(sp => sp.GetRequiredService<AnalysisJobHandler>());

var host = builder.Build();
host.Run();
