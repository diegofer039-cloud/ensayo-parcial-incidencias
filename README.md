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

## Despliegue

| | |
|---|---|
| URL pública | https://plataforma-incidencias-tt11.onrender.com |
| Servicio | `plataforma-incidencias-tt11` (Render Blueprint, plan free, región oregon) |
| Commit desplegado | `6c4057d` (`main`) |
| Build | `runtime: docker` (Render no tiene runtime nativo para .NET) |
| Pantalla | `/Operaciones/Incidencias` |

SQLite apunta a `/tmp/app.db` y `Semilla.InicializarAsync` ejecuta migración y
sembrado antes de aceptar tráfico, por lo que cada deploy arranca con los mismos
9 registros y los mismos identificadores.

Configuración por variables de entorno, definidas en `render.yaml`. Las claves van
como `sync: false` y se cargan una sola vez en el dashboard.

## Fusiones y conflictos

```
git log --graph --oneline --all
```

| | Rama | PR | Resolución |
|---|---|---|---|
| C0 | `main` (ancestro común) | — | `e51253f` |
| A | `feature/busqueda-algolia` | #1 | `dc38e8a` → merge `a68b81c` |
| B | `feature/cache-redis` | #2 | `43eb4bb` → merge `2f602aa`, conflicto `6156b2d` |
| C | `feature/websocket-piehost` | #3 | `d833be0` → merge `82259dd`, conflicto `e9c5d29` |

Orden de integración A → B → C, con `git merge main` en cada rama, sin force push
ni squash. Los dos commits de resolución quedan en el historial.

## Pruebas

Ejecutadas en local (`modo = real`) y en Render.

| # | Prueba | Resultado esperado | Resultado |
|---|---|---|---|
| 1 | Listado general | 8 incidencias abiertas, la #8 (cerrada) excluida | 8 filas: 9,7,6,5,4,3,2,1 |
| 2 | Título compartido | "Incidencias abiertas en tiempo real" | correcto |
| 3 | Algolia `?q=freno` | 1 resultado | 1 fila (id 1) |
| 4 | Algolia `?q=puerto` | solo la abierta #9 | 1 fila (id 9), la #8 filtrada |
| 5 | Algolia `?q=carga` | **0 resultados** (texto de la cerrada #8) | 0 filas |
| 6 | Algolia texto inexistente | 0 resultados | 0 filas |
| 7 | Caché | primera lectura `Base de datos`, siguientes `Redis` | `Base de datos` → `Redis` → `Redis` |
| 8 | Invalidación | log al cerrar, después `Base de datos` | `invalidada` → `Base de datos` |
| 9 | PieHost | evento **después** de persistir | `publicado en PieHost (Id=9, Estado=Cerrada)` |
| 10 | Dos sesiones | la otra quita la fila sin recargar | `navigationType: navigate`, fila eliminada |
| 11 | Consola sesión 2 | recibe `IncidenciaActualizada` | `IncidenciaActualizada recibida: Id=9 Estado=Cerrada` |
| 12 | Commit desplegado | Render sirve el HEAD de `main` | deployments en `6c4057d` |

Secuencia de la pregunta 4 verificada: persistir → invalidar Redis → publicar
PieHost → reconsultar (log `Base de datos`) → búsqueda en Algolia ya no devuelve
la incidencia cerrada.
