---
status: completed
parallelizable: false
blocked_by: ["12.0", "4.0", "6.0"]
---

<task_context>
<domain>frontend/pages</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>http_server</dependencies>
<unblocks>"14.0"</unblocks>
</task_context>

# Tarefa 13.0: Telas de Autenticação e Solicitação

## Visão Geral

Implementar as telas de login e criação de solicitação de análise no frontend. A tela de login permite autenticação com e-mail e senha. O formulário de solicitação permite informar URL do repositório, selecionar provedor, fornecer token e escolher tipos de análise. Corresponde às funcionalidades F08 do PRD (RF-36, RF-37) e às histórias HU-01, HU-02, HU-07.

<requirements>
- RF-36: Tela de login com autenticação própria
- RF-37: Formulário de criação de solicitação com validação de URL em tempo real
- Integração com API de autenticação (tarefa 4.0)
- Integração com API de solicitação (tarefa 6.0)
- Estética Cyber-Technical
</requirements>

## Subtarefas

- [x] 13.1 Implementar pagina de Login (`/login`): campos email e senha, botao de login, feedback de erro, redirecionamento pos-login
- [x] 13.2 Integrar login com API: `POST /api/v1/auth/login` → armazenar JWT → redirecionar para dashboard
- [x] 13.3 Implementar pagina de registro (opcional, link no login): campos email, senha, confirmacao
- [x] 13.4 Implementar pagina de criacao de solicitacao (`/requests/new`): campo URL, seletor de provedor (GitHub/Azure DevOps), campo token (opcional), checkboxes de tipos de analise
- [x] 13.5 Implementar validacao de URL em tempo real: regex HTTP/HTTPS, feedback visual imediato (icone verde/vermelho)
- [x] 13.6 Implementar selecao de tipos de analise: checkboxes para Obsolescencia, Seguranca, Observabilidade, Documentacao (minimo 1 selecionado)
- [x] 13.7 Integrar formulario com API: `POST /api/v1/analysis-requests` → redirecionamento para dashboard com nova solicitacao
- [x] 13.8 Implementar feedback visual: loading state no botao, mensagens de erro/sucesso estilizadas
- [x] 13.9 Implementar logout: limpar JWT, redirecionar para login

## Sequenciamento

- **Bloqueado por**: 12.0 (Setup Frontend), 4.0 (API Auth), 6.0 (API Solicitação)
- **Desbloqueia**: 14.0 (Dashboard de acompanhamento)
- **Paralelizável**: Não (depende de 12.0)

## Detalhes de Implementação

### Tela de Login

- Centralizada na tela, estilo Cyber-Technical
- Campo e-mail com ícone
- Campo senha com toggle visibility
- Botão "Entrar" com glow verde
- Link para registro
- Mensagem de erro em vermelho (#FF3131) para credenciais inválidas

### Formulário de Solicitação

- Campo URL com validação em tempo real (debounce 300ms)
- Dropdown/toggle para provedor: GitHub / Azure DevOps
- Campo token com máscara (tipo password)
- Grid de checkboxes para tipos de análise com ícones:
  - 🔄 Obsolescência
  - 🔒 Segurança
  - 📊 Observabilidade
  - 📄 Documentação
- Botão "Iniciar Análise" habilitado apenas quando form válido

### Integração com API

```typescript
// Login
const response = await apiClient.post('/api/v1/auth/login', { email, password });
localStorage.setItem('token', response.data.token);

// Criar solicitação
const response = await apiClient.post('/api/v1/analysis-requests', {
  repositoryUrl,
  provider,
  accessToken,
  selectedTypes
});
navigate(`/dashboard`);
```

## Critérios de Sucesso

- [x] Login funciona com credenciais validas (token armazenado)
- [x] Login exibe erro para credenciais invalidas
- [x] Validacao de URL em tempo real funciona (feedback visual)
- [x] Tipos de analise sao selecionaveis (minimo 1)
- [x] Solicitacao e criada via API com sucesso
- [x] Redirecionamento correto apos login e criacao
- [x] Estetica Cyber-Technical aplicada nas telas

## Conclusao

- [x] 13.0 Telas de Autenticacao e Solicitacao ✅ CONCLUIDA
  - [x] 13.1 Implementacao completada
  - [x] 13.2 Definicao da tarefa, PRD e tech spec validados
  - [x] 13.3 Analise de regras e conformidade verificadas
  - [x] 13.4 Revisao de codigo completada
  - [x] 13.5 Pronto para deploy
