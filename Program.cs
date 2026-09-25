// Program.cs
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

using EcommerceApp.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar licencia QuestPDF (gratuita para uso educativo/opensource)
QuestPDF.Settings.License = LicenseType.Community;

// Cargar User Secrets explícitamente en Development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// La connection string real NUNCA esta en appsettings*.json.
//   Local  -> .NET User Secrets      (dotnet user-secrets set "ConnectionStrings:DefaultConnection" ...)
//   Render -> variable de entorno   ConnectionStrings__DefaultConnection
// Se avisa por stderr en vez de tirar excepcion: las herramientas de diseno de
// EF Core (dotnet ef) arrancan el host para resolver el DbContext, y un throw
// aqui las dejaria sin poder funcionar.
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    Console.Error.WriteLine(
        "[AVISO CRITICO] No se encontro ConnectionStrings:DefaultConnection. " +
        "Definicla en User Secrets (local) o en la variable de entorno " +
        "ConnectionStrings__DefaultConnection (Render). Sin ella fallara el primer acceso a la base de datos.");
}

builder.Services.AddSingleton<StoreConfigService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Persistir las claves de DataProtection en Postgres. Sin esto las claves se
// guardan en /root/.aspnet/DataProtection-Keys dentro del contenedor y se pierden
// en cada deploy, invalidando todas las cookies de sesion. SetApplicationName
// fija el proposito del cifrado: si cambia, las claves actuales dejan de servir.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("EcommerceApp");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(defaultConnection, npgsql =>
    {
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Detras de un proxy inverso (Render / Cloudflare): sin esto ASP.NET ve http://,
// la cookie de Identity no se marca Secure y UseHttpsRedirection no puede
// resolver el puerto https. Debe ser el PRIMER middleware del pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Aplicar migraciones de EF Core pendientes.
// Se hace al arrancar (y no en el build de Docker) porque Render no expone las
// variables de entorno del servicio como build args. Migrate() es NO destructivo:
// solo aplica las migraciones que aun no estan en __EFMigrationsHistory.
try
{
    using var migrationScope = app.Services.CreateScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pendingMigrations = (await migrationDb.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Count > 0)
    {
        app.Logger.LogInformation("Aplicando {Count} migracion(es) pendiente(s): {Migrations}",
            pendingMigrations.Count, string.Join(", ", pendingMigrations));
        await migrationDb.Database.MigrateAsync();
    }
    else
    {
        app.Logger.LogInformation("Base de datos al dia, no hay migraciones pendientes.");
    }
}
catch (Exception ex)
{
    var migrationLogger = app.Services.GetRequiredService<ILogger<Program>>();
    migrationLogger.LogError(ex, "Error al verificar/aplicar migraciones de EF Core");
}

// Seed roles + 2 usuarios demo (Admin/User) - idempotente
try
{
    await DbSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error en DbSeeder");
}

app.Run();
