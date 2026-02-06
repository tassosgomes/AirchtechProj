# Especificação Técnica – Plataforma de Modernização de Software

## Resumo Executivo

Esta Tech Spec descreve a arquitetura e o plano de implementação da **Plataforma de Modernização de Software**, uma solução on-premise que automatiza a análise técnica de repositórios de código-fonte. A plataforma é composta por dois projetos independentes: uma **API Orquestradora** (ASP.NET Core, Clean Architecture) e um **Worker Genérico** (ASP.NET Core, stateless, container isolado), além de um **Frontend** React/TypeScript com tema Cyber-Technical, **RabbitMQ** como message broker e **PostgreSQL** para persistência.

A estratégia central é um pipeline orquestrado com **paralelismo por repositório**: múltiplos repositórios podem ser analisados simultaneamente, porém cada repositório processa seus pilares de análise sequencialmente (um pilar por vez). A API recebe solicitações, publica mensagens no RabbitMQ, e Workers consomem jobs de forma independente. A comunicação entre API e Worker é **assíncrona via RabbitMQ** — a API publica jobs em filas e o Worker consome, executa análise via GitHub Copilot SDK e publica resultados de volta em fila de resposta. API e Worker são **projetos separados**, cada um com seu próprio solution, deploy e ciclo de vida. Observabilidade é provida pelo **Sentry SDK** com bridge OpenTelemetry, garantindo tracing distribuído, logs estruturados e report de erros.

---

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌──────────────────┐     REST/JSON      ┌──────────────────────────┐
│  Frontend (React) │ ◄──────────────►  │  API Orquestradora       │
│  modernization-   │                   │  (Projeto Independente)  │
│  web              │                   │                          │
└──────────────────┘                    │  ┌─ Auth Module          │
                                        │  ├─ Orchestration Engine │
                                        │  ├─ Discovery Service    │
                                        │  ├─ Consolidation Engine │
                                        │  ├─ Prompt Catalog       │
                                        │  └─ Inventory Service    │
                                        └────────────┬─────────────┘
                                                     │ Publish jobs
                                                     ▼
                                        ┌──────────────────────────┐
                                        │  RabbitMQ                │
                                        │  (Message Broker)        │
                                        │  ┌─ analysis.jobs.queue │
                                        │  └─ analysis.results.q  │
                                        └────────────┬─────────────┘
                                                     │ Consume jobs
                                                     ▼
                                        ┌──────────────────────────┐
                                        │  Worker Genérico         │
                                        │  (Projeto Independente)  │
                                        │  ┌─ RabbitMQ Consumer   │
                                        │  ├─ Copilot SDK Client  │
                                        │  └─ Structured Output    │
                                        └────────────┬─────────────┘
                                                     │
                                                     ▼
                                        ┌──────────────────────────┐
                                        │  GitHub Copilot SDK      │
                                        │  (API Externa)           │
                                        └──────────────────────────┘

       ┌────────────────┐       ┌──────────────┐       ┌──────────────┐
       │  PostgreSQL    │       │  Sentry      │       │  Git Repos   │
       │  (Persistência)│       │  (Observab.) │       │  (GitHub/    │
       │                │       │              │       │   AzDevOps)  │
       └────────────────┘       └──────────────┘       └──────────────┘
```

- **API Orquestradora** *(Projeto independente)*: Componente central. Recebe solicitações REST, gerencia ciclo de vida dos jobs (QUEUED → COMPLETED/FAILED), executa Discovery, publica jobs de análise no RabbitMQ, consome resultados da fila de respostas, consolida e alimenta o inventário. Contém os módulos de autenticação, catálogo de prompts e inventário. Suporta **múltiplos repositórios em paralelo** — cada repositório é processado de forma independente.
- **Worker Genérico** *(Projeto independente)*: Processo stateless em container Docker, com seu próprio solution e deploy. Consome mensagens da fila `analysis.jobs` do RabbitMQ, recebe repositório + contexto + prompt, executa análise via GitHub Copilot SDK, e publica resultado JSON na fila `analysis.results`. Pode ser escalado horizontalmente (múltiplas instâncias consumindo a mesma fila).
- **RabbitMQ**: Message broker para comunicação assíncrona entre API e Worker. Duas filas principais: `analysis.jobs` (API → Worker) e `analysis.results` (Worker → API). Garante desacoplamento total entre os projetos e permite paralelismo e escalabilidade.
- **Frontend**: SPA React/TypeScript (Vite). Dashboard de acompanhamento, formulário de solicitação, visualização de resultados e inventário.
- **PostgreSQL**: Banco relacional para persistência de solicitações, contextos, resultados, inventário, prompts e usuários. Acessado apenas pela API; o Worker não tem acesso direto ao banco.
- **Sentry**: Plataforma de observabilidade para error tracking, performance monitoring e tracing distribuído.

---

## Design de Implementação

### Interfaces Principais

```csharp
// === API Orquestradora (Projeto API) ===

