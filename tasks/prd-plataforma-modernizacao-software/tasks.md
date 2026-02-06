# Implementação Plataforma de Modernização de Software - Resumo de Tarefas

## Visão Geral

Este documento consolida todas as tarefas necessárias para implementar a **Plataforma de Modernização de Software**, conforme definido no PRD e na Especificação Técnica. A plataforma é composta por 3 projetos independentes (API Orquestradora, Worker Genérico, Frontend) orquestrados via Docker Compose com RabbitMQ e PostgreSQL.

> **Nota**: São 18 tarefas principais divididas em 5 fases. Para funcionalidades deste porte (>10 tarefas), recomenda-se a execução faseada descrita abaixo.

---

## Fases de Implementação

### Fase 1 – Fundação (Semanas 1–3)
- [x] 1.0 Setup dos Projetos e Infraestrutura Docker
- [x] 2.0 Modelos de Domínio e Persistência (API)
- [x] 3.0 Integração RabbitMQ (API e Worker)
- [x] 4.0 Módulo de Autenticação
- [x] 5.0 Catálogo de Prompts (CRUD)

### Fase 2 – Pipeline Core (Semanas 4–6)
- [x] 6.0 API de Solicitação de Análise
- [x] 7.0 Discovery Service
- [x] 8.0 Worker Genérico – Execução de Análise
- [x] 9.0 Orquestração do Pipeline

### Fase 3 – Consolidação e Inventário (Semanas 7–8)
- [x] 10.0 Motor de Consolidação
- [ ] 11.0 Inventário de Software

### Fase 4 – Frontend (Semanas 9–11)
- [ ] 12.0 Setup Frontend React/Vite + Tema Cyber-Technical
- [ ] 13.0 Telas de Autenticação e Solicitação
- [ ] 14.0 Dashboard e Visualização de Resultados
- [ ] 15.0 Tela de Inventário e Histórico

### Fase 5 – Observabilidade e Hardening (Semana 12)
- [ ] 16.0 Observabilidade com Sentry
- [ ] 17.0 Segurança e Hardening
- [ ] 18.0 Documentação de Deploy On-Premise

---

## Análise de Paralelização

### Caminho Crítico

```
1.0 → 2.0 → 6.0 → 7.0 → 9.0 → 10.0 → 11.0 → 15.0 → 18.0
```

### Fluxos Paralelos (Lanes de Execução)

```
Lane A (API - Core):     1.0 → 2.0 → 4.0 → 5.0 → 6.0 → 7.0 → 9.0 → 10.0 → 11.0
Lane B (Worker/Infra):   1.0 → 3.0 → 8.0 ─────────────────────┘ (merge em 9.0)
Lane C (Frontend):       1.0 → 12.0 → 13.0 → 14.0 → 15.0
Lane D (Hardening):      ──────────────────────── 16.0 → 17.0 → 18.0
```

### Oportunidades de Paralelização

| Tarefas Paralelas | Pré-requisito Comum | Observação |
|---|---|---|
| 2.0 ∥ 3.0 | 1.0 concluída | Domínio e mensageria são independentes |
| 4.0 ∥ 5.0 | 2.0 concluída | Auth e Prompts não dependem entre si |
| 6.0 ∥ 8.0 | 2.0 + 3.0 concluídas | API de solicitação e Worker são projetos separados |
| 12.0 ∥ qualquer backend | 1.0 concluída | Frontend pode iniciar setup em paralelo |
| 16.0 ∥ 17.0 | 9.0 concluída | Observabilidade e segurança são ortogonais |

---

## Dependências Técnicas Externas

| Dependência | Necessária a partir de | Status |
|---|---|---|
| Docker / Docker Compose | Tarefa 1.0 | Obrigatório |
| PostgreSQL (imagem Docker) | Tarefa 2.0 | Obrigatório |
| RabbitMQ (imagem Docker) | Tarefa 3.0 | Obrigatório |
| GitHub Copilot SDK (.NET) | Tarefa 8.0 | Acesso antecipado necessário |
| Sentry DSN | Tarefa 16.0 | Configurar instância antes |
| Prompts de análise (conteúdo) | Tarefa 8.0 | Definidos pela equipe de arquitetura |

---

*Gerado em: 05/02/2026*
*PRD: tasks/prd-plataforma-modernizacao-software/prd.md*
*TechSpec: tasks/prd-plataforma-modernizacao-software/techspec.md*
