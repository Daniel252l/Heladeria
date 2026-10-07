# Fase 7 — Ventas / POS backend

## Diagnóstico previo

Existen `Ventas`, `VentasDetalle`, `Clientes`, `FormasPago`, `Pagos`, `PagoDetalles`, `VentaImpuestos`, `ProductoImpuestos`, `Impuestos`, `DevolucionesVentas`, `DevolucionVentaDetalles`, `SeriesDocumentos` y `CorrelativosDocumentos`. No existen tablas denominadas `MetodosPago`, `PagosVentas`, `DocumentosVenta`, `Descuentos` o `CostosVenta`. Hay promociones, pero están fuera de alcance.

Ventas ya tiene empresa, sucursal, cliente opcional, moneda, tipo de cambio, importes, flujo/estado y usuario. Caja/pedido son opcionales. No necesita crear un cliente ficticio ni un pago para guardarse. `Pagos` exige cliente o proveedor identificado: no es apropiado forzarlo para una venta anónima; Caja, pagos y CxC quedan fuera de esta fase.

Faltaban bodega/ubicación, RowVersion, clave de operación, auditoría de cierre/anulación y snapshots de unidad/factor/cantidad base/costo total. Se añaden sin inferir valores de ventas antiguas. Las ventas históricas incompletas son consultables, pero no operables automáticamente.

El `SubTotal` calculado heredado de `VentasDetalle` suma precio por cantidad menos descuento más impuesto, sin representar correctamente impuestos incluidos. Se conserva por compatibilidad, pero los nuevos importes autoritativos son `SubtotalNeto`, `MontoDescuento`, `Impuesto` y `TotalLinea`. `PrecioUnitario` es snapshot del precio de catálogo, posiblemente con impuesto incluido. La nueva tabla `VentaDetalleImpuestos` permite conservar tasas, nombres, inclusión y cálculo por línea; `VentaImpuestos` sigue almacenando el agregado por venta.

## Decisiones de alcance

- Flujo BORRADOR → CONFIRMADA → COMPLETADA; anulación desde cualquiera de esos estados. Una completada revierte inventario; nunca se borran movimientos originales.
- Contado, sin deuda ni registro de cobro/caja. `COMPLETADA` cierra el documento comercial y el inventario, no acredita un cobro procesado por Caja.
- Moneda base configurada en `Monedas`, tipo de cambio 1. No se interpreta el precio de catálogo en monedas extranjeras. Importes monetarios de dos decimales conforme a las columnas existentes.
- Precio de presentación prioritario. El referencial solo corresponde a unidad base o a una presentación de la misma unidad con factor 1; no se multiplica silenciosamente un precio referencial cuya semántica comercial no es segura.
- Descuento porcentual simple, entre 0 y 100 con dos decimales, permitido únicamente por producto. No se acepta precio, impuesto, total ni costo desde el cliente.
- Impuestos vigentes y activos por producto. Política simple sin composición: como máximo uno incluido y los adicionales calculados sobre la misma base neta. No se inventan impuestos, tasas ni FEL.
- Se reutiliza `ServicioInventario` para conversión y aplicación de salidas/entradas. Promedio, sin lotes/series/vencimiento; los productos que requieran esas estrategias o descomposición de combos se rechazan explícitamente.

## Modelo Database First

Se reutilizan las tablas comerciales existentes, los catálogos de producto/unidad/presentación, `Existencias`, `Movimientos`, `MovimientoDetalles`, `Kardex`, `CostosProductos`, `HistoricoCostosProductos`, empresas/sucursales/almacenes, flujos, numeración y seguridad.

Cambios controlados en `BaseDatos/Reparaciones/20261007_Fase7_VentasPos.sql`:

| Tabla | Ampliación |
|---|---|
| `Ventas` | BodegaId, UbicacionId, ClaveOperacion, VersionFila, fechas/usuarios de modificación/cierre/anulación, MotivoAnulacion |
| `VentasDetalle` | FactorConversion, CantidadBase, UnidadMedidaId, UnidadBaseId, ManejaExistencia, SubtotalNeto, DescuentoPrecioCatalogo, TotalLinea, CostoTotal, FuentePrecio |
| `VentaDetalleImpuestos` (nueva) | Snapshot del impuesto aplicado a cada línea: código, nombre, porcentaje, inclusión, base y monto |

