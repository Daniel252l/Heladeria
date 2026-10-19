# Fase 10 — Reportes, auditoría y hardening

Estado: implementación terminada y regresión técnica Release completada; pendiente de validación manual, no aprobada. Diagnóstico previo realizado el 2026-10-19, antes de editar código funcional.

## Diagnóstico previo

- Arquitectura .NET 8 por capas, Database First, 128 entidades. Se inspeccionaron controladores, contratos, servicios, configuración, escritores de auditoría, operaciones financieras y pruebas. No hay controlador/servicio REST de reportes; `Reportes` es un catálogo (Id, módulo, descripción, estado, tipo de cliente, comentario), no datos operativos. No existe `OperacionesAuditoria` en modelo ni SQL.
- Consulta **solo lectura de sys.tables/sys.columns/sys.indexes en SQL Server configurado para desarrollo** confirmó Bitacoras, HistorialAccesos, snapshots nullable, rowversion y los índices actuales. No se consultaron secretos ni se modificaron datos reales.
- Bitacoras registra seguridad, configuración, inventario/productos, compras, producción, ventas, caja y cartera. No tiene columna resultado ni traceId. ValorAnterior/ValorNuevo/Comentario/Equipo son texto libre: no se publicarán. HistorialAccesos y SesionesUsuarios no conservan empresa/sucursal del evento; no es seguro atribuir retrospectivamente un login a una sucursal por la asignación actual.
- Consultas existentes son operativas, no reportes: Caja/Cartera incluso usan reservas/transacciones de escritura para lecturas consistentes; no se reutilizará ese camino para reportes. ResumenCartera materializa filas; el nuevo reporte agregará en SQL. Catálogos/referencias operativos aún tienen consultas sin paginar y POS tiene consultas por producto: riesgo residual documentado, no se rediseñan módulos aprobados.
- Ventas distingue COMPLETADA, pagos de contado y aplicaciones CxC. CostoTotal/TotalLinea/CantidadBase históricos pueden ser NULL. OrdenesCompra.EsCredito NULL significa desconocido. Caja distingue APERTURA_CAJA y EsEfectivo; no sumar apertura como ingreso. Separar siempre monedas. Inventario se valoriza en moneda base de empresa, no se inventa conversión.
- Políticas de permisos cubren endpoints operativos. Lecturas de catálogos de configuración son deliberadamente para autenticados; escrituras requieren CONFIGURACION.ADMINISTRAR. Alcance financiero es empresa/sucursal activa; inventario admite sucursales asignadas. Reportes comprobará asignación operativa de cada sucursal solicitada y empresa del JWT.
- JWT firma HS256, issuer/audience, expiración 10 minutos y skew 15 segundos. Falta lista explícita de algoritmos aceptados. Refresh aleatorio, solo hash SHA256 en sesión, rotación, vencimiento absoluto, revocación y sesión lógica única; login serializa usuario con UPDLOCK/HOLDLOCK. PasswordHasher y contraseña temporal ya existen. Bloqueo real persiste mediante AccessFailedCount + fecha BLOQUEO_APLICADO en HistorialAccesos; sobrevive reinicio, depende de conservar ese historial. No requiere columna nueva para esta fase.
- Cookies HttpOnly, Path /api/autenticacion, Secure fuera de desarrollo; SameSite Strict por defecto, None en desarrollo. CORS usa orígenes explícitos. Falta comprobación de Origin en operaciones de cookie cuando se permite SameSite=None. API financiera usa Bearer, no cookie para autorizar operaciones. Data Protection no participa en JWT/refresh personalizados; no se introducirá almacenamiento de claves innecesario.
- Excepciones tienen mensaje/código/traceId, sin stack en respuesta. El logger y el middleware de excepciones del framework pueden registrar excepciones crudas. Validación MVC usa ProblemDetails con errores que pueden reflejar inputs. Se endurecerán sin publicar valores. No se encontró logging intencional de contraseñas/tokens ni EnableSensitiveDataLogging.
- HTTPS, HSTS fuera de desarrollo, nosniff, DENY, no-referrer y Swagger solo Development ya existen. /salud mezcla disponibilidad SQL y vida; faltan /health/live y /health/ready. Rate limit por IP en login/refresh/cambio de contraseña; no hay recuperación ni códigos implementados. No se agregan límites agresivos a lectura.
- TimeProvider, timestamps UTC, fechas civiles y zona efectiva ya están implementados. No se desplazarán históricos. Rowversion, UUID, huellas, locks SQL y transacciones financieras permanecen intactos. SQL dinámico existente es estructural controlado; inputs operativos usan parámetros. No se introducirá SQL crudo en reportes.
- Paginación base limita tamaño a 100, pero multiplicación página*tamaño puede desbordarse: requiere validación transversal. Reportes tendrá además rango máximo de fechas, orden estable y proyección paginada.
- Índices reales: PK Bitacoras; ventas por empresa/número y UUID; detalles por venta/línea; Kardex por detalle/bodega/tipo; existencias por empresa/producto/bodega/ubicación; CxC por venta, CxP por factura. No hay índices de reporte por empresa/sucursal/fecha. Se documentarán candidatos, sin alterar esquema sin planes/volumen que demuestren necesidad.
- appsettings base/producción no contienen clave JWT ni credenciales reales; Testing contiene clave sintética de pruebas. Desarrollo usa conexión integrada. Secretos deben venir de proveedor externo. No se imprimieron claves ni cadenas de conexión.
- `dotnet restore`: correcto. Auditoría NuGet incluyendo transitivas: sin paquetes vulnerables reportados en los seis proyectos (fuente api.nuget.org). No se actualizan paquetes.

