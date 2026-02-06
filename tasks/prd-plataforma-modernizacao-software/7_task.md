---
status: pending
parallelizable: false
blocked_by: ["2.0", "6.0"]
---

<task_context>
<domain>engine/discovery</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>external_apis</dependencies>
<unblocks>"9.0"</unblocks>
</task_context>

# Tarefa 7.0: Discovery Service

## Visão Geral

Implementar o serviço de Discovery que realiza a fase inicial do pipeline: clonar o repositório, analisar sua estrutura e gerar o Contexto Compartilhado (`SharedContext`). O foco de profundidade é em .NET/C# — outras linguagens são detectadas mas com análise básica. Corresponde à funcionalidade F03 do PRD (RF-12 a RF-17).

<requirements>
- RF-12: Clonar repositório de GitHub ou Azure DevOps via token (ou público se sem token)
- RF-13: Detectar linguagens de programação presentes
- RF-14: Identificar frameworks e bibliotecas
- RF-15: Listar todas as dependências (NuGet, npm, Maven, etc.)
- RF-16: Mapear a estrutura de diretórios e arquivos relevantes
- RF-17: Gerar artefato de Contexto Compartilhado (versionado e imutável)
- Timeout configurável (default: 10 min para clone)
- Retry com backoff exponencial para clone (máx. 3 tentativas via Polly)
- Foco de profundidade: .NET/C# (análise detalhada de .csproj, NuGet, etc.)
</requirements>

## Subtarefas

- [ ] 7.1 Criar `IDiscoveryService` na camada Application (conforme TechSpec): `ExecuteDiscoveryAsync(AnalysisRequest, CancellationToken)`
- [ ] 7.2 Implementar clone de repositório via `git` CLI (Process.Start) com suporte a GitHub e Azure DevOps
- [ ] 7.3 Implementar autenticação no clone: HTTPS com token embutido na URL (nunca logado); fallback para clone público se sem token
- [ ] 7.4 Implementar timeout configurável para clone (variável de ambiente, default 10 min)
- [ ] 7.5 Implementar retry com Polly: backoff exponencial, máx. 3 tentativas
- [ ] 7.6 Implementar detecção de linguagens: varredura de extensões de arquivo (.cs, .js, .ts, .py, .java, etc.) com contagem de linhas
- [ ] 7.7 Implementar detecção de frameworks .NET: parsear arquivos `.csproj` e `.sln` para identificar target framework, SDK e referências
- [ ] 7.8 Implementar listagem de dependências .NET: parsear `PackageReference` em `.csproj`, `packages.config`, `Directory.Build.props`
- [ ] 7.9 Implementar detecção básica para outras stacks: `package.json` (npm), `pom.xml` (Maven), `build.gradle`, `requirements.txt` (pip), `Gemfile`
- [ ] 7.10 Implementar mapeamento de estrutura de diretórios (árvore JSON com diretórios e arquivos relevantes, excluindo `bin/`, `obj/`, `node_modules/`, `.git/`)
- [ ] 7.11 Gerar e persistir `SharedContext` com: languages, frameworks, dependencies, directoryStructure, version
- [ ] 7.12 Limpar diretório temporário do clone após geração do contexto
- [ ] 7.13 Escrever testes unitários: detecção de linguagens, parsing de .csproj, parsing de package.json
- [ ] 7.14 Escrever teste de integração: clone de repositório público conhecido → geração de SharedContext

## Sequenciamento

- **Bloqueado por**: 2.0 (Entidade SharedContext), 6.0 (AnalysisRequest com URL e token)
- **Desbloqueia**: 9.0 (Orquestração usa SharedContext para publicar jobs)
- **Paralelizável**: Não (depende de 6.0)

## Detalhes de Implementação

### Fluxo do Discovery

```
1. Receber AnalysisRequest (URL + token + provider)
2. Criar diretório temporário
3. Clonar repositório (git clone com token via HTTPS)
4. Varrer arquivos: detectar linguagens, frameworks, dependências
5. Mapear estrutura de diretórios
6. Gerar SharedContext (JSON) e persistir no banco
7. Limpar diretório temporário
8. Retornar SharedContext
```

### Detecção de Linguagens

| Extensão | Linguagem |
|---|---|
| `.cs` | C# |
| `.csproj`, `.sln` | .NET |
| `.js`, `.jsx` | JavaScript |
| `.ts`, `.tsx` | TypeScript |
| `.py` | Python |
| `.java` | Java |
| `.go` | Go |
| `.rb` | Ruby |

### Detecção Profunda (.NET/C#)

Para projetos .NET, o Discovery deve extrair:
- Target Framework (ex.: `net8.0`)
- SDK type (ex.: `Microsoft.NET.Sdk.Web`)
- Todos os `PackageReference` com versão
- Referências entre projetos (`ProjectReference`)
- Configurações de build relevantes

### Formato do SharedContext

```json
{
  "id": "uuid",
  "requestId": "uuid",
  "version": 1,
  "languages": ["C#", "JavaScript"],
  "frameworks": [
    { "name": "ASP.NET Core", "version": "8.0", "type": "Web" }
  ],
  "dependencies": [
    { "name": "Newtonsoft.Json", "version": "13.0.3", "type": "NuGet" }
  ],
  "directoryStructure": { ... },
  "createdAt": "2026-02-05T10:00:00Z"
}
```

### Regras aplicáveis

- `rules/dotnet-architecture.md`: Service na 2-Application, implementação na 4-Infra
- `rules/dotnet-libraries-config.md`: Polly para retry
- `rules/dotnet-logging.md`: Logs estruturados para cada etapa do Discovery

## Critérios de Sucesso

- [ ] Clone de repositório público funciona sem token
- [ ] Clone com token funciona para GitHub e Azure DevOps
- [ ] Token nunca aparece em logs
- [ ] Detecção de linguagens identifica corretamente pelo menos 5 linguagens
- [ ] Parsing de `.csproj` extrai frameworks, dependências e target framework
- [ ] SharedContext é persistido corretamente no banco
- [ ] Timeout e retry funcionam conforme configurado
- [ ] Diretório temporário é limpo após execução
- [ ] Mínimo 6 testes unitários passando
- [ ] Teste de integração com repo público passando
