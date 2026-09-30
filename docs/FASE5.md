# Fase 5 — auditoría previa y contrato del flujo

## Flujo encontrado antes de implementar

Se revisó `BaseDatos/Heladeria.sql` completo y se contrastaron las entidades y el mapeo EF Database First. No existen `Compras`, `CompraDetalles` ni `Documentos`. El dominio real es **OrdenesCompra → RecepcionesCompra (parciales) → inventario**. `FacturasProveedores` puede referenciar una orden, pero ninguna recepción exige factura. `CuentasPorPagar` exige factura, no orden ni recepción: no se crea una obligación ficticia ni se implementan pagos.

Los estados comerciales pertenecen a `FlujosEstados`/`EstadosFlujo`, no a `Estados`. Los estados operativos de maestros pertenecen a `Estados`. El SQL no incluye datos iniciales de flujos de compras: se prepararán explícitamente los mínimos, sin modificar el esquema.

## Inventario de restricciones y datos obligatorios

Las PK documentales y de inventario son `Id IDENTITY`. Las referencias `Tipos`, `ClaseTipos`, `Estados`, `ClaseEstados`, `NivelesDeAccesos`, `Modulos` y `Roles` utilizan claves manuales. Nunca se presuponen sus valores.

| Tabla | Obligatorios, referencias y restricciones relevantes |
|---|---|
| Proveedores | Empresa FK, código varchar20, nombre150, DiasCredito smallint≥0, Estado FK. NIT20, teléfono20, correo100, dirección250, moneda FK y límite decimal12,2≥0 son nullable. UNIQUE(Empresa,Codigo). Sin RowVersion ni fechas de auditoría. |
| ProveedorProductos | Proveedor/Producto/Estado FK, EsPreferido. Código50 y PrecioUltimaCompra12,2 nullable. UNIQUE(Proveedor,Producto), incluso inactivos. No presentación, unidad, plazo ni empresa propios. |
| OrdenesCompra | Empresa/Sucursal/Proveedor/Moneda/Usuario FK; NumeroOrden30 UNIQUE(Empresa,Numero), fecha DT2(3), cambio19,8>0, SubTotal/Impuestos/TotalEstimado12,2, flujo+estado FK compuesta. Entrega esperada nullable. Sin RowVersion. |
| OrdenesCompraDetalle | Orden/Producto FK, línea>0 UNIQUE(Orden,Linea), cantidad19,6>0, precio12,2≥0, descuento/impuesto12,2. Presentación FK nullable. TotalLinea calculado persistido, decimal34,8 en EF. No factor/cantidad base/porcentaje/RowVersion. |
| RecepcionesCompra | Empresa/Sucursal/Orden/Usuario FK, número30 UNIQUE(Empresa,Numero), fecha DT2(3), flujo+estado FK compuesta. Observaciones500 nullable. No bodega, movimiento ni RowVersion. |
| RecepcionCompraDetalles | Recepción/DetalleOrden/Producto FK, cantidad19,6>0, costo19,6≥0. Lote FK/vencimiento nullable. No línea única, presentación, factor, bodega ni marca de aplicación. |
| FacturasProveedores | Empresa/Proveedor/Moneda/Estado FK, número50, fecha, cambio19,8>0, subtotal/impuestos/total12,2. Orden/serie30/vencimiento nullable. UNIQUE(Empresa,Proveedor,Serie,Numero). No detalle de factura. Solo auditada; no implementada en esta fase. |
| CuentasPorPagar | Empresa/Proveedor/Factura FK; emisión/vencimiento, monto/saldo12,2≥0, flujo+estado FK. UNIQUE(Factura). No es dependencia de la recepción. |
| HistoricoPreciosCompra | Empresa/Proveedor/Producto/Moneda FK, precio12,2≥0, fechaDT2(3); Recepcion FK nullable. No presentación ni UNIQUE. |
| SeriesDocumentos | Empresa/Tipo(Tipos)/Estado FK, serie20, longitud1..20; sucursal FK/prefijo20 nullable. UNIQUE(Empresa,Sucursal,Tipo,Serie). |
| Modulos / AccionesSistema | Módulo: Id20 manual, Nombre50, Icono50 y Activo obligatorios. Acción: Id identity, Codigo50/Nombre100/Estado FK; código único. |
| ClaseTipos / Tipos | Id int manual; clase: Nombre100 y Estado FK. Tipo: Nombre100, ClaseTipo/Estado FK; Modulo FK nullable; UNIQUE(ClaseTipo,Nombre). Se asigna MAX+1 bajo transacción/bloqueo, no un identity supuesto. |
| CorrelativosDocumentos | Serie FK UNIQUE, UltimoNumero bigint≥0, fechaDT2(3), RowVersion. |
| Bodegas | Empresa/Sucursal/Estado/Creador FK, código25 UNIQUE(Empresa,Codigo), nombre150 UNIQUE(Empresa,Sucursal,Nombre), bits negativo/principal, fecha, RowVersion. Padre y auditoría de modificación nullable. |
| Ubicaciones | Empresa/Bodega compuesta/TipoCatalogo/Estado/Creador FK; código40 UNIQUE(Empresa,Bodega,Codigo), nombre150, bits recepción/despacho, fecha, RowVersion. Padre y modificación nullable. |
| Productos | Empresa/Categoria/TipoProducto/UnidadBase FK compuestas; seguimiento/valuación Catalogos FK simples; SKU50 UNIQUE(Empresa,SKU), nombre200, bits comercial/seguimiento, Estado/Creador/fecha/RowVersion. Marca compuesta nullable. |
| ProductoPresentaciones | Empresa/Producto/Presentacion/Unidad FK compuestas, factor19,6>0, bits base/compra/venta, Estado/fecha/Creador/RowVersion. UNIQUE(Empresa,Producto,Presentacion). No snapshot de factor en orden. |
| UnidadesMedida / ConversionesUnidad | Unidad: Id int identity, Empresa/Creador/Estado FK, código20/nombre100 únicos por empresa, abreviatura20, permiso decimal y cantidad decimales0..6, fechas/RowVersion. Conversión: Id long identity, Empresa/Origen/Destino/Estado FK, factor19,8>0, unidades distintas y UNIQUE(Empresa,Origen,Destino); sin RowVersion. Las compras usan el factor real de ProductoPresentaciones; no concatenan conversiones alternativas. |
| Existencias | Empresa/Producto/Bodega/Ubicacion FK compuestas; UNIQUE(Empresa,Producto,Bodega,Ubicacion); cantidad/reservada/costo19,6; reservada/costo≥0; disponible calculado; RowVersion. |
| Movimientos | Empresa/Sucursal/Concepto FK, al menos una bodega, bodegas distintas; serie20+número>0 UNIQUE(Empresa,Serie,Numero); fecha, flujo+estado, fecha/Creador FK. Aplicación usuario/fecha ambos NULL o ambos presentes. Anulación usuario/fecha/motivo todos NULL o presentes. ModuloOrigen30/DocumentoOrigenId/Referencia100 permiten trazar recepción e idempotencia. Sin RowVersion/UNIQUE de origen. |
| MovimientoDetalles | Empresa/Movimiento/Producto/Unidad FK compuestas, línea>0 UNIQUE(Empresa,Movimiento,Linea), cantidad/factor/base19,6>0, costo19,6≥0. Presentación/lote compuestas y ubicaciones nullable. CostoTotal calculado. |
| Kardex | Empresa/Producto/Bodega/Ubicacion/Detalle FK; afectación ENTRADA/SALIDA; UNIQUE(Detalle,Bodega,Afectacion); cantidad>0 y costos≥0 decimal19,6; saldos, fechas. Lote nullable, total calculado. |
| CostosProductos | Empresa/Producto FK, costo19,6≥0, fecha, valuación Catalogos nullable. UNIQUE(Empresa,Producto). Sin RowVersion. |
| HistoricoCostosProductos | Empresa/Producto FK, costo nuevo19,6≥0, fecha, origen50; costo anterior/documento/usuario FK nullable. Sin UNIQUE. |
| ConceptosMovimientos | Empresa/Estado/Creador FK, código25 y nombre120 UNIQUE por empresa, operación ENTRADA/SALIDA/TRASLADO/AJUSTE, bits referencia/costo/cero/automático, fechas, RowVersion. |
| FlujosEstados / EstadosFlujo | Códigos/nombres únicos; Estado/Creador FK, fechas. EstadoFlujo UNIQUE(Flujo,Id)/(Flujo,Codigo)/(Flujo,Nombre), orden≥0, bits inicial/final/edición/motivo/sistema, RowVersion. |
| EstadoTransiciones | FK compuestas de origen/destino del mismo flujo, distintos; UNIQUE(Flujo,Origen,Destino); bits autorización/comentario, permiso100 nullable, Estado/auditoría/RowVersion. |
| Monedas / Impuestos | Moneda UNIQUE(Empresa,ISO), decimales0..6. Impuesto UNIQUE(Empresa,Codigo), porcentaje9,6 entre0 y100, fechas válidas, precio incluido. ProductoImpuestos UNIQUE(Producto,Impuesto). |
| Empresas | Id int identity; razón150/eslogan150/dirección250/NIT25/teléfono100/email100/imagen100/icono100/imagenReportes100 y Estado/Rol FK obligatorios. NIT/email UNIQUE global. Nombre comercial150/DPI25/URL150/ClienteFKTipos nullable. Sin RowVersion. Solo se consulta empresa existente. |
| Sucursales | Id long identity; nombre150/dirección250/teléfono100/email100/direcciónCorta50/códigoNombre175, Empresa/Estado/Proveedor FK y bits central/bodega/transportes obligatorios. ClienteFKClientes/Municipio/Departamento/sucursalCentral/transporte y otros metadatos nullable; sin RowVersion. Solo se consultan sucursales existentes. |
| Usuarios / Roles / UsuariosSucursales | Usuario Id int identity; email100/UserName50/IdentityId128 UNIQUE; hash, Rol/Estado/TipoInicio FK, AccessFailedCount, horario inicio/final, imagen50/Soundex300 y temporal obligatorios; stamp/fecha cambio/otros nullable. Rol Id20 manual, nombre50 UNIQUE, Nivel/Tipo FK, cambio contraseña1..48 y bit configuración obligatorios. Asignación Id long identity, Usuario/Sucursal/Estado FK y principal obligatorios, UNIQUE(Usuario,Sucursal). Se consultan, no se crean usuarios. Empresa.RoleId no sustituye permisos. |
| Bitacoras / permisos | Auditoría en transacción. Permisos únicos por código y módulo/acción/recurso; RolesPermisos UNIQUE(Rol,Permiso). Sin bypass ADMIN. |