// Orchestration
public interface IOrchestrationService
{
    Task<AnalysisRequest> CreateRequestAsync(CreateAnalysisCommand command, CancellationToken ct);
    Task ProcessPendingRequestsAsync(CancellationToken ct);
}

// Discovery
public interface IDiscoveryService
{
    Task<SharedContext> ExecuteDiscoveryAsync(AnalysisRequest request, CancellationToken ct);
}

// Job publishing (API → RabbitMQ)
public interface IJobPublisher
{
    Task PublishJobAsync(AnalysisJobMessage message, CancellationToken ct);
}

// Result consuming (RabbitMQ → API)
public interface IResultConsumer
{
    Task StartConsumingAsync(CancellationToken ct);
}

// === Worker Genérico (Projeto Worker) ===

// Job consuming (RabbitMQ → Worker)
public interface IJobConsumer
{
    Task StartConsumingAsync(CancellationToken ct);
}

// Analysis execution (Worker side)
public interface IAnalysisExecutor
{
    Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken ct);
}

// Result publishing (Worker → RabbitMQ)
public interface IResultPublisher
{
    Task PublishResultAsync(AnalysisResultMessage message, CancellationToken ct);
}

// Copilot SDK integration (Worker side)
public interface ICopilotClient
{
    Task<CopilotResponse> AnalyzeAsync(CopilotRequest request, CancellationToken ct);
}

// Consolidation
public interface IConsolidationService
{
    Task<ConsolidatedResult> ConsolidateAsync(Guid requestId, CancellationToken ct);
}

// Prompt Catalog
public interface IPromptCatalogService
{
    Task<Prompt> GetByAnalysisTypeAsync(AnalysisType type, CancellationToken ct);
    Task<Prompt> CreateOrUpdateAsync(UpsertPromptCommand command, CancellationToken ct);
}