## Plan acotado

Ocho reportes JSON y auditoría sobre tablas existentes; dos permisos nuevos mediante preparación explícita/idempotente ADMIN; sin tablas, migraciones, frontend, operaciones financieras ni escritura en BD real. Pruebas SQL Server aisladas para traducción/agregaciones y pruebas HTTP de seguridad.

## Endpoints y permisos

Todos los reportes requieren Bearer, sesión válida, contraseña no temporal y `REPORTES.CONSULTAR`. Auditoría requiere `AUDITORIA.CONSULTAR`. Los permisos no sustituyen el filtro por empresa ni la asignación operativa de sucursal.

| GET | Filtros específicos | Respuesta |
|---|---|---|
| `/api/reportes/ventas` | clienteId, estado, monedaId | ventas: datos/totales/periodo; cobrosPorForma |
| `/api/reportes/productos-vendidos` | productoId, clienteId, monedaId; estado solo COMPLETADA | página por producto/presentación/unidades/moneda |
| `/api/reportes/inventario` | productoId, bodegaId, ubicacionId; sin fechas | datos, valorInventario global, monedaBase, sucursalId |
| `/api/reportes/kardex` | productoId, bodegaId, concepto (código), tipo ENTRADA/SALIDA | página de movimientos base con origen y usuario |
| `/api/reportes/compras` | proveedorId, monedaId, estado, esCredito | datos/totales/periodo |
| `/api/reportes/caja` | cajaId, usuarioId, estado ABIERTA/CERRADA | datos/totales por forma/periodo |
| `/api/reportes/cuentas-por-cobrar` | terceroId (cliente), monedaId, estado, vencimiento | datos/totales/periodo |
| `/api/reportes/cuentas-por-pagar` | terceroId (proveedor), monedaId, estado, vencimiento | datos/totales/periodo |
| `/api/auditoria` | usuarioId, modulo, accion, entidad | página de metadatos seguros de Bitacoras |
| `/health/live` | ninguno | proceso vivo, no consulta SQL, anónimo |
| `/health/ready` | ninguno | disponibilidad de SQL, 200/503 sanitizado, anónimo |

