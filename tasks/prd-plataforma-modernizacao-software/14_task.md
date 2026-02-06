---
status: pending
parallelizable: false
blocked_by: ["12.0", "13.0", "10.0"]
---

<task_context>
<domain>frontend/pages</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>http_server</dependencies>
<unblocks>"15.0"</unblocks>
</task_context>

# Tarefa 14.0: Dashboard e Visualização de Resultados

## Visão Geral

Implementar o dashboard de acompanhamento do pipeline e a visualização de resultados consolidados. O dashboard mostra todas as solicitações com status de cada etapa (Fila → Discovery → Análises → Consolidação) e posição na fila. A tela de resultados exibe findings classificados por severidade com filtros. Corresponde à funcionalidade F08 do PRD (RF-38, RF-39) e às histórias HU-03, HU-04.

<requirements>
- RF-38: Dashboard com status de cada etapa do pipeline e posição na fila
- RF-39: Visualização de resultados consolidados com classificação de severidade
- Polling de status em intervalo configurável (ex.: 5 segundos)
- Feedback visual: spinners durante transições, badges de status coloridos
</requirements>

## Subtarefas

- [ ] 14.1 Implementar página Dashboard (`/dashboard`): lista de todas as solicitações do usuário com status atual
- [ ] 14.2 Implementar card de solicitação: URL do repo, provedor, status badge colorido, posição na fila (se QUEUED), data de criação
- [ ] 14.3 Implementar pipeline visual: indicador de progresso Fila → Discovery → Análises → Consolidação → Completo (com ícones e cores por etapa)
- [ ] 14.4 Implementar polling de status: buscar `GET /api/v1/analysis-requests` periodicamente (5s) para atualizar dashboard
- [ ] 14.5 Implementar navegação para detalhes: clicar no card abre `/requests/:id` com detalhes completos
- [ ] 14.6 Implementar página de detalhes da solicitação (`/requests/:id`): pipeline detalhado, jobs individuais com duração, status de cada pilar
- [ ] 14.7 Implementar página de resultados: exibir findings consolidados agrupados por severidade, com contadores (badge) por tipo
- [ ] 14.8 Implementar filtros nos resultados: por severidade, por categoria (pilar), por arquivo
- [ ] 14.9 Implementar card de finding: severidade (cor), categoria, título, descrição, arquivo com caminho (monospaced)
- [ ] 14.10 Implementar sumário visual: gráfico ou contadores de findings por severidade (barras ou donut chart)

## Sequenciamento

- **Bloqueado por**: 12.0 (Setup Frontend), 13.0 (Auth + criação de solicitação), 10.0 (API de resultados consolidados)
- **Desbloqueia**: 15.0 (Inventário — última tela)
- **Paralelizável**: Não (depende de 13.0)

## Detalhes de Implementação

### Status Badges (cores)

| Status | Cor | Ícone |
|---|---|---|
| Queued | `#8B949E` (cinza) | ⏳ |
| Discovery Running | `#00BFFF` (azul) | 🔍 |
| Analysis Running | `#39FF14` (verde neon) | ⚡ |
| Consolidating | `#FFD700` (amarelo) | 🔄 |
| Completed | `#39FF14` (verde) | ✅ |
| Failed | `#FF3131` (vermelho) | ❌ |

### Severity Badges (cores)

| Severidade | Cor |
|---|---|
| Critical | `#FF3131` (vermelho) |
| High | `#FF6B35` (laranja) |
| Medium | `#FFD700` (amarelo) |
| Low | `#00BFFF` (azul) |
| Informative | `#8B949E` (cinza) |

### Polling de Status

```typescript
useEffect(() => {
  const interval = setInterval(async () => {
    const response = await apiClient.get('/api/v1/analysis-requests');
    setRequests(response.data);
  }, 5000);
  return () => clearInterval(interval);
}, []);
```

### Layout do Dashboard

```
┌─────────────────────────────────────────────┐
│ Minhas Solicitações            [+ Nova]     │
├─────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────┐ │
│ │ github.com/org/repo-1                   │ │
│ │ ⏳ Fila → 🔍 Discovery → ⚡ Análise    │ │
│ │ Status: Analysis Running  |  Posição: — │ │
│ └─────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────┐ │
│ │ dev.azure.com/org/repo-2                │ │
│ │ ⏳ Fila                                  │ │
│ │ Status: Queued  |  Posição: 2            │ │
│ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

## Critérios de Sucesso

- [ ] Dashboard lista todas as solicitações com status correto
- [ ] Badges de status exibem cores corretas por etapa
- [ ] Posição na fila exibida para solicitações QUEUED
- [ ] Polling atualiza status automaticamente
- [ ] Resultados consolidados exibidos com classificação de severidade
- [ ] Filtros funcionam nos resultados
- [ ] Estética Cyber-Technical consistente
