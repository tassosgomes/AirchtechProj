using Testcontainers.RabbitMq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ApiRabbitMqOptions = ModernizationPlatform.Application.Configuration.RabbitMqOptions;
using ApiJobMessage = ModernizationPlatform.Application.DTOs.AnalysisJobMessage;
using ApiRabbitMqConnectionProvider = ModernizationPlatform.Infra.Messaging.Connection.RabbitMqConnectionProvider;
using ModernizationPlatform.Infra.Messaging.Publishers;
using ModernizationPlatform.Infra.Messaging.Setup;
using WorkerRabbitMqOptions = ModernizationPlatform.Worker.Application.Configuration.RabbitMqOptions;
using WorkerJobMessage = ModernizationPlatform.Worker.Application.DTOs.AnalysisJobMessage;
using ModernizationPlatform.Worker.Application.Interfaces;
using WorkerRabbitMqConnectionProvider = ModernizationPlatform.Worker.Infra.Messaging.Connection.RabbitMqConnectionProvider;
using ModernizationPlatform.Worker.Infra.Messaging.Consumers;

namespace ModernizationPlatform.API.IntegrationTests.Messaging;

public class RabbitMqIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;
    private bool _skipTests;

    public RabbitMqIntegrationTests()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            _skipTests = false;
        }
        catch (Exception)
        {
            _skipTests = true;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Should_Publish_And_Consume_Job_Message()
    {
        if (_skipTests)
        {
            return; // Skip test se Docker não está disponível
        }
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(5672);

        var apiOptions = Options.Create(new ApiRabbitMqOptions
        {
            Host = host,
            Port = port,
            Username = "guest",
            Password = "guest",
            PrefetchCount = 1
        });

        var workerOptions = Options.Create(new WorkerRabbitMqOptions
        {
            Host = host,
            Port = port,
            Username = "guest",
            Password = "guest",
            PrefetchCount = 1
        });

        var apiConnectionProvider = new ApiRabbitMqConnectionProvider(apiOptions, NullLogger<ApiRabbitMqConnectionProvider>.Instance);
        var workerConnectionProvider = new WorkerRabbitMqConnectionProvider(
            workerOptions,
            NullLogger<WorkerRabbitMqConnectionProvider>.Instance);

        var initializer = new RabbitMqQueueInitializer(apiConnectionProvider, NullLogger<RabbitMqQueueInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);

        var tcs = new TaskCompletionSource<ApiJobMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new TestJobHandler(tcs);
        var consumer = new RabbitMqJobConsumer(workerConnectionProvider, handler, workerOptions, NullLogger<RabbitMqJobConsumer>.Instance);

        await consumer.StartAsync(CancellationToken.None);

        var publisher = new RabbitMqJobPublisher(apiConnectionProvider, NullLogger<RabbitMqJobPublisher>.Instance);
        var jobMessage = new ApiJobMessage(
            JobId: Guid.NewGuid(),
            RequestId: Guid.NewGuid(),
            RepositoryUrl: "https://github.com/org/repo",
            Provider: "GitHub",
            AccessToken: "token",
            SharedContextJson: "{}",
            PromptContent: "Analyze",
            AnalysisType: "Security",
            TimeoutSeconds: 300);

        await publisher.PublishJobAsync(jobMessage, CancellationToken.None);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        await consumer.StopAsync(CancellationToken.None);

        Assert.True(completed == tcs.Task, "Mensagem não foi consumida dentro do tempo esperado.");
        Assert.Equal(jobMessage.JobId, tcs.Task.Result.JobId);
        Assert.Equal(jobMessage.RequestId, tcs.Task.Result.RequestId);
    }

    private sealed class TestJobHandler : IAnalysisJobHandler
    {
        private readonly TaskCompletionSource<ApiJobMessage> _tcs;

        public TestJobHandler(TaskCompletionSource<ApiJobMessage> tcs)
        {
            _tcs = tcs;
        }

        public Task HandleAsync(WorkerJobMessage message, CancellationToken cancellationToken)
        {
            var converted = new ApiJobMessage(
                message.JobId,
                message.RequestId,
                message.RepositoryUrl,
                message.Provider,
                message.AccessToken,
                message.SharedContextJson,
                message.PromptContent,
                message.AnalysisType,
                message.TimeoutSeconds);

            _tcs.TrySetResult(converted);
            return Task.CompletedTask;
        }
    }
}