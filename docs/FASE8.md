# Fase 8 — Caja backend

## Diagnóstico y alcance

Se inspeccionaron el maestro SQL, el modelo EF Database First y los servicios de Ventas/POS, seguridad, numeración y preparación existentes antes de implementar.

| Estructura solicitada | Estructura real / decisión |
|---|---|
| Cajas | Existe `Cajas`: EmpresaId, SucursalId, Código, Nombre y EstadoId; se reutiliza. |
| CajaSesiones | No existe con ese nombre. Se reutiliza `TurnosCaja`. |
| AperturasCaja / CierresCaja | No existen. Apertura y cierre pertenecen a `TurnosCaja`; no se crean tablas equivalentes. |
| MovimientosCaja | Existe y exige Monto > 0. Se amplía para vincular el origen y conservar semántica financiera. |
| ArqueosCaja | Existe con esperado, contado, diferencia calculada, fecha, usuario y observaciones. |
| FormasPago / Pagos / PagoDetalles | Existen. Permiten desglose mixto, pero faltaban vínculo con venta, UUID, recibido y snapshot de efectivo. |
| Ventas / VentasDetalle | Existen y se reutilizan sin alterar importes, estado ni inventario al cobrar. |
| Monedas / Usuarios / Sucursales / Empresas | Existen y determinan moneda, actor y alcance autorizado. |
| Bitacoras / SeriesDocumentos / CorrelativosDocumentos | Existen; se reutilizan para auditoría y numeración, sin contadores paralelos. |

También existen `ArqueoCajaDetalles` y `DenominacionesMoneda`. Se conservan, pero el arqueo de esta fase recibe únicamente el efectivo contado; no se implementó desglose por denominaciones. Se reutilizan `Tipos`, `ClaseTipos`, `FlujosEstados`, `EstadosFlujo`, `EstadoTransiciones`, `Modulos`, `AccionesSistema`, `Permisos` y `RolesPermisos`.

No se crean tablas nuevas. No hay frontend, Visual 6, CxC/CxP, FEL, impresión, delivery, conciliación bancaria ni contabilidad.

## Modelo Database First y SQL

Script incremental: `BaseDatos/Reparaciones/20261007_Fase8_Caja.sql`. El maestro `BaseDatos/Heladeria.sql` incorpora la misma ampliación al final, después de Fase 7.

| Tabla modificada | Nuevas columnas / garantías |
|---|---|
| TurnosCaja | MonedaId, UsuarioCierreId, FechaModificacion, NumeroApertura, NumeroCierre, TotalIngresos, TotalEgresos, ArqueoCierreId, VersionFila ROWVERSION. Índice único filtrado por CajaId donde FechaCierre IS NULL. |
| Pagos | VentaId y ClaveOperacion. FK a Ventas, unicidad filtrada por venta y por empresa/UUID. Ampliación controlada de CK_Pagos_Tercero. |
| PagoDetalles | MontoRecibido y EsEfectivo. Verifica recibido ≥ aplicado; fuera de efectivo deben ser iguales. |
| MovimientosCaja | PagoDetalleId, CodigoConcepto, EsEfectivo y NumeroDocumento. FK y unicidad por detalle de cobro. |
| ArqueosCaja | VersionTurno: versión de la sesión a la que corresponde el conteo. |

Los campos nuevos de negocio son nullable para conservar los históricos sin inventar moneda, cobros, usuarios de cierre ni saldos. `VersionFila` sí se genera por SQL Server. Las relaciones Empresa/Sucursal de sesión se obtienen a través de Caja; la moneda del movimiento se obtiene de su sesión, evitando duplicaciones innecesarias.

El CHECK original de `Pagos` obligaba a tener cliente o proveedor. Se conserva esa exclusividad para pagos sin VentaId. La excepción POS exige VentaId, UUID, sesión y sucursal, prohíbe proveedor y permite ClienteId NULL. **No se crea “Cliente Genérico”.** El servicio además valida empresa/sucursal/moneda, venta completada y suma total.

El script:

- Usa una transacción, XACT_ABORT y reserva de esquema. No contiene GO dentro de la transacción.
- Agrega columnas mediante SQL dinámico controlado y compila sus consumidores después del DDL.
- Detecta columnas, FK, restricciones e índices existentes; valida tipo, nulabilidad, longitud, precisión, escala y garantías de los objetos reutilizados.
- Compara restricciones en su forma compilada mediante tablas temporales vacías, conservando la precedencia lógica. SQL Server puede reescribir expresiones; una comparación textual ingenua no es suficiente.
- Tolera preparación parcial compatible, no reinventa datos y revierte los cambios del intento ante incompatibilidad.
- Rechaza cajas con empresa/sucursal incoherentes y varias sesiones abiertas de la misma caja. No cierra históricos automáticamente.
- No crea catálogos, cajas, sesiones, ventas, movimientos ni pagos.

EF se amplió en `Persistencia/Entidades/Caja.Fase8.cs` y `Persistencia/HeladeriaDbContext.Fase8.cs`. El hook existente de `HeladeriaDbContext.Fase7.cs` invoca la configuración adicional. No se usaron EF migrations. Al regenerar Database First, consolidar estas propiedades/configuraciones con las generadas para evitar duplicarlas.

## Caja, sesión y estados

Flujo `CAJA_SESION`: **ABIERTA → CERRADA**. No se creó ANULADA porque no existe reversión financiera en este alcance.

Una caja pertenece a una empresa y sucursal. `POST /api/caja/cajas` permite registrar una caja real explícitamente, activa, sin abrirla ni cargar datos ficticios. Código único por sucursal. No se implementó CRUD adicional.

Solo puede existir una sesión sin FechaCierre por caja, incluso si es un histórico. Un usuario sí puede abrir varias cajas: el modelo no lo prohíbe, y todas las acciones conservan el actor real. Los operadores autorizados de la misma sucursal pueden operar la sesión; no hay una restricción implícita al usuario de apertura.

Apertura valida caja operativa, asignación activa del usuario, empresa/sucursal operativas, moneda y saldo inicial ≥ 0. Usa la moneda base activa de la empresa, con dos decimales; no hay conversión de monedas. No crea monedas ni presupone GTQ en la lógica.

Saldo inicial positivo: movimiento `APERTURA_CAJA`, nunca una venta ni ingreso operativo. Saldo inicial cero: queda explícito en TurnosCaja y CAJA_ABIERTA, sin insertar un movimiento cero que violaría el CHECK heredado Monto > 0.

Series empresariales, reutilizando la numeración existente:

- CA: apertura.
- MC: cada movimiento, incluido cada detalle de cobro.
- CC: cierre.

## Movimientos, importes y saldos

Conceptos: `APERTURA_CAJA`, `COBRO_VENTA`, `INGRESO_CAJA`, `EGRESO_CAJA`. No hay ajuste silencioso de cierre ni reverso ficticio.

Los movimientos manuales son de efectivo, requieren motivo, monto positivo y permiso específico. No crean ventas ni pagos comerciales. Un egreso no puede exceder el efectivo esperado.

Todo importe se maneja en decimal con máximo dos decimales y dentro de decimal(12,2); se rechaza precisión excesiva, no se redondea silenciosamente. Los acumulados se validan para que puedan persistirse al cerrar.

```text
Ingresos = COBRO_VENTA + INGRESO_CAJA           (excluye apertura)
Egresos = EGRESO_CAJA
SaldoTeorico = SaldoInicial + Ingresos - Egresos
EfectivoEsperado = SaldoInicial + ingresos efectivos - egresos efectivos
```

SaldoTeorico incluye todas las formas; EfectivoEsperado solo representa efectivo físico. El desglose por forma expone SaldoInicial, Ingresos, Egresos y Neto por separado. Se conserva EsEfectivo como snapshot: cambiar una forma de pago posteriormente no reclasifica el historial.

## Venta versus cobro

Una venta **COMPLETADA no equivale a cobrada**. Caja registra un pago sobre una venta POS completada de la misma empresa/sucursal y moneda, con UUID y versión de venta válidas. Rechaza borradores, confirmadas, anuladas, crédito, históricos incompletos, vinculaciones antiguas a caja/pedido y documentos con devoluciones/notas/CxC que requieren conciliación.

