---
status: completed
parallelizable: false
blocked_by: ["6.0", "7.0", "8.0"]
---

<task_context>
<domain>engine/orchestration</domain>
<type>integration</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database, rabbitmq</dependencies>
<unblocks>"10.0"</unblocks>
</task_context>

# Tarefa 9.0: Orquestração do Pipeline

## Visão Geral

Implementar o motor de orquestração que coordena o pipeline completo: pegar solicitações da fila, executar Discovery, publicar jobs de análise no RabbitMQ para cada pilar selecionado (sequencialmente por repositório), consumir resultados e acionar a consolidação. Suporta múltiplos repositórios em paralelo. Corresponde à funcionalidade F02 do PRD (RF-07 a RF-10).

<requirements>
- RF-07: Processar solicitações da fila (múltiplos repos em paralelo)
- RF-08: Gerenciar transições de estado: QUEUED → DISCOVERY_RUNNING → ANALYSIS_RUNNING → CONSOLIDATING → COMPLETED | FAILED
- RF-09: Orquestrar execução sequencial: Discovery → Análises (sequenciais por pilar dentro do mesmo repo) → Consolidação
- RF-10: Retry automático para jobs com falha (máximo configurável)
- RF-23: Fan-out de jobs para repositórios grandes
- Paralelismo por repositório: múltiplos repos simultaneamente
- Pilares sequenciais por repositório: evitar conflito no Copilot SDK
</requirements>

## Subtarefas

- [x] 9.1 Criar `IOrchestrationService` na camada Application: `CreateRequestAsync`, `ProcessPendingRequestsAsync`
- [x] 9.2 Implementar `OrchestrationBackgroundService` (hosted service) que periodicamente busca solicitações QUEUED e inicia processamento
- [x] 9.3 Implementar fluxo de Discovery: chamar `IDiscoveryService` → atualizar status para DISCOVERY_RUNNING → salvar SharedContext → atualizar status
- [x] 9.4 Implementar publicação de jobs: para cada tipo de análise selecionado, buscar prompt no catálogo → criar `AnalysisJob` no banco → publicar `AnalysisJobMessage` no RabbitMQ
- [x] 9.5 Implementar sequenciamento de pilares por repositório: publicar 1 job por vez, aguardar resultado antes de publicar o próximo
- [x] 9.6 Implementar consumo de resultados (via `RabbitMqResultConsumer` da tarefa 3.0): atualizar `AnalysisJob` no banco com output e status
- [x] 9.7 Implementar transição para consolidação: quando todos os jobs de um request forem COMPLETED, atualizar status para CONSOLIDATING
- [x] 9.8 Implementar retry: se job falha e `retryCount < maxRetries`, republish com incremento; caso contrário, marcar como FAILED
- [x] 9.9 Implementar tratamento de falha global: se Discovery falha, marcar request como FAILED; se análise crítica falha após retries, marcar request como FAILED
- [x] 9.10 Implementar suporte a paralelismo por repositório: cada request é processado em uma "thread" lógica independente (SemaphoreSlim ou similar)
- [x] 9.11 Propagar `accessToken` da solicitação para as mensagens RabbitMQ (em memória, nunca persistido)
- [x] 9.12 Escrever testes unitários: transições de estado, lógica de retry, sequenciamento de pilares
- [x] 9.13 Escrever teste de integração: fluxo completo QUEUED → DISCOVERY → ANALYSIS → CONSOLIDATING (com mocks)

## Sequenciamento

- **Bloqueado por**: 6.0 (API de Solicitação), 7.0 (Discovery), 8.0 (Worker pronto para consumir)
- **Desbloqueia**: 10.0 (Consolidação — acionada pela orquestração)
- **Paralelizável**: Não (é a integração das partes)

## Detalhes de Implementação

### Máquina de Estados

```
QUEUED
  ↓ (OrchestrationService pega da fila)
DISCOVERY_RUNNING
  ↓ (Discovery conclui com sucesso)
ANALYSIS_RUNNING
  ↓ (Todos os jobs completam) ou ↓ (Falha com retry esgotado)
CONSOLIDATING                        FAILED
  ↓ (Consolidação conclui)
COMPLETED
```

### Modelo de Paralelismo (conforme TechSpec)

```
Repositório A: Discovery → [Obsolescence] → [Security] → [Observability] → [Documentation] → Consolidation
Repositório B: Discovery → [Security] → [Documentation] → Consolidation
                ↑ Processados simultaneamente
```

- Cada repositório tem seus pilares processados **sequencialmente** (1 por vez)
- Diferentes repositórios são processados **em paralelo**

### Fluxo Detalhado

```
1. BackgroundService busca requests com status QUEUED (ordered by CreatedAt)
2. Para cada request, inicia processamento em paralelo:
   a. Atualizar status → DISCOVERY_RUNNING
   b. Executar Discovery → gerar SharedContext
   c. Atualizar status → ANALYSIS_RUNNING
   d. Para cada AnalysisType selecionado (sequencial):
      i.   Buscar prompt no catálogo
      ii.  Criar AnalysisJob no banco (status: Pending)
      iii. Publicar AnalysisJobMessage no RabbitMQ
      iv.  Aguardar resultado (AnalysisResultMessage via consumer)
      v.   Atualizar AnalysisJob no banco
      vi.  Se FAILED: retry ou marcar request como FAILED
   e. Se todos os jobs COMPLETED:
      i.  Atualizar status → CONSOLIDATING
      ii. Acionar IConsolidationService
3. Aguardar novo ciclo de polling
```

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Service na 2-Application, BackgroundService na 1-Services
- `rules/dotnet-coding-standards.md`: async/await, CancellationToken
- `rules/dotnet-logging.md`: Logs estruturados para cada transição de estado

## Critérios de Sucesso

- [x] Solicitações QUEUED são processadas automaticamente pelo BackgroundService
- [x] Transições de estado ocorrem corretamente (QUEUED → ... → COMPLETED)
- [x] Discovery é executado antes das análises
- [x] Pilares são executados sequencialmente por repositório
- [x] Múltiplos repositórios são processados em paralelo
- [x] Retry funciona: job republished até maxRetries, depois FAILED
- [x] `accessToken` propagado via mensagem mas nunca persistido
- [x] Mínimo 6 testes unitários passando
- [x] Teste de integração do fluxo completo passando

## Checklist de Conclusão

- [x] 9.0 Orquestração do Pipeline ✅ CONCLUÍDA
  - [x] 9.1 Implementação completada
  - [x] 9.2 Definição da tarefa, PRD e tech spec validados
  - [x] 9.3 Análise de regras e conformidade verificadas
  - [x] 9.4 Revisão de código completada
  - [x] 9.5 Pronto para deploy
