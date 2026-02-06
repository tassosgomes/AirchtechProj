---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>infra/messaging</domain>
<type>implementation</type>
<scope>middleware</scope>
<complexity>high</complexity>
<dependencies>rabbitmq</dependencies>
<unblocks>"6.0", "8.0"</unblocks>
</task_context>

# Tarefa 3.0: Integração RabbitMQ (API e Worker)

## Visão Geral

Implementar a camada de mensageria RabbitMQ em ambos os projetos (API e Worker). A API publica jobs na fila `analysis.jobs` e consome resultados de `analysis.results`. O Worker consome jobs de `analysis.jobs` e publica resultados em `analysis.results`. Inclui Dead Letter Queue, retry com backoff, manual acknowledgment e health checks.

<requirements>
- RF-07: Enfileirar solicitações e processá-las (RabbitMQ como backbone)
- RF-18: Worker recebe entrada via fila RabbitMQ
- RF-22: Worker reporta status via fila RabbitMQ
- Filas durable com prefetch configurável
- Dead Letter Queue (`analysis.jobs.dlq`) para falhas
- Manual ack/nack
- Retry com backoff exponencial (3 tentativas via dead-letter + TTL)
- Propagação de `requestId` e `correlationId` via message headers
- Health checks de conectividade
</requirements>

## Subtarefas

- [ ] 3.1 Instalar pacote `RabbitMQ.Client` em ambos os projetos (API Infra.Messaging e Worker Infra.Messaging)
- [ ] 3.2 Criar classe de configuração `RabbitMqOptions` (Host, Port, Username, Password, PrefetchCount) lida de `appsettings.json` / variáveis de ambiente
- [ ] 3.3 Criar conexão RabbitMQ compartilhada (singleton) com retry de conexão via Polly
- [ ] 3.4 Declarar filas no startup: `analysis.jobs` (durable), `analysis.results` (durable), `analysis.jobs.dlq`
- [ ] 3.5 Implementar DTOs de mensagem: `AnalysisJobMessage` (jobId, requestId, repositoryUrl, provider, accessToken, sharedContextJson, promptContent, analysisType, timeoutSeconds) e `AnalysisResultMessage` (jobId, requestId, analysisType, status, outputJson, durationMs, errorMessage)
- [ ] 3.6 Implementar `RabbitMqJobPublisher` (API → `analysis.jobs`): serialização JSON, headers com requestId/correlationId
- [ ] 3.7 Implementar `RabbitMqResultConsumer` como BackgroundService (API ← `analysis.results`): desserialização, manual ack, delegação para handler
- [ ] 3.8 Implementar `RabbitMqJobConsumer` como BackgroundService (Worker ← `analysis.jobs`): desserialização, manual ack/nack, prefetch, delegação para handler
- [ ] 3.9 Implementar `RabbitMqResultPublisher` (Worker → `analysis.results`): serialização JSON, headers
- [ ] 3.10 Configurar Dead Letter Queue: `analysis.jobs` com `x-dead-letter-exchange` apontando para `analysis.jobs.dlq`; TTL progressivo para retry
- [ ] 3.11 Implementar health check de RabbitMQ na API (`AspNetCore.HealthChecks.RabbitMQ`) e no Worker (verificação de conectividade)
- [ ] 3.12 Registrar serviços no DI de ambos os projetos
- [ ] 3.13 Escrever testes unitários: serialização/desserialização de mensagens, lógica de retry
- [ ] 3.14 Escrever teste de integração: publicar e consumir mensagem em RabbitMQ real (Testcontainers)

## Sequenciamento

- **Bloqueado por**: 1.0 (Setup dos projetos)
- **Desbloqueia**: 6.0 (API Solicitação — precisa publicar jobs), 8.0 (Worker — precisa consumir jobs)
- **Paralelizável**: Sim — pode executar em paralelo com 2.0 (Domínio)

## Detalhes de Implementação

### Filas e Fluxo (conforme TechSpec)

| Fila | Direção | Payload |
|---|---|---|
| `analysis.jobs` | API → Worker | `{ jobId, requestId, repositoryUrl, provider, accessToken, sharedContextJson, promptContent, analysisType, timeoutSeconds }` |
| `analysis.results` | Worker → API | `{ jobId, requestId, analysisType, status, outputJson, durationMs, errorMessage? }` |
| `analysis.jobs.dlq` | Dead Letter | Mensagens que falharam após 3 tentativas |

### Configuração de Retry

```
Tentativa 1 → falha → nack → DLQ com TTL 5s → requeue
Tentativa 2 → falha → nack → DLQ com TTL 30s → requeue
Tentativa 3 → falha → nack → DLQ permanente (sem requeue)
```

### Segurança

- O campo `accessToken` **nunca deve ser logado** em nenhum nível (RF-47)
- Headers de mensagem devem conter `requestId` e `correlationId` para tracing (Sentry)

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Implementação na camada 4-Infra
- `rules/dotnet-observability.md`: Health checks obrigatórios

## Critérios de Sucesso

- [ ] Filas `analysis.jobs`, `analysis.results` e `analysis.jobs.dlq` criadas automaticamente no startup
- [ ] Publisher publica mensagem serializada em JSON com headers corretos
- [ ] Consumer consome mensagem, desserializa e faz ack manual
- [ ] Nack + requeue funciona para falhas transitórias
- [ ] DLQ recebe mensagens após 3 tentativas
- [ ] Health check de RabbitMQ funcionando na API
- [ ] `accessToken` não aparece em nenhum log
- [ ] Teste de integração com Testcontainers RabbitMQ passando
- [ ] `dotnet build` sem erros em ambos os projetos
