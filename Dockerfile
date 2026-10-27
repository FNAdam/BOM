# Salinan dari src/Bom.Api/Dockerfile, khusus untuk Back4App Containers.
# Back4App hanya punya satu field "Root Directory" yang dipakai sekaligus sebagai lokasi
# file Dockerfile DAN build context — beda dari Render yang punya field terpisah
# (Dockerfile Path vs Root Directory). Supaya Back4App bisa ketemu Dockerfile-nya sambil
# tetap build dari root repo (dibutuhkan karena COPY di bawah referensi src/Bom.Domain dll
# yang relatif ke root), file ini taruh di root dengan isi identik ke src/Bom.Api/Dockerfile.
# Kalau src/Bom.Api/Dockerfile diubah, update juga file ini (Render tetap pakai yang di src/Bom.Api/).
#
# Build lokal (dari root repo):
#   docker build -f Dockerfile -t bom-api .
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
# Deklarasi statis supaya platform yang butuh port tetap di build-time (mis. Back4App) tahu
# ke port mana harus route traffic — harus sama dengan default fallback di ENTRYPOINT di bawah.
EXPOSE 8080
# Platform PaaS (Render/Railway/dll) menyuntikkan $PORT saat runtime; Kestrel di-bind ke situ.
# Default 8080 dipakai kalau $PORT tidak di-set (mis. saat `docker run` manual, atau platform
# yang tidak inject $PORT seperti Back4App — harus cocok dengan EXPOSE di atas).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet Bom.Api.dll"]