## Decisiones de implementación

- Orden BORRADOR editable → CONFIRMADA → PARCIALMENTE_RECIBIDA → RECIBIDA. ANULADA solo antes de cualquier recepción. Recepción se crea ya APLICADA en la misma transacción que el inventario; no existe una segunda aplicación.
- Solicitudes no aceptan totales ni impuestos finales: precio negociado **sin impuesto** y porcentaje de descuento generan descuento monetario; impuestos activos/vigentes provienen de ProductoImpuestos. Se suman tasas configuradas sobre el neto descontado, sin capitalizar impuestos. Se respeta Monedas.Decimales hasta dos, límite de decimal(12,2). Una moneda con más decimales y un impuesto marcado IncluidoEnPrecio se rechazan con409: faltan capacidad monetaria y política fiscal inequívoca, respectivamente. No se adivina IVA ni recuperabilidad fiscal. `TotalLinea` permanece calculado por SQL; el DTO conserva su fórmula decimal sin forzar el redondeo monetario del encabezado.
- Cantidad recibida está expresada en la presentación de la línea; costo recibido queda expresado en moneda base por unidad base: `(Cantidad*PrecioUnitario-Descuento)/Cantidad*TipoCambio/Factor`, redondeado una sola vez al persistir decimal(19,6). Impuestos se mantienen separados. Factores/cantidades base usan el motor Fase4. Factor/unidad de presentación y unidad base de producto utilizado en órdenes quedan protegidos, incluso ante edición concurrente con la creación de una orden. Ajustes, órdenes, recepciones y ediciones relevantes comparten la reserva empresarial.
- La valuación disponible es PROMEDIO, seguimiento NINGUNO. Otros métodos/lotes/series/vencimientos se rechazan explícitamente, no se valoran como promedio de forma silenciosa.
- Recepción usa GUID de operación del cliente exclusivamente como identificador no secreto (nunca correlativo), persistido en ReferenciaExterna de sus movimientos. Repetirlo devuelve409. Numeración documental se reserva en servidor mediante SeriesDocumentos/CorrelativosDocumentos.
- Escrituras por empresa comparten la exclusión local y `sp_getapplock` transaccional del inventario Fase4, con aislamiento SERIALIZABLE; se valida pendiente dentro del bloqueo. RowVersion real se conserva en stock/correlativos. No se inventa RowVersion en compras.
- No se alterará `Heladeria.sql`, EF generado, datos reales ni frontend. Preparación únicamente explícita y confirmada.

