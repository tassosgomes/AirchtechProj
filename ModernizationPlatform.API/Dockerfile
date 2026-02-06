FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ./1-Services/ModernizationPlatform.API/ModernizationPlatform.API.csproj
RUN dotnet publish ./1-Services/ModernizationPlatform.API/ModernizationPlatform.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ModernizationPlatform.API.dll"]
