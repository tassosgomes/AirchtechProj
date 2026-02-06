---
status: pending
parallelizable: false
blocked_by: ["17.0"]
---

<task_context>
<domain>infra/documentation</domain>
<type>documentation</type>
<scope>configuration</scope>
<complexity>low</complexity>
<dependencies>none</dependencies>
<unblocks></unblocks>
</task_context>

# Tarefa 18.0: Documentação de Deploy On-Premise

## Visão Geral

Criar documentação completa para deploy on-premise da plataforma. Inclui guia de instalação, configuração, requisitos de infraestrutura, escalabilidade e troubleshooting. A plataforma é distribuída como Docker Compose e deve ser facilmente instalável na infraestrutura do cliente.

<requirements>
- Documentar requisitos mínimos de hardware e software
- Documentar configuração de variáveis de ambiente (.env)
- Documentar comandos de deploy e atualização
- Documentar escalabilidade de Workers
- Documentar requisitos de rede
- Documentar troubleshooting básico
</requirements>

## Subtarefas

- [ ] 18.1 Criar `README.md` na raiz do projeto com visão geral, pré-requisitos e quickstart
- [ ] 18.2 Documentar requisitos mínimos: Docker Engine 24+, Docker Compose v2, RAM mínima, disco, CPU
- [ ] 18.3 Documentar `.env.example` com descrição detalhada de cada variável de configuração
- [ ] 18.4 Documentar comandos de deploy: `docker compose up -d`, verificação de saúde, logs
- [ ] 18.5 Documentar escalabilidade: `docker compose up --scale worker=N` para Workers
- [ ] 18.6 Documentar requisitos de rede: portas necessárias, acesso à internet (Copilot SDK, Sentry), domínios a liberar
- [ ] 18.7 Documentar atualização: pull de novas imagens, migração de banco, rollback
- [ ] 18.8 Documentar troubleshooting: logs, health checks, problemas comuns (RabbitMQ não conecta, DB migration falha, etc.)
- [ ] 18.9 Documentar monitoramento: acesso ao RabbitMQ Management UI, Sentry dashboard, interpretação de health checks
- [ ] 18.10 Revisar e validar que todos os comandos documentados funcionam em ambiente limpo

## Sequenciamento

- **Bloqueado por**: 17.0 (Hardening — Docker Compose final)
- **Desbloqueia**: Nenhum (tarefa final)
- **Paralelizável**: Não (última tarefa)

## Detalhes de Implementação

### Estrutura do README

```markdown
# Plataforma de Modernização de Software

## Visão Geral
## Pré-requisitos
## Quickstart
## Configuração
### Variáveis de Ambiente
### Configuração do Sentry
### Configuração de Rede
## Deploy
### Primeiro Deploy
### Atualização
### Rollback
## Escalabilidade
## Monitoramento
### Health Checks
### RabbitMQ Management
### Sentry
## Troubleshooting
## Arquitetura
```

### Variáveis de Ambiente (.env)

| Variável | Descrição | Exemplo |
|---|---|---|
| `POSTGRES_HOST` | Host do PostgreSQL | `db` |
| `POSTGRES_PORT` | Porta do PostgreSQL | `5432` |
| `POSTGRES_DB` | Nome do banco | `modernization` |
| `POSTGRES_USER` | Usuário do banco | `modernization_user` |
| `POSTGRES_PASSWORD` | Senha do banco | `strong_password` |
| `RABBITMQ_HOST` | Host do RabbitMQ | `rabbitmq` |
| `RABBITMQ_PORT` | Porta AMQP | `5672` |
| `RABBITMQ_USER` | Usuário RabbitMQ | `guest` |
| `RABBITMQ_PASSWORD` | Senha RabbitMQ | `guest` |
| `JWT_SECRET` | Secret para assinatura JWT | `min-32-chars-secret` |
| `JWT_EXPIRATION_MINUTES` | Expiração do token | `60` |
| `SENTRY_DSN` | DSN do Sentry | `https://xxx@sentry.io/xxx` |
| `COPILOT_API_KEY` | Chave do GitHub Copilot SDK | `sk-xxx` |

### Requisitos de Rede

| Destino | Porta | Protocolo | Motivo |
|---|---|---|---|
| GitHub (github.com) | 443 | HTTPS | Clone de repositórios |
| Azure DevOps (dev.azure.com) | 443 | HTTPS | Clone de repositórios |
| GitHub Copilot API | 443 | HTTPS | Execução de análises |
| Sentry (sentry.io ou on-premise) | 443 | HTTPS | Envio de telemetria |

## Critérios de Sucesso

- [ ] README cobre todos os aspectos: install, config, deploy, escala, troubleshooting
- [ ] `.env.example` documenta todas as variáveis necessárias
- [ ] Comandos de deploy funcionam em ambiente limpo
- [ ] Escalabilidade de Workers documentada e testada
- [ ] Requisitos de rede documentados
- [ ] Troubleshooting cobre problemas mais comuns