## Proveedores y alcance

Mantenimiento sin DELETE. Código normalizado a mayúsculas; correo a minúsculas; NIT trim/mayúsculas y longitud20. Fase3 no implementa algoritmo fiscal de NIT: no se inventó uno. Código, NIT no vacío y correo no vacío se comprueban por empresa para evitar duplicados; NIT/correo permanecen nullable. SQL solo impone unicidad del código: las otras reglas dependen del servicio y de su reserva transaccional. Un proveedor con órdenes no finales no puede inactivarse por PUT completo ni por PUT estado.

ProveedorProductos no contiene presentación ni unidad. La asociación es exactamente proveedor+producto único, con código, precio referencial, preferido y estado. La compra **no exige** asociación previa: no existe FK/regla SQL que lo requiera. HistoricoPreciosCompra registra el precio de la presentación negociada y la moneda con su recepción. No se sobrescribe PrecioUltimaCompra de una asociación con un precio de presentación ambiguo.

Empresa/usuario/sucursal se obtienen del JWT validado y de asignaciones operativas reales. Órdenes y recepciones se limitan además a la sucursal **seleccionada actualmente**. Una asignación adicional no permite acceder desde el contexto de otra sucursal. Proveedores son empresariales. Bodega y ubicación deben pertenecer a la sucursal seleccionada y la ubicación debe admitir recepción. Lecturas/ediciones de recursos ajenos responden404; referencias inválidas al crear un documento responden400. No hay bypass por nombre de rol.

## Endpoints y permisos

