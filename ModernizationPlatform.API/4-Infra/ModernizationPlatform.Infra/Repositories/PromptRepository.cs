using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class PromptRepository : Repository<Prompt>, IPromptRepository
{
    public PromptRepository(AppDbContext context)
        : base(context)
    {
    }
}