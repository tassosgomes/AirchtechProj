# PRD – Plataforma de Modernização de Software

## Visão Geral

A **Plataforma de Modernização de Software** é uma solução on-premise que automatiza a análise técnica de repositórios de código-fonte. Ela gera inventários técnicos e diagnósticos acionáveis sobre obsolescência, segurança, observabilidade e documentação.

**Problema**: Hoje, a análise técnica de cada repositório é totalmente manual e leva de 3 a 5 dias, limitando a capacidade da equipe de arquitetura de manter visibilidade sobre o estado do parque de software.

**Solução**: Uma plataforma orquestrada por prompts, utilizando o GitHub Copilot SDK como motor de análise, capaz de processar um repositório completo em até 1 dia — reduzindo em até 80% o tempo de análise manual.

**Público-alvo**: Equipe de Arquitetura de Software, responsável por avaliar e manter o estado técnico do parque de software da organização.

---

## Objetivos

1. **Reduzir tempo de análise**: De 3–5 dias (manual) para ≤ 1 dia por repositório (automatizado)
2. **Criar inventário contínuo**: Manter visão atualizada de todas as tecnologias, dependências e estado técnico do parque de software
3. **Padronizar diagnósticos**: Garantir que toda análise siga critérios consistentes e reprodutíveis via prompts versionados
4. **Escalar análises**: Permitir execução paralela de múltiplas análises via workers stateless em containers
5. **Evoluir sem redeploy**: Permitir que novos tipos de análise sejam adicionados apenas criando/atualizando prompts

### Métricas de Sucesso

| Métrica | Meta |
|---|---|
| Tempo médio de análise completa por repositório | ≤ 24 horas |
| Taxa de falha de jobs | < 5% |
| Cobertura do parque no inventário | ≥ 90% em 6 meses |
| Aderência a padrões (diagnósticos consistentes) | ≥ 95% |

---

## Histórias de Usuário

### HU-01: Criar solicitação de análise
Como **arquiteto de software**, quero **criar uma solicitação de análise informando a URL do repositório e o provedor (GitHub ou Azure DevOps)** para que **a plataforma inicie automaticamente o pipeline de análise**.

### HU-02: Selecionar tipos de análise
Como **arquiteto de software**, quero **selecionar quais tipos de análise executar (Obsolescência, Segurança, Observabilidade, Documentação)** para que **eu foque nos aspectos mais relevantes para cada repositório**.

### HU-03: Acompanhar progresso
Como **arquiteto de software**, quero **acompanhar o status de cada etapa do pipeline em tempo real (Fila → Discovery → Análises → Consolidação) e ver a posição na fila** para que **eu saiba quando os resultados estarão disponíveis**.

### HU-04: Visualizar resultados consolidados
Como **arquiteto de software**, quero **visualizar os resultados de todas as análises em uma visão unificada com classificação de severidade** para que **eu possa priorizar ações de modernização**.

### HU-05: Consultar inventário de software
Como **arquiteto de software**, quero **consultar o inventário consolidado com tecnologias, dependências e achados históricos de todos os repositórios analisados** para que **eu tenha uma visão macro do estado do parque**.

### HU-06: Visualizar histórico de análises
Como **arquiteto de software**, quero **comparar resultados de análises ao longo do tempo para um mesmo repositório** para que **eu possa medir a evolução técnica**.

### HU-07: Autenticação
Como **arquiteto de software**, quero **me autenticar na plataforma com credenciais próprias** para que **apenas usuários autorizados acessem os dados de análise**.

---

## Funcionalidades Principais

### F01 – Autenticação e Controle de Acesso

A plataforma deve possuir autenticação própria na API para controlar o acesso dos usuários.

**Requisitos funcionais:**
1. RF-01: O sistema deve permitir criação de contas de usuário com e-mail e senha
2. RF-02: O sistema deve autenticar usuários via credenciais próprias (e-mail/senha)
3. RF-03: O sistema deve emitir e validar tokens de sessão (JWT ou similar)
4. RF-04: O sistema deve permitir revogação de sessões ativas

### F02 – API Orquestradora

Componente central responsável por receber solicitações, orquestrar o pipeline de análise e consolidar resultados.

