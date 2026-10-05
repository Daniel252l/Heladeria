# Fase Visual 4 — Producción y recetas

## Alcance y rutas

Se implementan siete pantallas reutilizando los formularios para alta/edición:

| Rutas | Pantalla | Permiso de ruta |
| --- | --- | --- |
| `/recetas` | Listado, filtros, paginación y grupos de versiones | `RECETAS.CONSULTAR` |
| `/recetas/nueva`, `/recetas/:id/editar` | Formulario de receta e ingredientes multimedida | `RECETAS.ADMINISTRAR` |
| `/recetas/:id` | Detalle y acceso al historial paginado | `RECETAS.CONSULTAR` |
| `/produccion` | Listado y filtros de órdenes | `PRODUCCION.CONSULTAR` |
| `/produccion/nueva`, `/produccion/:id/editar` | Planificación y escalamiento visual | `PRODUCCION.ADMINISTRAR` |
| `/produccion/:id` | Detalle, confirmación, anulación y resultados | `PRODUCCION.CONSULTAR` |
| `/produccion/:id/finalizar` | Ubicación, cantidad producida y finalización | `PRODUCCION.PRODUCIR` |

El menú existente incorpora el grupo Producción: Órdenes de producción y Recetas.
Las rutas conocidas sin permiso muestran 403. Los roles que administran/producen necesitan también CONSULTAR en su módulo para cargar los recursos; no se otorgan permisos automáticamente.

## Recetas y versionado

- Filtros por texto, producto, estado, vigencia y versión, con limpiar y paginación remota.
- Agrupación expandible por código/producto de las versiones presentes en la página; el detalle ofrece la historia completa paginada por código exacto y producto.
- Presentaciones específicas de recetas, incluyendo unidad base (`productoPresentacionId: null`, factor 1). No se reutilizan filtros de compra ni se inventan IDs.
- Rendimiento y cantidades base son equivalencias de vista. Se envían cantidades ingresadas e IDs; el servidor conserva autoridad sobre factores, conversiones y persistencia.
- Validación de ingrediente propio, duplicados, receta vacía, cantidad no positiva, unidad base ausente y presentación no disponible.
- En edición se conserva el producto terminado. Una receta usada muestra un aviso de nueva versión y bloquea el cambio de código. Se envía `versionFila` y se navega al ID retornado, aunque sea distinto.
- Las versiones históricas son de consulta. Los errores de vigencia preservan los campos; los conflictos permiten recargar explícitamente.

## Producción y finalización

- Filtros de número, producto, receta, estado y rango de fechas. Los almacenes corresponden a la sucursal activa.
- La selección de receta muestra producto, versión, rendimiento e ingredientes escalados; por ejemplo, de 5 L a 15 L multiplica visualmente por tres. No se envían cantidades base ni ingredientes calculados.
- BORRADOR: editar/confirmar/anular con ADMINISTRAR. CONFIRMADA: finalizar con PRODUCIR; anular con ADMINISTRAR si no existen consumos, producción terminada ni movimientos. FINALIZADA y ANULADA: solo consulta.
- Confirmación y anulación usan diálogo y la `versionFila` recibida. Se reemplaza la vista con el resultado del servidor.
- Finalización solicita cantidad en la unidad planificada y ubicación de la bodega de la orden. `crypto.randomUUID()` se genera una vez al montar la operación, no en cada render/envío. Se conserva al reintentar un error mientras no se abandone/reinicie la pantalla. Se bloquean envíos simultáneos.
- Un 409 ofrece consultar el estado actual; una ruta de finalización sobre una orden no confirmada no presenta formulario operativo.
- `EXISTENCIA_INSUFICIENTE` conserva el mensaje real (disponible/requerido) y resuelve el ID del ingrediente a su nombre y unidad base utilizando la orden, sin consultar ni inventar existencias.
- El resultado muestra cantidades base, materia prima, mano de obra, indirectos, total, costo unitario base, consumos reales y los IDs de movimientos devueltos. No se inventa tipo de movimiento ni navegación a un Kardex inexistente.
- Se reutilizan los formateadores aprobados en Compras: GTQ con 2 decimales para importes y entre 2 y 6 para costos unitarios base (`GTQ 0.00638 / ml`).

## Incompatibilidades contractuales resueltas

Los contratos originales no permitían conocer si una receta había sido usada, identificar algunas unidades base ni cargar opciones sin permisos de otros módulos. Además, faltaban el filtro de versión y un filtro exacto para paginar una familia de versiones sin mezclar códigos parecidos.

Se hicieron únicamente estas ampliaciones aditivas de lectura:

| Contrato | Adición mínima y motivo |
| --- | --- |
| Consulta de receta | `fueUtilizada`, `unidadBase`, `abreviaturaUnidadBase`, `presentacionRendimiento`: aviso fiable de versionado y equivalencias sin inferencias |
| Listado de recetas | Filtros opcionales `version` y `codigo` exacto, aplicados antes de contar/paginar |
| Resumen de orden | `unidadBase`, `abreviaturaUnidadBase`, `presentacion`: descripción inequívoca de cantidades y costos |
| Detalles de orden | `abreviaturaUnidadBase`: consumos escalados y mensajes de stock |
| `GET /api/recetas/referencias` | Productos de la empresa y estados disponibles, bajo `RECETAS.CONSULTAR` |
| `GET /api/produccion/productos` | Opciones de producto paginadas de la empresa, bajo `PRODUCCION.CONSULTAR` |
| `GET /api/produccion/recetas` | Consulta paginada reutilizando el servicio existente, bajo `PRODUCCION.CONSULTAR` |
| `GET /api/produccion/recetas/{id}` | Receta para escalar la orden sin exigir `RECETAS.CONSULTAR` |
| `GET /api/produccion/productos/{productoId}/presentaciones` | Presentaciones para planificar producción sin permisos de Productos/Recetas |

Las nuevas consultas verifican sucursal/empresa con las validaciones existentes y utilizan `AsNoTracking`. Los DTO de filtros son clases concretas con DataAnnotations. No se modificaron las escrituras, las reglas de versionado/producción, entidades EF, SQL ni la base de datos. No se ejecutó bootstrap ni preparación contra una base real.

Para validar manualmente se debe reiniciar/desplegar la API recompilada junto con este frontend: la API anterior no expone estas adiciones. No se requiere migración SQL ni volver a ejecutar bootstrap.

Archivos backend: `ContratosProduccion.cs`, `ServicioRecetas.cs`, `ServicioProduccion.cs`, `RecetasController.cs`, `ProduccionController.cs`; pruebas en `Heladeria.Pruebas/Fase6`.

## Interfaz y reutilización

- Se reutilizan layout, cliente HTTP con renovación de sesión, guards, `SelectorEntidad`, panel de filtros, paginación, tarjetas, entradas, insignias, estados de carga/error/vacío, diálogo de confirmación y formateadores.
- No se modificó `SelectorEntidad` ni se copiaron `opcionesLocales` a estado interno.
- Escritorio: tablas completas. Tablet: scroll horizontal. Móvil (<640 px): las mismas filas se presentan como tarjetas de dos columnas con etiquetas y acciones, sin duplicar DOM. Formularios en una columna y pie de acciones accesible.
- Cargas cancelables por contexto/sucursal, descarte de respuestas obsoletas, errores con reintento y ausencia de actualizaciones después del desmontaje. El envío de una mutación no se aborta artificialmente, pues puede haberse confirmado en el servidor.

## Verificación

Resultado final: lint correcto, build frontend correcto, 140/140 pruebas frontend (16 archivos), build Release con 0 errores/0 advertencias y 234/234 pruebas backend. Se añadieron 43 casos frontend y 14 backend respecto de las suites iniciales. Una corrida frontend bajo carga paralela produjo timeouts y fue detenida; la ejecución final secuencial pasó completa, sin aumentar timeouts ni omitir pruebas. Vite emitió únicamente un aviso de tiempos de sus plugins durante la compilación bajo carga.

Frontend: pruebas de listados/filtros, creación/edición, unidades/presentaciones, escalamiento, versionado/historia, validaciones, permisos CONSULTAR, nueve rutas 403, detalle 404, confirmación, anulación, stock insuficiente, concurrencia, clave estable, doble envío, resultados, costos, read-only y respuestas asíncronas obsoletas. Datos simulados separados de producción en `src/test/datosFaseVisual4.ts`.

Backend: regresión Fase 6 y nuevas pruebas de metadata sin escrituras, empresa/paginación, filtros exactos, permisos 401/403 y lecturas de producción sin permisos de otros módulos.

Comandos desde `Heladeria/heladeria-web`:

```powershell
npm run lint
npm run build
npm test
```

Desde el directorio de `Heladeria.sln`:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

La revisión visual de navegador utilizó un entorno temporal con respuestas simuladas (escritorio y móvil de 390 px), sin API/BD reales; los archivos temporales fueron retirados después de verificar el layout y la consola.

## Límites del contrato que se respetan

- No hay snapshot de existencias en la consulta de producción: se informa que la disponibilidad se valida al finalizar.
- El backend actual deja `costoPlanificado` en null; el detalle muestra «No disponible». No se calcula un presupuesto inventado. Los costos finales provienen de la respuesta.
- La respuesta no informa tipos/documentos de los movimientos: se muestran exclusivamente sus IDs.
- No hay endpoint de eventos de auditoría de producción en este contrato: la trazabilidad muestra fechas, receta/versiones, consumos, producción terminada y movimientos, no una cronología ficticia.
- La API no devuelve moneda para producción. Se utiliza GTQ conforme al alcance solicitado; no se añadió multimoneda.

No quedan cambios contractuales obligatorios pendientes para este alcance. No se implementó Fase Visual 5 ni Fase 7.
