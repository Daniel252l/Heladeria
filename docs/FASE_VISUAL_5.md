# Fase Visual 5 — Ventas / POS

Implementación de interfaz del 2026-10-07. Pendiente de aprobación mediante validación manual.
Sin Caja, FEL, impresión, crédito, delivery, cuentas por cobrar/pagar ni nuevas reglas comerciales.

## Pantallas y permisos

| Ruta | Experiencia | Permisos de entrada |
| --- | --- | --- |
| `/ventas` | Listado, búsqueda, estados, fechas, paginación | `VENTAS.CONSULTAR` |
| `/ventas/:id` | Detalle, importes reales, costos, movimientos e histórico | `VENTAS.CONSULTAR` |
| `/pos` | Catálogo y carrito operativos | `POS.OPERAR` |
| `/ventas/nueva` | Nueva venta usando el mismo editor POS | `VENTAS.ADMINISTRAR` + `POS.OPERAR` |
| `/ventas/:id/editar` | Edición exclusivamente de borradores | `VENTAS.ADMINISTRAR` + `POS.OPERAR` + `VENTAS.CONSULTAR` |

Se agregó el grupo Ventas al sidebar, respetando su orden alfabético y permisos efectivos.
Las rutas sin permiso usan la experiencia 403 existente. No se habilitan permisos por nombre de rol.

El catálogo y las referencias existentes del backend exigen `POS.OPERAR`. Por eso el editor requiere ese permiso además de `VENTAS.ADMINISTRAR`; no se amplió la autorización de estos endpoints. El POS con solo `POS.OPERAR` permite consulta y preparación visual, pero no ofrece guardar.

Acciones independientes del permiso de entrada:

- BORRADOR: editar con acceso al editor; confirmar con `VENTAS.ADMINISTRAR`; anular con `VENTAS.ANULAR`.
- CONFIRMADA: completar con **ambos** `VENTAS.VENDER` y `POS.OPERAR`; anular con `VENTAS.ANULAR`.
- COMPLETADA: consulta y anulación con `VENTAS.ANULAR`.
- ANULADA: consulta, sin operaciones.

## Componentes y carga de datos

- `PaginaVentas`: filtros aplicados explícitamente, rango de fechas y paginación remota.
- `PaginaPos`: referencias reales, catálogo con debounce de 300 ms, categoría, bodega y ubicación; editor compartido para nueva venta y borrador.
- `PaginaDetalleVenta` / `DetalleVenta`: resultados del servidor y ciclo de confirmación, cierre y anulación. También presenta la respuesta de creación sin exigir una lectura adicional al usuario que acaba de guardar.
- `ComponentesVentas`: insignias de estado, avisos, totales, datos y tablas que pasan a tarjetas en móvil.
- `useDatosVentas`: lecturas cancelables, descarte de resultados obsoletos, aislamiento por usuario/sucursal y exclusión de escrituras concurrentes por doble clic.
- `api/ventas.ts`: solicitudes de escritura tipadas mediante el cliente HTTP existente; conserva su renovación de sesión/401.
- `types/ventas.ts`: contratos camelCase concordantes con los DTO de Fase 7, incluyendo propiedades nullable.
- `utilidadesVentas`: conversión y preview exclusivamente visual, cantidades hasta 6 decimales, porcentajes hasta 2 decimales, moneda comercial a 2 y costo base de 2 a 6 decimales.

## Contratos HTTP

Lecturas existentes consumidas:

- `GET /api/ventas` (`busqueda`, `estadoId`, `fechaDesde`, `fechaHasta`, `pagina`, `tamanioPagina`).
- `GET /api/ventas/{id}`.
- `GET /api/ventas/referencias` (empresa/sucursal, moneda, clientes opcionales, categorías, almacenes).
- `GET /api/ventas/productos` (búsqueda, categoría, paginación, bodega, ubicación).
- `GET /api/ventas/productos/{productoId}/presentaciones` para cargar los productos del borrador a editar.

Lectura mínima añadida:

