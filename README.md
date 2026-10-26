# BOM - Bill of Materials (F&B) — Fase 1: Fondasi & Master Data

Aplikasi Bill of Materials untuk produk minuman F&B. **Fase 1** mencakup fondasi
arsitektur dan CRUD master data: Items, Item Categories, UOM (satuan ukur),
Suppliers, dan Item-Supplier Prices.

## Stack

| Layer     | Teknologi                                                            |
| --------- | ------------------------------------------------------------------- |
| Backend   | ASP.NET Core Web API (net10.0), EF Core 10, Npgsql, PostgreSQL 17    |
| Frontend  | React + TypeScript + Vite, react-router-dom                         |
| Database  | PostgreSQL (snake_case via EFCore.NamingConventions)                |

### Versi paket kunci

- `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3**
- `Microsoft.EntityFrameworkCore` (Design/Relational) **10.0.4**
- `EFCore.NamingConventions` **10.0.1** (snake_case otomatis)
- `Swashbuckle.AspNetCore` **10.2.3** (Swagger UI di Development)

## Struktur solusi

```
BOM.sln
src/
  Bom.Domain/          Entities + Enums (tanpa dependency)
  Bom.Application/     DTOs (records, DataAnnotations)
  Bom.Infrastructure/  BomDbContext, Configurations, Seed, Migrations, DesignTimeFactory
  Bom.Api/             Program.cs, Controllers, appsettings, launchSettings
tests/
  Bom.Tests/           xUnit smoke test (Domain)
web/                   Frontend Vite React-TS
docker-compose.yml     PostgreSQL 17
```

## Cara menjalankan

Prasyarat: .NET SDK 10, Node 24+, Docker (untuk PostgreSQL).

### 1. Jalankan PostgreSQL

```powershell
docker compose up -d
```

### 2. Terapkan migration (buat schema + seed)

```powershell
dotnet ef database update --project src/Bom.Infrastructure --startup-project src/Bom.Api
```

> Migration `InitialCreate` **sudah dibuat** di `src/Bom.Infrastructure/Migrations`,
> namun **belum di-apply** ke database (butuh DB hidup). Langkah ini yang menerapkannya.

### 3. Jalankan API

```powershell
dotnet run --project src/Bom.Api
```

- API: <http://localhost:5099>
- Swagger UI (Development): <http://localhost:5099/swagger>

### 4. Jalankan frontend

```powershell
cd web
npm install
npm run dev
```

- Frontend: <http://localhost:5173>

Base URL API diatur via `web/.env` (`VITE_API_URL=http://localhost:5099`).

## Daftar endpoint

Semua resource menyediakan CRUD standar (`GET` list, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`):

| Resource            | Base route                     |
| ------------------- | ------------------------------ |
| Items               | `/api/items`                   |
| Item Categories     | `/api/item-categories`         |
| UOM                 | `/api/uoms`                    |
| Suppliers           | `/api/suppliers`               |
| Item-Supplier Price | `/api/item-supplier-prices`    |

Catatan perilaku:

- Enum diserialisasi sebagai **string** (mis. `"RawMaterial"`, `"Weight"`, `"Standard"`).
- `GET /{id}` membalas **404** jika data tidak ada.
- `POST` membalas **201 Created**.
- Pelanggaran unique (mis. `Code` duplikat) membalas **409 Conflict**.
- `DELETE` yang melanggar foreign key (data masih direferensikan) membalas **409 Conflict** dengan pesan jelas.
- FK yang tidak valid saat create/update membalas **400** dengan pesan.

## Data seed

- **Item Categories**: Bahan Baku, Kemasan, Semi-finished, Produk Jadi.
- **UOM**: kg, g, L, ml, pcs (dengan `FactorToBase` ke satuan dasar dimensi masing-masing).

## Keputusan desain

- **snake_case** di database memakai `EFCore.NamingConventions` (kompatibel EF Core 10),
  di-set via `UseSnakeCaseNamingConvention()` pada Program.cs dan DesignTimeDbContextFactory.
- Enum disimpan sebagai **teks** di database agar mudah dibaca.
- Presisi: uang/harga = `numeric(18,4)`; factor/density/qty konversi = `numeric(18,6)`.
- Delete behavior: `Item -> ItemSupplierPrice` **Cascade**; FK lain (UOM, Supplier, Category) **Restrict**.
- **Tidak ada** auto-migrate saat startup — schema di-apply manual (langkah 2) agar host
  dapat start dan `dotnet ef migrations add` berjalan tanpa DB hidup.
- HTTP-only untuk dev (HTTPS redirection dinonaktifkan) agar mulus dengan Vite dev server.

## Verifikasi (offline, tanpa DB)

- `dotnet build` (root) — sukses.
- `dotnet ef migrations add InitialCreate ...` — migration + snapshot terbuat.
- `cd web; npm install; npm run build` — sukses.

`dotnet ef database update` dan pengujian runtime end-to-end membutuhkan PostgreSQL
(via Docker) dan merupakan langkah pengguna.
