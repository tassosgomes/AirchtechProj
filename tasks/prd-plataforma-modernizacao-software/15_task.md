---
status: completed
parallelizable: false
blocked_by: []
---

<task_context>
<domain>frontend/pages</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"16.0"</unblocks>
</task_context>

# Tarefa 15.0: Tela de Inventário e Histórico ✅ CONCLUÍDA

- [x] 15.0 Tela de Inventario e Historico ✅ CONCLUIDA
  - [x] 15.1 Implementacao completada
  - [x] 15.2 Definicao da tarefa, PRD e tech spec validados
  - [x] 15.3 Analise de regras e conformidade verificadas
  - [x] 15.4 Revisao de codigo completada
  - [x] 15.5 Pronto para deploy

## Visão Geral

Implementar as telas de inventário de software e histórico/evolução temporal por repositório. O inventário mostra todos os repositórios analisados com tecnologias, dependências e achados. O histórico permite comparar análises ao longo do tempo. Corresponde à funcionalidade F08 do PRD (RF-40, RF-41) e às histórias HU-05, HU-06.

<requirements>
- RF-40: Tela de inventário com filtros e busca
- RF-41: Histórico e evolução temporal por repositório
- Integração com API de inventário (tarefa 11.0)
- Paginação na listagem
- Filtros por tecnologia, dependência, severidade e data
</requirements>

## Subtarefas

- [x] 15.1 Implementar página de Inventário (`/inventory`): lista de repositórios analisados com resumo de tecnologias e achados
- [x] 15.2 Implementar card de repositório: URL, provedor, linguagens (badges), total de findings por severidade, data da última análise
- [x] 15.3 Implementar filtros: campo de busca por texto, filtro por tecnologia (dropdown), filtro por severidade (multi-select), filtro por data (date range)
- [x] 15.4 Implementar paginação na listagem
- [x] 15.5 Integrar com API: `GET /api/v1/inventory/repositories` com parâmetros de filtro e paginação
- [x] 15.6 Implementar página de Timeline (`/inventory/:id/timeline`): evolução temporal de findings por repositório
- [x] 15.7 Implementar visualização de timeline: gráfico de linhas ou barras mostrando contagem de findings por severidade ao longo do tempo
- [x] 15.8 Integrar com API: `GET /api/v1/inventory/repositories/{id}/timeline`
- [x] 15.9 Implementar navegação: do card do repositório → timeline; da timeline → detalhes de uma análise específica

## Sequenciamento

- **Bloqueado por**: 12.0 (Setup Frontend), 14.0 (Dashboard — fluxo de navegação), 11.0 (API de Inventário)
- **Desbloqueia**: 16.0 (Observabilidade inclui Sentry no frontend)
- **Paralelizável**: Não (depende de 14.0)

## Detalhes de Implementação

### Tela de Inventário

```
┌───────────────────────────────────────────────────────┐
│ Inventário de Software                                │
├───────────────────────────────────────────────────────┤
│ [🔍 Buscar...]  [Tecnologia ▼]  [Severidade ▼]  [📅] │
├───────────────────────────────────────────────────────┤
│ ┌───────────────────────────────────────────────────┐ │
│ │ github.com/org/repo-1          Última: 05/02/2026│ │
│ │ [C#] [ASP.NET Core] [React]                      │ │
│ │ 🔴 2  🟠 8  🟡 15  🔵 12  ⚪ 5               │ │
│ └───────────────────────────────────────────────────┘ │
│ ┌───────────────────────────────────────────────────┐ │
│ │ dev.azure.com/org/repo-2       Última: 01/02/2026│ │
│ │ [Java] [Spring Boot]                              │ │
│ │ 🔴 0  🟠 3  🟡 7  🔵 5  ⚪ 2                │ │
│ └───────────────────────────────────────────────────┘ │
│                    [< 1 2 3 ... >]                    │
└───────────────────────────────────────────────────────┘
```

### Tela de Timeline

```
┌───────────────────────────────────────────────────────┐
│ ← Voltar    github.com/org/repo-1                     │
├───────────────────────────────────────────────────────┤
│                                                       │
│   Findings por Severidade ao Longo do Tempo           │
│                                                       │
│   20 ┤ ──────────────── Critical                     │
│   15 ┤ ─────────── High                              │
│   10 ┤ ─────── Medium                                │
│    5 ┤ ───── Low                                     │
│      └──────────────────────────────────              │
│        Jan/26   Fev/26   Mar/26                       │
│                                                       │
├───────────────────────────────────────────────────────┤
│ Histórico de Análises                                 │
│ ┌─────────────────────────────────────────────────┐   │
│ │ 05/02/2026 — 🔴2 🟠8 🟡15 🔵12 ⚪5 [Ver]  │   │
│ │ 15/01/2026 — 🔴5 🟠12 🟡20 🔵8 ⚪3 [Ver]  │   │
│ └─────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────┘
```

### Integração com API

```typescript
// Listagem com filtros
const response = await apiClient.get('/api/v1/inventory/repositories', {
  params: { technology, severity, dateFrom, dateTo, _page, _size }
});

// Timeline
const response = await apiClient.get(`/api/v1/inventory/repositories/${id}/timeline`);
```

## Critérios de Sucesso

- [x] Inventário lista repositórios com resumo de tecnologias e achados
- [x] Filtros funcionam corretamente (tecnologia, severidade, data)
- [x] Paginação funciona na listagem
- [x] Timeline exibe evolução temporal de findings
- [x] Navegação entre inventário → timeline → detalhes funciona
- [x] Estética Cyber-Technical consistente