- `GET /api/ventas/estados`, protegida con `VENTAS.CONSULTAR` y validación de sucursal activa. Devuelve únicamente `id`, `codigo` y `nombre` de estados del flujo `VENTA_POS`, incluidos históricos no operativos para poder filtrar ventas antiguas.
- Justificación: el listado debe poder filtrar estados con permiso de consulta, pero `/referencias` exige `POS.OPERAR` y expone datos operativos adicionales. No se deducen IDs a partir de páginas parciales ni se hardcodean.
- Archivos backend: `ContratosVentas.cs`, `VentasController.cs`, `ServicioVentas.Consultas.cs`, `VentasHttpPruebas.cs`. No se modificaron operaciones de escritura, cálculos, permisos anteriores, EF ni SQL.

Escrituras existentes consumidas sin modificar sus contratos:

- `POST /api/ventas`: sucursal de referencias, bodega/ubicación seleccionadas, cliente nullable, moneda, detalles con producto/presentación, cantidad y porcentaje de descuento.
- `PUT /api/ventas/{id}`: mismo cuerpo más `versionFila` vigente.
- `POST /api/ventas/{id}/confirmar`: `versionFila`.
- `POST /api/ventas/{id}/completar`: `versionFila` y `claveOperacion`.
- `POST /api/ventas/{id}/anular`: `versionFila` y motivo de 3 a 500 caracteres, sin espacios marginales.

No se envían empresaId, precios, costos, impuestos, cantidades base ni totales calculados en UI.

## Carrito, precios e inventario

- Consolida el mismo producto/presentación; mantiene separadas presentaciones distintas.
- Unidad base conserva `productoPresentacionId = null`; no se inventan factores ni precios.
- Selección de presentación, cantidad escrita, incremento, decremento y eliminación; equivalencia visible (por ejemplo, 2 vasos de factor 250 = 500 ml).
- `precioValido = false`: muestra “Precio no configurado” y deshabilita agregar. Validación defensiva adicional antes de guardar.
- Descuento editable únicamente si `permiteDescuento = true`; backend conserva la autoridad.
- `disponibleBase = null`: “Disponibilidad no calculada”, nunca cero implícito.
- Bodega/ubicación se derivan de referencias y se autoseleccionan solo si existe una única opción. No se usan IDs identity fijos.
- Cliente opcional, incluso con catálogo vacío; no se crean clientes ni datos de preparación.
- Preview marcado como estimación; después de guardar/actualizar se presenta la respuesta real del servidor, incluidos totales y nueva versión.
- Confirmar no modifica el stock local. Error `EXISTENCIA_INSUFICIENTE` conserva estado/cantidades/movimientos, muestra el nombre del producto cuando el backend identifica su ID y no simula ventas parciales.

## Concurrencia e idempotencia

- Todas las mutaciones de una venta existente usan su última `versionFila` recibida. Conflicto por versión bloquea el envío y ofrece recargar; no se sobrescribe automáticamente.
- El cierre genera `crypto.randomUUID()` y conserva la UUID técnica **antes** del POST en `sessionStorage`, separada por usuario, empresa, sucursal y venta. No persiste tokens, clientes, precios ni carrito.
- La clave sobrevive a cerrar el diálogo, volver a la ruta o recargar la misma pestaña. Si no se puede conservar, se aborta antes de enviar.
- Un reintento con clave existente consulta primero `GET /api/ventas/{id}`. COMPLETADA/ANULADA muestra el resultado existente sin POST. Cambio de estado/versión requiere recarga. Consulta fallida impide un reintento ciego.
- Timeout/error de red/408/5xx dispara solo una consulta de reconciliación; nunca otro cierre automático. Si no se conoce el resultado, informa incertidumbre y conserva la clave.
- El botón se bloquea y una referencia interna excluye doble clic antes del siguiente render.
- **Permiso de reconciliación:** el GET existente exige `VENTAS.CONSULTAR`. Si un operador tiene permisos para cerrar pero carece de consulta, un resultado incierto puede requerir a un usuario con consulta para verificar el estado. La UI no elude el 403 ni amplía permisos.
- La clave se conserva durante la sesión de la pestaña; no se promete coordinación entre distintas pestañas ni persistencia tras cerrar el navegador. El control final de idempotencia sigue en backend.