**Requisitos funcionais:**
5. RF-05: O sistema deve aceitar solicitações de análise contendo URL do repositório, provedor (GitHub/Azure DevOps) e token de acesso
6. RF-06: O sistema deve validar a URL do repositório (formato HTTP/HTTPS válido) com feedback imediato
7. RF-07: O sistema deve enfileirar solicitações e processá-las uma por vez (um repositório de cada vez), sem limite de solicitações na fila
8. RF-08: O sistema deve gerenciar o ciclo de vida da solicitação com os estados: QUEUED → DISCOVERY_RUNNING → ANALYSIS_RUNNING → CONSOLIDATING → COMPLETED | FAILED
9. RF-09: O sistema deve orquestrar a execução sequencial das fases: Discovery → Análises (paralelas) → Consolidação
10. RF-10: O sistema deve implementar retry automático para jobs com falha (máximo configurável)
11. RF-11: O sistema deve expor endpoints REST para criação, consulta de status e obtenção de resultados

### F03 – Discovery (Descoberta)

Fase inicial que analisa o repositório e gera o Contexto Compartilhado.

**Requisitos funcionais:**
12. RF-12: O sistema deve clonar o repositório a partir de GitHub ou Azure DevOps usando o token fornecido. Se não fornecido considerar que é um repositório publico
13. RF-13: O sistema deve detectar linguagens de programação presentes no repositório
14. RF-14: O sistema deve identificar frameworks e bibliotecas utilizados
15. RF-15: O sistema deve listar todas as dependências (pacotes NuGet, npm, Maven, etc.)
16. RF-16: O sistema deve mapear a estrutura de diretórios e arquivos relevantes
17. RF-17: O sistema deve gerar um artefato de Contexto Compartilhado (versionado e imutável) contendo: linguagens, frameworks, dependências, estrutura e metadados

### F04 – Worker Genérico de Análise

Worker stateless e único, responsável por executar qualquer tipo de análise baseado em prompt.

**Requisitos funcionais:**
18. RF-18: O worker deve receber como entrada: repositório, contexto compartilhado, prompt de análise e opções de execução
19. RF-19: O worker deve executar a análise via GitHub Copilot SDK
20. RF-20: O worker deve produzir saída estruturada em formato padronizado (JSON)
21. RF-21: O worker deve respeitar timeout configurável por tipo de análise
22. RF-22: O worker deve reportar status (RUNNING, COMPLETED, FAILED) e logs à orquestradora
23. RF-23: O sistema deve suportar fan-out de jobs (ex.: uma dependência por job, um módulo por job) para repositórios grandes

### F05 – Catálogo de Prompts

Cadastro de prompts que orientam as análises, com 1 prompt por pilar de análise.

**Requisitos funcionais:**
24. RF-24: O sistema deve manter um cadastro de prompts (CRUD) com 1 prompt por pilar de análise
25. RF-25: Cada prompt deve conter metadados: id, tipo de análise e data de criação
26. RF-26: O sistema deve suportar os tipos de análise: Obsolescência, Segurança, Observabilidade e Documentação (4 pilares)
27. RF-27: O sistema deve permitir adicionar ou atualizar prompts sem necessidade de redeploy da aplicação

### F06 – Consolidação de Resultados

Fase que unifica os outputs de todas as análises.

**Requisitos funcionais:**
28. RF-28: O sistema deve normalizar os outputs de todos os workers em formato unificado
29. RF-29: O sistema deve correlacionar achados entre diferentes tipos de análise
30. RF-30: O sistema deve classificar achados por severidade (Crítico, Alto, Médio, Baixo, Informativo)
31. RF-31: O sistema deve gerar uma visão consolidada por repositório

### F07 – Inventário de Software

Base de dados com visão histórica e evolutiva do parque de software.

**Requisitos funcionais:**
32. RF-32: O sistema deve armazenar: repositórios analisados, tecnologias, dependências e achados por análise
33. RF-33: O sistema deve manter histórico de todas as análises por repositório
34. RF-34: O sistema deve permitir consulta e filtragem do inventário por tecnologia, dependência, severidade e data
35. RF-35: O sistema deve apresentar evolução temporal dos achados por repositório

### F08 – Frontend

Interface web com estética Cyber-Technical para interação do usuário com a plataforma.

**Requisitos funcionais:**
36. RF-36: O sistema deve apresentar tela de login com autenticação própria
37. RF-37: O sistema deve apresentar formulário de criação de solicitação com validação de URL em tempo real
38. RF-38: O sistema deve apresentar dashboard de acompanhamento com status de cada etapa do pipeline e posição na fila
39. RF-39: O sistema deve apresentar visualização de resultados consolidados com classificação de severidade
40. RF-40: O sistema deve apresentar tela de inventário com filtros e busca
41. RF-41: O sistema deve apresentar histórico e evolução temporal por repositório
42. RF-42: A interface deve seguir o guia de estilos Cyber-Technical como ponto de partida (paleta escura, tipografia técnica, componentes com estética de terminal/comando)

