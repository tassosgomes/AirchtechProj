---
status: pending
parallelizable: false
blocked_by: ["10.0"]
---

<task_context>
<domain>engine/inventory</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"15.0"</unblocks>
</task_context>

# Tarefa 11.0: Inventário de Software

## Visão Geral

Implementar a base de dados do inventário de software com visão histórica e evolutiva do parque. Cada análise concluída alimenta o inventário com repositórios, tecnologias, dependências e achados. A API expõe endpoints para consulta com filtros, paginação e evolução temporal. Corresponde à funcionalidade F07 do PRD (RF-32 a RF-35) e às histórias HU-05, HU-06.

<requirements>
- RF-32: Armazenar repositórios analisados, tecnologias, dependências e achados
- RF-33: Manter histórico de todas as análises por repositório
- RF-34: Permitir consulta e filtragem por tecnologia, dependência, severidade e data
- RF-35: Apresentar evolução temporal dos achados por repositório
- Paginação obrigatória em listagens
- Alimentado automaticamente após consolidação de cada análise
</requirements>

## Subtarefas

- [ ] 11.1 Criar `IInventoryService` na camada Application: `QueryAsync(InventoryFilter)`, `GetTimelineAsync(Guid repositoryId)`
- [ ] 11.2 Implementar lógica de alimentação do inventário: após consolidação (tarefa 10.0), criar/atualizar registro na tabela `repositories` e associar achados
- [ ] 11.3 Criar `InventoryFilter` DTO: technology, dependency, severity, dateFrom, dateTo, page, size
- [ ] 11.4 Implementar consulta de repositórios com filtros avançados: filtrar por tecnologia detectada, dependência, severidade dos achados, período
- [ ] 11.5 Implementar timeline por repositório: listar análises históricas com resumo de findings por severidade (evolução temporal)
- [ ] 11.6 Criar `InventoryController` com endpoints: `GET /api/v1/inventory/repositories`, `GET /api/v1/inventory/repositories/{id}/timeline`, `GET /api/v1/inventory/findings`
- [ ] 11.7 Implementar paginação (`_page`, `_size`) em todos os endpoints de listagem
- [ ] 11.8 Criar DTOs de resposta: `RepositorySummary`, `RepositoryTimeline`, `FindingSummary`
- [ ] 11.9 Escrever testes unitários: filtros, paginação, cálculo de timeline
- [ ] 11.10 Escrever teste de integração: alimentar inventário → consultar com filtros → verificar timeline

## Sequenciamento

- **Bloqueado por**: 10.0 (Consolidação alimenta o inventário)
- **Desbloqueia**: 15.0 (Frontend tela de inventário)
- **Paralelizável**: Não (depende de 10.0)

## Detalhes de Implementação

### Endpoints (conforme TechSpec)

| Método | Path | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/v1/inventory/repositories` | Listar repositórios com filtros e paginação | Autenticado |
| `GET` | `/api/v1/inventory/repositories/{id}/timeline` | Histórico/evolução temporal | Autenticado |
| `GET` | `/api/v1/inventory/findings` | Buscar achados com filtros | Autenticado |

### Formato da Timeline

```json
{
  "repositoryId": "uuid",
  "repositoryUrl": "https://github.com/org/repo",
  "analyses": [
    {
      "analysisId": "uuid",
      "completedAt": "2026-01-15T10:00:00Z",
      "summary": {
        "Critical": 5,
        "High": 12,
        "Medium": 20,
        "Low": 8,
        "Informative": 3
      }
    },
    {
      "analysisId": "uuid",
      "completedAt": "2026-02-05T18:00:00Z",
      "summary": {
        "Critical": 2,
        "High": 8,
        "Medium": 15,
        "Low": 12,
        "Informative": 5
      }
    }
  ]
}
```

### Filtros Suportados

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `technology` | string | Filtrar por linguagem/framework (ex.: "C#", "ASP.NET Core") |
| `dependency` | string | Filtrar por nome de dependência (ex.: "Newtonsoft.Json") |
| `severity` | enum | Filtrar por severidade mínima (ex.: "High") |
| `dateFrom` | date | Filtrar análises a partir de |
| `dateTo` | date | Filtrar análises até |
| `_page` | int | Página (default: 1) |
| `_size` | int | Itens por página (default: 20) |

### Regras aplicáveis

- `rules/restful.md`: Paginação `_page`/`_size`, Problem Details
- `rules/dotnet-architecture.md`: CQRS nativo (IQueryHandler)
- `rules/dotnet-testing.md`: xUnit, AwesomeAssertions

## Critérios de Sucesso

- [ ] Inventário é alimentado automaticamente após cada consolidação
- [ ] Consulta com filtros retorna resultados corretos
- [ ] Paginação funciona em todos os endpoints de listagem
- [ ] Timeline mostra evolução temporal correta por repositório
- [ ] Endpoints protegidos por JWT
- [ ] Mínimo 5 testes unitários passando
- [ ] Teste de integração passando
