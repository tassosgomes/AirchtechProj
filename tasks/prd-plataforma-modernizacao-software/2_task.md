---
status: completed
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>engine/domain</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database</dependencies>
<unblocks>"4.0", "5.0", "6.0"</unblocks>
</task_context>

# Tarefa 2.0: Modelos de Domínio e Persistência (API)

## Visão Geral

Implementar todas as entidades de domínio, enums, interfaces de repositório, configuração do EF Core com PostgreSQL e migrações iniciais. Esta tarefa estabelece a camada de dados que será usada por todas as funcionalidades da API.

<requirements>
- Implementar entidades: `AnalysisRequest`, `SharedContext`, `AnalysisJob`, `Finding`, `Prompt`, `User`, `Repository`
- Implementar enums: `AnalysisType`, `Severity`, `RequestStatus`, `JobStatus`, `SourceProvider`
- Configurar EF Core DbContext com PostgreSQL (Npgsql)
- Criar mapeamentos (Fluent API) para todas as entidades
- Criar migrações iniciais
- Implementar interfaces de repositório genérico e específicos
- Garantir que os índices definidos na TechSpec sejam criados
- Implementar testes unitários para validações das entidades
</requirements>

## Subtarefas

- [x] 2.1 Criar enums no projeto Domain: `AnalysisType` (Obsolescence, Security, Observability, Documentation), `Severity` (Critical, High, Medium, Low, Informative), `RequestStatus` (Queued, DiscoveryRunning, AnalysisRunning, Consolidating, Completed, Failed), `JobStatus` (Pending, Running, Completed, Failed), `SourceProvider` (GitHub, AzureDevOps)
- [x] 2.2 Criar entidade `User` com propriedades: Id (UUID), Email, PasswordHash, CreatedAt
- [x] 2.3 Criar entidade `AnalysisRequest` com propriedades: Id, RepositoryUrl, Provider, Status, SelectedTypes (List), RetryCount, CreatedAt, CompletedAt — incluir métodos de transição de estado
- [x] 2.4 Criar entidade `SharedContext` com propriedades: Id, RequestId, Version, Languages, Frameworks, Dependencies, DirectoryStructureJson, CreatedAt
- [x] 2.5 Criar entidade `AnalysisJob` com propriedades: Id, RequestId, Type, Status, OutputJson, Duration
- [x] 2.6 Criar entidade `Finding` com propriedades: Id, JobId, Severity, Category, Title, Description, FilePath
- [x] 2.7 Criar entidade `Prompt` com propriedades: Id, AnalysisType, Content, CreatedAt, UpdatedAt
- [x] 2.8 Criar entidade `Repository` (inventário) com propriedades: Id, Url, Name, Provider, LastAnalysisAt
- [x] 2.9 Criar interfaces de repositório: `IRepository<T>` (genérico), `IAnalysisRequestRepository`, `IPromptRepository`, `IFindingRepository`, `IInventoryRepository`
- [x] 2.10 Configurar `AppDbContext` com mapeamentos Fluent API para todas as entidades (nomes de tabela, índices, constraints)
- [x] 2.11 Instalar pacotes: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`
- [x] 2.12 Criar migration inicial com todas as tabelas
- [x] 2.13 Implementar repositórios concretos na camada Infra
- [x] 2.14 Escrever testes unitários: validações de entidades (transições de estado inválidas, campos obrigatórios)

## Sequenciamento

- **Bloqueado por**: 1.0 (Setup dos projetos)
- **Desbloqueia**: 4.0 (Autenticação), 5.0 (Prompts), 6.0 (Solicitação de Análise)
- **Paralelizável**: Sim — pode executar em paralelo com 3.0 (RabbitMQ)

## Detalhes de Implementação

### Esquema de Banco de Dados (conforme TechSpec)

| Tabela | Colunas Principais | Índices |
|---|---|---|
| `users` | id (UUID PK), email, password_hash, created_at | UNIQUE(email) |
| `analysis_requests` | id (UUID PK), repository_url, provider, status, selected_types (jsonb), retry_count, created_at, completed_at | IX_status, IX_created_at |
| `shared_contexts` | id (UUID PK), request_id (FK), version, languages (jsonb), frameworks (jsonb), dependencies (jsonb), directory_structure (jsonb), created_at | IX_request_id |
| `analysis_jobs` | id (UUID PK), request_id (FK), analysis_type, status, output (jsonb), duration_ms, created_at | IX_request_id, IX_status |
| `findings` | id (UUID PK), job_id (FK), severity, category, title, description, file_path | IX_job_id, IX_severity |
| `prompts` | id (UUID PK), analysis_type (UNIQUE), content, created_at, updated_at | UNIQUE(analysis_type) |
| `repositories` | id (UUID PK), url (UNIQUE), name, provider, last_analysis_at | UNIQUE(url) |

### Entidades de Domínio (referência)

```csharp
public class AnalysisRequest
{
    public Guid Id { get; private set; }
    public string RepositoryUrl { get; private set; }
    public SourceProvider Provider { get; private set; }
    public RequestStatus Status { get; private set; }
    public List<AnalysisType> SelectedTypes { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Métodos de transição de estado:
    // StartDiscovery(), StartAnalysis(), StartConsolidation(), Complete(), Fail(), IncrementRetry()
}
```

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Entidades no 3-Domain, Repositórios no 4-Infra
- `rules/dotnet-coding-standards.md`: PascalCase, propriedades com `private set`
- `rules/dotnet-libraries-config.md`: EF Core com Npgsql, FluentValidation

## Critérios de Sucesso

- [x] Todas as 7 entidades implementadas com propriedades e métodos de domínio
- [x] Todos os 5 enums implementados
- [x] EF Core DbContext configurado com Fluent API para todas as entidades
- [x] Migration inicial criada e aplicável (`dotnet ef database update`)
- [x] Índices definidos na TechSpec presentes na migration
- [x] Repositórios concretos implementados
- [x] Testes unitários passando para validações de entidades (mínimo 5 testes)
- [x] `dotnet build` da solution API sem erros

## Checklist de Conclusão

- [x] 2.0 Modelos de Domínio e Persistência (API) ✅ CONCLUÍDA
    - [x] 2.1 Implementação completada
    - [x] 2.2 Definição da tarefa, PRD e tech spec validados
    - [x] 2.3 Análise de regras e conformidade verificadas
    - [x] 2.4 Revisão de código completada
    - [x] 2.5 Pronto para deploy
