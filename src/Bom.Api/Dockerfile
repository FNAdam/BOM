# Dockerfile untuk Bom.Api — dipakai saat deploy ke platform container (Render/Railway/Fly/dll).
# Build context HARUS root repo (folder yang berisi BOM.sln), karena project ini referensi
# Bom.Domain/Bom.Application/Bom.Infrastructure lewat path relatif ../.
#
# Build lokal (dari root repo):
#   docker build -f src/Bom.Api/Dockerfile -t bom-api .
#   docker run -p 8080:8080 -e PORT=8080 -e ConnectionStrings__Default="..." bom-api

# ---- Stage 1: build & publish ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BOM.sln ./
COPY src/Bom.Domain/Bom.Domain.csproj src/Bom.Domain/
COPY src/Bom.Application/Bom.Application.csproj src/Bom.Application/
COPY src/Bom.Infrastructure/Bom.Infrastructure.csproj src/Bom.Infrastructure/
COPY src/Bom.Api/Bom.Api.csproj src/Bom.Api/
RUN dotnet restore src/Bom.Api/Bom.Api.csproj

COPY src/ src/
RUN dotnet publish src/Bom.Api/Bom.Api.csproj -c Release -o /app --no-restore

# ---- Stage 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
# Platform PaaS (Render/Railway/dll) menyuntikkan $PORT saat runtime; Kestrel di-bind ke situ.
# Default 8080 dipakai kalau $PORT tidak di-set (mis. saat `docker run` manual).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet Bom.Api.dll"]