### F09 – Observabilidade da Plataforma

A plataforma utiliza **Sentry** como ferramenta de observabilidade.

**Requisitos funcionais:**
43. RF-43: O sistema deve emitir logs estruturados para todas as operações
44. RF-44: O sistema deve expor métricas por job (duração, status, tipo de análise)
45. RF-45: O sistema deve suportar tracing distribuído por requestId via Sentry
46. RF-46: O sistema deve reportar erros e exceções automaticamente ao Sentry

### F10 – Segurança da Plataforma

**Requisitos funcionais:**
47. RF-47: Tokens de acesso a repositórios nunca devem ser persistidos em banco de dados
48. RF-48: Segredos devem ser gerenciados via secret manager
49. RF-49: Workers devem executar em containers isolados
50. RF-50: O sistema deve aplicar princípio de least privilege em todos os componentes

---

## Experiência do Usuário

### Persona Principal

**Arquiteto de Software** — Profissional técnico sênior responsável por avaliar e evoluir o parque de software da organização. Precisa de visibilidade rápida e diagnósticos padronizados para tomar decisões de modernização.

### Fluxo Principal

1. Arquiteto faz login na plataforma
2. Cria nova solicitação: informa URL do repositório, seleciona provedor (GitHub/Azure DevOps), fornece token de acesso
3. Seleciona os tipos de análise desejados (Obsolescência, Segurança, Observabilidade, Documentação)
4. Acompanha o progresso no dashboard (Fila → Discovery → Análises → Consolidação)
5. Visualiza resultados consolidados com achados classificados por severidade
6. Consulta inventário para visão macro do parque

### Diretrizes de UI/UX

A interface segue o conceito **Cyber-Technical** como ponto de partida:

- **Paleta**: Fundo escuro (#0D0F12), cards em #161B22, destaque neon verde (#39FF14), alertas em vermelho (#FF3131)
- **Tipografia**: Sans-serif (Inter/Roboto) para interface; monospaced (JetBrains Mono/Fira Code) para dados técnicos
- **Componentes**: Cards sem sombra com bordas finas, status badges em formato pílula, botões com glow sutil
- **Feedback**: Validação instantânea de URL, spinners estilizados durante transições de etapa

> **Nota**: O guia de UI é um ponto de partida e poderá evoluir durante o desenvolvimento.

---

## Restrições Técnicas de Alto Nível

1. **Deploy on-premise**: A plataforma deve ser distribuída como Docker Compose, executável em infraestrutura do cliente
2. **Provedores de código**: Integração obrigatória com GitHub e Azure DevOps (via API/token)
3. **Motor de análise**: GitHub Copilot SDK — custos embutidos na assinatura do cliente
4. **Linguagem prioritária para Discovery**: .NET/C# como primeira linguagem suportada com profundidade; demais linguagens detectadas mas com análise básica
5. **Processamento sequencial**: Fila para solicitações ilimitadas, processamento de 1 repositório por vez
6. **Isolamento**: Workers devem executar em containers isolados
7. **Dados**: Código-fonte pode ser enviado para APIs externas (GitHub Copilot SDK) sem restrições de residência
8. **Persistência**: Tokens de acesso a repositórios nunca persistidos; apenas em memória durante a execução
9. **Observabilidade**: Sentry como ferramenta de monitoramento, tracing e report de erros

---

## Não-Objetivos (Fora de Escopo)

1. **SaaS / Multi-tenant**: A plataforma é exclusivamente on-premise nesta fase
2. **Integrações com ferramentas externas**: Sem integração com Jira, Slack, e-mail ou similares
3. **Suporte aprofundado a outras linguagens**: Fora .NET/C#, as demais linguagens terão apenas detecção básica no Discovery
4. **Acessibilidade avançada**: Sem requisitos WCAG específicos nesta versão
5. **Conformidade regulatória**: Sem requisitos de LGPD, SOC2 ou certificações específicas
6. **Usuários não técnicos**: A plataforma é direcionada exclusivamente para a equipe de arquitetura
7. **Edição de código**: A plataforma apenas analisa e diagnostica; não sugere ou aplica correções automaticamente

---

## Questões em Aberto

1. **Retenção de dados**: A definir — por quanto tempo os resultados e o inventário devem ser mantidos
2. **Tamanho máximo de repositório**: A definir — limite de tamanho/número de arquivos para clonagem e análise
3. **Estratégia de atualização on-premise**: A definir — como a plataforma será atualizada (rolling update, blue-green, manual)

---

*Documento criado em: 05/02/2026*
*Status: Rascunho para validação*
