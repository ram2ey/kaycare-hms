# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY KayCare.sln .
COPY src/KayCare.API/KayCare.API.csproj          src/KayCare.API/
COPY src/KayCare.Core/KayCare.Core.csproj        src/KayCare.Core/
COPY src/KayCare.Infrastructure/KayCare.Infrastructure.csproj src/KayCare.Infrastructure/

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

# Railway injects PORT env var; ASP.NET Core reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "KayCare.API.dll"]
