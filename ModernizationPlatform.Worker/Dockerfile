FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ./1-Services/ModernizationPlatform.Worker/ModernizationPlatform.Worker.csproj
RUN dotnet publish ./1-Services/ModernizationPlatform.Worker/ModernizationPlatform.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ModernizationPlatform.Worker.dll"]
