# ── Stage 1: Build & Test ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files first for layer caching
COPY UserAuth.sln .
COPY UserAuth.Domain/UserAuth.Domain.csproj UserAuth.Domain/
COPY UserAuth.Application/UserAuth.Application.csproj UserAuth.Application/
COPY UserAuth.Infrastructure/UserAuth.Infrastructure.csproj UserAuth.Infrastructure/
COPY UserAuth.API/UserAuth.API.csproj UserAuth.API/
COPY UserAuth.Tests/UserAuth.Tests.csproj UserAuth.Tests/

# Restore packages
RUN dotnet restore

# Copy everything else
COPY . .

# Build entire solution
RUN dotnet build UserAuth.sln -c Release --no-restore

# Run unit & integration tests
RUN dotnet test UserAuth.Tests/UserAuth.Tests.csproj -c Release --no-build --logger "console;verbosity=minimal"

# ── Stage 2: Publish ─────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish UserAuth.API/UserAuth.API.csproj -c Release -o /app/publish --no-build

# ── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserAuth.API.dll"]