Se cobra el total inmediato, nunca parcialmente: suma de MontoAplicado = TotalVenta. No se altera TotalVenta, estado, VersionFila comercial, existencias, movimientos de inventario, Kardex ni costos. El vínculo financiero es `Pagos.VentaId` + `Pagos.TurnoCajaId`; no se rellena `Ventas.TurnoCajaId`, cuyo uso previo tiene otra semántica en Fase 7.

`GET ventas-pendientes` muestra solo POS completadas elegibles, positivas y sin pago, de la sucursal autorizada. Devuelve número, fecha, cliente opcional, total, saldo pendiente, moneda y versión. Sin crédito, el saldo es total o cero. Ventas de importe cero no generan un pago ficticio de cero y no se incluyen entre las pendientes de cobro.

## Formas, pagos mixtos y vuelto

La preparación crea/reutiliza EFECTIVO y TARJETA compatibles. Otras formas configuradas se admiten si son activas, de la empresa, no son crédito y sus flags corresponden a una forma no física (AfectaCaja=false y PermiteCambio=false). EFECTIVO debe tener ambos flags true. No se interpretan formas ambiguas silenciosamente.

`PagoDetalles` permite combinar formas; cada forma aparece una sola vez en la solicitud (máximo diez detalles). No se cobra dos veces la venta.

Ejemplo total 80, efectivo recibido 100:

- MontoAplicado = 80.
- MontoRecibido = 100.
- Vuelto = 20.
- Movimiento efectivo = **80**, no 100.

Fuera de efectivo, recibido debe ser igual a aplicado. La referencia es opcional salvo que el catálogo la exija. Se aceptan identificadores cortos de autorización; se rechazan referencias con 13 o más dígitos, texto libre o etiquetas PAN/CVV/CVC/tarjeta. No hay campos para número completo de tarjeta, CVV ni integración bancaria. Nunca introducir datos sensibles en observaciones u otros textos libres.

## Idempotencia, anulación y concurrencia

- `ClaveOperacion` es UUID no vacía, única por empresa.
- Misma UUID + misma sesión/venta/desglose normalizado: devuelve el cobro existente, `Reintento=true`, sin consumir correlativo ni crear movimiento. Admite la versión original ya obsoleta e incluso sesión cerrada, porque no es una nueva operación.
- Misma UUID con otra solicitud: COBRO_INVALIDO. UUID distinta para una venta ya cobrada: VENTA_YA_COBRADA (con versión de sesión actual).
- Venta ya cobrada: Fase 7 bloquea la anulación **antes** de revertir inventario. No se borra el pago. No existe endpoint de reversión financiera en esta fase, por lo que se requiere un proceso futuro autorizado para resolverla.
- Escrituras en transacción Serializable, coordinador compartido y `sp_getapplock` empresarial `Heladeria.Inventario.{EmpresaId}`; no se creó una reserva independiente de Ventas. Así se serializan cobro y anulación entre procesos.
- RowVersion de sesión y venta, más índices únicos, protegen apertura/cobro/cierre concurrentes. Los correlativos conservan su control de concurrencia existente.
- El detalle de sesión se lee bajo la misma reserva transaccional, para no combinar encabezado, pagos y movimientos de momentos distintos. No modifica datos.
- No hay reintento automático ciego de escritura. Ante resultado de cobro incierto, consultar/reintentar con la misma UUID.
- Un error revierte sesión/pago/detalles/movimientos/arqueo/cierre/correlativos y auditoría de éxito. Después del rollback se intenta guardar una bitácora de rechazo independiente, sin contenido sensible ni cambios parciales del negocio.

## Arqueo y cierre

El arqueo guarda esperado, contado, diferencia, fecha, usuario, observaciones y versión de sesión. Actualiza su versión y conserva el snapshot sobre el que se contó. Un movimiento/cobro posterior, o un nuevo arqueo, invalida el anterior.

Cierre exige sesión abierta, versión actual, última medición vigente y transición configurada con CAJA.CERRAR. Verifica coherencia de libro, pagos y movimientos. Persiste fecha/usuario, número CC, ArqueoCierreId, efectivo esperado/contado y totales operativos por ingreso/egreso. Diferencia usa la columna calculada existente:

