(function () {
    "use strict";

    var contenedor = document.getElementById("tabla-incidencias");
    if (!contenedor) return;

    var modo = contenedor.dataset.modoRealtime || "signalr";
    var endpointDatos = contenedor.dataset.endpointDatos || "";
    var urlSignalR = contenedor.dataset.signalrUrl || "";
    var urlPieSocket = contenedor.dataset.piesocketUrl || "";

    function tokenAntiforgery() {
        var campo = contenedor.querySelector('input[name="__RequestVerificationToken"]');
        return campo ? campo.value : "";
    }

    function escapar(valor) {
        return String(valor == null ? "" : valor)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function pintarFila(i) {
        var cerrada = i.estado === "Cerrada";
        var enlace = endpointDatos
            ? endpointDatos.replace(/\/datos\/?$/, "")
            : "";

        return (
            '<tr data-id="' + i.id + '" data-estado="' + escapar(i.estado) + '">' +
            "<td>" + i.id + "</td>" +
            "<td>" + escapar(i.estacion) + "</td>" +
            "<td>" + escapar(i.descripcion) + "</td>" +
            "<td>" + escapar(i.prioridad) + "</td>" +
            '<td><span class="badge ' + (cerrada ? "bg-secondary" : "bg-success") + '">' +
            escapar(i.estado) + "</span></td>" +
            "<td>" + escapar(new Date(i.fecha).toLocaleString()) + "</td>" +
            '<td class="text-end">' +
            (cerrada
                ? ""
                : '<form method="post" action="' + escapar(enlace) + "/cerrar?id=" + i.id + '" class="d-inline">' +
                  '<input name="__RequestVerificationToken" type="hidden" value="' + escapar(tokenAntiforgery()) + '" />' +
                  '<button type="submit" class="btn btn-sm btn-outline-danger">Cerrar</button>' +
                  "</form>") +
            "</td></tr>"
        );
    }

    function vacia() {
        return '<tr><td colspan="7" class="text-center text-muted">No hay incidencias abiertas.</td></tr>';
    }

    async function refrescar() {
        if (!endpointDatos) return;

        try {
            var respuesta = await fetch(endpointDatos, { headers: { Accept: "application/json" } });
            if (!respuesta.ok) throw new Error("HTTP " + respuesta.status);

            var datos = await respuesta.json();
            var cuerpo = document.getElementById("cuerpo-incidencias");
            if (!cuerpo) return;

            cuerpo.innerHTML = datos.length ? datos.map(pintarFila).join("") : vacia();
            console.info("[tiempo-real] listado consultado al servidor: " + datos.length + " incidencias abiertas");
        } catch (error) {
            console.error("[tiempo-real] no se pudo consultar el estado vigente", error);
        }
    }

    function aplicarEvento(mensaje) {
        var datos = mensaje && mensaje.data ? mensaje.data : mensaje;
        if (!datos || datos.id == null) return;

        var id = Number(datos.id);
        var estado = datos.estado || datos.Estado || "";
        var fila = contenedor.querySelector('tr[data-id="' + id + '"]');

        console.info("[tiempo-real] IncidenciaActualizada recibida: Id=" + id + " Estado=" + estado);

        if (estado === "Cerrada") {
            if (fila) fila.remove();
            var cuerpo = document.getElementById("cuerpo-incidencias");
            if (cuerpo && !cuerpo.querySelector("tr")) cuerpo.innerHTML = vacia();
            return;
        }

        if (!fila) refrescar();
    }

    function conectarSignalR() {
        if (!window.signalR || !urlSignalR) {
            console.error("[tiempo-real] signalr.min.js no está cargado");
            return;
        }

        var conexion = new signalR.HubConnectionBuilder()
            .withUrl(urlSignalR)
            .withAutomaticReconnect()
            .build();

        conexion.on("IncidenciaActualizada", aplicarEvento);
        conexion.onreconnected(function () {
            console.info("[tiempo-real] reconectado: consultando estado vigente");
            refrescar();
        });

        conexion
            .start()
            .then(function () {
                console.info("[tiempo-real] conectado al canal SignalR");
                refrescar();
            })
            .catch(function (error) {
                console.error("[tiempo-real] fallo al conectar con SignalR", error);
            });
    }

    function conectarPieSocket() {
        if (!urlPieSocket) {
            console.error("[tiempo-real] falta la URL del canal PieHost");
            return;
        }

        var reintentos = 0;
        var socket = null;

        function abrir() {
            socket = new WebSocket(urlPieSocket);

            socket.onopen = function () {
                reintentos = 0;
                console.info("[tiempo-real] conectado al canal PieHost");
                refrescar();
            };

            socket.onmessage = function (evento) {
                try {
                    var mensaje = JSON.parse(evento.data);
                    if (mensaje.event === "system:connected") return;
                    aplicarEvento(mensaje);
                } catch (error) {
                    console.error("[tiempo-real] mensaje no interpretable", error);
                }
            };

            socket.onclose = function () {
                reintentos += 1;
                var espera = Math.min(1000 * Math.pow(2, reintentos - 1), 30000);
                console.warn("[tiempo-real] canal cerrado, reconectando en " + espera + " ms");
                setTimeout(abrir, espera);
            };

            socket.onerror = function () {
                console.error("[tiempo-real] error del WebSocket de PieHost");
            };
        }

        abrir();
    }

    if (modo === "piehost") {
        conectarPieSocket();
    } else {
        conectarSignalR();
    }
})();
