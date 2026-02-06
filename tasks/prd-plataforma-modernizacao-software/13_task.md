---
status: pending
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

- [ ] 13.1 Implementar página de Login (`/login`): campos email e senha, botão de login, feedback de erro, redirecionamento pós-login
- [ ] 13.2 Integrar login com API: `POST /api/v1/auth/login` → armazenar JWT → redirecionar para dashboard
- [ ] 13.3 Implementar página de registro (opcional, link no login): campos email, senha, confirmação
- [ ] 13.4 Implementar página de criação de solicitação (`/requests/new`): campo URL, seletor de provedor (GitHub/Azure DevOps), campo token (opcional), checkboxes de tipos de análise
- [ ] 13.5 Implementar validação de URL em tempo real: regex HTTP/HTTPS, feedback visual imediato (ícone verde/vermelho)
- [ ] 13.6 Implementar seleção de tipos de análise: checkboxes para Obsolescência, Segurança, Observabilidade, Documentação (mínimo 1 selecionado)
- [ ] 13.7 Integrar formulário com API: `POST /api/v1/analysis-requests` → redirecionamento para dashboard com nova solicitação
- [ ] 13.8 Implementar feedback visual: loading state no botão, mensagens de erro/sucesso estilizadas
- [ ] 13.9 Implementar logout: limpar JWT, redirecionar para login

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

- [ ] Login funciona com credenciais válidas (token armazenado)
- [ ] Login exibe erro para credenciais inválidas
- [ ] Validação de URL em tempo real funciona (feedback visual)
- [ ] Tipos de análise são selecionáveis (mínimo 1)
- [ ] Solicitação é criada via API com sucesso
- [ ] Redirecionamento correto após login e criação
- [ ] Estética Cyber-Technical aplicada nas telas
