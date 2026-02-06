---
status: pending
parallelizable: false
blocked_by: ["9.0"]
---

<task_context>
<domain>engine/consolidation</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"11.0", "14.0"</unblocks>
</task_context>

# Tarefa 10.0: Motor de Consolidação

## Visão Geral

Implementar o motor de consolidação que unifica os outputs de todas as análises de um repositório, normaliza os achados em formato padrão, correlaciona achados entre diferentes pilares e classifica por severidade. Gera a visão consolidada que será exposta ao usuário. Corresponde à funcionalidade F06 do PRD (RF-28 a RF-31) e à história HU-04.

<requirements>
- RF-28: Normalizar outputs de todos os workers em formato unificado
- RF-29: Correlacionar achados entre diferentes tipos de análise
- RF-30: Classificar achados por severidade (Crítico, Alto, Médio, Baixo, Informativo)
- RF-31: Gerar visão consolidada por repositório
- Acionado pela orquestração quando todos os jobs de um request são COMPLETED
</requirements>

## Subtarefas

- [x] 10.1 Criar `IConsolidationService` na camada Application: `ConsolidateAsync(Guid requestId, CancellationToken)`
- [x] 10.2 Implementar normalização: parsear `OutputJson` de cada `AnalysisJob`, extrair findings em formato padronizado (`Finding` entity)
- [x] 10.3 Implementar correlação de achados: identificar findings que se referem ao mesmo arquivo, componente ou dependência em diferentes pilares
- [x] 10.4 Implementar classificação de severidade: aplicar regras para categorizar como Critical, High, Medium, Low, Informative
- [x] 10.5 Persistir findings no banco (`findings` table) associados ao respectivo job
- [x] 10.6 Gerar visão consolidada: agregar findings por severidade, por categoria e por arquivo
- [x] 10.7 Implementar endpoint `GET /api/v1/analysis-requests/{id}/results`: retornar findings consolidados com filtros e agrupamentos
- [x] 10.8 Atualizar status da request para COMPLETED após consolidação bem-sucedida
- [x] 10.9 Escrever testes unitários: normalização de output, classificação de severidade, correlação
- [x] 10.10 Escrever teste de integração: consolidar resultado de 2+ jobs mockados

## Sequenciamento

- **Bloqueado por**: 9.0 (Orquestração aciona consolidação)
- **Desbloqueia**: 11.0 (Inventário consome dados consolidados), 14.0 (Frontend exibe resultados)
- **Paralelizável**: Não (depende de 9.0)

## Detalhes de Implementação

### Fluxo de Consolidação

```
1. Receber requestId (todos os jobs COMPLETED)
2. Buscar todos os AnalysisJobs do request
3. Para cada job: parsear OutputJson → extrair findings
4. Normalizar findings (formato padronizado)
5. Correlacionar: agrupar por filePath, componente, dependência
6. Classificar severidade
7. Persistir findings no banco
8. Atualizar request status → COMPLETED
9. Retornar ConsolidatedResult
```

### Formato da Visão Consolidada

```json
{
  "requestId": "uuid",
  "repositoryUrl": "https://github.com/org/repo",
  "completedAt": "2026-02-05T18:00:00Z",
  "summary": {
    "totalFindings": 42,
    "bySeverity": {
      "Critical": 2,
      "High": 8,
      "Medium": 15,
      "Low": 12,
      "Informative": 5
    },
    "byCategory": {
      "Obsolescence": 12,
      "Security": 10,
      "Observability": 8,
      "Documentation": 12
    }
  },
  "findings": [
    {
      "id": "uuid",
      "severity": "Critical",
      "category": "Security",
      "title": "Dependência com CVE conhecida",
      "description": "...",
      "filePath": "src/Api.csproj",
      "correlatedWith": ["uuid-finding-obsolescence"]
    }
  ]
}
```

### Regras de Correlação

- Findings que apontam para o **mesmo `filePath`** são candidatos a correlação
- Findings que mencionam a **mesma dependência** (por nome) são correlacionados
- Correlation é indicada via campo `correlatedWith` (lista de IDs)

### Regras aplicáveis

- `rules/restful.md`: Paginação nos resultados, Problem Details
- `rules/dotnet-architecture.md`: Service na 2-Application
- `rules/dotnet-testing.md`: xUnit, AwesomeAssertions

## Critérios de Sucesso

- [x] Outputs de múltiplos jobs são normalizados em formato unificado
- [x] Findings são classificados por severidade corretamente
- [x] Correlação identifica achados relacionados entre pilares
- [x] Findings persistidos no banco com relacionamento correto ao job
- [x] Endpoint `/consolidated` retorna visão consolidada com sumário
- [x] Status da request atualizado para COMPLETED
- [x] Mínimo 5 testes unitários passando (6 testes criados)
- [x] Teste de integração passando (2 testes de integração criados)

---

## ✅ Status da Conclusão

- [x] 10.0 Motor de Consolidação - **CONCLUÍDA**
  - [x] Implementação completada
  - [x] Definição da tarefa, PRD e tech spec validados
  - [x] Análise de regras e conformidade verificadas
  - [x] Revisão de código completada
  - [x] Build e testes passando (6 unitários + 2 integração)
  - [x] Relatório de revisão criado ([10_task_review.md](10_task_review.md))
  - [x] ✅ Pronto para deploy
