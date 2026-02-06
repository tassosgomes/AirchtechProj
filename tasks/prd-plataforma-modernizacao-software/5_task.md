---
status: pending
parallelizable: true
blocked_by: ["2.0", "4.0"]
---

<task_context>
<domain>engine/prompts</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>database</dependencies>
<unblocks>"8.0", "9.0"</unblocks>
</task_context>

# Tarefa 5.0: Catálogo de Prompts (CRUD)

## Visão Geral

Implementar o cadastro de prompts que orientam as análises do Worker. Cada prompt corresponde a um pilar de análise (Obsolescência, Segurança, Observabilidade, Documentação). Os prompts podem ser criados e atualizados sem redeploy, permitindo evolução contínua das análises. Corresponde à funcionalidade F05 do PRD (RF-24 a RF-27).

<requirements>
- RF-24: Manter cadastro de prompts (CRUD) com 1 prompt por pilar
- RF-25: Cada prompt deve conter metadados: id, tipo de análise e data de criação
- RF-26: Suportar os tipos: Obsolescência, Segurança, Observabilidade e Documentação
- RF-27: Permitir adicionar/atualizar prompts sem redeploy
- Endpoints protegidos por autenticação JWT
- Validação: analysis_type único por prompt
</requirements>

## Subtarefas

- [x] 5.1 Criar `IPromptCatalogService` na camada Application (se não existir da TechSpec): `GetAllAsync`, `GetByIdAsync`, `GetByAnalysisTypeAsync`, `CreateOrUpdateAsync`
- [x] 5.2 Implementar `PromptCatalogService` com lógica de CRUD e validação de unicidade de `AnalysisType`
- [x] 5.3 Criar `PromptsController` com endpoints: `GET /api/v1/prompts`, `GET /api/v1/prompts/{id}`, `POST /api/v1/prompts`, `PUT /api/v1/prompts/{id}`
- [x] 5.4 Criar DTOs: `CreatePromptRequest` (analysisType, content), `UpdatePromptRequest` (content), `PromptResponse` (id, analysisType, content, createdAt, updatedAt)
- [x] 5.5 Implementar validação com FluentValidation: content obrigatório, analysisType deve ser válido
- [x] 5.6 Aplicar `[Authorize]` no controller
- [x] 5.7 Escrever testes unitários: CRUD operations, validação de tipo duplicado
- [x] 5.8 Escrever teste de integração: criar prompt → listar → atualizar → buscar por tipo

## Sequenciamento

- **Bloqueado por**: 2.0 (Entidade Prompt e DbContext), 4.0 (Autenticação para proteger endpoints)
- **Desbloqueia**: 8.0 (Worker precisa do prompt para executar análise), 9.0 (Orquestração busca prompt por tipo)
- **Paralelizável**: Sim — pode executar em paralelo com 4.0 (após 2.0)

## Detalhes de Implementação

### Endpoints (conforme TechSpec)

| Método | Path | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/v1/prompts` | Listar todos os prompts | Autenticado |
| `GET` | `/api/v1/prompts/{id}` | Obter prompt por ID | Autenticado |
| `POST` | `/api/v1/prompts` | Criar novo prompt | Autenticado |
| `PUT` | `/api/v1/prompts/{id}` | Atualizar prompt existente | Autenticado |

### Regra de Negócio

- Cada `AnalysisType` pode ter **no máximo 1 prompt** (UNIQUE constraint no banco)
- `POST` com `AnalysisType` já existente deve retornar 409 Conflict
- `PUT` atualiza apenas o `Content` e o `UpdatedAt`
- Exclusão não é suportada nesta versão (prompts são imutáveis em termos de tipo)

### Regras aplicáveis

- `rules/restful.md`: Versionamento, Problem Details, códigos HTTP corretos
- `rules/dotnet-architecture.md`: Service na Application, Controller na Services
- `rules/dotnet-testing.md`: xUnit, AAA pattern

## Critérios de Sucesso

- [x] CRUD completo funcionando para os 4 tipos de análise
- [x] Constraint de unicidade por `AnalysisType` respeitada
- [x] Endpoints protegidos por JWT (401 sem token)
- [x] Respostas seguem padrão Problem Details para erros
- [x] Mínimo 4 testes unitários passando
- [x] Teste de integração do fluxo completo passando
