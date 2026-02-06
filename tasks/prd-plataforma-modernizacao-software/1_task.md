---
status: pending
parallelizable: false
blocked_by: []
---

<task_context>
<domain>infra/devops</domain>
<type>configuration</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>docker</dependencies>
<unblocks>"2.0", "3.0", "12.0"</unblocks>
</task_context>

# Tarefa 1.0: Setup dos Projetos e Infraestrutura Docker

## Visão Geral

Criar a estrutura base dos dois projetos independentes (API Orquestradora e Worker Genérico) seguindo Clean Architecture, além do Docker Compose que orquestra todos os 5 serviços (API, Worker, RabbitMQ, PostgreSQL, Frontend). Esta é a tarefa fundacional — todas as demais dependem dela.

<requirements>
- Criar solution independente para API: `ModernizationPlatform.API.sln`
- Criar solution independente para Worker: `ModernizationPlatform.Worker.sln`
- Ambas as solutions devem seguir Clean Architecture com pastas numeradas (`1-Services/`, `2-Application/`, `3-Domain/`, `4-Infra/`, `5-Tests/`)
- Criar `docker-compose.yml` na raiz com 5 serviços: `api`, `worker`, `rabbitmq`, `db`, `frontend`
- Criar Dockerfile para API e Worker
- Criar `.env.example` com todas as variáveis de configuração
- Garantir que `docker compose up` suba todos os serviços corretamente
</requirements>

## Subtarefas

- [ ] 1.1 Criar estrutura da solution API (`ModernizationPlatform.API/`) com projetos: `ModernizationPlatform.API` (1-Services), `ModernizationPlatform.Application` (2-Application), `ModernizationPlatform.Domain` (3-Domain), `ModernizationPlatform.Infra` (4-Infra), `ModernizationPlatform.Infra.Messaging` (4-Infra), `ModernizationPlatform.API.UnitTests` (5-Tests), `ModernizationPlatform.API.IntegrationTests` (5-Tests)
- [ ] 1.2 Criar estrutura da solution Worker (`ModernizationPlatform.Worker/`) com projetos: `ModernizationPlatform.Worker` (1-Services), `ModernizationPlatform.Worker.Application` (2-Application), `ModernizationPlatform.Worker.Domain` (3-Domain), `ModernizationPlatform.Worker.Infra.CopilotSdk` (4-Infra), `ModernizationPlatform.Worker.Infra.Messaging` (4-Infra), `ModernizationPlatform.Worker.UnitTests` (5-Tests), `ModernizationPlatform.Worker.IntegrationTests` (5-Tests)
- [ ] 1.3 Configurar referências entre projetos (ex.: Application referencia Domain; Infra referencia Application; Services referencia todos)
- [ ] 1.4 Adicionar pacotes NuGet base: `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Serilog.AspNetCore`, `System.Text.Json` em ambos os projetos
- [ ] 1.5 Criar `Program.cs` mínimo para API (ASP.NET Core Web API) e Worker (Worker Service / Background Service)
- [ ] 1.6 Criar `Dockerfile` multi-stage para API e Worker (build + runtime)
- [ ] 1.7 Criar `docker-compose.yml` com serviços: `api` (porta 5000), `worker`, `rabbitmq` (imagem `rabbitmq:3-management`, portas 5672/15672), `db` (PostgreSQL, porta 5432), `frontend` (porta 3000)
- [ ] 1.8 Criar `.env.example` com variáveis: `POSTGRES_*`, `RABBITMQ_*`, `JWT_SECRET`, `SENTRY_DSN`, `COPILOT_*`
- [ ] 1.9 Validar que `docker compose build` e `docker compose up` funcionam sem erros
- [ ] 1.10 Criar `README.md` na raiz com instruções básicas de setup

## Sequenciamento

- **Bloqueado por**: Nenhum (tarefa inicial)
- **Desbloqueia**: 2.0 (Domínio), 3.0 (RabbitMQ), 12.0 (Frontend)
- **Paralelizável**: Não (é pré-requisito de todas)

## Detalhes de Implementação

### Estrutura da API (conforme TechSpec)

```
ModernizationPlatform.API/
├── ModernizationPlatform.API.sln
├── Dockerfile
├── 1-Services/
│   └── ModernizationPlatform.API/
│       ├── Controllers/
│       ├── Middleware/
│       ├── BackgroundServices/
│       └── Program.cs
├── 2-Application/
│   └── ModernizationPlatform.Application/
│       ├── Commands/
│       ├── Queries/
│       ├── Handlers/
│       ├── DTOs/
│       ├── Interfaces/
│       └── Messaging/
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
└── 5-Tests/
    ├── ModernizationPlatform.API.UnitTests/
    └── ModernizationPlatform.API.IntegrationTests/
```

### Estrutura do Worker (conforme TechSpec)

```
ModernizationPlatform.Worker/
├── ModernizationPlatform.Worker.sln
├── Dockerfile
├── 1-Services/
│   └── ModernizationPlatform.Worker/
│       ├── Consumers/
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
│   └── ModernizationPlatform.Worker.Infra.Messaging/
└── 5-Tests/
    ├── ModernizationPlatform.Worker.UnitTests/
    └── ModernizationPlatform.Worker.IntegrationTests/
```

### Docker Compose (serviços esperados)

| Serviço | Imagem | Portas | Dependências |
|---|---|---|---|
| `db` | `postgres:16-alpine` | 5432 | — |
| `rabbitmq` | `rabbitmq:3-management` | 5672, 15672 | — |
| `api` | Build local | 5000 | `db`, `rabbitmq` |
| `worker` | Build local | — | `rabbitmq` |
| `frontend` | Build local (Node) | 3000 | `api` |

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Clean Architecture com camadas numeradas
- `rules/dotnet-folders.md`: Estrutura de pastas numerada
- `rules/dotnet-coding-standards.md`: Nomenclatura em inglês, PascalCase

## Critérios de Sucesso

- [ ] Ambas as solutions compilam sem erros (`dotnet build`)
- [ ] `docker compose build` executa sem erros
- [ ] `docker compose up` sobe todos os 5 serviços
- [ ] API responde em `http://localhost:5000` (mesmo que apenas 200 OK)
- [ ] RabbitMQ Management UI acessível em `http://localhost:15672`
- [ ] PostgreSQL aceita conexão na porta 5432
- [ ] Estrutura de pastas segue exatamente o padrão numerado da TechSpec