| Método / ruta | Permiso | Resultado normal |
|---|---|---|
| GET `/api/compras/proveedores` | PROVEEDORES.CONSULTAR |200, SQL paginado/filtros|
| GET `/api/compras/proveedores/{id}` | PROVEEDORES.CONSULTAR |200|
| POST `/api/compras/proveedores` | PROVEEDORES.ADMINISTRAR |201 + Location|
| PUT `/api/compras/proveedores/{id}` | PROVEEDORES.ADMINISTRAR |200|
| PUT `/api/compras/proveedores/{id}/estado` | PROVEEDORES.ADMINISTRAR |200|
| GET `/api/compras/proveedores/{id}/productos` | PROVEEDORES.CONSULTAR |200, SQL paginado|
| POST `/api/compras/proveedores/{id}/productos` | PROVEEDORES.ADMINISTRAR |201|
| PUT `/api/compras/proveedores/{id}/productos/{relacionId}` | PROVEEDORES.ADMINISTRAR |200|
| PUT `/api/compras/proveedores/{id}/productos/{relacionId}/estado` | PROVEEDORES.ADMINISTRAR |200|
| GET `/api/compras/referencias` | COMPRAS.CONSULTAR |200, monedas/estados/series|
| GET `/api/compras` | COMPRAS.CONSULTAR |200, SQL paginado/filtros|
| GET `/api/compras/{id}` | COMPRAS.CONSULTAR |200, encabezado/líneas/totales/pendientes/recepciones|
| POST `/api/compras` | COMPRAS.ADMINISTRAR |201 + Location; BORRADOR|
| PUT `/api/compras/{id}` | COMPRAS.ADMINISTRAR |200; únicamente BORRADOR sin recepciones|
| POST `/api/compras/{id}/confirmar` | COMPRAS.ADMINISTRAR |200; no recibe body|
| PUT `/api/compras/{id}/estado` | COMPRAS.ADMINISTRAR |200; body `{ "estado": "CONFIRMADA" }` o `ANULADA`|
| POST `/api/compras/{id}/recepciones` | COMPRAS.RECIBIR |201 + Location; aplicada atómicamente|
| GET `/api/compras/recepciones/{id}` | COMPRAS.CONSULTAR |200|

Estados de recepción y estados recibidos de orden no se cambian manualmente. No se expone endpoint de reaplicación ni reversa/anulación de recepción. La anulación de orden se bloquea al existir cualquier recepción, incluso externa. DTO HTTP: clases selladas init con DataAnnotations y validación defensiva en servicio; respuestas no exponen EF.

Errores controlados400 (`VALIDACION_COMPRAS`),401,403,404 (`RECURSO_NO_ENCONTRADO`) y409 (`CONFLICTO_COMPRAS` o `CONFLICTO_PERSISTENCIA`). Se preserva429 del limitador existente en autenticación; no se agregó una política nueva de rate limiting para Compras. No se devuelven excepciones SQL. Los200/201 utilizados siguen el estilo actual; no se inventan operaciones para producir204.

## Preparación explícita

Desde la carpeta de la solución, con ConnectionStrings y Seguridad configurados por los proveedores seguros ya existentes:

```powershell
dotnet run --configuration Release --project Heladeria.Bootstrap -- --preparar-fase5
```

Exige terminal interactiva y escribir exactamente `PREPARAR FASE 5`. No pide contraseña, no crea usuarios, no ejecuta el bootstrap inicial y no corre al iniciar API. Se requiere administrador activo con empresa/sucursal principal operativas, SEGURIDAD.ADMINISTRAR operativo y flujo aplicado de inventario preparado en Fase4.

Orden dentro de una única transacción SERIALIZABLE con `sp_getapplock` empresarial de inventario y `Heladeria.Bootstrap.Fase5`:

1. Crear/reutilizar módulos COMPRAS y PROVEEDORES (incluido Icono obligatorio).
2. Crear/reutilizar acciones CONSULTAR, ADMINISTRAR, RECIBIR.
3. Crear/reutilizar los cinco permisos anteriores y sus RolesPermisos ADMIN/Permitido1.
4. Crear/reutilizar flujo ORDENES_COMPRA y sus cinco estados, y RECEPCIONES_COMPRA/APLICADA.
5. Crear/reutilizar seis transiciones: borrador→confirmada/anulada, confirmada→anulada/parcialmente recibida/recibida, parcialmente recibida→recibida; con permiso efectivo correspondiente.
6. Crear/reutilizar concepto empresarial RECEPCION-COMPRA (ENTRADA, automático, referencia requerida, afecta costo, permite costo cero explícitamente).
7. Crear/reutilizar ClaseTipos DOCUMENTOS_COMPRA y tres Tipos documentales. Sus claves manuales se calculan, no se suponen.
8. Crear/reutilizar SeriesDocumentos OC, RC, IC, prefijos respectivos, longitud10, sucursalNULL/empresa principal; sus CorrelativosDocumentos inician en0 solo si nuevos y sin documentos previos de esa serie.
9. Registrar una sola Bitacora FASE5_CONFIGURADA por empresa.

Reejecución compatible no duplica ni reinicia correlativos. Referencias incompatibles o documentos previos sin correlativo provocan rechazo/rollback; no se sobrescribe configuración ajena. La terminal lista tablas, IDs reales y códigos creados. Alcance del preparador: empresa principal administrativa elegida determinísticamente; no configura automáticamente otras empresas. No altera moneda, IVA, productos, proveedores ni existencias. Moneda base real y moneda documental activas deben estar configuradas en Monedas; si no hay exactamente una base se devuelve409. Elegir moneda oficial requiere configuración consciente, no una suposición por país.

## Transacción de recepción y trazabilidad

Antes de insertar se comprueban contexto/orden/estado, clave UUID, proveedor, moneda empresarial y base, ubicación de recepción, detalles pertenecientes a la orden sin duplicados, cantidades positivas ≤pendiente, producto operativo comprable/inventariable, presentación de compra y precisión/conversión. Recepciones externas con estado distinto de APLICADA bloquean el siguiente ingreso hasta revisión.