`Diferencia = Contado - Esperado` (puede ser negativa, cero o positiva).

No se genera movimiento para “cuadrar” caja. Una cerrada es solo lectura: no admite nuevos cobros, movimientos, arqueos ni segundo cierre. La caja física puede abrir después una sesión nueva explícita, sin arrastrar automáticamente el saldo anterior.

Sesiones históricas sin los nuevos snapshots se muestran con EsFase8=false y saldo teórico/efectivo desconocidos (null); se rechaza operarlas. No se infieren valores ni se desbloquea automáticamente una caja ocupada por un histórico.

## Permisos y endpoints

Se mantiene JWT, validación de sesión y asignación empresa/sucursal, protección IDOR y los límites de solicitudes de autenticación existentes. EmpresaId no se acepta como autoridad desde el body. Todos los DTO HTTP nuevos son clases selladas con init y DataAnnotations.

| Método / ruta bajo `/api/caja` | Permisos |
|---|---|
| GET cajas | CAJA.CONSULTAR |
| POST cajas | CAJA.OPERAR |
| GET sesiones | CAJA.CONSULTAR |
| GET sesiones/{id} | CAJA.CONSULTAR |
| GET referencias | CAJA.CONSULTAR |
| GET ventas-pendientes | CAJA.CONSULTAR |
| POST sesiones/abrir | CAJA.ABRIR |
| POST sesiones/{id}/movimientos | CAJA.OPERAR + CAJA.MOVIMIENTO_MANUAL |
| POST sesiones/{id}/cobrar-venta | CAJA.OPERAR + CAJA.COBRAR |
| POST sesiones/{id}/arqueo | CAJA.OPERAR |
| POST sesiones/{id}/cerrar | CAJA.CERRAR |

CAJA.ANULAR queda reservado, asignado inicialmente solo a ADMIN, sin endpoint que anule pagos o sesiones. Los siete permisos se asignan exclusivamente a ADMIN en la preparación; no se amplían roles ajenos.

Listados paginados: Pagina ≥ 1, TamanioPagina 1–100, Busqueda. Sesiones agrega CajaId y Abierta. El detalle incluye encabezado, movimientos, totales por tipo/forma, cobros con desglose y arqueos.

Errores sin stack trace: VALIDACION_CAJA (400), RECURSO_NO_ENCONTRADO (404), CONFLICTO_CAJA, CAJA_NO_ABIERTA, CAJA_YA_ABIERTA, VENTA_YA_COBRADA, COBRO_INVALIDO y CONCURRENCIA_CAJA (409). JWT/permisos conservan 401/403.

Auditoría: CAJA_CREADA, CAJA_ABIERTA, MOVIMIENTO_CAJA_CREADO, VENTA_COBRADA, COBRO_RECHAZADO, ARQUEO_REGISTRADO, CAJA_CERRADA, OPERACION_CAJA_RECHAZADA y FASE8_CONFIGURADA. No se registra el body ni la referencia bancaria en los mensajes de rechazo.

## Preparación explícita y despliegue manual pendiente

**No se ejecutó SQL ni bootstrap contra la instalación real.** Las pruebas usan únicamente SQLite y LocalDB desechables.

Para una instalación existente, un operador autorizado deberá:

1. Respaldar, detener escrituras y revisar compatibilidad de la base objetivo. Fases anteriores deben estar aplicadas.
2. En SSMS, seleccionar explícitamente la base correcta y ejecutar completo `BaseDatos/Reparaciones/20261007_Fase8_Caja.sql`. No ejecutar el maestro sobre una BD existente. El incremental no contiene USE ni conexión embebida.
3. Revisar el mensaje `FASE 8: esquema Caja validado. Sin datos operativos. Revise y ejecute --preparar-fase8 explícitamente.` Ante error, revisar la incompatibilidad; no deshabilitar CHECK/FK ni inventar datos. Puede repetirse después de resolverla.
4. Desplegar la API/modelo actualizados; también la anulación de Ventas consulta la nueva relación de pago, por lo que el esquema debe preceder al binario.
5. Con la conexión suministrada por el mecanismo seguro existente (sin secretos versionados), ejecutar desde la carpeta de la solución:

```powershell
dotnet run --project .\Heladeria.Bootstrap --configuration Release -- --preparar-fase8
```

Confirmación exacta: **PREPARAR FASE 8**. Requiere terminal interactiva. No solicita ni cambia contraseñas. Usa la empresa/sucursal principal operativa del administrador, valida dependencias y ejecuta todo en una transacción. No se dispara al iniciar API.

Prepara módulo, acciones/permisos ADMIN, flujo/estados/transición, conceptos mediante Tipos/ClaseTipos, tipos documentales, series/correlativos, EFECTIVO/TARJETA y marcador FASE8_CONFIGURADA. Reutiliza configuración compatible; la incompatible se rechaza sin sobrescribirla. Repetir no duplica catálogos ni reinicia contadores. No crea cajas, aperturas, movimientos, cobros, ventas ni moneda base.

6. Validar manualmente con datos reales autorizados: registrar/seleccionar caja, consultar referencias, abrir con saldo controlado, seleccionar una venta completada pendiente, cobrar, registrar un movimiento, contar y cerrar. Conservar las UUID de cobro y renovar VersionFila después de cada operación.

Ejemplo de contrato de cobro, sustituyendo IDs y versiones por los devueltos por API:

```json
{
  "ventaId": 123,
  "versionVenta": "VERSION_BASE64_DE_VENTA",
  "versionFila": "VERSION_BASE64_DE_SESION",
  "claveOperacion": "UUID_GENERADA_PARA_ESTE_COBRO",
  "pagos": [
    { "formaPagoId": 10, "montoAplicado": 80, "montoRecibido": 100, "referencia": null }
  ]
}
```

Los IDs del ejemplo no son constantes de implementación. El total y las formas deben corresponder a las referencias reales. Para arqueo: VersionFila, EfectivoContado, Observaciones opcional. Para cierre: VersionFila del arqueo vigente y ArqueoId retornado.

## Pruebas y validación

Archivos de pruebas nuevos: `EscenarioCaja.cs`, `CajaPruebas.cs`, `CajaHttpPruebas.cs`, `CajaSqlPruebas.cs`, bajo `Heladeria.Pruebas/Fase8`.

Cobertura: preparación idempotente/rollback; apertura cero/positiva/duplicada/inactiva/ajena; consultas; ingresos/egresos, precisión y saldo; efectivo/tarjeta/mixto/vuelto; estados de venta; UUID y cambio de payload; validación de referencias seguras, moneda, forma, importe y versiones; pendientes; IDOR; arqueos con diferencias ±/0; cierre/duplicación/sesión cerrada; históricos solo lectura; libro inconsistente; rollback tardío en apertura, movimiento, cobro, arqueo y cierre; bloqueo de anulación de venta cobrada sin modificar inventario/costos.

Pruebas HTTP ejecutan rutas reales: 401, 403, validación MVC 400, 404, conflictos 409, apertura 201 y ciclo financiero 200. Se comprueban por separado los pares de permisos para movimientos y cobros.

SQL Server LocalDB: maestro completo, esquema previo/parcial, objeto incompatible y rollback, repetición sin/con datos de prueba, índices, CHECK y computed columns reales, ROWVERSION y doble apertura/cobro/cierre desde DbContexts y coordinadores independientes. Son instancias privadas recién creadas, sin conexiones de appsettings; se eliminan al terminar. El helper usa nombres únicos de instancia **y base/archivos**, evitando colisiones entre pruebas paralelas.

No se debilitaron expectativas de regresión. Se corrigió el aislamiento del helper LocalDB (nombre físico compartido) y un helper nuevo que intentaba derivar el conteo de un saldo histórico desconocido, en lugar de enviar un conteo explícito.

Comandos de cierre:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Resultado de cierre: 81 pruebas nuevas de Fase 8; 399 pruebas totales aprobadas, 0 fallidas y 0 omitidas. Build Release con 0 errores y 0 advertencias. Se mantuvieron las 318 pruebas previas, incluyendo Seguridad, Configuración, Productos, Inventario/Kardex/costo promedio, Compras, Producción/Recetas y Ventas/POS.

La validación manual de negocio queda pendiente; no se autoriza ni automatiza la operación de la BD real.
