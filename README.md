# Plataforma de Incidencias — Ensayo del parcial

Proyecto de ensayo para el parcial de **Ramas, fusiones y despliegue en Render** (90 min, 20 puntos).

Stack: ASP.NET Core MVC + Identity, EF Core y SQLite. Servicios: búsqueda en Algolia, caché en Redis y tiempo real con PieHost.

## Credenciales de ensayo

Cuenta de prueba (sembrada automáticamente):

```
supervisor@operaciones.local / Supervisor123!
```

**Nunca** se suben claves al repositorio. Las credenciales reales viven en
user-secrets (local) y en las variables de entorno del servicio de Render.

```bash
dotnet user-secrets set "PieHost:Key" "<valor>" --project src/PlataformaIncidencias
dotnet user-secrets list --project src/PlataformaIncidencias
```

## Modos de integración

La clave `Integrations:Modo` decide qué implementación se registra en DI:

| Valor | Algolia | Redis | Tiempo real |
|---|---|---|---|
| `local` (defecto) | `BuscadorLocal` | `CacheLocal` (IMemoryCache) | `NotificadorLocal` (SignalR) |
| `real` | `BuscadorAlgolia` | `CacheRedis` | `NotificadorPieHost` (REST de PieSocket) |

Variables requeridas cuando `Integrations:Modo = real`:

```
Integrations__Modo=real
Algolia__ApplicationId=...
Algolia__AdminApiKey=...
Algolia__Indice=incidencias
Redis__ConnectionString=...
PieHost__Cluster=free.blr2
PieHost__Key=...
PieHost__Secret=...
PieHost__Channel=incidencias
```

`PieHost:Secret` es **solo de servidor**. Al navegador únicamente se expone
`PieHost:Key`, que es la clave pública que el SDK de cualquier cliente necesita
para conectarse.

## Ejecutar

```bash
dotnet run --project src/PlataformaIncidencias --urls http://localhost:5077
```

Pantalla: `/Operaciones/Incidencias`