Se reserva número RC en CorrelativosDocumentos; se crea RecepcionesCompra/APLICADA y sus detalles. Cada línea origina un movimiento IC con correlativo real, ConceptosMovimientos.RECEPCION-COMPRA, ModuloOrigen=COMPRAS, DocumentoOrigenId=recepción y ReferenciaExterna=UUID. El núcleo compartido con ajustes Fase4 crea MovimientoDetalles (cantidad presentación/factor/base/costo base), actualiza stock destino, crea Kardex y costos/histórico. La unidad del costo que entra al núcleo es explícita: el ajuste manual entrega costo por unidad solicitada y el núcleo lo divide por el factor; Compras ya entrega costo por unidad base y no se vuelve a dividir. Luego se registra precio negociado/histórico, estado parcial o recibido y Bitacoras. Todos los SaveChanges están **dentro de la misma transacción**, nunca commits intermedios.

Una recepción puede tener varios movimientos, uno por línea; el GET devuelve todos sus IDs. Bodega/ubicación se conservan en Movimientos/MovimientoDetalles/Kardex porque la recepción no tiene esas columnas. Referencia UUID no es número de documento ni secreto. Repetir UUID dentro de la empresa devuelve409 aunque se cambie de orden. Ante pérdida de respuesta después de commit, consultar orden/recepciones y no sustituir UUID para repetir el mismo ingreso físico.

Rollback borra los efectos de todos los INSERT/UPDATE, incluidos correlativos y auditoría; se limpia ChangeTracker para no conservar entidades fallidas. IDENTITY puede consumir valores por intento revertido: los huecos no representan instalaciones/documentos parciales. Un error SQL nunca expone contraseña ni cadena de conexión al cliente.

## Inventario, costos y concurrencia

- Compra/confirmación/anulación sin recepción: no afectan inventario.
- Entrada: presentación×factor real→cantidad base; precisión de ambas unidades validada. Base directa usa factor1. No se usa double ni se truncan cantidades silenciosamente.
- Existencias identifica empresa/producto/bodega/ubicación; cantidad reservada no se altera. Recepción nunca provoca saldo negativo.
- Todo costo de inventario persistido es costo por **unidad base**. `MovimientoDetalles.CostoUnitario` multiplica `CantidadBase` en la columna calculada `CostoTotal`; `Kardex.CostoUnitario` multiplica su cantidad base. `Existencias.CostoPromedio`, `CostosProductos.CostoActual` e `HistoricoCostosProductos` conservan esa misma unidad.
- PROMEDIO por existencia de ubicación: `(saldoAnteriorBase*costoBaseAnterior+entradaBase*costoBaseEntrada)/(saldoAnteriorBase+entradaBase)`; persistencia a seis decimales AwayFromZero. Caso clave: 2500 ML×0.020000 +3000 ML×0.025000 →5500 ML a0.022727/ML, equivalente a Q22.727 por litro. Kardex.SaldoCosto usa el promedio base ya persistido: 5500×0.022727 =124.998500.
- CostosProductos conserva la semántica existente Fase4: snapshot del último promedio afectado; no se presenta como promedio global empresarial de todas las ubicaciones. Histórico agrega OrigenCambio=RECEPCION_COMPRA, DocumentoOrigenId y usuario; se guarda solo al cambiar costo. Esa diferencia de alcance se declara como deuda/política pendiente.
- Concepto, moneda, flujo y estados deben estar preparados; no se asignan IDs mágicos ni correlativos MAX+1 en compras. En `POST /api/inventario/ajustes`, `costoUnitario` significa costo por presentación cuando se envía `productoPresentacionId`; sin presentación significa costo por unidad base. La respuesta conserva el nombre `costoUnitario` por compatibilidad, pero devuelve el costo ya normalizado por unidad base.
- SERIALIZABLE + SQL applock empresarial coordinan instancias de API; la exclusión local compartida evita carreras en un proceso y pruebas. Se valida pendiente bajo bloqueo. Existencias y CorrelativosDocumentos conservan sus RowVersion reales. No se habilitan reintentos automáticos del proveedor para reejecutar transacciones documentales.
- El contexto se refresca bajo reserva al iniciar escritura. Ediciones de factor/unidad relevantes comparten reserva: la validación de protección no consulta primero y escribe después de una recepción concurrente sin volver a protegerse.
- Reserva empresarial es conservadora y serializa productos distintos: mejora futura de granularidad requiere coordinar correlativos, órdenes y stock en orden fijo, sin romper Fase4.

Auditoría: PROVEEDOR_CREADO/ACTUALIZADO/ESTADO_CAMBIADO, asociaciones/estados, COMPRA_CREADA/ACTUALIZADA/CONFIRMADA/ANULADA, ENTRADA_COMPRA, COSTO_ACTUALIZADO, RECEPCION_APLICADA, FASE5_CONFIGURADA. UserAgent/IP/comentario se limitan al tamaño real. No secretos.

## Facturas, CxP y límites fiscales

