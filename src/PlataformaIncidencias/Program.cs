using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Hubs;
using PlataformaIncidencias.Services;
using PlataformaIncidencias.Services.Local;
using PlataformaIncidencias.Services.Real;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var puertoRender = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(puertoRender, out var puerto) && puerto > 0)
{
    builder.WebHost.ConfigureKestrel(opciones => opciones.ListenAnyIP(puerto));
}

builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
#pragma warning disable ASPDEPR005
    opciones.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
    opciones.KnownIPNetworks.Clear();
    opciones.KnownProxies.Clear();
});

var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(cadenaConexion));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(opciones =>
    {
        opciones.SignIn.RequireConfirmedAccount = false;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

RegistrarIntegraciones(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.MapHub<ActualizacionHub>("/hubs/actualizaciones");

await Semilla.InicializarAsync(app.Services);

app.Run();

static void RegistrarIntegraciones(IServiceCollection servicios, IConfiguration configuracion)
{
    var modo = configuracion["Integrations:Modo"] ?? "local";

    if (!string.Equals(modo, "real", StringComparison.OrdinalIgnoreCase))
    {
        servicios.AddScoped<IBuscadorIncidencias, BuscadorLocal>();
        servicios.AddScoped<ICacheListado, CacheLocal>();
        servicios.AddScoped<INotificadorEnTiempoReal, NotificadorLocal>();
        return;
    }

    var appId = configuracion["Algolia:ApplicationId"]
        ?? throw new InvalidOperationException("Falta la variable de entorno Algolia:ApplicationId.");
    var claveAdmin = configuracion["Algolia:AdminApiKey"]
        ?? throw new InvalidOperationException("Falta la variable de entorno Algolia:AdminApiKey.");
    var cluster = configuracion["PieHost:Cluster"]
        ?? throw new InvalidOperationException("Falta la variable de entorno PieHost:Cluster.");
    var cadenaRedis = configuracion["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Falta la variable de entorno Redis:ConnectionString.");

    servicios.AddHttpClient<IBuscadorIncidencias, BuscadorAlgolia>(cliente =>
    {
        cliente.BaseAddress = new Uri($"https://{appId}-dsn.algolia.net/");
        cliente.DefaultRequestHeaders.Add("X-Algolia-Application-Id", appId);
        cliente.DefaultRequestHeaders.Add("X-Algolia-API-Key", claveAdmin);
        cliente.Timeout = TimeSpan.FromSeconds(5);
    });

    var opcionesRedis = ConfigurationOptions.Parse(cadenaRedis);
    opcionesRedis.AbortOnConnectFail = false;
    servicios.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(opcionesRedis));
    servicios.AddScoped<ICacheListado, CacheRedis>();

    servicios.AddHttpClient<INotificadorEnTiempoReal, NotificadorPieHost>(cliente =>
    {
        cliente.BaseAddress = new Uri($"https://{cluster}.piesocket.com/");
        cliente.Timeout = TimeSpan.FromSeconds(5);
    });
}
