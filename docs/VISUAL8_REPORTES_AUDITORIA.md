# Visual 8 — Reportes y auditoría

Estado: implementada, pendiente de validación manual del usuario. No implica aprobación de la fase.

## Alcance y diagnóstico previo

Antes de editar se inspeccionaron router, layout/sidebar, sesión y permisos, cliente HTTP, manejo de errores, tablas de Ventas/Caja/Cartera, filtros, paginación, selectores, tarjetas, fechas, importes y estilos responsive. También se leyeron, sin editar, `ContratosReportes.cs`, `ReportesController.cs`, `AuditoriaController.cs` y los contratos de los catálogos reutilizados.

Hallazgos:

- No existían rutas, contratos TypeScript, cliente API ni pantallas frontend para los reportes de Fase 10.
- Ya existían `RutaConPermiso`, grupos colapsables del sidebar, `solicitar`, renovación central de sesión y un hook de consulta con cancelación y aislamiento por usuario/sucursal.
- Las respuestas no comparten una única forma: Ventas contiene `ventas` y `cobrosPorForma`; Inventario contiene `datos`, valor y moneda; productos vendidos, Kardex y auditoría son paginados sin `periodo`; los restantes incluyen `datos`, `totales` y `periodo`.
- Inventario no acepta fechas. El estado omitido en Ventas usa el predeterminado del backend, COMPLETADA; no se etiqueta falsamente como “Todos”.
- Existen campos históricos nulos. No se sustituyen por cero, Contado, pagado ni éxito financiero.
- Las tablas reutilizadas de Ventas normalmente se convierten en tarjetas móviles. Para comparar reportes se aplica una excepción CSS local: tabla con encabezados y scroll horizontal, sin alterar las pantallas anteriores.

No se modificó ningún proyecto backend, SQL, EF, permiso backend, regla financiera/inventario, endpoint, health check ni configuración Swagger. No se ejecutó bootstrap ni se consultó/modificó la BD real. No se agregaron dependencias ni se cambiaron configuraciones para aprobar pruebas.

## Vistas, rutas, endpoints y permisos

| Vista | Ruta frontend | Endpoint GET | Permiso |
| --- | --- | --- | --- |
| Ventas | `/reportes/ventas` | `/api/reportes/ventas` | `REPORTES.CONSULTAR` |
| Productos vendidos | `/reportes/productos-vendidos` | `/api/reportes/productos-vendidos` | `REPORTES.CONSULTAR` |
| Inventario | `/reportes/inventario` | `/api/reportes/inventario` | `REPORTES.CONSULTAR` |
| Kardex | `/reportes/kardex` | `/api/reportes/kardex` | `REPORTES.CONSULTAR` |
| Compras | `/reportes/compras` | `/api/reportes/compras` | `REPORTES.CONSULTAR` |
| Caja | `/reportes/caja` | `/api/reportes/caja` | `REPORTES.CONSULTAR` |
| CxC | `/reportes/cuentas-por-cobrar` | `/api/reportes/cuentas-por-cobrar` | `REPORTES.CONSULTAR` |
| CxP | `/reportes/cuentas-por-pagar` | `/api/reportes/cuentas-por-pagar` | `REPORTES.CONSULTAR` |
| Auditoría | `/auditoria` | `/api/auditoria` | `AUDITORIA.CONSULTAR` |

`/reportes` redirige a Ventas después de validar el permiso. El grupo Reportes y el acceso Auditoría reutilizan el sidebar y su orden alfabético. Se decide por permisos de sesión, nunca por un nombre de rol hardcodeado. ADMIN con permisos ve los accesos; CONSULTA sin ellos no los ve y las rutas directas lo envían a 403 sin consultar los reportes.

## Filtros y consultas

Todas las vistas admiten sucursal de la sesión, búsqueda, página y tamaño; las fechas solo aparecen donde están soportadas. Los filtros particulares son:

| Vista | Filtros particulares |
| --- | --- |
| Ventas | clienteId, estado, monedaId |
| Productos vendidos | productoId, clienteId, monedaId |
| Inventario | productoId, bodegaId, ubicacionId; sin fechas |
| Kardex | productoId, bodegaId, concepto, tipo ENTRADA/SALIDA |
| Compras | proveedorId, monedaId, estado, esCredito |
| Caja | cajaId, usuarioId, estado |
| CxC / CxP | terceroId, monedaId, estado, vencimiento |
| Auditoría | usuarioId, modulo, accion, entidad |

Estado editable y estado aplicado son independientes. “Aplicar filtros” envía todos los criterios juntos y vuelve a página 1. “Limpiar” elimina criterios opcionales y restaura la sucursal activa. Paginar conserva los criterios aplicados, aunque se hayan escrito otros sin aplicar. Tamaños existentes: 10, 20, 50, todos inferiores al máximo 100. IDs positivos/enteros, límites de longitud y fechas con mínimo/máximo mediante controles nativos.