Filtros comunes: `sucursalId` (por defecto la activa), `busqueda` (150 caracteres), `pagina` (1 por defecto), `tamanioPagina` (20 por defecto, máximo 100). Excepto inventario, `fechaDesde`/`fechaHasta` son fechas civiles ISO `YYYY-MM-DD`, inclusivas; por defecto los últimos 30 días civiles, incluyendo hoy en la zona efectiva de la sucursal. Máximo 366 días por petición; años 1900–9998. Si se envía solo hasta, desde será 29 días antes. Si se envía solo desde, hasta será hoy. Una cartera emitida antes del rango no se incluye: indicar un rango explícito para revisar pendientes antiguos, y recorrer ventanas para más de un año. No se ofrece un saldo histórico «al cierre».

`datos` usa el contrato existente `{elementos,pagina,tamanioPagina,totalRegistros,totalPaginas}`. Totales globales son del filtro completo, no de la página. Agrupaciones resumen limitadas a 100; si se exceden se rechaza con 400, nunca se truncan silenciosamente. Orden estable con IDs como desempate. Productos devuelve grupos paginados, no totales monetarios sumados entre monedas.

Estados de ventas: BORRADOR, CONFIRMADA, COMPLETADA, ANULADA; por defecto COMPLETADA. Compras: BORRADOR, CONFIRMADA, PARCIALMENTE_RECIBIDA, RECIBIDA, ANULADA. Cartera: PENDIENTE, PARCIAL, PAGADA, ANULADA; vencimiento: VIGENTE, VENCIDA, PAGADA. Códigos en mayúsculas. Parámetros no pertenecientes al contrato no se utilizan.

### Semántica de importes y fechas

- Ventas filtra **FechaVenta**, no fecha del pago. Desglose de cobros es el aplicado acumulado a esas ventas, incluso si se cobró en otra fecha. COMPLETADA no significa COBRADA: `estadoCobro` informa PENDIENTE/PARCIAL/COBRADA/SIN_IMPORTE/NO_APLICA y `cobrado` suma pagos vigentes directos y aplicaciones CxC. El desglose por forma de aplicaciones históricas se limita a pagos atribuibles inequívocamente a una sola CxC; no inventa distribución de pagos históricos multicuenta.
- Ventas/products no recalculan costos usando costo promedio actual. Usan CostoTotal persistido de cada línea. Margen = subtotal neto − descuento neto − costo, excluye impuestos. Si falta algún snapshot/costo o el costo no es comparable con la moneda de venta (`TipoCambio != 1` o moneda no base), costo/margen se devuelven NULL. También NULL para ventas no completadas. No se implementan conversiones históricas ficticias.
- Productos agrupa por producto/presentación/unidades/moneda, no mezcla litros con vasos ni monedas. CantidadBase/Importe permanecen NULL si alguna línea histórica carece del snapshot correspondiente. Nombres/SKU/presentación provienen del catálogo actual; no hay snapshot nominal histórico.
- Inventario: filas de Existencias de productos con manejo de existencia; productos sin stock registrado no generan filas sintéticas. Incluye existencias de productos inactivos si manejan stock. Disponible = cantidad − reservada; valorización = cantidad × costo promedio (no solo disponible). MonedaBase es NULL si no hay una única moneda base operativa: el importe no debe etiquetarse con una moneda inventada.
- Kardex conserva cantidades/costos en unidad base y saldo persistido. Orden descendente por fecha, movimiento e Id. Nunca recalcula movimientos históricos.
- Compras filtra fecha civil de emisión, sin convertirla a UTC. Recibido se expresa como número de recepciones y líneas completamente recibidas, evitando sumar cantidades de unidades heterogéneas. No declara una orden «pagada» por estar recibida. Vínculo CxP solo cuando existe una cuenta inequívoca; saldo NULL sin cuenta, con multiplicidad histórica o moneda no comparable. EsCredito NULL sigue siendo desconocido.
- Caja filtra por **fecha de apertura de sesión**; los importes incluyen todos los movimientos de las sesiones seleccionadas, no solo movimientos del rango. APERTURA_CAJA va separada; ingresos = INGRESO_CAJA + COBRO_VENTA + COBRO_CUENTA_POR_COBRAR; egresos = EGRESO_CAJA + PAGO_CUENTA_POR_PAGAR. Saldo teórico = inicial + ingresos − egresos. Efectivo esperado aplica solo movimientos EsEfectivo=true; diferencia = contado − esperado. Las formas no efectivas no aumentan efectivo. Históricos sin metadatos completos no se reinterpretan: importes derivados NULL, `conciliable=false`, excluidos del resumen por forma. `conciliable` indica que se puede calcular, no una certificación de conciliación contable.
- CxC/CxP filtra emisión civil y muestra saldo **actual**, no saldo a fechaHasta. Aplicado = original − saldo persistido. Resúmenes por moneda excluyen ANULADA de pendiente/vigente/vencido; cantidad incluye todas las cuentas filtradas. Vence hoy es VIGENTE. PAGADA requiere estado PAGADA y saldo cero; inconsistencias de saldo cero con otro estado se identifican REQUIERE_REVISION. No se reparan.
- Timestamps se publican UTC con Z; días civiles no llevan offset. Se usa TimeProvider y zona sucursal→empresa→configuración. La conversión de un límite civil ambiguo/inexistente devuelve 400, no un desplazamiento arbitrario.