`VentasDetalle.CostoUnitario` existente guarda **costo por unidad base** en las ventas nuevas. Cantidades/factores/costos: decimal(19,6). Precio e importes comerciales: decimal(12,2). No hay una tabla paralela de costos de venta ni un motor de inventario adicional.

`NumeroFactura` existente almacena el número comercial interno `VT-0000000001`; **no representa una factura FEL**. `VersionFila` es ROWVERSION de SQL Server, expuesto como Base64. La clave UUID tiene índice único filtrado por empresa, excluyendo NULL.

El SQL es transaccional, usa XACT_ABORT y reserva de esquema. Las referencias a columnas recién agregadas se ejecutan con SQL dinámico constante/controlado, evitando errores de compilación del mismo batch. Revisa columnas, tipos, nulabilidad, índices, FK y restricciones existentes. Un objeto incompatible provoca rechazo, no sobrescritura ni inferencia de históricos. Los nuevos snapshots quedan NULL en históricos; no se altera el costo ni el importe anterior.

`BaseDatos/Heladeria.sql` incorpora exactamente la misma ampliación al final. El modelo EF se amplió mediante `Ventas.Fase7.cs` y `HeladeriaDbContext.Fase7.cs`; no hay migraciones EF. En un futuro re-scaffold de Database First hay que consolidar esas propiedades/configuraciones con las regeneradas para no duplicarlas.

## Flujo y operaciones

1. Crear BORRADOR: valida referencias, calcula y guarda snapshots; asigna correlativo VT. No reserva stock.
2. Actualizar BORRADOR: requiere versión actual y recalcula todo el detalle en backend; conserva número de venta.
3. Confirmar: requiere versión actual y valida que la configuración comercial siga coincidiendo con el snapshot. No reserva ni descuenta stock.
4. Completar CONFIRMADA: requiere versión actual, UUID no vacío y permisos de venta/POS. Valida todas las líneas y el stock acumulado por producto antes de aplicar salidas. Guarda costos y cierra dentro de la misma transacción.
5. Anular: motivo de 3–500 caracteres y versión actual. Borrador/confirmada no generan inventario. Completada genera entradas reversas y conserva los movimientos originales. Segunda anulación devuelve 409.

Si cambian precio, impuesto vigente, factor, unidad o condición inventariable entre el borrador y el cierre, responde 409: actualizar el borrador o anular la confirmada y crear otra. Nunca se cambia silenciosamente el precio aceptado.

Se rechazan operaciones sobre ventas de otros flujos, crédito, Caja, pedidos o históricos sin snapshots. La anulación de una completada también se bloquea si tiene devoluciones, notas de crédito/débito o cuentas por cobrar. Si el producto/almacén quedó inactivo o cambió su unidad base, no se fuerza una reversión insegura: se requiere regularizar la configuración.

## Cálculos

Todo usa `decimal` en servidor. El cliente envía únicamente producto/presentación/cantidad/porcentaje de descuento, no precio, total, impuesto, costo ni empresa.

- `CantidadBase = Cantidad × FactorConversion`, validando precisión de unidad de entrada y unidad base mediante el motor existente. No se inventan factores ni se redondean cantidades inválidas.
- Unidad base: `ProductoPresentacionId=null`, factor 1.
- Presentación: debe pertenecer al producto/empresa, estar activa y permitir venta; su catálogo y unidad también deben estar operativos.
- Precio: `PrecioVentaEstandar` de presentación; si es NULL, solo se permite usar `PrecioVentaReferencial` cuando la unidad coincide con la base y el factor es 1. Un precio configurado en cero/negativo no activa un fallback. Precio inexistente/inválido rechaza la venta.
- Descuento porcentual entre 0 y 100, hasta dos decimales; solo con `PermiteDescuento=true`.
- Impuestos: asociaciones e impuestos activos/vigentes de la empresa. Una asociación tributaria ajena provoca conflicto, no se omite silenciosamente. Sin impuestos configurados/vigentes no se inventa una tasa. Máximo un impuesto incluido; los agregados no se componen entre sí.
- Redondeo monetario por línea a dos decimales, `AwayFromZero`. Header suma los resultados de las líneas. Cada importe se valida contra su capacidad SQL.

