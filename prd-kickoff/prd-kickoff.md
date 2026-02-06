# PRD – Plataforma de Modernização de Software

## 1. Visão Geral

Esta solução tem como objetivo criar uma **plataforma automatizada de modernização de software**, capaz de analisar repositórios de código-fonte, gerar um inventário técnico e produzir diagnósticos acionáveis sobre obsolescência, segurança, observabilidade e documentação.

A arquitetura é baseada em **orquestração + workers genéricos orientados por prompt**, utilizando o GitHub Copilot SDK como motor principal de análise.

---

## 2. Objetivos Estratégicos

* Criar um inventário contínuo do parque de software
* Reduzir esforço manual de análise técnica
* Padronizar diagnósticos e recomendações
* Escalar análises de forma paralela e resiliente
* Permitir evolução rápida via prompts, sem redeploy

---

## 3. Princípios de Arquitetura

* Prompt-driven, não code-driven
* Descoberta antes de análise
* Workers genéricos e stateless
* Contexto compartilhado versionado
* Resultados estruturados e auditáveis

---

## 4. Visão Macro da Arquitetura

Componentes principais:

* Frontend
* API Orquestradora
* Fila/Eventos
* Worker Genérico de Análise
* Catálogo de Prompts
* Inventário de Software

---

## 5. Frontend

### Responsabilidades

* Criar solicitações de análise
* Selecionar tipos de análise
* Acompanhar status e progresso
* Visualizar resultados e histórico

---

## 6. API Orquestradora

### Responsabilidades

* Criar e gerenciar solicitações
* Orquestrar pipeline de jobs
* Controlar estados e retries
* Consolidar resultados
* Expor dados ao frontend

### Estados de Solicitação

* CREATED
* DISCOVERY_RUNNING
* ANALYSIS_RUNNING
* CONSOLIDATING
* COMPLETED
* FAILED

---

## 7. Pipeline de Análise

### 7.1 Discovery

* Clonagem do repositório
* Detecção de linguagens
* Identificação de frameworks
* Listagem de dependências
* Mapeamento estrutural

Output: **Contexto Compartilhado**

### 7.2 Contexto Compartilhado

Artefato versionado e imutável contendo:

* Linguagens
* Frameworks
* Dependências
* Estrutura do repositório
* Metadados

---

## 8. Workers

### 8.1 Worker Genérico

Worker único responsável por executar qualquer análise.

### Parâmetros de Entrada

* Repositório
* Token (opcional)
* Contexto Compartilhado
* Prompt de análise
* Opções de execução

### Responsabilidades

* Executar análise via GitHub Copilot SDK
* Controlar timeout e isolamento
* Produzir saída estruturada
* Reportar status e logs

---

## 9. Análises

### Tipos Suportados

* Obsolescência
* Segurança
* Observabilidade
* Documentação
* Custom

Todas as análises:

* Consomem apenas o contexto
* Não dependem de outras análises

---

## 10. Fan-out de Jobs

Usado quando o domínio é grande.

Exemplos:

* Uma dependência por job
* Um módulo por job

Benefícios:

* Melhor uso de janela de contexto
* Paralelismo
* Retry granular

---

## 11. Catálogo de Prompts

### Características

* Prompts versionados
* Metadata associada
* Auditáveis

### Exemplo de Metadata

* id
* versão
* tipo de análise
* data de criação

---

## 12. Consolidação de Resultados

Responsável por:

* Normalizar outputs
* Correlacionar achados
* Classificar severidade
* Gerar visão unificada

---

## 13. Inventário de Software

### Conteúdo

* Repositórios analisados
* Tecnologias utilizadas
* Dependências
* Achados históricos
* Evolução no tempo

---

## 14. Observabilidade

* Logs estruturados
* Métricas por job
* Tracing por requestId

---

## 15. Segurança

* Tokens nunca persistidos
* Segredos via secret manager
* Isolamento por container
* Least privilege

---

## 16. Escalabilidade

* Workers stateless
* Execução em containers
* Escala horizontal
* Backpressure via fila

---

## 17. Riscos

* Limite de contexto de LLM
* Dependência de ferramentas externas
* Custo computacional

---

## 18. Métricas de Sucesso

* Tempo médio de análise
* Taxa de falhas
* Cobertura do inventário
* Aderência a padrões

---

## 19. Roadmap Inicial

1. API Orquestradora (base)
2. Worker Genérico
3. Discovery
4. Primeiros prompts
5. Inventário
6. Frontend

---

## 20. Considerações Finais

Esta plataforma é desenhada para evoluir continuamente, priorizando flexibilidade, escalabilidade e redução de esforço humano por meio de automação orientada por IA.