## Anulación y errores

- Motivo obligatorio de 3–500 caracteres. Venta completada advierte que se generará reversión de inventario.
- La respuesta reemplaza el detalle e incluye movimientos originales y reversos. ANULADA no ofrece otro botón de anulación.
- Doble anulación y validación comercial muestran el mensaje del backend; no se usan `alert()` ni trazas técnicas.
- 403/404 utilizan las páginas existentes; 401 conserva el mecanismo de autenticación/refresh del cliente compartido.
- Los modales mantienen foco visible, confinamiento de Tab/Shift+Tab y restauración del foco al cerrar.

## Responsive y accesibilidad

- Escritorio: catálogo a la izquierda y carrito a la derecha; lista del carrito con scroll interno y separación del encabezado fijo.
- Móvil/tablet: disposición vertical y enlace directo al carrito; tablas como tarjetas en móvil y scroll interno en tablet.
- Acciones del encabezado envuelven correctamente incluso a 768 px; corrección CSS acotada a `.ventas-vista`.
- Labels reales, botones nativos, etiquetas accesibles para cantidades/acciones, regiones de totales, estados/errores anunciados y foco visible.
- Smoke con navegador headless y API completamente simulada: POS, detalle, listado y confirmación a 320, 390, 768, 1366 y 1600 px; comprueba overflow de página/modal y errores de consola. Capturas e informe en `heladeria-web/test-results/visual-ventas/` (excluidos de versionado).
- Script reproducible: `scripts/verificar-visual-ventas.mjs`; recibe URL local, ubicación de paquetes Playwright si no está instalado en el proyecto y canal opcional `msedge`/`chrome`. No añade dependencias al proyecto ni usa perfiles personales del navegador.

## Validación y pendientes manuales

Pruebas automatizadas cubren listado/filtros/paginación, estados y permisos, precio inválido, disponibilidad nullable, multimedida, cantidades/descuentos, payload mínimo, respuestas autoritativas, confirmación, cierre, UUID estable, respuesta perdida, doble clic, stock insuficiente, versión obsoleta, anulación/reverso, doble anulación, 403/404, foco y solicitudes asíncronas obsoletas. También se prueba el endpoint de estados con solo permiso de consulta sin habilitar catálogo POS.

Resultados finales:

| Validación | Resultado |
| --- | --- |
| `npm run lint` | Correcto, sin diagnósticos |
| `npm run build` | Correcto, sin advertencia de tamaño de chunks; Ventas/POS con carga diferida |
| `npm test` | 218 aprobadas, 0 fallidas, 0 omitidas; 19 archivos |
| `dotnet build --configuration Release` | 0 errores, 0 advertencias |
| `dotnet test --configuration Release --no-build` | 318 aprobadas, 0 fallidas, 0 omitidas |
| Smoke visual aislado | 20 verificaciones, 5 tamaños, 0 desbordamientos de página/modal y 0 errores de consola |

No se ejecutó bootstrap ni se crearon ventas/datos en la BD real. Los tests backend utilizan sus bases aisladas habituales y el smoke visual intercepta **todas** las llamadas API.

Pendiente de validación manual sobre la instalación del usuario:

1. Permisos efectivos del operador, visibilidad del sidebar y rutas directas.
2. Listado, filtros, detalle y datos reales de referencias/almacenes/clientes.
3. Catálogo, búsqueda, categorías, presentaciones (en especial Vaso) y precios faltantes.
4. Crear, editar, confirmar y completar; contrastar inventario, Kardex y costos en backend.
5. Caso 20 L / 18,500 ml disponibles: error de stock sin mutaciones parciales.
6. Reintento de cierre y respuesta perdida; UUID y consulta de estado antes de reintentar.
7. Anulación de completada y reversión única; intento concurrente/RowVersion obsoleta.
8. Descuentos permitidos/prohibidos, permisos parciales, sesión expirada y errores HTTP.
9. Responsive con datos reales, imágenes, navegación por teclado y consola.

La fase no está aprobada por estas pruebas automáticas: requiere la validación manual solicitada. No se avanzó a Caja ni a otra fase.