Sin impuesto incluido: cantidad × precio → subtotal; descuento porcentual → base neta; impuestos agregados → total.

Con impuesto incluido, primero se separa la base neta. Ejemplo: precio de catálogo 112, IVA incluido 12%, descuento 10%:

| Concepto | Valor |
|---|---:|
| SubtotalNeto | 100.00 |
| DescuentoPrecioCatalogo | 11.20 |
| MontoDescuento (neto) | 10.00 |
| BaseImponible | 90.00 |
| Impuesto | 10.80 |
| TotalLinea | 100.80 |

Así se cumple `SubtotalNeto - MontoDescuento + Impuesto = TotalLinea` sin cobrar dos veces el impuesto incluido. `VentaImpuestos` conserva el agregado; el detalle conserva la política aplicada aunque cambie luego el catálogo.

## Inventario, costo y reversión

Ejemplo: 2 L × factor 1000 = 2000 ml. Si la existencia de la ubicación tiene costo promedio base 0.006540, el costo de venta es 13.080000. El precio comercial y ese costo son independientes.

El cierre usa el **costo promedio actual de la existencia de esa ubicación**, no un costo global ni un costo enviado por el cliente. Registra ese costo y su total en cada línea. El concepto `VENTA-SALIDA` no afecta el costo promedio ni `CostosProductos`; sí actualiza cantidad y genera Movimiento/Detalle/Kardex. Cada línea inventariable produce un movimiento con serie VS, número correlativo, ModuloOrigen=VENTAS y DocumentoOrigenId de la venta.

Para un producto vendido en varias presentaciones se suma primero la cantidad base requerida: no basta con que cada línea aislada tenga stock. Se descuentan reservas de la cantidad disponible. Si falta stock en cualquier producto, se rechaza todo el cierre con `EXISTENCIA_INSUFICIENTE` (409).

La anulación genera `VENTA-ANULACION-ENTRADA` (serie VA), en la bodega/ubicación original, por la cantidad base histórica y al costo base guardado al vender. No depende del factor actual de la presentación ni borra la salida. La entrada **sí** participa en la valoración promedio existente, igual que una devolución al costo original; puede actualizar `CostosProductos`/`HistoricoCostosProductos` a través del mismo motor. Esto es distinto de alterar costos por una salida de venta.

Productos no inventariables válidos se venden sin movimiento y con costo de inventario cero. No se soportan lotes, series, vencimientos, FIFO u otras valoraciones ni explosión de combos en este alcance. El listado POS excluye las configuraciones no soportadas y la escritura las rechaza.

## Atomicidad, idempotencia y seguridad

- Transacción Serializable, coordinador compartido de inventario y `sp_getapplock` empresarial `Heladeria.Inventario.{EmpresaId}`. Compatible con Compras, Producción, Productos e Inventario existentes.
- Bajo esa reserva, lecturas/locks/aplicaciones de productos se ordenan por ProductoId y número de línea. En SQL Server se toman además reservas por producto en ese mismo orden.
- RowVersion protege venta, existencias y correlativos con el modelo existente. No hay reintento automático ciego de una operación de venta.
- Repetir completar una venta ya completada con **la misma UUID** devuelve su resultado sin nuevas salidas, incluso si la versión del primer intento quedó obsoleta. Otra UUID para esa venta devuelve conflicto; una UUID ya usada por otra venta de la empresa también devuelve conflicto. Una anulada nunca se vuelve a completar.
- Rollback revierte documento, líneas, costos, movimientos, Kardex, existencias, correlativos y auditoría de éxito. La auditoría de un rechazo controlado se registra **separadamente después del rollback**; no es una instalación/venta parcial. Si falla también la conexión de auditoría, se conserva el error original y el manejador HTTP registra el rechazo con TraceId.
- Empresa y sucursal proceden del contexto autenticado. Nunca se sustituye un Id de URL por un Id de contexto. Se comprueba asignación operativa del usuario a la sucursal y pertenencia del cliente/producto/presentación/bodega/ubicación. Recursos fuera del alcance devuelven 404.
- Se conservan JWT, sesiones, cambio de contraseña obligatorio, autorización y limitación de solicitudes existentes. No se han modificado los límites ni el login.
- Auditoría: VENTA_CREADA, VENTA_MODIFICADA, VENTA_CONFIRMADA, VENTA_COMPLETADA, VENTA_ANULADA, VENTA_RECHAZADA; el motor registra VENTA_INVENTARIO_DESCONTADO y VENTA_INVENTARIO_REVERTIDO con módulo INVENTARIO y referencias al movimiento.