## Auditoría segura

Se reutiliza Bitacoras, sin auditoría paralela. DTO: Id, Fecha, UsuarioId/UserName, Modulo, Entidad/EntidadId, Accion, EmpresaId, SucursalId y Resultado. `REGISTRADA` no afirma éxito financiero: la tabla no tiene resultado explícito. Acciones terminadas en RECHAZADA/RECHAZADO y ACCESO_DENEGADO se señalan RECHAZADA.

No se seleccionan ni serializan ValorAnterior, ValorNuevo, Comentario, Equipo, IP, contraseñas, hashes, tokens, cookies ni documentos de pago. Búsqueda solo en metadatos, nunca en payloads. No se publican registros sin alcance empresa/sucursal ni HistorialAccesos/SesionesUsuarios: no poseen el alcance histórico necesario. La trazabilidad disponible es el Id de bitácora y documento/entidad; no se inventa traceId histórico. Los errores HTTP sí conservan traceId.

## Preparación explícita de permisos (no ejecutada contra BD real)

Desde el directorio de la solución y con la configuración externa de conexión correspondiente:

```powershell
dotnet run --project Heladeria.Bootstrap -- --preparar-fase10
```

Confirmación interactiva: `PREPARAR FASE 10`. No pide contraseña ni crea usuarios.

Orden: validar administrador principal/estado existente; crear o reutilizar acción CONSULTAR; módulos REPORTES/AUDITORIA; permisos REPORTES.CONSULTAR y AUDITORIA.CONSULTAR; relaciones RolesPermisos solo ADMIN; marcador FASE10_CONFIGURADA. IDs generados por SQL. Ejecución serializable + applock, transaccional e idempotente; reutiliza registros compatibles, rechaza incompatibles sin sobrescribir. Lista los registros efectivamente creados. No otorga permisos a CONSULTA. No se ejecuta al arrancar API.

No se requiere script SQL, cambio de esquema, migración ni re-scaffold EF. El maestro permanece intacto. Las únicas escrituras nuevas de preparación son datos de catálogo/roles y una bitácora, cuando el usuario ejecute explícitamente el comando.

## Hardening aplicado y compatibilidad

- JWT acepta explícitamente HS256, exige firma/expiración; valida issuer/audience no vacíos y SameSite soportado. Sin detalles internos en challenge.
- Origin/Fetch-Metadata en POST de autenticación: rechaza orígenes ajenos antes de usar/rotar cookies. Permite mismo origen (Swagger), orígenes CORS explícitos y clientes no navegador sin Origin. CORS rechaza comodines, userinfo y rutas mal configuradas. No se cambia el cliente frontend.
- HTTPS/HSTS, headers existentes y Bearer de Swagger se reutilizan. Se añade Cache-Control: no-store a `/api`. Swagger continúa solo en Development.
- Live independiente de SQL y ready con dependencia SQL; `/salud` se conserva compatible.
- Validación HTTP mantiene controles DataAnnotations y devuelve 400 con código/traceId/errores por campo sin reflejar valores de entrada. No se eliminan reglas.
- Excepción no controlada registra solo tipo y traceId; no mensaje arbitrario/stack. Se evita el log crudo duplicado del ExceptionHandlerMiddleware del framework. Los logs deben mantenerse con acceso restringido; no habilitar SensitiveDataLogging ni logging de cuerpos/cookies/Authorization en proxy/APM.
- Paginación: se detectó multiplicación int susceptible de desbordamiento en configuración, productos, compras, producción, caja y cartera. Helper común calcula en long y satura el offset. Los contratos operativos existentes conservan página vacía fuera de rango (incluido Clientes); los nuevos reportes rechazan offsets abusivos con 400. Ventas ya tenía validación propia y Clientes ya calculaba en long: se conservan.
- No se alteró escritura financiera, rowversion, UUID, locks, reintentos, cálculo de costo ni lógica de permisos existentes.

