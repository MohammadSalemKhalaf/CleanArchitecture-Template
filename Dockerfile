# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

# ---- Build -------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Restore first, from project files only, so this layer is reused until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Directory.Build.props src/BannedSymbols.txt src/
COPY src/TemplateApp.Domain/TemplateApp.Domain.csproj src/TemplateApp.Domain/
COPY src/TemplateApp.Application/TemplateApp.Application.csproj src/TemplateApp.Application/
COPY src/TemplateApp.Contracts/TemplateApp.Contracts.csproj src/TemplateApp.Contracts/
COPY src/TemplateApp.Infrastructure/TemplateApp.Infrastructure.csproj src/TemplateApp.Infrastructure/
COPY src/TemplateApp.Api/TemplateApp.Api.csproj src/TemplateApp.Api/
RUN dotnet restore src/TemplateApp.Api/TemplateApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/TemplateApp.Api/TemplateApp.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ---- Runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

COPY --from=build /app/publish .

# Non-root user provided by the base image. The app listens on 8080 (ASPNETCORE_HTTP_PORTS default).
USER $APP_UID
EXPOSE 8080

# The same image serves traffic and, with "--migrate", applies database migrations and exits.
ENTRYPOINT ["dotnet", "TemplateApp.Api.dll"]