## API y permisos

| Método y ruta | Permiso |
|---|---|
| GET /api/ventas | VENTAS.CONSULTAR |
| GET /api/ventas/{id} | VENTAS.CONSULTAR |
| POST /api/ventas | VENTAS.ADMINISTRAR |
| PUT /api/ventas/{id} | VENTAS.ADMINISTRAR |
| POST /api/ventas/{id}/confirmar | VENTAS.ADMINISTRAR |
| POST /api/ventas/{id}/completar | VENTAS.VENDER **y** POS.OPERAR |
| POST /api/ventas/{id}/anular | VENTAS.ANULAR |
| GET /api/ventas/referencias | POS.OPERAR |
| GET /api/ventas/productos | POS.OPERAR |
| GET /api/ventas/productos/{productoId}/presentaciones | POS.OPERAR |

Creación responde 201, consultas/operaciones exitosas 200; validación 400, sin JWT 401, falta de permiso 403, fuera de alcance/inexistente 404, estado/concurrencia/stock 409. Respuesta de error conserva `exito`, `mensaje`, `codigo` y `traceId`.

Listado de ventas: `busqueda` (número/cliente), `estadoId`, `fechaDesde`, `fechaHasta`, `pagina`, `tamanioPagina` (1–100). No incluye ventas de otra sucursal aunque el usuario tenga asignaciones a ambas: debe cambiar de sucursal por el mecanismo de sesión existente.

## Contratos para el futuro POS

`GET /api/ventas/referencias` devuelve moneda base/FX, estados, clientes y categorías operativos, bodegas/ubicaciones accesibles de la sucursal activa.

`GET /api/ventas/productos?busqueda=...&categoriaId=...&pagina=1&tamanioPagina=20&bodegaId=...&ubicacionId=...` devuelve página `{ elementos, pagina, tamanioPagina, totalRegistros, totalPaginas }`. Bodega y ubicación son opcionales; ubicación requiere bodega.

Cada producto contiene SKU/nombre/imagen/categoría, unidad base, condición inventariable/descuento, moneda, presentaciones e impuestos. `presentaciones` contiene la opción base **más** las presentaciones vendibles; cada opción incluye Id, unidad/abreviatura/factor/precio/fuente y `precioValido`. Una presentación sin precio puede verse como no disponible para vender; el cierre nunca inventa precio.

Sin bodega, `disponibleBase=null` (no inventar disponibilidad). Con bodega se suma existencia menos reservas de sus ubicaciones operativas; con ubicación se restringe a ella. Es informativo: el cierre valida nuevamente la ubicación exacta. No inventariables tienen disponibilidad NULL.

El endpoint `.../{productoId}/presentaciones` devuelve **un objeto ProductoPosDto**, no un array raíz: consumir `respuesta.presentaciones`. No exige ensamblar múltiples endpoints por producto.

`VentaConsultaDto` devuelve `encabezado`, `detalles` y `movimientos`. La cabecera tiene versión Base64, estado y clave de operación; cada línea expone conversión/precio/costo/impuestos guardados. Costos mantienen hasta seis decimales en JSON; el futuro frontend no debe redondearlos a dos.

## Activación manual: SQL primero, preparación después

**No se ejecutó ninguna preparación CLI ni ningún cambio contra la BD real durante esta implementación.**

1. Respaldar y revisar la BD de destino y el script. Detener temporalmente escrituras durante el cambio de esquema.
2. En SSMS seleccionar explícitamente la base correcta y ejecutar **solo** `BaseDatos/Reparaciones/20261007_Fase7_VentasPos.sql` para una instalación existente. No ejecutar el maestro, que crea una BD nueva. El script de reparación opera sobre la base seleccionada, no incluye un `USE` impuesto.
3. Esperar el mensaje `FASE 7: esquema de ventas preparado...`. Ante error, corregir la incompatibilidad indicada y repetir; no borrar históricos ni rellenar factores inventados.
4. Desde el directorio de la solución, con la conexión segura habitual del bootstrap configurada mediante el proveedor de secretos/entorno existente:

```powershell
dotnet run --project Heladeria.Bootstrap --configuration Release -- --preparar-fase7
```

Confirmación interactiva exacta: `PREPARAR FASE 7`.

5. La preparación toma la empresa de la asignación principal operativa de ADMIN (orden por usuario), igual que las fases anteriores. Para otras empresas no se concede ni prepara configuración de forma implícita; requieren preparación explícitamente definida para ese ámbito.

La utilidad comprueba esquema y dependencias previas. En una transacción crea/reutiliza módulos VENTAS/POS; acciones CONSULTAR/ADMINISTRAR/VENDER/ANULAR/OPERAR; cinco permisos con asignación **solo ADMIN**; flujo VENTA_POS con cuatro estados y cinco transiciones; dos conceptos; ClaseTipos DOCUMENTOS_VENTAS con tipos Venta POS/Salida por venta/Anulación de venta; series VT/VS/VA y correlativos; bitácora FASE7_CONFIGURADA. Los IDs identity vienen de SQL Server; los catálogos sin identity usan el mecanismo existente bajo reserva/transacción. La consola informa los registros realmente creados.

Repetir con configuración compatible no duplica registros ni reinicia números. Configuración incompatible se rechaza; no se sobrescribe. No crea clientes, monedas, formas de pago, productos, precios, ventas ni existencias. No se ejecuta al arrancar la API.

## Prueba manual con datos existentes

Antes de vender verificar moneda base activa con dos decimales, presentación con precio positivo y PermiteVenta, producto activo/vendible, almacén accesible y stock suficiente. No asumir los IDs del ejemplo: obtenerlos desde referencias y búsqueda POS (puede buscar Helado Vainilla/Chocolate).

Crear venta con los IDs reales devueltos:

```json
{
  "sucursalId": "ID_REAL_DE_LA_SUCURSAL_ACTIVA",
  "bodegaId": "ID_REAL_DE_LA_BODEGA",
  "ubicacionId": "ID_REAL_DE_LA_UBICACION",
  "clienteId": null,
  "monedaId": "ID_REAL_DE_LA_MONEDA_BASE",
  "detalles": [{
    "productoId": "ID_REAL_DEL_PRODUCTO",
    "productoPresentacionId": "ID_REAL_DE_PRODUCTOPRESENTACIONES",
    "cantidad": 2,
    "porcentajeDescuento": 0
  }]
}
```

Los marcadores son explicativos: sustituirlos por números, no cadenas. Para unidad base usar `productoPresentacionId=null`. Confirmar con `{"versionFila":"BASE64_DEVUELTO"}`; volver a tomar la versión devuelta. Completar con `{"versionFila":"VERSION_CONFIRMADA","claveOperacion":"UUID_GENERADA_POR_EL_CLIENTE"}`. Reutilizar **esa misma UUID** si se pierde la respuesta. Anular con versión actual y motivo. Nunca cambiar de UUID para reintentar un cierre cuyo resultado se desconoce: consultar primero.

Verificación de solo lectura tras una venta controlada (sustituir parámetros por la venta/empresa autorizadas):

```sql
DECLARE @VentaId bigint = /* Id devuelto por la API */ NULL;
DECLARE @EmpresaId int = /* empresa del contexto */ NULL;
SELECT v.*, ef.Codigo AS Estado
FROM dbo.Ventas v JOIN dbo.EstadosFlujo ef ON ef.Id=v.EstadoId AND ef.FlujoEstadoId=v.FlujoEstadoId
WHERE v.Id=@VentaId AND v.EmpresaId=@EmpresaId;
SELECT d.* FROM dbo.VentasDetalle d JOIN dbo.Ventas v ON v.Id=d.VentaId
WHERE v.Id=@VentaId AND v.EmpresaId=@EmpresaId;
SELECT m.*, d.ProductoId, d.CantidadBase, d.CostoUnitario
FROM dbo.Movimientos m JOIN dbo.MovimientoDetalles d ON d.MovimientoId=m.Id
WHERE m.ModuloOrigen='VENTAS' AND m.DocumentoOrigenId=@VentaId AND m.EmpresaId=@EmpresaId;
SELECT * FROM dbo.Bitacoras WHERE EmpresaId=@EmpresaId
AND (Accion LIKE 'VENTA_%' OR Accion='FASE7_CONFIGURADA') ORDER BY Id DESC;
```