// Inventory
public interface IInventoryService
{
    Task<PagedResult<RepositorySummary>> QueryAsync(InventoryFilter filter, CancellationToken ct);
    Task<RepositoryTimeline> GetTimelineAsync(Guid repositoryId, CancellationToken ct);
}
```

### Modelos de Dados

#### Entidades de Domínio

```csharp
public class AnalysisRequest
{
    public Guid Id { get; private set; }
    public string RepositoryUrl { get; private set; }
    public SourceProvider Provider { get; private set; }  // GitHub | AzureDevOps
    public RequestStatus Status { get; private set; }     // Queued | DiscoveryRunning | AnalysisRunning | Consolidating | Completed | Failed
    public List<AnalysisType> SelectedTypes { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}

public class SharedContext
{
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public int Version { get; private set; }
    public List<string> Languages { get; private set; }
    public List<FrameworkInfo> Frameworks { get; private set; }
    public List<DependencyInfo> Dependencies { get; private set; }
    public string DirectoryStructureJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

public class AnalysisJob
{
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public AnalysisType Type { get; private set; }
    public JobStatus Status { get; private set; }  // Pending | Running | Completed | Failed
    public string OutputJson { get; private set; }
    public TimeSpan? Duration { get; private set; }
}

public class Finding
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Severity Severity { get; private set; }  // Critical | High | Medium | Low | Informative
    public string Category { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string FilePath { get; private set; }
}

public class Prompt
{
    public Guid Id { get; private set; }
    public AnalysisType AnalysisType { get; private set; }  // Obsolescence | Security | Observability | Documentation
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}

public enum AnalysisType { Obsolescence, Security, Observability, Documentation }
public enum Severity { Critical, High, Medium, Low, Informative }
```

#### Esquema de Banco de Dados (PostgreSQL)

| Tabela | Colunas Principais | Índices |
|---|---|---|
| `users` | id (UUID PK), email, password_hash, created_at | UNIQUE(email) |
| `analysis_requests` | id (UUID PK), repository_url, provider, status, selected_types (jsonb), retry_count, created_at, completed_at | IX_status, IX_created_at |
| `shared_contexts` | id (UUID PK), request_id (FK), version, languages (jsonb), frameworks (jsonb), dependencies (jsonb), directory_structure (jsonb), created_at | IX_request_id |
| `analysis_jobs` | id (UUID PK), request_id (FK), analysis_type, status, output (jsonb), duration_ms, created_at | IX_request_id, IX_status |
| `findings` | id (UUID PK), job_id (FK), severity, category, title, description, file_path | IX_job_id, IX_severity |
| `prompts` | id (UUID PK), analysis_type (UNIQUE), content, created_at, updated_at | UNIQUE(analysis_type) |
| `repositories` | id (UUID PK), url (UNIQUE), name, provider, last_analysis_at | UNIQUE(url) |

### Endpoints de API

#### Autenticação

| Método | Path | Descrição |
|---|---|---|
| `POST` | `/api/v1/auth/register` | Criar conta (email + senha) |
| `POST` | `/api/v1/auth/login` | Autenticar e receber JWT |
| `POST` | `/api/v1/auth/revoke` | Revogar sessão ativa |

#### Solicitações de Análise

| Método | Path | Descrição |
|---|---|---|
| `POST` | `/api/v1/analysis-requests` | Criar solicitação (URL, provedor, token, tipos) |
| `GET` | `/api/v1/analysis-requests` | Listar solicitações com paginação (`_page`, `_size`) |
| `GET` | `/api/v1/analysis-requests/{id}` | Status detalhado (inclui posição na fila) |
| `GET` | `/api/v1/analysis-requests/{id}/results` | Resultados consolidados |

#### Catálogo de Prompts

| Método | Path | Descrição |
|---|---|---|
| `GET` | `/api/v1/prompts` | Listar prompts |
| `GET` | `/api/v1/prompts/{id}` | Obter prompt por ID |
| `POST` | `/api/v1/prompts` | Criar prompt |
| `PUT` | `/api/v1/prompts/{id}` | Atualizar prompt |

#### Inventário

| Método | Path | Descrição |
|---|---|---|
| `GET` | `/api/v1/inventory/repositories` | Listar repositórios com filtros e paginação |
| `GET` | `/api/v1/inventory/repositories/{id}/timeline` | Histórico/evolução temporal |
| `GET` | `/api/v1/inventory/findings` | Buscar achados com filtros (severidade, tecnologia, data) |

#### Mensagens RabbitMQ (Comunicação API ↔ Worker)

| Fila | Direção | Payload |
|---|---|---|
| `analysis.jobs` | API → Worker | `{ jobId, requestId, repositoryUrl, provider, accessToken, sharedContextJson, promptContent, analysisType, timeoutSeconds }` |
| `analysis.results` | Worker → API | `{ jobId, requestId, analysisType, status (Completed/Failed), outputJson, durationMs, errorMessage? }` |

> **Nota**: O `accessToken` é transmitido na mensagem de job e mantido em memória pelo Worker apenas durante a execução. Mensagens são persistidas no RabbitMQ (durable queues) para garantir entrega. O campo `accessToken` **não é logado** em nenhum nível.

---

## Pontos de Integração

### GitHub / Azure DevOps (Clone)

- **Protocolo**: HTTPS git clone via `git` CLI ou LibGit2Sharp
- **Autenticação**: Token fornecido na solicitação (nunca persistido; mantido em memória com `SecureString` ou scoped lifetime)
- **Fallback**: Se token não fornecido, tenta clone público
- **Timeout**: Configurável por variável de ambiente (default: 10 min)
- **Tratamento de erro**: Retry com backoff exponencial (máx. 3 tentativas via Polly)

### GitHub Copilot SDK (Análise)

- **Protocolo**: SDK .NET oficial (HTTP/REST subjacente)
- **Entrada**: Código-fonte + prompt + contexto compartilhado
- **Saída**: Resposta textual que o Worker parseia em JSON estruturado
- **Timeout**: Configurável por tipo de análise (default: 30 min)
- **Idempotência**: Cada job possui UUID; re-execução gera novo output sem side-effects
- **Rate limiting**: Respeitar limites do SDK; implementar throttling via prefetch count do RabbitMQ consumer

### RabbitMQ (Mensageria)

- **Protocolo**: AMQP 0-9-1
- **Biblioteca**: `RabbitMQ.Client` (.NET) — biblioteca oficial
- **Filas**: `analysis.jobs` (durable, com prefetch configurável) e `analysis.results` (durable)
- **Dead Letter Queue**: `analysis.jobs.dlq` para mensagens que falharam após max retries
- **Acknowledgment**: Manual ack após processamento completo do job; nack + requeue em caso de falha transitória
- **Retry**: 3 tentativas com backoff exponencial via dead-letter + TTL; após exaustão, mensagem vai para DLQ
- **Serialização**: JSON (System.Text.Json) com schema versionado
- **Health Check**: `AspNetCore.HealthChecks.RabbitMQ` na API; verificação de conectividade no Worker
- **Tracing**: `requestId` e `correlationId` propagados via message headers para Sentry

### Sentry (Observabilidade)

- **SDK**: `Sentry.AspNetCore` (API) + `@sentry/react` (Frontend)
- **Integração**: Sentry SDK captura automaticamente exceptions, transactions e breadcrumbs
- **Tracing**: O `requestId` da solicitação é propagado como tag em todas as transactions do pipeline
- **Bridge OpenTelemetry**: O Sentry SDK para .NET integra-se com OpenTelemetry, permitindo correlação de traces

---

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|---|---|---|---|
| Infraestrutura Docker | Novo deployment | Criação de docker-compose.yml com 5 containers (API, Worker, RabbitMQ, PostgreSQL, Frontend). Baixo risco. | Validar recursos mínimos do host |
| RabbitMQ | Nova dependência | Message broker para comunicação API↔Worker. Risco baixo (tecnologia madura, imagem Docker oficial). | Configurar management UI para monitoramento de filas |
| GitHub Copilot SDK | Dependência externa | Custos embutidos na assinatura; indisponibilidade impacta análises. Risco médio. | Implementar retry + circuit breaker |
| Repositórios do cliente | Acesso externo | Clone via token; volume de dados variável. Risco médio. | Definir limite de tamanho de repo |
| Rede do cliente | Conectividade | Worker precisa de acesso à internet para Copilot SDK e Sentry. Risco baixo. | Documentar requisitos de rede |

---

## Abordagem de Testes

### Testes Unitários

- **Framework**: xUnit + AwesomeAssertions (https://github.com/AwesomeAssertions/AwesomeAssertions / https://www.nuget.org/profiles/AwesomeAssertions) + Moq + AutoFixture (conforme `rules/dotnet-testing.md`)
- **Componentes**: Lógica de domínio (entidades, validações), serviços de aplicação (orquestração, consolidação, classificação de severidade), parsing de output do Copilot SDK
- **Mocks**: `ICopilotClient`, `IRepository<T>`, `IDiscoveryService` — todos os serviços externos mockados
- **Cobertura alvo**: ≥ 70% conforme regras do projeto
- **Naming**: `MethodName_Condition_ExpectedBehavior`

### Testes de Integração

- **Framework**: `Microsoft.AspNetCore.Mvc.Testing` + Testcontainers (PostgreSQL + RabbitMQ)
- **Escopo**: Endpoints REST completos (request → response), persistência EF Core, fluxo de autenticação JWT, publicação/consumo de mensagens RabbitMQ
- **Skip quando Docker indisponível**: Testes detectam ausência de Docker engine e fazem skip limpo sem quebrar o build
- **Dados de teste**: Seed via EF Core migrations em banco efêmero

### Testes E2E (Fase futura)

- **Framework**: Playwright (conforme regras)
- **Escopo**: Login → Criar solicitação → Acompanhar pipeline → Visualizar resultados
- **Ambiente**: Docker Compose completo com Copilot SDK mockado via WireMock

---

## Sequenciamento de Desenvolvimento

### Ordem de Construção

1. **Fase 1 – Fundação** (Semanas 1-3)
   - Setup dos dois solutions independentes (API + Worker), ambos Clean Architecture
   - Docker Compose com PostgreSQL, RabbitMQ, API, Worker e Frontend
   - Modelos de domínio e migrações EF Core/PostgreSQL (API)
   - Integração RabbitMQ: publisher (API) + consumer (Worker) com filas durable
   - Módulo de autenticação (registro, login, JWT)
   - CRUD de Prompts
   - Testes unitários das entidades e validações

2. **Fase 2 – Pipeline Core** (Semanas 4-6)
   - API de criação de solicitação + publicação de jobs no RabbitMQ
   - Discovery Service (clone + detecção de stack .NET/C#)
   - Worker: consumo de jobs, integração GitHub Copilot SDK, publicação de resultados
   - API: consumo de resultados do RabbitMQ e consolidação
   - Orquestração com paralelismo por repositório (pilares sequenciais por repo)
   - Testes de integração dos endpoints + mensageria

3. **Fase 3 – Consolidação e Inventário** (Semanas 7-8)
   - Motor de consolidação (normalização, correlação, classificação de severidade)
   - Inventário com histórico e filtros
   - Paginação e busca conforme padrões REST

4. **Fase 4 – Frontend** (Semanas 9-11)
   - Setup React/Vite com tema Cyber-Technical
   - Telas: Login, Dashboard, Formulário de solicitação, Resultados, Inventário
   - Integração com API + polling de status

5. **Fase 5 – Observabilidade e Hardening** (Semana 12)
   - Integração Sentry (API + Frontend)
   - Health checks (PostgreSQL, Copilot SDK connectivity)
   - Revisão de segurança (tokens em memória, least privilege)
   - Documentação de deploy on-premise

### Dependências Técnicas

- **RabbitMQ**: Imagem Docker oficial (`rabbitmq:3-management`). Necessário desde a Fase 1
- **GitHub Copilot SDK**: Acesso antecipado ao SDK .NET necessário antes da Fase 2
- **Sentry DSN**: Configuração da instância Sentry (SaaS ou on-premise) antes da Fase 5
- **Prompts de análise**: Conteúdo dos 4 prompts (Obsolescência, Segurança, Observabilidade, Documentação) deve ser definido pela equipe de arquitetura antes da Fase 2

---

## Monitoramento e Observabilidade

- **Error tracking**: Sentry SDK captura todas as exceptions não tratadas com contexto (user, request, tags)
- **Performance monitoring**: Transactions do Sentry rastreiam duração de cada fase do pipeline (Discovery, Analysis, Consolidation)
- **Tracing distribuído**: `requestId` propagado como Sentry tag + OpenTelemetry `trace_id` para correlação
- **Métricas por job**: Duração, status final e tipo de análise registrados em log estruturado (JSON)
- **Logs estruturados**: Formato JSON padronizado conforme `rules/dotnet-logging.md`, com campos: timestamp, level, message, service.name, trace_id, requestId
- **Health checks**: Endpoint `/health` com verificações de PostgreSQL, RabbitMQ e conectividade externa (conforme `rules/dotnet-observability.md`)
- **Alertas recomendados no Sentry**: Taxa de erros > 5%, duração de pipeline > 24h, falhas consecutivas de job

---

## Considerações Técnicas

### Decisões Principais

| Decisão | Escolha | Justificativa | Alternativa Rejeitada |
|---|---|---|---|
| **Banco de dados** | PostgreSQL | Open-source, excelente suporte Docker Compose, sem custo de licença, EF Core Npgsql maduro. Ideal para deploy on-premise. | Oracle (requer licença, complexidade de containerização) |
| **Mensageria** | RabbitMQ | Permite processamento paralelo de múltiplos repositórios. Desacoplamento total API↔Worker. Escalabilidade horizontal de Workers. Dead Letter Queue para tratamento de falhas. Imagem Docker madura e leve. | Banco + BackgroundService (não suporta paralelismo; acoplamento direto), Redis Streams (menos recursos de DLQ/retry) |
| **Separação de projetos** | API e Worker como solutions independentes | Cada componente tem ciclo de vida, deploy e escalabilidade próprios. Worker pode ser escalado horizontalmente sem afetar API. Facilita manutenção e evolução independente. | Monorepo único (acoplamento de deploy; um redeploy afeta ambos) |
| **Modelo de paralelismo** | Paralelo por repositório, sequencial por pilar | Múltiplos repos simultaneamente maximizam throughput. Pilares sequenciais por repo evitam conflito no Copilot SDK e simplificam consolidação. | Totalmente paralelo (risco de rate limiting do SDK; consolidação complexa) |
| **CQRS** | Nativo (sem MediatR) | Conforme `rules/dotnet-architecture.md`. Interfaces `ICommandHandler<,>` e `IQueryHandler<,>` com Dispatcher nativo. | MediatR (dependência evitada por regra) |
| **Frontend** | React + Vite + TypeScript | Guia UI menciona Lucide React/Phosphor Icons, indicando React. Vite oferece DX superior e builds rápidos. | Angular (não há indicação no projeto), SSR (desnecessário para app interna) |
| **Comunicação API ↔ Worker** | RabbitMQ (mensageria assíncrona) | Desacoplamento total: API publica em `analysis.jobs`, Worker consome; Worker publica em `analysis.results`, API consome. Suporta múltiplos Workers concorrentes. Auditável via RabbitMQ Management UI. | HTTP callback (acoplamento direto; não escala; falha se API estiver indisponível), gRPC (overhead para este caso) |
| **Token de repositório** | In-memory scoped (nunca persistido) | Requisito RF-47. Token é recebido na request, mantido em memória durante execução e descartado após conclusão. | Vault/Secret Manager (adicionaria complexidade; token já é efêmero) |

### Riscos Conhecidos

| Risco | Impacto | Mitigação |
|---|---|---|
| **Repositórios muito grandes** | Discovery e análise podem exceder timeout de 24h | Implementar limite configurável de tamanho; fan-out de jobs por módulo (RF-23) |
| **Indisponibilidade do Copilot SDK** | Pipeline fica bloqueado | Circuit breaker com Polly; retry automático (RF-10); status FAILED com possibilidade de re-execução |
| **Parsing de output não-estruturado do SDK** | Resultados inconsistentes | Prompt engineering rigoroso com exemplos de output JSON; validação de schema na consolidação |
| **Concorrência de Workers** | Rate limiting do Copilot SDK com múltiplos Workers | Controlar via prefetch count do RabbitMQ (limita jobs simultâneos por Worker); monitorar quotas do SDK |
| **RabbitMQ indisponível** | Pipeline bloqueado; jobs não são despachados | Health check na API; restart automático via Docker Compose restart policy; filas durable para persistir mensagens |

### Requisitos Especiais

- **Segurança**: Tokens de repo nunca persistidos (RF-47); transmitidos via mensagem RabbitMQ e mantidos em memória no Worker apenas durante execução. Secrets via variáveis de ambiente no Docker Compose (RF-48). Workers em containers isolados com network própria (RF-49). Princípio de least privilege: Worker não acessa banco diretamente; comunica-se exclusivamente via RabbitMQ (RF-50). Credenciais RabbitMQ gerenciadas via `.env`.
- **Performance**: Discovery deve processar repositórios de até 500MB em < 30 minutos. Múltiplos repositórios processados em paralelo. Paginação obrigatória em todas as listagens conforme `rules/restful.md`.
- **Deploy**: Distribuído como `docker-compose.yml` com 5 serviços: `api`, `worker`, `rabbitmq`, `db`, `frontend`. Configuração via `.env` file. Workers podem ser escalados via `docker compose up --scale worker=N`.

### Conformidade com Padrões

| Regra | Status | Notas |
|---|---|---|
| `dotnet-architecture.md` (Clean Architecture) | ✅ Conforme | Camadas 1-Services → 2-Application → 3-Domain → 4-Infra |
| `dotnet-coding-standards.md` (Nomenclatura inglês, PascalCase) | ✅ Conforme | Todo código em inglês |
| `dotnet-libraries-config.md` (EF Core, FluentValidation, Polly) | ✅ Conforme | Substituição: PostgreSQL no lugar de Oracle (justificado acima) |
| `dotnet-testing.md` (xUnit + AwesomeAssertions) | ✅ Conforme | AAA pattern, naming convention |
| `dotnet-observability.md` (Health Checks) | ✅ Conforme | Endpoint `/health` com tags |
| `dotnet-logging.md` (JSON estruturado, OpenTelemetry) | ⚠️ Desvio parcial | Sentry SDK como exportador primário (conforme PRD RF-43/46); bridge OpenTelemetry mantém compatibilidade |
| `restful.md` (Versionamento, RFC 9457, Paginação) | ✅ Conforme | `/api/v1/*`, Problem Details, `_page`/`_size` |
| `dotnet-folders.md` (Estrutura numerada) | ✅ Conforme | `1-Services/`, `2-Application/`, etc. |
| `react-logging.md` (OpenTelemetry frontend) | ⚠️ Desvio parcial | Sentry React SDK substitui OpenTelemetry puro; propagação de trace mantida |

---

### Estrutura dos Projetos

> API e Worker são **projetos independentes**, cada um com seu próprio solution, Dockerfile e ciclo de deploy.

#### Projeto 1 – API Orquestradora

```
ModernizationPlatform.API/
├── ModernizationPlatform.API.sln
├── Dockerfile
├── 1-Services/
│   └── ModernizationPlatform.API/
│       ├── Controllers/
│       ├── Middleware/
│       ├── BackgroundServices/      # ResultConsumer (RabbitMQ → API)
│       └── Program.cs
├── 2-Application/
│   └── ModernizationPlatform.Application/
│       ├── Commands/
│       ├── Queries/
│       ├── Handlers/
│       ├── DTOs/
│       ├── Interfaces/
│       └── Messaging/               # IJobPublisher, IResultConsumer
├── 3-Domain/
│   └── ModernizationPlatform.Domain/
│       ├── Entities/
│       ├── Enums/
│       ├── Interfaces/
│       └── Services/
├── 4-Infra/
│   ├── ModernizationPlatform.Infra/
│   │   ├── Persistence/
│   │   ├── Repositories/
│   │   └── Migrations/
│   └── ModernizationPlatform.Infra.Messaging/
│       └── RabbitMqJobPublisher.cs   # Publica jobs na fila
│       └── RabbitMqResultConsumer.cs  # Consome resultados da fila
└── 5-Tests/
    ├── ModernizationPlatform.API.UnitTests/
    └── ModernizationPlatform.API.IntegrationTests/
```

#### Projeto 2 – Worker Genérico

```
ModernizationPlatform.Worker/
├── ModernizationPlatform.Worker.sln
├── Dockerfile
├── 1-Services/
│   └── ModernizationPlatform.Worker/
│       ├── Consumers/                # RabbitMQ job consumer
│       └── Program.cs
├── 2-Application/
│   └── ModernizationPlatform.Worker.Application/
│       ├── Handlers/
│       ├── DTOs/
│       └── Interfaces/
├── 3-Domain/
│   └── ModernizationPlatform.Worker.Domain/
│       ├── Entities/
│       └── Interfaces/
├── 4-Infra/
│   ├── ModernizationPlatform.Worker.Infra.CopilotSdk/
│   │   └── CopilotClient.cs
│   └── ModernizationPlatform.Worker.Infra.Messaging/
│       └── RabbitMqJobConsumer.cs     # Consome jobs da fila
│       └── RabbitMqResultPublisher.cs # Publica resultados na fila
└── 5-Tests/
    ├── ModernizationPlatform.Worker.UnitTests/
    └── ModernizationPlatform.Worker.IntegrationTests/
```

#### Docker Compose (Raiz)

```
modernization-platform/
├── docker-compose.yml
├── .env.example
├── api/                              # Submódulo ou clone do projeto API
├── worker/                           # Submódulo ou clone do projeto Worker
└── frontend/
    └── modernization-web/
        ├── src/
        │   ├── components/
        │   ├── pages/
        │   ├── services/
        │   ├── hooks/
        │   └── theme/
        ├── package.json
        └── vite.config.ts
```

---

*Documento criado em: 05/02/2026*
*PRD de referência: tasks/prd-plataforma-modernizacao-software/prd.md*
