---
status: pending
parallelizable: true
blocked_by: ["2.0", "3.0", "4.0"]
---

<task_context>
<domain>engine/orchestration</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>database, rabbitmq</dependencies>
<unblocks>"7.0", "9.0"</unblocks>
</task_context>

# Tarefa 6.0: API de Solicitação de Análise

## Visão Geral

Implementar os endpoints REST para criação e consulta de solicitações de análise. O arquiteto informa URL do repositório, provedor (GitHub/Azure DevOps), token de acesso opcional e tipos de análise desejados. A API valida os dados, persiste a solicitação com status QUEUED e expõe endpoints de consulta com posição na fila. Corresponde à funcionalidade F02 do PRD (RF-05 a RF-11) e às histórias HU-01, HU-02, HU-03.

<requirements>
- RF-05: Aceitar solicitações com URL do repositório, provedor e token de acesso
- RF-06: Validar URL do repositório (formato HTTP/HTTPS válido) com feedback imediato
- RF-07: Enfileirar solicitações (sem limite na fila)
- RF-08: Gerenciar ciclo de vida: QUEUED → DISCOVERY_RUNNING → ANALYSIS_RUNNING → CONSOLIDATING → COMPLETED | FAILED
- RF-10: Implementar retry automático (máximo configurável)
- RF-11: Endpoints REST para criação, consulta de status e obtenção de resultados
- Token de acesso (accessToken) NUNCA deve ser persistido no banco (RF-47)
</requirements>

## Subtarefas

- [ ] 6.1 Criar command `CreateAnalysisCommand` (repositoryUrl, provider, accessToken, selectedTypes)
- [ ] 6.2 Criar `ICommandHandler<CreateAnalysisCommand, AnalysisRequest>` com validação e persistência
- [ ] 6.3 Implementar validação com FluentValidation: URL HTTPS válida, provider válido, pelo menos 1 tipo selecionado
- [ ] 6.4 Criar `AnalysisRequestsController` com endpoints: `POST /api/v1/analysis-requests`, `GET /api/v1/analysis-requests`, `GET /api/v1/analysis-requests/{id}`, `GET /api/v1/analysis-requests/{id}/results`
- [ ] 6.5 Implementar paginação na listagem (`_page`, `_size`) conforme `rules/restful.md`
- [ ] 6.6 Implementar cálculo de posição na fila (contagem de requests com status QUEUED criadas antes)
- [ ] 6.7 Criar DTOs: `CreateAnalysisRequest`, `AnalysisRequestResponse` (com posição na fila), `AnalysisRequestListResponse`
- [ ] 6.8 Garantir que `accessToken` não é persistido — mantido apenas em memória durante a criação do job
- [ ] 6.9 Aplicar `[Authorize]` em todos os endpoints
- [ ] 6.10 Escrever testes unitários: validação de URL, transições de estado, cálculo de posição na fila
- [ ] 6.11 Escrever teste de integração: criar solicitação → consultar status → verificar posição na fila

## Sequenciamento

- **Bloqueado por**: 2.0 (Entidade AnalysisRequest), 3.0 (RabbitMQ para futuro publish), 4.0 (Auth)
- **Desbloqueia**: 7.0 (Discovery precisa da solicitação), 9.0 (Orquestração processa solicitações)
- **Paralelizável**: Sim — pode executar em paralelo com 8.0 (Worker)

## Detalhes de Implementação

### Endpoints (conforme TechSpec)

| Método | Path | Descrição | Auth |
|---|---|---|---|
| `POST` | `/api/v1/analysis-requests` | Criar solicitação | Autenticado |
| `GET` | `/api/v1/analysis-requests` | Listar com paginação | Autenticado |
| `GET` | `/api/v1/analysis-requests/{id}` | Status detalhado + posição na fila | Autenticado |
| `GET` | `/api/v1/analysis-requests/{id}/results` | Resultados consolidados | Autenticado |

### Payload de Criação

```json
{
  "repositoryUrl": "https://github.com/org/repo",
  "provider": "GitHub",
  "accessToken": "ghp_xxxx",
  "selectedTypes": ["Obsolescence", "Security", "Observability", "Documentation"]
}
```

### Resposta com Posição na Fila

```json
{
  "id": "uuid",
  "repositoryUrl": "https://github.com/org/repo",
  "provider": "GitHub",
  "status": "Queued",
  "queuePosition": 3,
  "selectedTypes": ["Obsolescence", "Security"],
  "createdAt": "2026-02-05T10:00:00Z"
}
```

### Regra de Segurança (RF-47)

- O `accessToken` é recebido no body do POST
- É mantido em memória (variável local) durante o processamento
- É passado para o RabbitMQ via mensagem (tarefa 9.0)
- **Nunca** é salvo na tabela `analysis_requests`

### Regras aplicáveis

- `rules/restful.md`: Paginação `_page`/`_size`, Problem Details, versionamento
- `rules/dotnet-architecture.md`: CQRS nativo (ICommandHandler)
- `rules/dotnet-coding-standards.md`: async/await, CancellationToken

## Critérios de Sucesso

- [ ] Criação de solicitação persiste no banco com status QUEUED
- [ ] Validação rejeita URLs inválidas com Problem Details
- [ ] `accessToken` NÃO aparece em nenhuma coluna do banco
- [ ] Listagem com paginação funciona corretamente
- [ ] Posição na fila é calculada corretamente
- [ ] Endpoints protegidos por JWT
- [ ] Mínimo 6 testes unitários passando
- [ ] Teste de integração do fluxo completo passando