`URLSearchParams` serializa solamente claves permitidas por cada contrato. Conserva expresamente `esCredito=false`. No hay consultas por fila, resumen separado ni consultas por cada tecla. El hook existente cancela cargas obsoletas; un paso asíncrono antes del GET evita la primera llamada cancelada por el montaje de StrictMode. Cambiar usuario/sucursal activa/tipo desmonta el estado anterior.

Catálogos únicamente cuando corresponde y existe permiso:

- Cliente: `GET /api/clientes`, requiere `VENTAS.CONSULTAR`.
- Producto: `GET /api/productos`, requiere `PRODUCTOS.CONSULTAR`.
- Proveedor: `GET /api/compras/proveedores`, requiere `PROVEEDORES.CONSULTAR`.
- Caja: `GET /api/caja/cajas?sucursalId=...`, requiere `CAJA.CONSULTAR`.

Se reutiliza `SelectorEntidad`, con paginación/búsqueda explícita. Sin el permiso de catálogo se ofrece ID opcional y no se consulta ese catálogo. Moneda, bodega, ubicación y usuario se filtran por ID; no se amplían permisos ni se carga un módulo completo para obtener opciones. Cambiar la sucursal editable limpia bodega/ubicación/caja antes de aplicar.

## Semántica y contratos TypeScript

`types/reportes.ts` define los filtros por endpoint, los nueve tipos de respuesta, el período y cada fila/resumen. `ResultadoReporte` es una unión discriminada; el render no depende de casts a respuestas intercambiables. No se usa `any`. Venta admite defensivamente modalidad nula aunque el contrato actual declare bool; Compra y los históricos nullable se reflejan expresamente.

- **Ventas:** totales del servidor separados por moneda y estado; costo/margen nulos como `—`. Estado de venta distinto del estado de cobro. “Cobros aplicados” no se denomina ventas por forma de pago: son cobros acumulados de las ventas filtradas, no necesariamente cobrados dentro del período.
- **Productos:** presentación, unidad comercial, unidad base y cantidades se muestran por fila. Sin conversión, suma entre presentaciones o moneda combinada.
- **Inventario:** valor, disponibilidad y costo promedio oficiales del backend. No se recalculan. Moneda desconocida se indica; nunca se inventa GTQ.
- **Kardex:** se muestran cantidades, costos y saldos históricos persistidos. El contrato no devuelve moneda; se informa esa limitación y no se asigna ninguna. Costos unitarios conservan hasta 6 decimales.
- **Compras:** true Crédito, false Contado, null Sin definir. RECIBIDA no es PAGADA. Sin CxP se muestra “Sin cuenta vinculada”; saldo null se muestra `—`.
- **Caja:** apertura separada de ingresos operativos. Forma no efectiva no se presenta como efectivo. Se mantiene el signo y valor de diferencia: Sobrante / Sin diferencia / Faltante. Cierre, valores históricos, efectivo contado y diferencia null se muestran `—`; conciliable sigue el servidor.
- **Cartera:** emisión/vencimiento civiles; saldos actuales aunque el filtro sea un período de emisión. Resúmenes por moneda, cero pagado como cero, clasificación REQUIERE_REVISION visible si llega, sin inventar un filtro que el backend no acepte. CxP incluye compra origen.
- **Auditoría:** nueve columnas explícitas de metadatos. No se recorren propiedades arbitrarias del objeto. Nunca se solicitan detalles ni se renderizan ValorAnterior, ValorNuevo, Comentario, Equipo, IP, tokens, cookies, hashes o cuerpos. REGISTRADA no se interpreta como operación financiera exitosa. Empresa/sucursal se identifican por ID sin consultas adicionales por fila.

Los importes normales usan dos decimales. Los costos unitarios usan `formatoCosto` (mínimo 2, máximo 6), sin modificar cifras persistidas ni cálculos. Un null no se convierte en cero.

## Fechas

Se reutilizan `mostrarFechaCivil` y `mostrarInstante`. Los inputs envían YYYY-MM-DD sin conversión a timestamp. Se añadió a `mostrarInstante` un tercer parámetro opcional para la zona del reporte: por defecto conserva la zona global anterior. Esto permite consultar otra sucursal sin cambiar el formato global de Ventas/Caja/etc.

Si la respuesta incluye `periodo`, se muestra el rango efectivo devuelto, su sucursal y zona. Si no incluye período (productos/Kardex/auditoría), se muestra solamente el rango solicitado y se indica que el endpoint no devuelve el efectivo. No se inventan fechas por defecto. La zona en esos tres casos proviene de la sucursal seleccionada de la sesión; si falta, se indica UTC. No se usan ajustes manuales -6 ni la zona implícita del navegador.

