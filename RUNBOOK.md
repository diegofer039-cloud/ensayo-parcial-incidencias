# RUNBOOK — Parcial: Ramas, fusiones y despliegue en Render

90 minutos · 20 puntos · Ensayo completo en este repo.

> **Este archivo se versiona. Nunca pegues claves reales aquí.** Los valores viven en
> `dotnet user-secrets list --project src/PlataformaIncidencias` (local) y en las
> variables de entorno de Render.

---

## 0. Cronograma

| Min | Qué hacer |
|-----|-----------|
| 0–7 | Revisar proyecto, conexiones y commit inicial. Abrir las tres pestañas. |
| 7–43 | **12 min por rama**: implementar A, B, C y abrir sus PR. |
| 43–63 | Fusionar A → B → C y resolver los dos conflictos. |
| 63–80 | Desplegar en Render y probar Algolia, Redis y PieHost en producción. |
| 80–90 | Entregar enlaces, historial y demostración. |

Si a los 43 min no tienes los tres PR abiertos, fusiona igual: los PR pueden seguir
abiertos y actualizarse solos cuando empujes la resolución.

---

## 1. Cuentas de ensayo

```
supervisor@operaciones.local / Supervisor123!
```

Ruta de la pantalla: `/Operaciones/Incidencias`

---

## 2. Reglas que no se pueden romper

1. **Nunca** trabajar directamente en `main`.
2. A, B y C nacen **del mismo commit de `main`** y **antes** de integrar cualquiera.
3. Fusión en orden: **A → B → C**.
4. En B y C: `git merge main` (**no** rebase, **no** squash, **no** force push).
5. Cada resolución de conflicto es un **commit aparte**, no se enmienda.
6. Las tres ramas cambian la **misma línea** `<h1>Incidencias abiertas</h1>`.
7. Ninguna clave en el repositorio.

---

## 3. Comandos — creación de ramas y PR

```powershell
# desde main, en el commit inicial (antes de integrar nada)
$C0 = git rev-parse --short HEAD
git branch feature/busqueda-algolia $C0
git branch feature/cache-redis      $C0
git branch feature/websocket-piehost $C0
```

Por cada rama:

```powershell
git checkout feature/<nombre>
# ... implementar ...
git add -A
git commit -m "<tipo>(<scope>): <descripcion>"
git push -u origin feature/<nombre>
gh pr create --base main --head feature/<nombre> --title "<titulo>" --body "<qué hace>"
```

`main` no se mueve hasta que fusiones.

---

## 4. Fusiones y los dos conflictos

### Paso 1 — Fusionar A

```powershell
gh pr merge 1 --merge
git fetch origin; git checkout main; git pull
```

### Paso 2 — Primer conflicto (en B)

```powershell
git checkout feature/cache-redis
git merge main          # → CONFLICT
```

**Conflicto esperado en 3 sitios:**

| Archivo | Qué cambia | Resolución |
|---|---|---|
| `IncidenciasController.cs` | firma del constructor | `ctor(db, IBuscadorIncidencias buscador, ICacheListado cache)` |
| `IncidenciasController.cs` | cuerpo de `Index` | conservar **ambos**: `q` vacío → `cache.ObtenerAbiertasAsync(...)`; `q` con texto → Algolia **sin** caché |
| `Views/Incidencias/Index.cshtml` | línea `<h1>` + formulario | h1 = **"Incidencias abiertas con consulta rápida"** + conservar el formulario de búsqueda de A |

Después de resolver:

```powershell
dotnet build PlataformaIncidencias.slnx     # obligatorio antes de commitear
git add -A
git commit -m "merge: incorporar main (PR A) conservando busqueda y cache"
git push origin feature/cache-redis
gh pr merge 2 --merge
git fetch origin; git checkout main; git pull
```

### Paso 3 — Segundo conflicto (en C)

```powershell
git checkout feature/websocket-piehost
git merge main          # → CONFLICT
```

**Conflicto esperado en 2 sitios:**

| Archivo | Qué cambia | Resolución |
|---|---|---|
| `IncidenciasController.cs` | firma del constructor | `ctor(db, buscador, cache, notificador, registro)` — **las tres funciones** |
| `IncidenciasController.cs` | cuerpo de `Cerrar` | orden: `SaveChanges` → `cache.InvalidarAsync` → `notificador.PublicarAsync` |
| `Views/Incidencias/Index.cshtml` | línea `<h1>` + formulario | h1 = **"Incidencias abiertas en tiempo real"** + conservar el formulario de A |

```powershell
dotnet build PlataformaIncidencias.slnx
git add -A
git commit -m "merge: incorporar main (PR A + B) conservando busqueda, cache y WebSocket"
git push origin feature/websocket-piehost
gh pr merge 3 --merge
git fetch origin; git checkout main; git pull
```

### Paso 4 — Evidencia

```powershell
git log --graph --oneline --all
gh pr list --state all        # deben salir los tres en MERGED
```