No se crean FacturasProveedores ni CuentasPorPagar: la recepción no las exige. No hay pagos ni obligación inventada por una orden. No se modifica crédito/saldo del proveedor. Tasas no incluidas vigentes son configuración ProductoImpuestos→Impuestos por empresa; no se asume IVA12%. El modelo no distingue impuesto de compra/venta ni recuperable/no recuperable, ni guarda desglose por impuesto de orden. El precio de solicitud se define neto; cualquier política de capitalización, impuestos incluidos, retenciones o impuesto fiscal específico requiere definición/aprobación antes de ampliar el comportamiento. Su uso ambiguo no se procesa silenciosamente.

## Pruebas

Se mantienen las104 previas. Nuevas pruebas en `Heladeria.Pruebas/Fase5`: proveedores/listado/filtros/CRUD/duplicados/estado/IDOR/asociación; órdenes válidas/totales/correlativo/conversión/sin stock/fechas/referencias/cantidad/precio/duplicados/moneda/impuestos/estados/factor histórico; recepción parcial/completa/exceso/UUID/ubicación/detalles/inventario/Movimiento/Kardex/costo/históricos/auditoría; contexto reutilizado; preparación idempotente/incompatible; 401/403/400/404 y flujo completo HTTP con permisos efectivos, sin bypass ADMIN; concurrentes mismo stock, exceso y misma UUID.

Dos pruebas SQLite **relacionales efímeras** verifican rollback real de recepción después de haber persistido stock dentro de transacción, y rollback de preparación después de crear módulos/permisos/flujos antes del error, con reintento posterior. Solo el contexto de prueba adapta el default de bytes de RowVersion; EF generado/productivo no cambia. Las pruebas rápidas InMemory no prueban rollback, FK ni locks de SQL Server. SQLite no sustituye validación SQL Server de applock/RowVersion/precisiones; esta última queda como prueba de integración en BD aislada autorizada, nunca la BD real del usuario. Limitaciones documentadas por [Microsoft](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy) y [proveedor SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations).

Validación final actualizada el30/09/2026, desde la solución:

```text
dotnet build --configuration Release
Compilación correcta. 0 advertencias, 0 errores. Tiempo 00:00:02.35.

dotnet test --configuration Release --no-build
182 aprobadas, 0 fallidas, 0 omitidas. Duración 12s.
```

Total: **78 nuevas +104 existentes =182**. Las tres pruebas añadidas por la corrección cubren costo manual por presentación, persistencia base y no doble conversión en recepción; las pruebas existentes de promedio/Kardex se corrigieron a escala base. No se ejecutaron npm/frontend, preparadores/bootstrap ni el script de reparación contra SQL Server. El esquema conserva SHA256 `4F58E2641ACB37804948A61D5B7EAFD0E4FBFA5B94F419048704B790F0431028`. Prueba específica SQL Server aislada pendiente: las pruebas actuales no certifican sp_getapplock ni generación real de RowVersion de ese proveedor.

## Deuda estructural y propuestas pendientes de aprobación

**Nada de este apartado se ejecutó.** Toda DDL requiere primero inspección de datos existentes, copia de seguridad, aprobación y actualización Database First mediante scaffold; no se crean migraciones Code First paralelas.

| Problema | Protección actual / impacto pendiente |
|---|---|
| FK simples de órdenes a proveedor/sucursal/moneda y detalles a presentación | Servicio exige pertenencia empresarial/producto; SQL externo puede cruzar empresas/productos. Proponer claves alternativas y FK compuestas, tras limpiar inconsistencias. EF debe regenerar navegación/clave principal. |
| Recepción no contiene clave única UUID | UUID en movimientos + applock impide repetición por backend, no escrituras externas/reintentos con UUID distinto. Proponer UUID en recepción con UNIQUE empresarial y persistirlo desde servicio; requiere nuevo campo EF. |
| Falta UNIQUE(Recepcion,DetalleOrden) | Servicio rechaza duplicados internos, pero SQL externo puede insertar la misma línea dos veces. Proponer índice único, sin impedir parciales en recepciones diferentes. |
| Sin RowVersion proveedores, asociaciones, órdenes, recepciones, CostosProductos | SERIALIZABLE/applock protege operaciones colaboradoras, no detecta DTO de edición obsoleto de proveedores/órdenes: última edición administrativa gana. Proponer RowVersion y token HTTP; requiere scaffold/DTO/UI posterior. |
| Impuesto sin uso compra/venta, recuperabilidad ni desglose/snapshot | Política neta explícita y bloqueo de incluido; no inferir capitalización/IVA. Definir política y posibles campos/desglose antes de implementarlos; impacta EF/totales históricos. |
| Factor ausente en detalle de orden | Se protege factor/unidad utilizados; SQL externo puede reinterpretar historia. Proponer factor/base/unidad snapshot en detalle, manteniendo movimiento histórico existente. |
| CostosProductos global pero promedio por ubicación | Snapshot del último promedio según Fase4; decidir si se requiere resumen empresarial ponderado o costo estándar. No mezclar ambos ámbitos sin aprobación. |
| HistoricoPreciosCompra sin presentación | Se conoce presentación al seguir recepción→detalle, pero precio de asociación no se actualiza ambiguamente. Proponer referencia a detalle/presentación si se necesita consulta independiente. |
| Importe decimal12,2 con cantidad decimal19,6/costo19,6/moneda hasta6 decimales | Se validan límites y se bloquean monedas>2; precios por presentación pueden superar capacidad. Revisar escalas antes de ampliarlas; regenerar EF y validadores. |
| Sin checks de descuento/impuesto/totales no negativos/coherencia | Servicio valida/calcula y rechaza importes externos incoherentes al recibir; agregar checks tras auditar datos. EF debe recogerlos. |
| Índices incompletos para fecha/alcance/pendiente/idempotencia | SQL paginado; proponer índices de orden por empresa/sucursal/fecha y detalles recibidos por detalle de orden, origen/referencia de movimientos. Índices no autorizados no se aplican. |
| Sucursales.ProveedorId NOT NULL | Dependencia técnica inicial ya respetada por bootstrap; no significa que sucursal sea proveedor comercial. No se altera en esta fase. |
| Series no referenciadas por FK desde orden/recepción/movimiento | Reserva segura de SeriesDocumentos/CorrelativosDocumentos por API y número documental único. Escritor externo puede ignorar reserva. No reiniciar correlativos automáticamente. |

