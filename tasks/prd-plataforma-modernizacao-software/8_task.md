---
status: pending
parallelizable: true
blocked_by: ["3.0"]
---

<task_context>
<domain>engine/worker</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>external_apis, rabbitmq</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 8.0: Worker Genérico – Execução de Análise

## Visão Geral

Implementar o Worker Genérico, um processo stateless em container Docker que consome jobs da fila RabbitMQ, executa análises via GitHub Copilot SDK e publica resultados estruturados. O Worker é um projeto independente com seu próprio solution e Dockerfile. Corresponde à funcionalidade F04 do PRD (RF-18 a RF-23).

<requirements>
- RF-18: Worker recebe: repositório, contexto compartilhado, prompt de análise e opções
- RF-19: Executar análise via GitHub Copilot SDK
- RF-20: Produzir saída estruturada em JSON padronizado
- RF-21: Respeitar timeout configurável por tipo de análise
- RF-22: Reportar status (RUNNING, COMPLETED, FAILED) e logs à orquestradora
- RF-23: Suportar fan-out de jobs para repositórios grandes
- Token de acesso mantido apenas em memória durante execução
- Worker não acessa banco de dados diretamente
</requirements>

## Subtarefas

- [ ] 8.1 Implementar `IAnalysisExecutor` na camada Application do Worker: `ExecuteAsync(AnalysisInput, CancellationToken)`
- [ ] 8.2 Criar `AnalysisInput` DTO: repositoryUrl, provider, accessToken, sharedContext, promptContent, analysisType, timeoutSeconds
- [ ] 8.3 Criar `AnalysisOutput` DTO: findings (lista), metadata, executionDurationMs
- [ ] 8.4 Implementar `ICopilotClient` na camada Infra (CopilotSdk): enviar código + prompt + contexto ao SDK, receber resposta
- [ ] 8.5 Implementar `CopilotClient`: configuração do SDK, montagem do request (código + contexto + prompt), chamada ao SDK, tratamento de timeout
- [ ] 8.6 Implementar parsing de output do Copilot SDK: extrair JSON estruturado da resposta textual, validar schema
- [ ] 8.7 Implementar `AnalysisExecutor`: clonar repo (reutilizar lógica), enviar para Copilot SDK, parsear resultado, gerar output JSON
- [ ] 8.8 Integrar consumer RabbitMQ (da tarefa 3.0) com `AnalysisExecutor`: receber `AnalysisJobMessage` → executar → publicar `AnalysisResultMessage`
- [ ] 8.9 Implementar timeout configurável por tipo de análise (default: 30 min)
- [ ] 8.10 Implementar lógica de status: publicar RUNNING ao iniciar, COMPLETED ou FAILED ao finalizar
- [ ] 8.11 Garantir que `accessToken` é mantido em memória (variável scoped) e nunca logado
- [ ] 8.12 Escrever testes unitários: parsing de output, lógica de timeout, tratamento de erro do SDK
- [ ] 8.13 Escrever teste de integração: mock do Copilot SDK + RabbitMQ real (Testcontainers)

## Sequenciamento

- **Bloqueado por**: 3.0 (RabbitMQ consumer/publisher implementados)
- **Desbloqueia**: 9.0 (Orquestração depende do Worker para processar jobs)
- **Paralelizável**: Sim — pode executar em paralelo com 6.0, 7.0 (são projetos independentes)

## Detalhes de Implementação

### Fluxo do Worker

```
1. Consumer recebe AnalysisJobMessage da fila analysis.jobs
2. Publicar status RUNNING
3. Clonar repositório (token em memória)
4. Montar CopilotRequest: código-fonte + prompt + contexto compartilhado
5. Enviar para GitHub Copilot SDK
6. Parsear resposta textual em JSON estruturado
7. Publicar AnalysisResultMessage na fila analysis.results (COMPLETED ou FAILED)
8. Limpar diretório temporário do clone
9. Fazer ack na mensagem original
```

### Integração GitHub Copilot SDK

```csharp
public interface ICopilotClient
{
    Task<CopilotResponse> AnalyzeAsync(CopilotRequest request, CancellationToken ct);
}

// CopilotRequest: código-fonte relevante, prompt do pilar, contexto compartilhado
// CopilotResponse: texto com output que será parseado em JSON
```

### Formato de Output Esperado

```json
{
  "findings": [
    {
      "severity": "High",
      "category": "Obsolescence",
      "title": "Framework desatualizado",
      "description": "Projeto utiliza .NET 5 que está em end-of-life",
      "filePath": "src/Api/Api.csproj"
    }
  ],
  "metadata": {
    "analysisType": "Obsolescence",
    "totalFindings": 1,
    "executionDurationMs": 45000
  }
}
```

### Tratamento de Erros

- **Timeout do SDK**: Publicar FAILED com `errorMessage` indicando timeout
- **Parsing falhou**: Publicar FAILED com output bruto no `errorMessage`
- **Exceção não tratada**: Nack na mensagem (retry via DLQ)
- **Token inválido (clone falhou)**: Publicar FAILED com mensagem genérica (sem expor token)

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Clean Architecture no Worker
- `rules/dotnet-coding-standards.md`: async/await, CancellationToken em tudo
- `rules/dotnet-testing.md`: xUnit, mocks para ICopilotClient

## Critérios de Sucesso

- [ ] Worker consome mensagem da fila e executa análise
- [ ] Output JSON estruturado gerado corretamente
- [ ] Timeout é respeitado (cancellation após limite)
- [ ] Status RUNNING/COMPLETED/FAILED publicado corretamente
- [ ] `accessToken` nunca aparece em logs
- [ ] Worker funciona como container Docker isolado
- [ ] Mínimo 5 testes unitários passando (parsing, timeout, erros)
- [ ] Teste de integração com mock do SDK passando