## Verificación automatizada

Pruebas nuevas en `Heladeria.Pruebas/Fase7`: cálculo decimal/tributario, precios reales/faltantes, multimedida, descuentos, filtros POS/paginación/disponibilidad, producto/presentación/estado/empresa, cliente/moneda/almacén, snapshots, transiciones, stock/reservas/agregado por producto, costo/Kardex, correlativos, doble cierre, claves repetidas, anulaciones y costo histórico tras entradas intermedias, concurrencia, RowVersion, IDOR, HTTP 201/200/400/401/403/404/409, permisos VENDER+POS, preparación idempotente/incompatible y rollback relacional.

Los tests SQLite provocan fallos reales tras una primera salida y al final de una anulación, comprobando rollback de stock, Kardex, documentos, costos, históricos y correlativos. Los tests concurrentes usan contextos independientes y el coordinador compartido. No equivalen a una prueba de carga distribuida entre réplicas SQL Server.

Se sustituyó la antigua prueba que conectaba a `appsettings.Development.json` y esperaba 127 tablas: ahora crea una instancia LocalDB privada, ejecuta el maestro, compara las 128 tablas con EF y verifica las nuevas consultas, eliminando luego únicamente sus recursos de prueba. Requiere Windows y SQL Server LocalDB (`SqlLocalDB.exe` en PATH); nunca lee la conexión real.

Validación SQL opt-in reproducible, también aislada:

```powershell
./BaseDatos/Validacion/Validar-Fase7.ps1
```

Requiere LocalDB y sqlcmd. Verifica esquema previo, repetición, estado parcial compatible, rechazo de columna incompatible con rollback del DDL, maestro íntegro, FK verificadas y ausencia de ventas creadas. Las bases e instancias desechables se eliminan al terminar. No invoca bootstrap.

### Cierre de validación técnica — 2026-10-07

- `dotnet build --configuration Release`: finalizó correctamente, 0 errores y 0 advertencias.
- `dotnet test --configuration Release --no-build`: suite completa sin filtros, 315 aprobadas, 0 fallidas y 0 omitidas (81 casos de Fase 7 y 234 casos anteriores).
- Regresión aprobada: seguridad/autenticación/permisos, configuración y alcance empresa/sucursal, productos, inventario, Kardex, costo promedio, compras, producción, recetas y concurrencia, además de Ventas/POS.
- No falló ninguna prueba durante este cierre; no se requirieron cambios de código, pruebas, expectativas ni esquema. Solo se actualizó este documento con el resultado final.
- Se confirmó la evidencia previa de validación SQL aislada: script repetible, preparación parcial compatible, rollback ante incompatibilidad y ejecución sin datos ficticios de negocio. El maestro y la reparación contienen la misma ampliación. La suite volvió a verificar el maestro en una instancia LocalDB desechable.
- No se ejecutaron scripts ni Bootstrap contra la BD real; tampoco se ejecutó la CLI de Bootstrap. Las pruebas de preparación usan exclusivamente sus bases aisladas.
- Fase 7 queda técnicamente lista para validación manual; esto no sustituye su aprobación funcional. La aplicación manual del script y la preparación explícita en la BD real permanecen pendientes del operador.

## Límites y pendientes manuales

- Aplicar el SQL y ejecutar la preparación explícita son tareas pendientes del operador en su BD real; no se hicieron automáticamente.
- Validar desde Swagger con los productos/precios/stock reales elegidos por el operador, incluidos reintento y anulación controlados.
- No se implementaron frontend, Caja/cobros/arqueos, FEL, impresión, delivery, crédito/CxC/CxP, notificaciones, fidelización ni promociones complejas. No se avanzó a Fase Visual 5.