SQL orientativo, **no aplicado**, para revisión aislada (otros cambios necesitan plan de saneamiento/backfill):

```sql
-- Aprobar y comprobar ausencia de duplicados antes de crear el índice.
CREATE UNIQUE INDEX UX_RecepcionCompraDetalles_Linea
ON dbo.RecepcionCompraDetalles (RecepcionCompraId, OrdenCompraDetalleId);

-- UUID único en la cabecera permitiría varios movimientos de una sola recepción.
ALTER TABLE dbo.RecepcionesCompra ADD ClaveOperacion UNIQUEIDENTIFIER NULL;
CREATE UNIQUE INDEX UX_RecepcionesCompra_Operacion
ON dbo.RecepcionesCompra (EmpresaId, ClaveOperacion) WHERE ClaveOperacion IS NOT NULL;

-- Ejemplo; evaluar igualmente proveedores/asociaciones/recepciones/costos.
ALTER TABLE dbo.OrdenesCompra ADD VersionFila ROWVERSION NOT NULL;

CREATE INDEX IX_OrdenesCompra_AlcanceFecha
ON dbo.OrdenesCompra (EmpresaId, SucursalId, FechaEmision DESC, Id DESC);
CREATE INDEX IX_RecepcionCompraDetalles_Pendiente
ON dbo.RecepcionCompraDetalles (OrdenCompraDetalleId) INCLUDE (CantidadRecibida, RecepcionCompraId);
CREATE INDEX IX_Movimientos_OperacionCompra
ON dbo.Movimientos (EmpresaId, ModuloOrigen, ReferenciaExterna) INCLUDE (DocumentoOrigenId);

-- Ejemplo de FK empresarial: auditar registros y agregar claves alternativas
-- equivalentes para sucursales/monedas/presentaciones antes de completar el plan.
ALTER TABLE dbo.Proveedores ADD CONSTRAINT UQ_Proveedores_EmpresaId UNIQUE (EmpresaId, Id);
ALTER TABLE dbo.OrdenesCompra WITH CHECK ADD CONSTRAINT FK_OrdenesCompra_ProveedorEmpresa
FOREIGN KEY (EmpresaId, ProveedorId) REFERENCES dbo.Proveedores (EmpresaId, Id);
```

Las precisiones/checks, snapshots y política fiscal no tienen DDL definitivo sin decisiones funcionales. Nunca se propone unicidad de UUID en Movimientos: una recepción puede generar varios movimientos con la misma clave.

## Guía corta Swagger (solo al ejecutar manualmente)

1. Ejecutar preparador explícito anterior y confirmar. **No se ejecutó durante implementación.** Verificar una moneda base real activa en Monedas; si falta, configurarla conscientemente por el proceso administrativo autorizado, no elegir IDs/moneda ficticios. No incluir secretos en SQL/documentos.
2. Iniciar API con su configuración segura existente; login, cambio de contraseña si requerido, sesión/sucursal seleccionada, Authorize con access token.
3. Consultar `GET /api/compras/referencias` para IDs reales de moneda, estados y series; `GET /api/compras/proveedores` para proveedor; `GET /api/productos` y `/{productoId}/presentaciones` para producto comprable y presentación habilitada. Usar `GET /api/inventario/almacenes` y elegir una ubicación de recepción **de la sucursal seleccionada**.
4. POST `/api/compras` con la estructura siguiente, sustituyendo los marcadores por IDs obtenidos (no es JSON ejecutable mientras existan marcadores):

```text
{
  "sucursalId": <sucursal de sesión>,
  "proveedorId": <proveedor activo de empresa>,
  "fechaEmision": "2026-09-17T12:00:00Z",
  "monedaId": <moneda real de empresa>,
  "tipoCambio": 1,
  "detalles": [{
    "productoId": <producto>, "productoPresentacionId": <presentación compra>,
    "cantidad": 10, "precioUnitario": 25, "porcentajeDescuento": 0
  }]
}
```

TipoCambio1 solo para moneda base; moneda extranjera requiere tipo negociado positivo real. Precio25 es ejemplo por presentación, no costo25 por ml ni precio de venta. Cantidad/factor/pending vienen del GET; no enviar totales.

5. Confirmar `POST /api/compras/{id}/confirmar` sin body; comprobar que stock no cambia.
6. `POST /api/compras/{id}/recepciones` con UUID nuevo de operación física y IDs reales:

```text
{
  "claveOperacion": <UUID generado una sola vez para esta recepción>,
  "bodegaId": <bodega>, "ubicacionId": <ubicación de recepción>,
  "observaciones": "Recepción parcial de prueba",
  "detalles": [{ "ordenCompraDetalleId": <Id detalle devuelto>, "cantidad": 6 }]
}
```

7. GET orden: recibida6/pendiente4 y PARCIALMENTE_RECIBIDA; GET Location recepción: estadoAPLICADA, detalles/costo base/IDs movimientos. Verificar stock/Kardex/auditoría. Otra operación física recibe4 con otra UUID→RECIBIDA. Exceso/UUID repetida/anulación después de recibir→409; cero/negativa→400; usuario sin permiso→403; sin token→401; compra/proveedor ajeno→404.
8. No existe reversa automática de estos datos de prueba: usar BD de validación autorizada si se pretende descartarlos. No borrar ni anular recepciones aplicadas manualmente sin proceso de inventario definido.

Consultas SQL **solo lectura**, sin IDs supuestos:

```sql
SELECT p.Codigo, rp.RoleId, rp.Permitido
FROM dbo.Permisos p JOIN dbo.RolesPermisos rp ON rp.PermisoId=p.Id
WHERE p.Codigo LIKE 'COMPRAS.%' OR p.Codigo LIKE 'PROVEEDORES.%';
SELECT f.Codigo AS Flujo, e.Id, e.Codigo AS Estado, e.PermiteEdicion, e.EsFinal
FROM dbo.EstadosFlujo e JOIN dbo.FlujosEstados f ON f.Id=e.FlujoEstadoId
WHERE f.Codigo IN ('ORDENES_COMPRA','RECEPCIONES_COMPRA');
SELECT s.EmpresaId, s.Serie, c.UltimoNumero
FROM dbo.SeriesDocumentos s JOIN dbo.CorrelativosDocumentos c ON c.SerieDocumentoId=s.Id
JOIN dbo.Tipos t ON t.Id=s.TipoDocumentoId WHERE t.ModuloId='COMPRAS';
SELECT Id, EmpresaId, OrdenCompraId, NumeroRecepcion FROM dbo.RecepcionesCompra;
SELECT Id, EmpresaId, DocumentoOrigenId, ReferenciaExterna, SerieDocumento, NumeroDocumento
FROM dbo.Movimientos WHERE ModuloOrigen='COMPRAS';
SELECT EmpresaId, ProductoId, BodegaId, UbicacionId, Cantidad, CostoPromedio FROM dbo.Existencias;
SELECT Id, EmpresaId, Accion, Entidad, EntidadId, Fecha FROM dbo.Bitacoras
WHERE ModuloId IN ('COMPRAS','PROVEEDORES') ORDER BY Id DESC;
```

### Reparación controlada del dato de prueba ProductoId 1

`BaseDatos/Reparaciones/20260930_RepararCostosPruebaProducto1.sql` no se ejecuta desde la aplicación ni desde el bootstrap. Está limitado por precondiciones a la historia real cerrada de ProductoId1: MovimientoDetalle/Kardex1 entrada de3 L ×1000 a Q20 mal escalado; detalle/Kardex2 salida de2 vasos ×250 valorizada a Q20; detalle/Kardex3 recepción de3 L a Q0.025 ya correcta; dos históricos; una existencia final de5500 ML. Exige exactamente los IDs1,2,3 y todos los valores previos documentados, corrige únicamente columnas de costo y deja el saldo valorizado histórico en60,50 y125. La primera ejecución usa `@ConfirmarCommit = 0`: muestra ANTES, valida, muestra DESPUÉS y hace ROLLBACK. Solo después de revisar ambos juegos de resultados se puede cambiar la variable a1 y reejecutar. Cualquier fila adicional o diferencia produce `THROW` y rollback.

## Archivos de implementación

- Aplicacion/Compras/ContratosCompras: solicitudes, DTO, interfaces y errores.
- Infraestructura/Compras/ServicioComprasBase, ServicioProveedores, ServicioCompras.
- Infraestructura/Inventario/ServicioInventario: núcleo compartido, origen documental, semántica explícita de costo por unidad solicitada/base, conversión, tracker/rollback seguro.
- Infraestructura/Productos/ServicioProductos: protección de unidad/factor históricos con reserva compartida.
- Infraestructura/Seguridad/Bootstrap/PreparadorFase5; interfaz IPreparadorFase5; InyeccionDependencias y CLI Bootstrap/Program.
- Api/Controllers/ComprasController, ProveedoresController; permisos del sistema; manejador de errores existente.
- Pruebas/Fase4 y Fase5: conversión de cantidad/costo por presentación, promedio base, Kardex/histórico, fixture Fase4 reutilizable y fábrica HTTP con BD efímera estable por fábrica; dependencia SQLite únicamente en proyecto de pruebas.
- BaseDatos/Reparaciones/20260930_RepararCostosPruebaProducto1.sql: inspección/reparación transaccional opt-in exclusiva del dato de prueba; no modifica Heladeria.sql.

Sin frontend de Compras, cambios de estructura/EF generado, ejecución real de preparadores/bootstrap ni Fase6.