## Reutilización, UX y responsive

Reutilizados: LayoutAutenticado, grupos del sidebar, RutaConPermiso, useAutenticacion, solicitar/renovación, useCargaVentas, EncabezadoPagina, PanelFiltros, Entrada, Seleccion, SelectorEntidad, Boton, Tarjeta, Insignia, EstadoCargando/EstadoVacio/EstadoError, Paginacion, TablaVentas, DiferenciaCaja y formatos monetarios/fechas existentes.

Nuevos componentes acotados: FiltrosReportes, TablaReporte (adapta TablaVentas), ResumenReporte, EstadoReporte, ErrorReporte, PeriodoConsultado y resultados específicos. No se creó una segunda biblioteca visual ni un framework de reportes.

Cada pantalla tiene encabezado, descripción, filtros, Aplicar/Limpiar, carga, vacío, error, tablas y paginación. HTTP 401 conserva renovación central; 403 muestra “No tiene permisos para consultar este reporte.”; 400 muestra la validación controlada del backend; 500 y fallos de red usan mensaje genérico sin detalles internos. Se conserva traceId como referencia de soporte cuando llega.

320/390 px: filtros apilados, tarjetas adaptables, menú móvil existente, tablas completas con scroll horizontal interno y contenedor enfocable por teclado. 768 px: filtros en dos columnas. Escritorio: tres columnas y tarjetas de resumen en cuadrícula. No se ocultan columnas críticas; la excepción CSS queda bajo `.reporte-tabla`. Verificado el desplazamiento horizontal por flecha derecha y ausencia de desbordamiento horizontal del documento.

## Archivos

Nuevos (rutas relativas a la solución):

- `heladeria-web/src/api/reportes.ts`
- `heladeria-web/src/types/reportes.ts`
- `heladeria-web/src/features/reportes/configuracionReportes.ts`
- `heladeria-web/src/features/reportes/utilidadesReportes.ts`
- `heladeria-web/src/features/reportes/useReporte.ts`
- `heladeria-web/src/features/reportes/PaginaReportes.tsx`
- `heladeria-web/src/features/reportes/FiltrosReportes.tsx`
- `heladeria-web/src/features/reportes/ComponentesReportes.tsx`
- `heladeria-web/src/features/reportes/ResultadosComerciales.tsx`
- `heladeria-web/src/features/reportes/ResultadosExistencias.tsx`
- `heladeria-web/src/features/reportes/ResultadosFinancieros.tsx`
- `heladeria-web/src/features/reportes/ResultadoAuditoria.tsx`
- `heladeria-web/src/features/reportes/reportes.css`
- `heladeria-web/src/features/reportes/Reportes.test.tsx`
- `heladeria-web/src/test/datosFaseVisual8.ts`
- `heladeria-web/scripts/verificar-visual-reportes.mjs`
- `docs/VISUAL8_REPORTES_AUDITORIA.md`

Modificados:

- `heladeria-web/src/auth/permisos.ts`: constantes de los permisos ya existentes.
- `heladeria-web/src/rutas/EnrutadorAplicacion.tsx`: rutas protegidas, carga lazy y aislamiento del contexto.
- `heladeria-web/src/layouts/LayoutAutenticado.tsx`: grupos/enlaces y etiquetas de navegación.
- `heladeria-web/src/utils/fechas.ts`: zona explícita opcional compatible con llamadas anteriores.
- `heladeria-web/src/rutas/EnrutadorAplicacion.test.tsx`: diez rutas protegidas nuevas y expectativa de orden ampliada sin quitar enlaces anteriores.

Generados para revisión, sin datos reales: `heladeria-web/test-results/visual-reportes/` (36 PNG y resultado.json). `dist/` se regeneró con el build habitual.

## Pruebas y resultados

Pruebas nuevas: filtros de los nueve endpoints inspeccionando URLSearchParams reales, limpiar/paginar/tamaño, sucursal, sin consultas por tecla/fila, catálogo según permiso, true/false, whitelist de parámetros, StrictMode, respuestas atrasadas, navegación/permisos, períodos/zonas, monedas, nullable, precisión, estados, caja y auditoría sin payloads sensibles. Se mantienen todas las pruebas de las fases anteriores. La expectativa de orden del sidebar se amplió porque ahora existen nueve enlaces adicionales; no se debilitó su comprobación.

Comandos desde `heladeria-web`:

```text
npm run lint
npm run build
npm test
```

Resultados finales de la ejecución completa:

