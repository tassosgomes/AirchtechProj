using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Domain.Services;

namespace ModernizationPlatform.Infra.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IGitCloneService _gitCloneService;
    private readonly ILanguageDetectorService _languageDetector;
    private readonly IDotNetProjectAnalyzer _dotNetAnalyzer;
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly IDirectoryStructureMapper _structureMapper;
    private readonly IRepository<SharedContext> _sharedContextRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        IGitCloneService gitCloneService,
        ILanguageDetectorService languageDetector,
        IDotNetProjectAnalyzer dotNetAnalyzer,
        IDependencyAnalyzer dependencyAnalyzer,
        IDirectoryStructureMapper structureMapper,
        IRepository<SharedContext> sharedContextRepository,
        IUnitOfWork unitOfWork,
        ILogger<DiscoveryService> logger)
    {
        _gitCloneService = gitCloneService ?? throw new ArgumentNullException(nameof(gitCloneService));
        _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        _dotNetAnalyzer = dotNetAnalyzer ?? throw new ArgumentNullException(nameof(dotNetAnalyzer));
        _dependencyAnalyzer = dependencyAnalyzer ?? throw new ArgumentNullException(nameof(dependencyAnalyzer));
        _structureMapper = structureMapper ?? throw new ArgumentNullException(nameof(structureMapper));
        _sharedContextRepository = sharedContextRepository ?? throw new ArgumentNullException(nameof(sharedContextRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SharedContext> ExecuteDiscoveryAsync(
        AnalysisRequest request,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation("Starting discovery for request {RequestId}, repository {RepositoryUrl}",
            request.Id, request.RepositoryUrl);

        string? repositoryPath = null;

        try
        {
            // Step 1: Clone repository
            _logger.LogInformation("Step 1/5: Cloning repository");
            repositoryPath = await _gitCloneService.CloneRepositoryAsync(
                request.RepositoryUrl,
                request.Provider,
                accessToken,
                cancellationToken);

            // Step 2: Detect languages
            _logger.LogInformation("Step 2/5: Detecting languages");
            var languages = await _languageDetector.DetectLanguagesAsync(repositoryPath);

            // Step 3: Analyze .NET projects (deep analysis)
            _logger.LogInformation("Step 3/5: Analyzing .NET projects");
            var (dotNetFrameworks, dotNetDependencies) = await _dotNetAnalyzer.AnalyzeAsync(repositoryPath);

            // Step 4: Analyze other dependencies (basic analysis)
            _logger.LogInformation("Step 4/5: Analyzing other dependencies");
            var otherDependencies = await _dependencyAnalyzer.AnalyzeDependenciesAsync(repositoryPath);

            // Step 5: Map directory structure
            _logger.LogInformation("Step 5/5: Mapping directory structure");
            var directoryStructure = _structureMapper.MapStructure(repositoryPath);

            // Combine all dependencies
            var allDependencies = new List<DependencyInfo>();
            allDependencies.AddRange(dotNetDependencies);
            allDependencies.AddRange(otherDependencies);

            // Create SharedContext
            var sharedContext = CreateSharedContext(
                request.Id,
                languages,
                dotNetFrameworks,
                allDependencies,
                directoryStructure);

            // Persist SharedContext
            await _sharedContextRepository.AddAsync(sharedContext, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Discovery completed successfully for request {RequestId}. Found {LanguageCount} languages, {FrameworkCount} frameworks, {DependencyCount} dependencies",
                request.Id, languages.Count, dotNetFrameworks.Count, allDependencies.Count);

            return sharedContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery failed for request {RequestId}", request.Id);
            throw;
        }
        finally
        {
            // Always cleanup the cloned repository
            if (!string.IsNullOrWhiteSpace(repositoryPath))
            {
                _gitCloneService.CleanupRepository(repositoryPath);
            }
        }
    }

    private SharedContext CreateSharedContext(
        Guid requestId,
        List<LanguageInfo> languages,
        List<FrameworkInfo> frameworks,
        List<DependencyInfo> dependencies,
        DirectoryNode directoryStructure)
    {
        // Convert to simple string lists for languages
        var languageNames = languages.Select(l => l.Name).Distinct().ToList();

        // Convert frameworks to string format: "Name Version (Type)"
        var frameworkStrings = frameworks
            .Select(f => $"{f.Name} {f.Version ?? "unknown"} ({f.Type})")
            .Distinct()
            .ToList();

        // Convert dependencies to string format: "Name@Version (Type)"
        var dependencyStrings = dependencies
            .Select(d => $"{d.Name}@{d.Version ?? "latest"} ({d.Type})")
            .Distinct()
            .ToList();

        // Serialize directory structure to JSON
        var directoryStructureJson = JsonSerializer.Serialize(directoryStructure, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new SharedContext(
            requestId: requestId,
            version: 1,
            languages: languageNames,
            frameworks: frameworkStrings,
            dependencies: dependencyStrings,
            directoryStructureJson: directoryStructureJson);
    }
}
