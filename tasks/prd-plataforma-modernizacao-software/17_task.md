---
status: pending
parallelizable: true
blocked_by: ["9.0", "8.0"]
---

<task_context>
<domain>infra/security</domain>
<type>implementation</type>
<scope>performance</scope>
<complexity>medium</complexity>
<dependencies>docker</dependencies>
<unblocks>"18.0"</unblocks>
</task_context>

# Tarefa 17.0: Segurança e Hardening

## Visão Geral

Revisar e implementar medidas de segurança conforme os requisitos da funcionalidade F10 do PRD (RF-47 a RF-50). Inclui garantia de que tokens nunca são persistidos, gerenciamento de segredos via variáveis de ambiente, isolamento de containers e princípio de least privilege. Também inclui implementação de health checks para todos os componentes.

<requirements>
- RF-47: Tokens de acesso a repositórios nunca devem ser persistidos em banco de dados
- RF-48: Segredos devem ser gerenciados via secret manager (variáveis de ambiente no Docker Compose)
- RF-49: Workers devem executar em containers isolados
- RF-50: Princípio de least privilege em todos os componentes
- Health checks: PostgreSQL, RabbitMQ, conectividade externa
</requirements>

## Subtarefas

- [ ] 17.1 Auditoria de segurança: verificar que `accessToken` não é persistido em nenhuma tabela, log ou evento
- [ ] 17.2 Implementar scrubbing de tokens: middleware/filtro que remove tokens de logs e responses de erro
- [ ] 17.3 Verificar que todas as credenciais (DB, RabbitMQ, JWT Secret, Sentry DSN) estão em variáveis de ambiente (`.env`)
- [ ] 17.4 Configurar Docker Compose com networks isoladas: `frontend-net` (frontend ↔ api), `backend-net` (api ↔ db, api ↔ rabbitmq), `worker-net` (worker ↔ rabbitmq)
- [ ] 17.5 Verificar que Worker NÃO tem acesso direto ao banco (network isolation + sem connection string)
- [ ] 17.6 Configurar usuários não-root nos Dockerfiles (least privilege)
- [ ] 17.7 Implementar health check endpoint `/health` na API: verificar PostgreSQL, RabbitMQ
- [ ] 17.8 Implementar health check no Worker: verificar conectividade RabbitMQ
- [ ] 17.9 Configurar health checks no Docker Compose (HEALTHCHECK directive)
- [ ] 17.10 Revisar configuração JWT: secret com entropia suficiente, expiração adequada, algoritmo seguro (HS256 mínimo)
- [ ] 17.11 Implementar rate limiting na API (opcional, boas práticas)
- [ ] 17.12 Documentar modelo de segurança no README

## Sequenciamento

- **Bloqueado por**: 9.0 (Pipeline funcional), 8.0 (Worker funcional)
- **Desbloqueia**: 18.0 (Documentação)
- **Paralelizável**: Sim — pode executar em paralelo com 16.0 (Observabilidade)

## Detalhes de Implementação

### Network Isolation (Docker Compose)

```yaml
networks:
  frontend-net:
    driver: bridge
  backend-net:
    driver: bridge
  worker-net:
    driver: bridge

services:
  frontend:
    networks: [frontend-net]
  api:
    networks: [frontend-net, backend-net, worker-net]
  db:
    networks: [backend-net]
  rabbitmq:
    networks: [backend-net, worker-net]
  worker:
    networks: [worker-net]
```

### Health Check Endpoint

```
GET /health
{
  "status": "Healthy",
  "checks": {
    "postgresql": { "status": "Healthy", "duration": "15ms" },
    "rabbitmq": { "status": "Healthy", "duration": "8ms" }
  }
}
```

### Checklist de Least Privilege

| Componente | Acesso Permitido | Acesso Proibido |
|---|---|---|
| Frontend | API (porta 5000) | DB, RabbitMQ, Worker |
| API | DB, RabbitMQ | — |
| Worker | RabbitMQ, Internet (Copilot SDK) | DB diretamente |
| DB | Aceita conexões de API | Não exposto externamente |
| RabbitMQ | API e Worker | Não exposto externamente (exceto Management UI para dev) |

### Regras aplicáveis

- `rules/dotnet-observability.md`: Health checks com tags
- `rules/dotnet-architecture.md`: Princípio de least privilege

## Critérios de Sucesso

- [ ] `accessToken` não existe em nenhuma tabela do banco (verificação por query)
- [ ] `accessToken` não aparece em nenhum log (grep em logs)
- [ ] Todas as credenciais estão em `.env` (nenhum hardcoded)
- [ ] Worker não consegue conectar ao PostgreSQL (network isolation)
- [ ] Containers executam como usuário não-root
- [ ] Health check `/health` retorna status de PostgreSQL e RabbitMQ
- [ ] Docker Compose HEALTHCHECK configurado para todos os serviços