- `npm run lint`: código de salida 0; 0 errores y 0 advertencias.
- `npm run build`: código de salida 0; TypeScript y Vite correctos, 0 errores y 0 advertencias; 1942 módulos transformados.
- `npm test`: código de salida 0; **521 aprobadas, 0 fallidas, 0 omitidas; 27 archivos aprobados**. Duración informada: 90.43 s.
- Incremento de cobertura: **51 casos nuevos** (41 en Reportes y 10 rutas protegidas), sobre las 470 pruebas anteriores. Se conserva la regresión completa de las fases visuales previas.

Prueba visual adicional ejecutada sobre el build, con Edge/Chromium headless y TODAS las API interceptadas, incluida autenticación:

```text
npm run preview -- --host 127.0.0.1 --port 5190 --strictPort
node scripts/verificar-visual-reportes.mjs http://127.0.0.1:5190 <node_modules-con-playwright> msedge
```

Resultado: 9 vistas × 4 anchos = 36 capturas, 0 errores de consola/página, 0 peticiones no previstas. Zona del navegador Asia/Tokyo y sucursal America/Guatemala para verificar que la presentación no depende de la zona del navegador. Comprobadas navegación móvil/escritorio, rutas bloqueadas de CONSULTA, un GET inicial por reporte, filtros reales, paginación, limpiar y scroll horizontal por teclado. No se instaló una librería: se utilizó Playwright del runtime disponible, como los scripts visuales existentes.

## Checklist manual — validar una pantalla a la vez

Marcar únicamente después de confirmación del usuario; ninguna está aprobada por esta implementación.

1. [ ] **Menú ADMIN:** abrir Reportes y Auditoría; comprobar accesos y navegación móvil/escritorio.
2. [ ] **Ventas:** elegir rango/sucursal; comparar período, subtotal/descuento/impuesto/total/costo/margen por moneda con API. Diferenciar COMPLETADA de COBRADA/PARCIAL/PENDIENTE. Revisar Crédito/Contado, nulls y cobros por forma.
3. [ ] **Productos vendidos:** comprobar presentaciones, unidades, cantidad y cantidad base, monedas y costos/márgenes no disponibles; sin suma de unidades distintas.
4. [ ] **Inventario:** comprobar existencia/reservada/disponible y valor oficial; costo pequeño con precisión. No debe existir filtro de fechas.
5. [ ] **Kardex:** comprobar ENTRADA/SALIDA, concepto, origen, documento, usuario, cantidades y saldos; navegar páginas. No debe asignarse una moneda no devuelta.
6. [ ] **Compras:** revisar Crédito/Contado/Sin definir, recepción, cuenta vinculada y saldo null. RECIBIDA nunca se interpreta como PAGADA.
7. [ ] **Caja:** separar apertura/ingresos/egresos, efectivo/tarjeta; revisar diferencia positiva/cero/negativa y registros históricos no conciliables.
8. [ ] **CxC:** revisar resúmenes por moneda, estado, vencimiento, saldo cero pagado y clasificación.
9. [ ] **CxP:** mismo recorrido, documento de proveedor y compra origen; sin inventar datos financieros faltantes.
10. [ ] **Auditoría:** filtros usuario/módulo/acción/entidad/fechas; solo metadatos permitidos; REGISTRADA no significa éxito financiero.
11. [ ] **Filtros:** completar varios criterios, limpiar Network y aplicar. Verificar la URL y ausencia de consulta al escribir. Limpiar elimina parámetros opcionales. Una validación inválida conserva selección y muestra el mensaje controlado.
12. [ ] **Paginación:** siguiente/anterior y 10/20/50; conservar filtros aplicados, no valores todavía sin aplicar. Aplicar nuevos filtros vuelve a página 1.
13. [ ] **CONSULTA:** sin REPORTES.CONSULTAR/AUDITORIA.CONSULTAR no debe ver enlaces ni entrar por URL directa. No se amplían sus permisos.
14. [ ] **Responsive:** repetir a 320, 390, 768 y escritorio; tablas con scroll interno, foco visible y todas las columnas accesibles; navegación funcional.

## Limitaciones deliberadas

- No se validó contra datos reales: corresponde a la validación manual del usuario. Las pruebas automáticas usan fixtures y red interceptada.
- No se calcula un período efectivo cuando el endpoint no lo devuelve. Se distingue expresamente del rango solicitado.
- Kardex no devuelve moneda. Auditoría solo devuelve IDs de empresa/sucursal; no se solicitan nombres por cada fila.
- Moneda, bodega, ubicación y usuario se filtran por identificador. Los demás selectores necesitan su permiso de catálogo independiente del permiso de reportes.
- No existe consolidación multiempresa/multimoneda, exportación, impresión, PDF/Excel, gráficos complejos, notificaciones ni funcionalidades de otra fase.
- No se reejecutó la suite backend ni se modificó su base aprobada de 617 pruebas.

FASE VISUAL 8 IMPLEMENTADA — PENDIENTE DE VALIDACIÓN MANUAL DEL USUARIO
