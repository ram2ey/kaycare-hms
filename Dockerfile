# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY KayCare.sln .
COPY src/KayCare.API/KayCare.API.csproj                       src/KayCare.API/
COPY src/KayCare.Core/KayCare.Core.csproj                     src/KayCare.Core/
COPY src/KayCare.Infrastructure/KayCare.Infrastructure.csproj  src/KayCare.Infrastructure/
COPY src/KayCare.Tests/KayCare.Tests.csproj                   src/KayCare.Tests/

RUN dotnet restore KayCare.sln

# Copy remaining source and publish
COPY . .
RUN dotnet publish src/KayCare.API \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Render injects PORT at runtime (default 10000).
# Shell-form CMD expands ${PORT} when the container starts — not at build time.
EXPOSE 10000
CMD ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet KayCare.API.dll
