---
status: pending
parallelizable: true
blocked_by: ["2.0"]
---

<task_context>
<domain>engine/auth</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>database</dependencies>
<unblocks>"5.0", "6.0", "13.0"</unblocks>
</task_context>

# Tarefa 4.0: Módulo de Autenticação

## Visão Geral

Implementar autenticação própria na API com registro de usuário, login via e-mail/senha, emissão de JWT e revogação de sessão. Corresponde à funcionalidade F01 do PRD (RF-01 a RF-04) e às histórias HU-07.

<requirements>
- RF-01: Permitir criação de contas de usuário com e-mail e senha
- RF-02: Autenticar usuários via credenciais próprias (e-mail/senha)
- RF-03: Emitir e validar tokens de sessão (JWT)
- RF-04: Permitir revogação de sessões ativas
- Senhas devem ser armazenadas com hash seguro (bcrypt ou similar)
- JWT deve conter claims: userId, email, roles
- Endpoints protegidos devem exigir token válido (middleware)
</requirements>

## Subtarefas

- [ ] 4.1 Instalar pacotes: `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`
- [ ] 4.2 Criar `JwtOptions` (Secret, Issuer, Audience, ExpirationMinutes) em configuração
- [ ] 4.3 Criar `IAuthService` na camada Application com métodos: `RegisterAsync`, `LoginAsync`, `RevokeAsync`
- [ ] 4.4 Implementar `AuthService`: hash de senha com BCrypt, geração de JWT, validação de credenciais
- [ ] 4.5 Implementar mecanismo de revogação: lista de tokens revogados (in-memory ou tabela `revoked_tokens`)
- [ ] 4.6 Criar `AuthController` com endpoints: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/revoke`
- [ ] 4.7 Criar DTOs: `RegisterRequest` (email, password), `LoginRequest` (email, password), `LoginResponse` (token, expiresAt)
- [ ] 4.8 Implementar validação com FluentValidation: e-mail válido, senha com mínimo de 8 caracteres
- [ ] 4.9 Configurar middleware JWT no `Program.cs` (autenticação + autorização)
- [ ] 4.10 Aplicar atributo `[Authorize]` nos controllers que exigem autenticação
- [ ] 4.11 Escrever testes unitários: geração de JWT, validação de senha, registro duplicado, login inválido
- [ ] 4.12 Escrever teste de integração: fluxo completo register → login → acesso autenticado → revoke

## Sequenciamento

- **Bloqueado por**: 2.0 (Entidade User e DbContext)
- **Desbloqueia**: 5.0 (Prompts — endpoints protegidos), 6.0 (Solicitação — endpoints protegidos), 13.0 (Frontend Login)
- **Paralelizável**: Sim — pode executar em paralelo com 5.0 (após 2.0)

## Detalhes de Implementação

### Endpoints (conforme TechSpec)

| Método | Path | Descrição | Auth |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Criar conta (email + senha) | Público |
| `POST` | `/api/v1/auth/login` | Autenticar e receber JWT | Público |
| `POST` | `/api/v1/auth/revoke` | Revogar sessão ativa | Autenticado |

### Fluxo de Autenticação

1. **Registro**: Receber e-mail + senha → validar → hash senha → persistir `User` → retornar 201
2. **Login**: Receber e-mail + senha → buscar User → verificar hash → gerar JWT → retornar token
3. **Revogação**: Receber token → adicionar à lista de revogados → retornar 204
4. **Middleware**: Validar JWT em cada request → verificar se não está revogado → popular `HttpContext.User`

### Respostas de Erro (RFC 9457 Problem Details)

- 400: Dados inválidos (email formato errado, senha fraca)
- 401: Credenciais inválidas
- 409: E-mail já cadastrado

### Regras aplicáveis

- `rules/restful.md`: Versionamento `/api/v1/`, Problem Details RFC 9457
- `rules/dotnet-coding-standards.md`: Nomenclatura, async/await
- `rules/dotnet-testing.md`: xUnit, AwesomeAssertions, AAA pattern

## Critérios de Sucesso

- [ ] Registro cria usuário com senha hashificada no banco
- [ ] Login retorna JWT válido com claims corretos
- [ ] Token expirado é rejeitado pelo middleware
- [ ] Token revogado é rejeitado pelo middleware
- [ ] E-mail duplicado retorna 409 com Problem Details
- [ ] Credenciais inválidas retornam 401
- [ ] Mínimo 6 testes unitários passando
- [ ] Teste de integração do fluxo completo passando
