using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Application.Services;
using ModernizationPlatform.Worker.Consumers;
using ModernizationPlatform.Worker.Infra.Messaging.Connection;
using ModernizationPlatform.Worker.Infra.Messaging.Consumers;
using ModernizationPlatform.Worker.Infra.Messaging.Messaging;
using ModernizationPlatform.Worker.Infra.Messaging.Publishers;
using ModernizationPlatform.Worker.Infra.Messaging.Setup;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.RabbitMq;
using Xunit;

namespace ModernizationPlatform.Worker.IntegrationTests.Messaging;

public class WorkerAnalysisIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;
    private bool _skipTests;

    public WorkerAnalysisIntegrationTests()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _skipTests = false;
        }
        catch
        {
            _skipTests = true;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Worker_Should_Consume_Job_And_Publish_Result()
    {
        if (_skipTests)
        {
            return;
        }

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(5672);

        var options = Options.Create(new RabbitMqOptions
        {
            Host = host,
            Port = port,
            Username = "guest",
            Password = "guest",
            PrefetchCount = 1
        });

        var connectionProvider = new RabbitMqConnectionProvider(options, NullLogger<RabbitMqConnectionProvider>.Instance);
        var initializer = new RabbitMqQueueInitializer(connectionProvider, NullLogger<RabbitMqQueueInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);

        var resultTcs = new TaskCompletionSource<AnalysisResultMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resultChannel = connectionProvider.GetConnection().CreateModel();
        var resultConsumer = new AsyncEventingBasicConsumer(resultChannel);
        resultConsumer.Received += (_, args) =>
        {
            var message = RabbitMqJsonSerializer.Deserialize<AnalysisResultMessage>(args.Body);
            if (message is not null)
            {
                resultTcs.TrySetResult(message);
            }

            resultChannel.BasicAck(args.DeliveryTag, false);
            return Task.CompletedTask;
        };
        resultChannel.BasicConsume(RabbitMqQueueNames.AnalysisResults, autoAck: false, resultConsumer);

        var resultPublisher = new RabbitMqResultPublisher(connectionProvider, NullLogger<RabbitMqResultPublisher>.Instance);
        var handler = CreateHandler(resultPublisher);
        var consumer = new RabbitMqJobConsumer(connectionProvider, handler, options, NullLogger<RabbitMqJobConsumer>.Instance);
        await consumer.StartAsync(CancellationToken.None);

        PublishJob(connectionProvider, CreateMessage());

        var completed = await Task.WhenAny(resultTcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        await consumer.StopAsync(CancellationToken.None);

        Assert.True(completed == resultTcs.Task, "Resultado nao recebido dentro do tempo esperado.");
        var result = await resultTcs.Task;
        Assert.Equal("COMPLETED", result.Status);
    }

    private static AnalysisJobHandler CreateHandler(IResultPublisher resultPublisher)
    {
        var gitCloneService = new FakeGitCloneService();
        var snapshotBuilder = new RepositorySnapshotBuilder();
        var copilotClient = new FakeCopilotClient();
        var outputParser = new AnalysisOutputParser();
        var executor = new AnalysisExecutor(
            gitCloneService,
            snapshotBuilder,
            copilotClient,
            outputParser,
            NullLogger<AnalysisExecutor>.Instance);

        var options = Options.Create(new AnalysisTimeoutOptions { DefaultSeconds = 60 });

        return new AnalysisJobHandler(
            executor,
            resultPublisher,
            options,
            NullLogger<AnalysisJobHandler>.Instance);
    }

    private static AnalysisJobMessage CreateMessage()
    {
        return new AnalysisJobMessage(
            JobId: Guid.NewGuid(),
            RequestId: Guid.NewGuid(),
            RepositoryUrl: "https://github.com/org/repo",
            Provider: "GitHub",
            AccessToken: "token",
            SharedContextJson: "{}",
            PromptContent: "Analyze",
            AnalysisType: "Security",
            TimeoutSeconds: 120);
    }

    private static void PublishJob(IRabbitMqConnectionProvider connectionProvider, AnalysisJobMessage message)
    {
        using var channel = connectionProvider.GetConnection().CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        var body = RabbitMqJsonSerializer.Serialize(message);
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqQueueNames.AnalysisJobs,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private sealed class FakeGitCloneService : IGitCloneService
    {
        private string? _path;

        public Task<string> CloneRepositoryAsync(string repositoryUrl, string provider, string? accessToken, CancellationToken cancellationToken)
        {
            _path = Path.Combine(Path.GetTempPath(), $"worker_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_path);
            File.WriteAllText(Path.Combine(_path, "sample.cs"), "public class Sample { }");
            return Task.FromResult(_path);
        }

        public void CleanupRepository(string repositoryPath)
        {
            if (!string.IsNullOrWhiteSpace(repositoryPath) && Directory.Exists(repositoryPath))
            {
                Directory.Delete(repositoryPath, true);
            }
        }
    }

    private sealed class FakeCopilotClient : ICopilotClient
    {
        public Task<CopilotResponse> AnalyzeAsync(CopilotRequest request, CancellationToken cancellationToken)
        {
            var payload = new
            {
                findings = new[]
                {
                    new
                    {
                        severity = "High",
                        category = "Security",
                        title = "Issue",
                        description = "Desc",
                        filePath = "sample.cs"
                    }
                },
                metadata = new
                {
                    analysisType = request.AnalysisType,
                    totalFindings = 1,
                    executionDurationMs = 100
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return Task.FromResult(new CopilotResponse(json));
        }
    }
}
