using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Application.Services;
using ModernizationPlatform.Worker.Consumers;
using ModernizationPlatform.Worker.Infra.CopilotSdk;
using ModernizationPlatform.Worker.Infra.Messaging;
using ModernizationPlatform.Worker.Logging;
using ModernizationPlatform.Worker.Observability;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

var serviceName = builder.Configuration["ServiceName"] ?? "modernization-worker";
var serviceVersion = builder.Configuration["ServiceVersion"]
	?? typeof(Program).Assembly.GetName().Version?.ToString()
	?? "unknown";
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? builder.Configuration["SENTRY_DSN"];
var sentryRelease = builder.Configuration["Sentry:Release"] ?? builder.Configuration["SENTRY_RELEASE"];
var sentryTracesSampleRate = ResolveSampleRate(builder.Configuration["Sentry:TracesSampleRate"], 1.0);
var sentryEnabled = !string.IsNullOrWhiteSpace(sentryDsn);

builder.Logging.ClearProviders();
var logger = new LoggerConfiguration()
	.Enrich.FromLogContext()
	.WriteTo.Console(new StructuredJsonFormatter(serviceName, serviceVersion))
	.CreateLogger();
builder.Logging.AddSerilog(logger, dispose: true);

if (sentryEnabled)
{
	builder.Logging.AddSentry(options =>
	{
		ConfigureSentry(options, sentryDsn, sentryRelease, sentryTracesSampleRate, builder.Environment);
		options.MinimumEventLevel = LogLevel.Error;
		options.MinimumBreadcrumbLevel = LogLevel.Information;
	});
}

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddCopilotSdk(builder.Configuration);
builder.Services.AddOpenTelemetry()
	.WithTracing(tracing =>
	{
		tracing
			.AddSource(WorkerTelemetry.ActivitySourceName)
			.AddHttpClientInstrumentation();

		if (sentryEnabled)
		{
			tracing.AddSentry();
		}
	});

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
