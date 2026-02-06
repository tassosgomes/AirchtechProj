---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>frontend/setup</domain>
<type>implementation</type>
<scope>configuration</scope>
<complexity>medium</complexity>
<dependencies>none</dependencies>
<unblocks>"13.0", "14.0", "15.0"</unblocks>
</task_context>

# Tarefa 12.0: Setup Frontend React/Vite + Tema Cyber-Technical

## Visão Geral

Criar o projeto frontend SPA com React, TypeScript e Vite, implementando o tema Cyber-Technical como base visual. Inclui setup do projeto, sistema de design (cores, tipografia, componentes base), roteamento, serviço de API e estrutura de pastas. Corresponde parcialmente à funcionalidade F08 do PRD (RF-42).

<requirements>
- RF-42: Interface deve seguir guia de estilos Cyber-Technical (paleta escura, tipografia técnica, componentes com estética de terminal/comando)
- React + TypeScript + Vite
- Tema Cyber-Technical: fundo #0D0F12, cards #161B22, destaque neon verde #39FF14, alertas #FF3131
- Tipografia: Sans-serif (Inter/Roboto) para interface; monospaced (JetBrains Mono/Fira Code) para dados técnicos
- Componentes base: Cards, Badges, Buttons, Input, Layout
- Dockerfile para build de produção (nginx)
</requirements>

## Subtarefas

- [ ] 12.1 Criar projeto React/TypeScript com Vite: `npm create vite@latest modernization-web -- --template react-ts`
- [ ] 12.2 Instalar dependências: `react-router-dom`, `axios`, `lucide-react` (ícones), `@fontsource/inter`, `@fontsource/jetbrains-mono`
- [ ] 12.3 Configurar tema Cyber-Technical em CSS/variáveis: paleta de cores, tipografia, espaçamentos
- [ ] 12.4 Criar componentes base reutilizáveis: `Card`, `Button`, `Badge` (severity), `Input`, `TextArea`, `Spinner`, `StatusBadge`
- [ ] 12.5 Criar layout principal: `AppLayout` com sidebar/header dark theme, área de conteúdo
- [ ] 12.6 Configurar React Router com rotas: `/login`, `/dashboard`, `/requests/new`, `/requests/:id`, `/inventory`, `/inventory/:id/timeline`
- [ ] 12.7 Criar serviço API (`apiClient.ts`): instância axios com baseURL configurável, interceptor para JWT (header Authorization)
- [ ] 12.8 Criar hook `useAuth`: gerenciar token JWT no localStorage, login/logout, verificar autenticação
- [ ] 12.9 Criar guard de rota autenticada (`PrivateRoute`): redirecionar para `/login` se não autenticado
- [ ] 12.10 Criar `Dockerfile` para frontend: build com Node → serve com nginx
- [ ] 12.11 Configurar proxy de desenvolvimento (vite.config.ts) para API local (porta 5000)

## Sequenciamento

- **Bloqueado por**: 1.0 (Docker Compose para integração)
- **Desbloqueia**: 13.0 (Telas de Auth), 14.0 (Dashboard), 15.0 (Inventário)
- **Paralelizável**: Sim — pode executar em paralelo com todo o backend (2.0 a 11.0)

## Detalhes de Implementação

### Paleta Cyber-Technical (conforme PRD/TechSpec)

| Token | Cor | Uso |
|---|---|---|
| `--bg-primary` | `#0D0F12` | Fundo principal |
| `--bg-secondary` | `#161B22` | Cards, painéis |
| `--accent-green` | `#39FF14` | Destaque, sucesso, ações primárias |
| `--accent-red` | `#FF3131` | Alertas, erros, severidade Critical |
| `--accent-yellow` | `#FFD700` | Warnings, severidade High |
| `--accent-blue` | `#00BFFF` | Links, informativo |
| `--text-primary` | `#E6EDF3` | Texto principal |
| `--text-secondary` | `#8B949E` | Texto secundário |
| `--border` | `#30363D` | Bordas de cards |

### Tipografia

- **Interface**: Inter (sans-serif), 14px base
- **Dados técnicos**: JetBrains Mono (monospaced), 13px
- **Títulos**: Inter Bold, 18-24px

### Estrutura de Pastas

```
frontend/modernization-web/
├── src/
│   ├── components/       # Componentes reutilizáveis (Card, Badge, Button...)
│   ├── pages/            # Páginas por rota (Login, Dashboard, Inventory...)
│   ├── services/         # API client, auth service
│   ├── hooks/            # Custom hooks (useAuth, useApi...)
│   ├── theme/            # Variáveis CSS, global styles
│   ├── types/            # TypeScript types/interfaces
│   └── App.tsx
├── package.json
├── vite.config.ts
├── tsconfig.json
└── Dockerfile
```

### Regras aplicáveis

- `docs-ui/guia-ui.md`: Referência principal para estética Cyber-Technical
- `rules/react-logging.md`: Sentry React SDK (será adicionado na tarefa 16.0)

## Critérios de Sucesso

- [ ] Projeto inicializa sem erros (`npm run dev`)
- [ ] Tema Cyber-Technical aplicado (fundo escuro, tipografia correta, cores neon)
- [ ] Componentes base renderizam corretamente
- [ ] Roteamento funciona para todas as rotas definidas
- [ ] API client configurado com interceptor JWT
- [ ] Guard de rota redireciona para login quando não autenticado
- [ ] Build de produção funciona (`npm run build`)
- [ ] Dockerfile gera imagem funcional com nginx