Tiene que verse: ancestro común, orden A→B→C y **los dos commits de resolución**.

---

## 5. Checklist de la rúbrica (20 puntos)

| Pts | Qué se evalúa | Cómo demostrarlo |
|-----|---------------|------------------|
| 2 | Origen de las ramas | `git log --graph --oneline --all` muestra las tres partiendo del mismo commit |
| 2 | Commits y PR independientes | `gh pr list --state all` → tres PR distinguishibles |
| 4 | Fusiones y conflictos | orden A,B,C + los dos commits de resolución visibles, sin squash |
| 2 | Algolia | búsqueda real; **una incidencia cerrada no aparece** |
| 2 | Redis | hit observable en logs, TTL 60 s, invalidación al cerrar |
| 2 | PieHost | evento **después** de persistir; actualización entre sesiones sin recarga |
| 5 | Render | URL + login (1) · tres servicios en producción (3) · commit final desplegado (1) |
| 1 | Evidencia y explicación | tres PR, historial, README y explicación oral de las dos resoluciones |

**Si Render no funciona al cierre, Render recibe 0 de 5 aunque funcione en local.**

---

## 6. Verificación funcional (hacerla en los primeros minutos del examen)

| Servicio | Qué probar | Qué debe verse |
|---|---|---|
| Algolia | buscar texto de una abierta | filas filtradas |
| Algolia | buscar texto de una **cerrada** | **0 filas** |
| Algolia | buscar vacío | listado completo |
| Redis | cargar dos veces el listado | log `Base de datos` la 1ª, `Redis` después |
| Redis | cerrar una incidencia | log `invalidada` y luego `Base de datos` |
| PieHost | cerrar en una sesión | la otra sesión quita la fila **sin recargar** |
| PieHost | desconectar y reconectar | consulta el estado vigente al reconectar |

---

## 7. Variables de entorno (Render → Environment)

Nunca las pongas en `appsettings.json` ni en el repo.

```
Integrations__Modo=real
Algolia__ApplicationId=<Application ID>
Algolia__AdminApiKey=<Search API Key>
Algolia__Indice=<nombre del indice>
Redis__ConnectionString=redis://default:<password>@<host>:<puerto>
PieHost__Cluster=<cluster>
PieHost__Key=<API key>            # pública: también va al navegador
PieHost__Secret=<API secret>      # SOLO servidor, jamás al HTML
PieHost__Channel=<canal>
```

Cómo recuperarlas en local:

```powershell
dotnet user-secrets list --project src/PlataformaIncidencias
```

**PieHost:Secret es solo de servidor.** Al navegador solo llega `PieHost:Key`, que es
la clave pública que cualquier cliente necesita para conectarse al canal.

---

## 8. Equivalencias de nombres (mi ensayo → el proyecto del docente)

El proyecto que entrega el docente tendrá otros nombres. Mapea sobre la marcha:

| En este repo | Buscar en el proyecto del docente |
|---|---|
| `IncidenciasController` + `[Route("Operaciones/Incidencias")]` | el controlador de la pantalla de operaciones |
| `ListarAbiertasAsync` | el método que trae el listado |
| `Index` / `Cerrar` | las acciones de listar y cerrar |
| `IBuscadorIncidencias` | el cliente de búsqueda que ya venga inyectado |
| `ICacheListado` | el cliente de caché |
| `INotificadorEnTiempoReal` | el publicador del canal |
| `_Tabla.cshtml` | el partial de la tabla, si existe |
| `Integrations:Modo` | la llave que use el proyecto para elegir implementación |
| `<h1>Incidencias abiertas</h1>` | la línea compartida que cambian las tres ramas |

---

## 9. Riesgos conocidos

| Riesgo | Qué hacer |
|---|---|
| **SQLite efímero en Render** | El filesystem se pierde en cada redeploy. Confirmar si hay disco persistente. Si no: re-sembrar antes de la demo y **cruzar Algolia por estación+descripción, no por `Id`**, porque los IDs se reasignan al re-sembrar. |
| **Render free se duerme** | El primer request tarda ~30 s y corta WebSockets. Hacer un warm-up justo antes de la demo. |
| **Índice de Algolia vacío** | Verificarlo antes de empezar: `GET /1/indexes/<indice>` con la Search API Key. |
| **Redis no conecta** | Probar a mano antes de tocar la app (AUTH + PING). Si el host exige TLS, usar `rediss://`. |
| **No llega el evento** | Mirar la consola del navegador: `[tiempo-real] IncidenciaActualizada recibida`. Si conecta pero no recibe, revisar el shape del payload (`id` vs `Id`). |

---

## 10. Orden de la secuencia pedida en la pregunta 4

```
cerrar incidencia
  → guardar en base            (SaveChanges)
  → invalidar la clave Redis   (antes de volver a consultarlo)
  → publicar IncidenciaActualizada por PieHost
  → re-consulta (log: Base de datos)
  → la otra sesión quita la fila sin recargar
  → buscar en Algolia lo que se cerró → 0 resultados
```
