using System.Text.Json.Serialization;
using Bom.Application.Services;
using Bom.Application.Validators;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "BomCors";

// --- Services ---
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=bom;Username=bom;Password=bompass";

builder.Services.AddDbContext<BomDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// Layanan domain BOM (interface di Application, implementasi di Infrastructure).
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<ICostingService, CostingService>();
builder.Services.AddScoped<IMrpService, MrpService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum diserialisasi sebagai string, bukan angka.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Validasi request kaya (FluentValidation) + auto-validation level MVC (SharpGrip):
// request invalid balas 400 ValidationProblemDetails, melengkapi DataAnnotations yang ada.
builder.Services.AddValidatorsFromAssemblyContaining<UpsertItemValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Origin yang diizinkan diambil dari konfigurasi (Cors:AllowedOrigins), bukan hardcode,
// sehingga origin produksi (mis. GitHub Pages) bisa ditambah lewat appsettings/env var
// tanpa mengubah kode. Default dev (Vite) tetap disediakan sebagai fallback.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:5174"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// CATATAN: sengaja TIDAK memanggil db.Database.Migrate() saat startup.
// Migration di-apply manual oleh user (butuh DB hidup), dan agar `dotnet ef migrations add`
// serta host startup tidak bergantung pada koneksi database.

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection dimatikan untuk pengembangan (HTTP only) agar mulus dengan React dev server.
app.UseCors(CorsPolicy);

app.MapControllers();

app.Run();

// Dibuat publik & partial agar WebApplicationFactory<Program> dapat memakainya sebagai entry point
// pada test level-HTTP (Microsoft.AspNetCore.Mvc.Testing).
public partial class Program { }
