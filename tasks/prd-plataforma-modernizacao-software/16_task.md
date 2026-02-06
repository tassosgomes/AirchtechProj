---
status: pending
parallelizable: true
blocked_by: ["9.0", "12.0"]
---

<task_context>
<domain>infra/observability</domain>
<type>integration</type>
<scope>middleware</scope>
<complexity>medium</complexity>
<dependencies>external_apis</dependencies>
<unblocks>"17.0"</unblocks>
</task_context>

# Tarefa 16.0: Observabilidade com Sentry

## Visão Geral

Integrar o Sentry como plataforma de observabilidade em todos os componentes: API, Worker e Frontend. Inclui error tracking, performance monitoring, tracing distribuído, logs estruturados e métricas por job. Corresponde à funcionalidade F09 do PRD (RF-43 a RF-46).

<requirements>
- RF-43: Emitir logs estruturados para todas as operações
- RF-44: Expor métricas por job (duração, status, tipo de análise)
- RF-45: Suportar tracing distribuído por requestId via Sentry
- RF-46: Reportar erros e exceções automaticamente ao Sentry
- Sentry SDK: `Sentry.AspNetCore` (API/Worker) + `@sentry/react` (Frontend)
- Bridge OpenTelemetry para correlação de traces
- Logs JSON estruturados com: timestamp, level, message, service.name, trace_id, requestId
</requirements>

## Subtarefas

- [ ] 16.1 Instalar `Sentry.AspNetCore` e `Sentry.Extensions.Logging` na API e no Worker
- [ ] 16.2 Configurar Sentry no `Program.cs` da API: DSN via variável de ambiente, environment, release, traces sample rate
- [ ] 16.3 Configurar Sentry no `Program.cs` do Worker: DSN via variável de ambiente, environment, release
- [ ] 16.4 Implementar middleware de tracing: propagar `requestId` como tag em todas as transactions
- [ ] 16.5 Configurar logs estruturados (JSON): Serilog com sink para console (JSON format) + integração Sentry
- [ ] 16.6 Implementar campos obrigatórios nos logs: timestamp, level, message, service.name, trace_id, requestId
- [ ] 16.7 Registrar métricas por job: log estruturado com duração, status final, tipo de análise em cada conclusão de job
- [ ] 16.8 Configurar bridge OpenTelemetry no Sentry SDK para correlação de traces
- [ ] 16.9 Instalar `@sentry/react` no Frontend
- [ ] 16.10 Configurar Sentry no Frontend: DSN, environment, error boundary, performance monitoring
- [ ] 16.11 Propagar `requestId` do backend nos headers de resposta para o frontend
- [ ] 16.12 Verificar que `accessToken` NÃO aparece em nenhum log ou evento do Sentry (scrubbing)
- [ ] 16.13 Escrever testes: verificar que logs são emitidos nos formatos corretos, que tags são propagadas

## Sequenciamento

- **Bloqueado por**: 9.0 (Pipeline funcional para gerar traces), 12.0 (Frontend para integração Sentry)
- **Desbloqueia**: 17.0 (Hardening)
- **Paralelizável**: Sim — pode executar em paralelo com 17.0

## Detalhes de Implementação

### Configuração Sentry (API)

```csharp
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = 1.0;
    o.SendDefaultPii = false;  // Não enviar PII
});
```

### Log Estruturado (formato JSON)

```json
{
  "timestamp": "2026-02-05T10:30:00.123Z",
  "level": "Information",
  "message": "Analysis job completed",
  "service.name": "modernization-api",
  "trace_id": "abc123",
  "requestId": "uuid-request",
  "jobId": "uuid-job",
  "analysisType": "Security",
  "durationMs": 45000,
  "status": "Completed"
}
```

### Sentry Frontend

```typescript
import * as Sentry from '@sentry/react';

Sentry.init({
  dsn: import.meta.env.VITE_SENTRY_DSN,
  environment: import.meta.env.VITE_ENV,
  integrations: [Sentry.browserTracingIntegration()],
  tracesSampleRate: 1.0,
});
```

### Alertas Recomendados (documentar)

- Taxa de erros > 5%
- Duração de pipeline > 24h
- Falhas consecutivas de job

### Regras aplicáveis

- `rules/dotnet-observability.md`: Health checks (já implementados)
- `rules/dotnet-logging.md`: Formato JSON, campos obrigatórios
- `rules/react-logging.md`: Sentry React SDK

## Critérios de Sucesso

- [ ] Exceptions são reportadas automaticamente ao Sentry (API, Worker, Frontend)
- [ ] Transactions de performance são rastreadas no Sentry
- [ ] `requestId` aparece como tag em todos os eventos do pipeline
- [ ] Logs JSON contêm todos os campos obrigatórios
- [ ] Métricas por job (duração, status, tipo) são registradas
- [ ] `accessToken` NÃO aparece em nenhum evento ou log do Sentry
- [ ] Frontend captura erros e os reporta ao Sentry