## Pruebas, rendimiento y riesgos residuales

Pruebas nuevas: HTTP 401/403 por cada endpoint; filtros inválidos y paginación; aislamiento empresa/sucursal de los nueve caminos; totales fuera de página, orden, fechas civiles/límites UTC, monedas separadas, costos históricos NULL, productos, inventario, auditoría sin secretos; errores/logs sanitizados, JWT HS256, Origin y salud; SQL Server LocalDB privado con agregaciones reales, CxC/CxP parcial/pagada/vencida, caja con efectivo/tarjeta, compra recibida, Kardex, ausencia de escrituras de reportes y preparación idempotente/rollback. Los fixtures LocalDB tienen nombres únicos, no leen appsettings y se eliminan al finalizar.

La primera regresión detectó una incompatibilidad introducida al rechazar globalmente páginas enormes: una prueba existente de Clientes exige 200/página vacía. Se corrigió la implementación, **sin modificar esa prueba**. Una compilación intermedia coincidió con testhost aún activo y falló por DLL bloqueadas; se esperó a su finalización antes del cierre secuencial. No es una regresión funcional ni se deshabilitaron tests.

### Resultado final de validación (2026-10-19)

| Comando | Resultado |
|---|---|
| `dotnet restore` | Correcto, código de salida 0. |
| `dotnet build --configuration Release` | Correcto, 0 errores y 0 advertencias. |
| `dotnet test --configuration Release --logger "trx;LogFileName=fase10-cierre.trx"` | Suite completa: 577 aprobadas, 0 fallidas, 0 omitidas; 2 min 40 s; código de salida 0. Incluye 52 pruebas nuevas de Fase 10 y 525 anteriores. |
| `dotnet list package --vulnerable` | Completado contra NuGet; ningún paquete vulnerable reportado en los seis proyectos. |
| `dotnet list package --vulnerable --include-transitive` | Completado; ningún paquete vulnerable reportado, incluidas dependencias transitivas. |

Evidencia de la última ejecución: `Heladeria.Pruebas/TestResults/fase10-cierre.trx`. Una ejecución anterior con `dotnet test --configuration Release --no-build` también aprobó las 577 pruebas; la ejecución final recompiló antes de probar el último ajuste de reutilización del conteo agregado. No se actualizaron paquetes.

**Limitación de entorno del comando literal `dotnet build` (Debug):** se intentó y terminó con 20 advertencias y 4 errores MSB3026/MSB3027/MSB3021 porque Visual Studio y la API Debug ya abierta tenían bloqueadas las DLL de salida. No se detuvieron ni reiniciaron procesos del usuario. Release sí compiló y probó correctamente. Antes de la validación manual, el usuario debe detener la API anterior, recompilar y reiniciarla para no servir binarios antiguos. No se afirma que el build Debug haya pasado.

La validación SQL y la preparación de permisos se ejecutaron exclusivamente en LocalDB aislado de pruebas. No se ejecutaron scripts ni bootstrap contra la BD real; la inspección de su esquema fue únicamente de lectura. No se modificaron frontend, esquema, históricos ni reglas financieras.

Riesgos/limitaciones:

1. Consultas agregadas ejecutadas en SQL, sin materialización de todo el conjunto ni N+1 por fila. Solo página y resúmenes acotados se materializan. Ventas, compras y cartera reutilizan el conteo del resumen, sin repetir Count. Reportes no llaman SaveChanges ni coordinadores transaccionales de escritura. Entre conteo, resumen y página puede haber cambios concurrentes: es una lectura operativa, no un snapshot financiero atómico certificado. No se activó SNAPSHOT en SQL ni se añadieron bloqueos largos.
2. El rango/límite de salida no reemplaza índices ni pruebas de carga. Candidatos a evaluar con planes reales: Ventas(EmpresaId,SucursalId,FechaVenta,Id), OrdenesCompra(EmpresaId,SucursalId,FechaEmision,Id), Bitacoras(EmpresaId,SucursalId,Fecha,Id), Kardex(EmpresaId,BodegaId,FechaMovimiento,Id), MovimientosCaja(TurnoCajaId), CxC/CxP(EmpresaId,SucursalId,FechaEmision,Id), TurnosCaja(CajaId,FechaApertura,Id). No se crearon sin medir volumen/planes; LIKE contiene sigue siendo no sargable. Queries tienen timeout del proveedor y CancellationToken, sin índices ficticios ni promesa de SLA.
3. Los metadatos antiguos sin alcance/costo/moneda confiable no se reconstruyen ni se actualizan. No hay dashboard global multiempresa ni agregación entre sucursales; se consulta una sucursal autorizada cada vez.
4. Bloqueo por cuenta depende de HistorialAccesos y configuración de duración. No purgar BLOQUEO_APLICADO vigente; cambiar la duración afecta la ventana derivada. Rate limit es local por IP/proceso; reverse proxy debe configurarse con proxies confiables, nunca confiar libremente en X-Forwarded-For. Despliegue multiinstancia/rate limit distribuido queda fuera de alcance.
5. No se habilitan recuperación, notificaciones, PDF/Excel, FEL, reversión financiera, BI ni frontend. Catálogos operativos grandes y POS N+1 preexistente quedan como trabajo de rendimiento posterior, no se rediseñan aquí.
6. Producción debe proporcionar clave JWT externa fuerte, conexión con mínimos privilegios/TLS, AllowedHosts, orígenes HTTPS concretos, retención de logs, respaldo y política de rotación de claves. La API no puede certificar la configuración efectiva de un despliegue que no se proporcionó. No se localizaron secretos reales en archivos de configuración inspeccionados; clave Testing es sintética.

## Checklist manual en Swagger

1. Revisar cambios y configurar conexión/secretos externos. Detener desde Visual Studio la API Debug anterior y compilar/reiniciar antes de probar; el proceso en ejecución conserva el binario anterior. Ejecutar preparación Fase 10 **solo cuando se autorice**; repetir y comprobar ausencia de duplicados. Entrar con ADMIN y contraseña definitiva. No es necesario ejecutar bootstrap inicial otra vez.
2. GET reportes/ventas con fechas y sucursal reales. Comparar total de COMPLETADAS contra SQL/ventas. Verificar una contado pendiente, otra cobrada y crédito parcial/pagada. Comparar cobrosPorForma con pagos, no con venta completada. Cambiar tamaño/página: totales iguales.
3. GET reportes/productos-vendidos: comparar cantidad comercial y base, presentación, costo snapshot y margen neto. Un costo desconocido debe ser NULL. Monedas distintas nunca se suman.
4. GET reportes/inventario y reportes/kardex: verificar producto/bodega/ubicación, unidad base, existencia×costo y orden determinista. Un servicio sin inventario no debe aparecer como existencia artificial.
5. GET reportes/compras: orden recibida con/sin factura, modalidad true/false/null, líneas recibidas; no interpretar recibido como pagado. Saldo solo si cuenta/moneda son inequívocos.
6. GET reportes/caja: sesión con apertura, ingresos, egresos y cobro mixto. Apertura separada de ingresos; tarjeta no suma efectivo; contado y diferencia al cerrar. Históricos incompletos no producen cifras ficticias.
7. GET reportes/cuentas-por-cobrar y cuentas-por-pagar: filtrar emisión para incluir documentos antiguos; revisar vigente (vence hoy incluido), vencida, pagada y anulada; original−saldo=aplicado; resumen por moneda.
8. GET api/auditoria: filtrar usuario/módulo/acción/entidad/sucursal/fechas; paginar; confirmar que no aparecen comentarios, snapshots, hash, cookies ni tokens. Resultado REGISTRADA no debe presentarse como éxito certificado.
9. Sin Bearer: 401. Usuario CONSULTA sin nuevos permisos: 403. Sucursal sin asignación u otra empresa: 403 sin datos. Un filtro de producto/tercero ajeno no debe devolver filas.
10. pagina=0, tamanioPagina=101, overflow de offset, estado/código inválido, fechas invertidas o rango >366: 400 controlado con traceId. Rangos de día incluyen el día civil completo de la sucursal, no del servidor.
11. /health/live: 200 aun sin SQL; /health/ready: 503 si SQL no está disponible y sin detalles internos. Hacer esta verificación en entorno aislado, no detener SQL de producción.
12. Swagger mismo origen y frontend autorizado conservan login/refresh; Origin ajeno devuelve 403 sin cookie nueva. Verificar HttpOnly/Secure/SameSite/Path y no-store. Swagger no publicado en producción.

