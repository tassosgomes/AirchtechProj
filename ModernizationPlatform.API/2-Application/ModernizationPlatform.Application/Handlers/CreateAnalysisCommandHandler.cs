using FluentValidation;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Handlers;

public sealed class CreateAnalysisCommandHandler : ICommandHandler<CreateAnalysisCommand, AnalysisRequest>
{
    private readonly IAnalysisRequestRepository _analysisRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAnalysisCommand> _validator;

    public CreateAnalysisCommandHandler(
        IAnalysisRequestRepository analysisRequestRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateAnalysisCommand> validator)
    {
        _analysisRequestRepository = analysisRequestRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<AnalysisRequest> HandleAsync(CreateAnalysisCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var request = new AnalysisRequest(command.RepositoryUrl, command.Provider, command.SelectedTypes);

        await _analysisRequestRepository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request;
    }
}
