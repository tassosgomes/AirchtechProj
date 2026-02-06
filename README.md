# Plataforma de Modernização de Software

## Visão geral

Este repositório contém a fundação da plataforma, com a API orquestradora, Worker genérico, mensageria e base de dados, além de um frontend mínimo para validação do Docker Compose.

## Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose

## Setup rápido

1. Copie o arquivo de variáveis de ambiente:

   - Copie .env.example para .env e ajuste os valores conforme necessário.

2. Build das soluções .NET:

   - `dotnet build ModernizationPlatform.API/ModernizationPlatform.API.sln`
   - `dotnet build ModernizationPlatform.Worker/ModernizationPlatform.Worker.sln`

3. Subir os serviços com Docker Compose:

   - `docker compose build`
   - `docker compose up`

## Endpoints de verificação

- API: http://localhost:5000/health
- RabbitMQ Management UI: http://localhost:15672
- PostgreSQL: localhost:5432
- Frontend: http://localhost:3000

## Estrutura de pastas

- ModernizationPlatform.API: solução da API orquestradora
- ModernizationPlatform.Worker: solução do worker genérico
- frontend: aplicação Node.js mínima para validação do compose
