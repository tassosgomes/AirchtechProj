namespace ModernizationPlatform.Infra.Messaging.Messaging;

public static class RabbitMqQueueNames
{
    public const string AnalysisJobs = "analysis.jobs";
    public const string AnalysisResults = "analysis.results";
    public const string AnalysisJobsDlq = "analysis.jobs.dlq";
}