## Archivos creados/modificados

Nuevos:

- Heladeria.Aplicacion/Reportes/ContratosReportes.cs
- Heladeria.Aplicacion/Seguridad/Bootstrap/IPreparadorFase10.cs
- Heladeria.Infraestructura/Reportes/ServicioReportes.cs
- Heladeria.Infraestructura/Reportes/ServicioReportes.Ventas.cs
- Heladeria.Infraestructura/Reportes/ServicioReportes.InventarioCompras.cs
- Heladeria.Infraestructura/Reportes/ServicioReportes.Caja.cs
- Heladeria.Infraestructura/Reportes/ServicioReportes.CarteraAuditoria.cs
- Heladeria.Infraestructura/Seguridad/Bootstrap/PreparadorFase10.cs
- Heladeria.Api/Controllers/ReportesController.cs
- Heladeria.Api/Controllers/AuditoriaController.cs
- Heladeria.Api/Configuracion/OrigenAutenticacionMiddleware.cs
- Heladeria.Pruebas/Fase10/ReportesHttpPruebas.cs
- Heladeria.Pruebas/Fase10/ReportesServicioPruebas.cs
- Heladeria.Pruebas/Fase10/ReportesSqlPruebas.cs
- Heladeria.Pruebas/Fase10/HardeningPruebas.cs
- docs/FASE10_REPORTES_AUDITORIA_HARDENING.md

Modificados:

- Heladeria.Api/Program.cs
- Heladeria.Api/Configuracion/ConfiguracionCors.cs
- Heladeria.Api/Configuracion/ConfiguracionSeguridad.cs
- Heladeria.Api/Configuracion/EncabezadosSeguridadMiddleware.cs
- Heladeria.Api/ManejoErrores/ManejadorGlobalExcepciones.cs
- Heladeria.Aplicacion/Configuracion/ContratosConfiguracion.cs
- Heladeria.Aplicacion/Seguridad/Autorizacion/ContratosAutorizacion.cs
- Heladeria.Infraestructura/InyeccionDependencias.cs
- Heladeria.Infraestructura/Configuracion/ServicioConfiguracionBase.cs
- Heladeria.Infraestructura/Configuracion/ServicioOrganizacionConfiguracion.cs
- Heladeria.Infraestructura/Productos/ServicioFase4Base.cs
- Heladeria.Infraestructura/Compras/ServicioComprasBase.cs
- Heladeria.Infraestructura/Produccion/ServicioProduccionBase.cs
- Heladeria.Infraestructura/Caja/ServicioCaja.Consultas.cs
- Heladeria.Infraestructura/Cartera/ServicioCartera.Consultas.cs
- Heladeria.Bootstrap/Program.cs

No se modificaron frontend, SQL, entidades ni mapeo EF; ninguna prueba preexistente fue debilitada o eliminada.
