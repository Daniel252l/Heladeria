# Fase 9 — cuentas por cobrar y cuentas por pagar

## Alcance y diagnóstico

Backend financiero sobre la arquitectura existente. No se implementaron pantallas de cartera, contabilidad, conciliación bancaria, transferencias, FEL, impresión, notificaciones ni reversión financiera. No se ejecutaron SQL ni bootstrap contra la conexión de desarrollo real.

El esquema ya contenía `CuentasPorCobrar`, `CuentasPorPagar`, `AplicacionesCuentasPorCobrar`, `AplicacionesCuentasPorPagar`, `FacturasProveedores`, `Pagos` y `PagoDetalles`. CxC exige cliente y venta; CxP exige proveedor y **factura del proveedor**, no solamente orden de compra. Las claves únicas existentes impiden dos cuentas para una venta/factura. Los estados financieros usan la FK compuesta `(FlujoEstadoId, EstadoId)` hacia `EstadosFlujo`, no el catálogo global `Estados`.

Ventas ya tenía `EsCredito`, pero Fase 7 forzaba/rechazaba crédito en el servicio POS. Compras se representa por `OrdenesCompra`, sin modalidad financiera previa. Completar una venta y recibir inventario son estados comerciales: ninguno equivale por sí mismo a haber cobrado/pagado.

**Decisión confirmada por el usuario:** registrar explícitamente la factura real después de recibir la compra. La recepción no se bloquea por falta de factura y nunca inventa un documento del proveedor.

## Esquema Database First

No se agregan tablas. Extensiones en entidades parciales y configuración parcial EF; SQL es la fuente de verdad, sin migraciones Code First.

| Tabla existente | Extensión |
|---|---|
| Ventas | `FechaVencimientoCredito date NULL`; se reutiliza `EsCredito` |
| OrdenesCompra | `EsCredito bit NULL`, `VersionFila rowversion` |
| CuentasPorCobrar / CuentasPorPagar | `SucursalId`, `MonedaId`, `UsuarioCreacionId`, `FechaCreacion datetime2(3)`, `UsuarioModificacionId`, `FechaModificacion datetime2(3)`, `VersionFila rowversion` |
| Pagos | `HuellaSolicitud varbinary(32) NULL`, `Observacion nvarchar(500) NULL`; se reutilizan UUID, turno, tercero, moneda y detalle de Fase 8 |
| FacturasProveedores | `ClaveOperacion uniqueidentifier NULL`, `HuellaSolicitud varbinary(32) NULL` |

Las nuevas columnas de metadata son opcionales para **no inventar datos históricos**. Sus CHECK exigen metadata completa para cuentas Fase 9. Hay FKs para sucursal, moneda y usuarios de auditoría. Se conservan las restricciones de montos anteriores y se valida saldo no negativo y no mayor al original en nuevas cuentas.

Índices nuevos filtrados: factura por `(EmpresaId, ClaveOperacion)` y por `OrdenCompraId` para documentos Fase 9. No se impone retroactivamente una factura única por orden a históricos; el servicio rechaza reconocer una orden con cualquier factura previa hasta conciliación explícita.

Se amplían de manera controlada los CHECK `CK_Pagos_Tercero` y `CK_MovimientosCaja_Snapshot_Fase8`. El cobro POS sigue pudiendo ser anónimo; un pago de cartera siempre es nominativo, tiene UUID/huella y no utiliza `Pagos.VentaId`. Así varios abonos de una venta no chocan con el índice de cobro POS único.

Las aplicaciones existentes se conservan como historial independiente. `TotalAplicado` es derivado; no se agrega otra columna acumuladora ni una tabla de pagos paralela. Los importes se validan con dos decimales; no se alteran conversiones ni costos unitarios base de inventario.

Después de un futuro scaffold, conservar/reintegrar estas extensiones y mappings sin declarar propiedades duplicadas. Ejecutar pruebas de modelo y scripts tras cualquier regeneración.

## Modalidades y origen

### Ventas / CxC

En crear/actualizar venta: `esCredito: true`, cliente identificado y `fechaVencimientoCredito` civil vigente. `false` representa CONTADO. Omitir modalidad al crear conserva el flujo contado anterior; omitirla al editar **preserva** la modalidad existente. Una actualización explícita a contado limpia vencimiento; los cambios siguen limitados a BORRADOR.

Al completar una venta de crédito se crea exactamente una cuenta PENDIENTE por el total comercial calculado por backend. Es la **misma transacción** de salida de inventario, Kardex, estado comercial, correlativos y auditoría. Si falla la cuenta, se revierte el cierre completo. El reintento del cierre existente no duplica cuenta. Ventas contado no generan CxC y se cobran como antes en Caja.

No se convierten ventas históricas en deuda. El crédito histórico que no tenga configuración/snapshots de esta fase queda pendiente de conciliación, no se cobra automáticamente ni se infiere vencimiento.

### Compras / CxP

Crear la orden con `esCredito: true`. En documentos anteriores `NULL` significa modalidad **desconocida**, no contado. Omitir el campo en una edición conserva su valor, incluso NULL; nuevas órdenes sin el campo mantienen el flujo contado anterior.

Primero confirmar y recibir completamente la orden. Después registrar la factura real mediante `POST /api/cuentas-por-pagar/desde-compra` con número, serie opcional, fechas, UUID y versión de compra. El servicio verifica proveedor operativo, empresa/sucursal, modalidad CREDITO, flujo ORDENES_COMPRA/RECIBIDA, cantidades recibidas completas, ausencia de factura previa y moneda base con tipo de cambio 1.

Esta fase reconoce **una factura por el total de la orden totalmente recibida**. Los importes se toman de los cálculos persistidos de la orden; el cliente no puede enviar un saldo ni inventar otro total. Si la factura real difiere de la orden, es parcial o consolida varias órdenes, no usar este endpoint hasta conciliar el documento. No se implementaron esas variantes.

La factura y la cuenta se guardan atómicamente; su repetición exacta devuelve la misma operación. Las compras contado o de modalidad desconocida no generan obligaciones. Tampoco se marcan como pagadas: el flujo anterior de recepción no prueba un pago y no se inventa uno.

## Saldo, estados y vencimientos

Antes de aplicar dinero se comprueba que `MontoOriginal - aplicaciones de pagos válidos = SaldoPendiente`. Si hay discrepancia se rechaza para conciliación; nunca se repara silenciosamente. Cada nueva aplicación recalcula ese saldo, no acepta un saldo del cliente y no puede excederlo.

Estados persistidos en flujos independientes `CUENTAS_POR_COBRAR` / `CUENTAS_POR_PAGAR`:

- PENDIENTE: sin aplicaciones.
- PARCIAL: saldo entre cero y original.
- PAGADA: saldo cero.
- ANULADA: catálogo disponible para consulta; sin endpoint de anulación financiera en esta fase.

La clasificación VIGENTE/VENCIDA se calcula comparando `FechaVencimiento` con el día civil de la zona de sucursal (con fallback empresa/configuración existente), solo para cuentas pendientes. El día del vencimiento aún es vigente. PAGADA y ANULADA mantienen su clasificación. No se persiste un estado VENCIDA ni se usa un desfase horario fijo.

`TimeProvider` suministra timestamps UTC; EF restaura `DateTimeKind.Utc` y HTTP emite `Z`. Emisión/vencimiento son `DateOnly`. Se conservan fechas civiles de órdenes de compra sin convertirlas en timestamps.

## Pagos, Caja y atomicidad

Un POST aplica un monto exacto con una forma de pago, sin vuelto. CxC admite EFECTIVO/TARJETA operativos; CxP admite EFECTIVO. No se habilitan otras formas por tener nombres similares.

Se reutilizan `Pagos`, `PagoDetalles`, una aplicación financiera y `MovimientosCaja`. Conceptos separados:

- `COBRO_CUENTA_POR_COBRAR`: ingreso, afecta efectivo solo si la forma es efectivo.
- `PAGO_CUENTA_POR_PAGAR`: egreso de efectivo.

No se mezclan con COBRO_VENTA ni movimientos manuales. El movimiento vincula pago/detalle y referencia el documento, la cuenta y el tercero. Su correlativo usa la serie MC existente; no hacen falta series adicionales para cuentas cuyo documento visible es el origen.

La sesión debe estar ABIERTA, operativa, en la empresa/sucursal y moneda de la cuenta. CxP exige efectivo suficiente. Se vuelve a validar origen y tercero antes de escribir. El pago actualiza saldo/estado de cuenta y versión de Caja, invalidando cualquier arqueo anterior. Resumen, totales por forma, efectivo esperado, arqueo y cierre incluyen los conceptos nuevos. El listado `Cobros` de Caja sigue siendo exclusivamente cobro POS; los pagos financieros se consultan en el historial de la cuenta y en los movimientos de Caja.

Se usa el coordinador existente, transacción SERIALIZABLE y el mismo `sp_getapplock` empresarial `Heladeria.Inventario.{empresa}` utilizado por Ventas/Compras/Caja. No hay transacciones anidadas independientes ni commits intermedios. Las llamadas internas de Caja consumen la transacción del servicio financiero. Ante error se revierte pago, detalle, aplicación, movimiento, saldo, estado, versión y correlativo. Solo puede persistir la bitácora separada del rechazo, sin cambios comerciales/financieros.

### Idempotencia y concurrencia

Cada POST financiero exige UUID no vacía. La huella SHA-256 incluye sentido CxC/CxP, cuenta, contexto/usuario, sesión, forma, monto decimal normalizado, referencia/observación normalizadas y versiones del request. La factura tiene su propia UUID/huella con documento y versión de origen.

- Misma UUID + mismo payload: devuelve operación existente, `reintento: true`, incluso si Caja ya cerró.
- Misma UUID + payload diferente, incluso otras versiones/usuario: HTTP 409 `CLAVE_OPERACION_CONFLICTIVA`.
- UUID nueva sobre cuenta saldada: `CUENTA_YA_PAGADA`.
- Versión de cuenta obsoleta: `CONCURRENCIA_CXC` / `CONCURRENCIA_CXP`.
- Versión de Caja obsoleta: `CONCURRENCIA_CAJA`.

Se requieren versiones actuales de cuenta y Caja; cinco abonos enviados con la misma versión no se aplican todos ciegamente. Uno puede completar y los restantes deben recargar explícitamente y reenviar los intentos fallidos. Si la respuesta se perdió, reintentar **primero el payload original** con su UUID, no reconstruirlo con otra versión. Nunca cambiar UUID a ciegas después de un timeout.

## Anulaciones y auditoría

No se implementa reversión financiera. Una venta con CxC o una compra con CxP rechaza anulación directa con `OPERACION_FINANCIERA_EXISTENTE`, incluso si aún no tiene aplicaciones. Es deliberadamente conservador: no se deja una obligación viva cuyo origen fue anulado. Las compras recibidas sin cuenta siguen sujetas a la protección previa de recepción/inventario. Los documentos contado sin operaciones financieras conservan las reglas anteriores.

`Bitacoras` registra CUENTA_GENERADA, ABONO_APLICADO/PAGO_APLICADO y estado/saldo resultante bajo CXC/CXP. Rechazos financieros se auditan después del rollback con código y contexto, sin datos de tarjeta, tokens ni texto de la referencia/observación. Los rechazos del cierre de venta por crédito también conservan el registro VENTA_RECHAZADA. Las referencias de pago reutilizan la validación segura de Caja: no PAN, CVV/CVC ni secuencias largas de dígitos. Nunca escribir información sensible en observaciones.

## Permisos y endpoints

La preparación asigna solo a ADMIN: `CXC.CONSULTAR`, `CXC.ADMINISTRAR`, `CXC.COBRAR`, `CXP.CONSULTAR`, `CXP.ADMINISTRAR`, `CXP.PAGAR`. No amplía el rol CONSULTA. Se usan las policies existentes y se vuelve a comprobar la asignación operativa a sucursal.

| Endpoint | Permiso |
|---|---|
| GET `/api/cuentas-por-cobrar` | CXC.CONSULTAR |
| GET `/api/cuentas-por-cobrar/{id}` | CXC.CONSULTAR |
| GET `/api/cuentas-por-cobrar/resumen` | CXC.CONSULTAR |
| GET `/api/cuentas-por-cobrar/referencias` | CXC.CONSULTAR |
| POST `/api/cuentas-por-cobrar/{id}/abonos` | CXC.COBRAR + CAJA.OPERAR + CAJA.COBRAR |
| GET `/api/cuentas-por-pagar` | CXP.CONSULTAR |
| GET `/api/cuentas-por-pagar/{id}` | CXP.CONSULTAR |
| GET `/api/cuentas-por-pagar/resumen` | CXP.CONSULTAR |
| GET `/api/cuentas-por-pagar/referencias` | CXP.CONSULTAR |
| POST `/api/cuentas-por-pagar/{id}/pagos` | CXP.PAGAR + CAJA.OPERAR |
| POST `/api/cuentas-por-pagar/desde-compra` | CXP.ADMINISTRAR |

Filtros comunes: `busqueda`, `terceroId` (cliente en CxC, proveedor en CxP), `estado`, `vencimiento`, `fechaDesde`, `fechaHasta`, `sucursalId`, `pagina`, `tamanioPagina`. El filtro de sucursal nunca amplía el contexto autorizado. El listado está paginado y ordenado por Id descendente. No hay endpoints redundantes por cliente/proveedor: se usa `terceroId`.

Resumen agrupado por `monedaId`/`moneda`, sin sumar monedas distintas: `totalPendiente`, `totalVencido`, `totalVigente`, `cantidadPendientes`, `cantidadVencidas`. Históricos son consultables sin inventar metadata; la sucursal se deriva del origen cuando está disponible. Una factura histórica sin orden/sucursal identificable no se expone a otra sucursal ni se opera automáticamente. El detalle devuelve encabezado, saldo, historial independiente, usuario real, forma(s), referencia, pago, sesión y movimientos asociados.

Ejemplo de registro de factura (sustituir IDs/versiones/UUID por los de la operación real):

```json
{
  "ordenCompraId": 123,
  "versionCompra": "VERSION_BASE64_DEL_GET",
  "claveOperacion": "GENERAR_UUID_NUEVA",
  "serie": "SERIE_REAL",
  "numeroDocumento": "NUMERO_REAL",
  "fechaDocumento": "2026-10-09",
  "fechaVencimiento": "2026-11-08"
}
```

Ejemplo de abono/pago:

```json
{
  "claveOperacion": "GENERAR_UUID_NUEVA",
  "versionFila": "VERSION_CUENTA_DEL_GET",
  "sesionCajaId": 123,
  "versionCaja": "VERSION_SESION_DEL_GET",
  "formaPagoId": 123,
  "monto": 30.00,
  "referencia": "AUT-REAL",
  "observacion": "Abono acordado"
}
```

Los valores 123 son únicamente placeholders documentales; el código nunca asume IDs identity.

## Errores controlados

401 sin autenticación; 403 sin cualquiera de los permisos necesarios; 404 cuenta/origen fuera del contexto o inexistente. Validaciones de DTO/importe/referencia retornan 400, no 500. Los helpers compartidos conservan `VALIDACION_CAJA` para importes/referencias y `CONCURRENCIA_CAJA` para sesión.

Errores financieros relevantes: `CLIENTE_REQUERIDO_PARA_CREDITO`, `VALIDACION_CXC`, `VALIDACION_CXP`, `VALIDACION_CARTERA`, `CUENTA_CXC_NO_ENCONTRADA`, `CUENTA_CXP_NO_ENCONTRADA`, `COMPRA_NO_ENCONTRADA`, `CUENTA_YA_PAGADA`, `ABONO_SUPERA_SALDO`, `PAGO_SUPERA_SALDO`, `SALDO_INSUFICIENTE`, `FORMA_PAGO_INVALIDA`, `REFERENCIA_REQUERIDA`, `DOCUMENTO_FINANCIERO_DUPLICADO`, `CLAVE_OPERACION_CONFLICTIVA`, `CONCURRENCIA_CXC`, `CONCURRENCIA_CXP`, `OPERACION_FINANCIERA_EXISTENTE`, `CONFLICTO_CARTERA`. Caja cerrada conserva `CAJA_NO_ABIERTA`.

## SQL y preparación explícita (no ejecutados contra BD real)

1. Respaldar y revisar el esquema/datos en un ambiente de prueba primero. Detener escritores durante la actualización estructural; la reserva de esquema no sustituye una ventana de mantenimiento.
2. Para una base existente de Fase 8, ejecutar manualmente **solo** `BaseDatos/Reparaciones/20261009_Fase9_CxC_CxP.sql` en la base seleccionada. No ejecutar el maestro de creación sobre una instalación existente.
3. El script usa una transacción, XACT_ABORT y SQL dinámico controlado para DDL y toda referencia a columnas nuevas. Valida tipos, tamaños, precisión, nullability, FKs, CHECK canónicos e índices. No descarta restricciones personalizadas ni cambia datos para hacerlas pasar. Tolera columnas ya añadidas si son compatibles; ante incompatibilidad devuelve error 51090 y revierte los cambios de esa ejecución. Las columnas existentes de una ejecución anterior no desaparecen por ese rollback.
4. Puede ejecutarse otra vez: no duplica columnas/índices/constraints y no crea cuentas, pagos ni catálogos. Mensaje final esperado: `FASE 9: esquema CxC/CxP validado. Sin datos operativos. Revise y ejecute --preparar-fase9 explícitamente.`
5. El maestro incluye el mismo bloque Fase 9 después de Fase 8, separado por GO. El script Fase 8 reconoce las dos expresiones ampliadas exactas para no degradarlas si se revalida después.
6. Desde la carpeta de la solución, y solo con conexión de destino revisada:

```powershell
dotnet run --project Heladeria.Bootstrap --configuration Release -- --preparar-fase9
```

7. Escribir exactamente **PREPARAR FASE 9**. El proceso exige terminal interactiva; no se ejecuta al iniciar la API. Solo configura módulos, permisos/acciones ADMIN, estados financieros, cuatro tipos de Caja (dos de movimiento y dos de operación de pago) y marcador `FASE9_CONFIGURADA`. Reutiliza series/formas previas, no crea clientes, proveedores, cuentas, pagos, ventas ni movimientos. Repetición compatible no duplica registros. Incompatibilidad revierte la preparación.

## Pruebas y regresión

Nuevas pruebas Fase 9 con SQLite relacional para escenarios de negocio/rollback y SQL Server LocalDB privado para DDL, rowversion y concurrencia. Los helpers SQL crean una instancia/base aleatoria, nunca cargan cadenas de appsettings y eliminan exclusivamente esa base de prueba al finalizar. Los preparadores/bootstrap ejecutados en tests pertenecen solo a esa instancia desechable, no a la instalación real.

Cobertura: generación nominativa desde venta y factura posterior a recepción, contado sin cuenta, edición compatible sin reclasificación, parciales/total, exceso, UUID repetida/conflictiva, caja cerrada/insuficiente, tarjeta CxC/no CxP, permisos HTTP independientes, alcance, auditoría UTC, vencimiento civil, históricos de solo lectura, saldo inconsistente, invalidación de arqueo/cierre, anulación bloqueada y fallo inyectado con rollback completo (incluido inventario/Kardex al fallar la cuenta del cierre de venta).

SQL prueba preparación parcial compatible/incompatible, repetición, restricciones personalizadas y ausencia de operaciones ficticias. Dos pagos finales simultáneos usan DbContexts/conexiones/coordinadores independientes: solo uno aplica. La UUID idéntica devuelve un reintento; UUID distinta no permite doble pago. Cinco abonos con versión inicial idéntica exigen recarga para los rechazados y terminan con cinco pagos exactos, correlativos, movimientos y auditoría correctos.

La única prueba previa adaptada es la de esquema anterior a Fase 9 de `CajaSqlPruebas`: usa `AnyAsync()` para comprobar ausencia de pagos en lugar de proyectar columnas financieras nuevas sobre un esquema deliberadamente Fase 8. No se eliminó ninguna aserción funcional ni se ocultó una regresión.

Compatibilidad mínima frontend: los resúmenes/cierre existentes de Caja incluyen conceptos CxC/CxP en ingresos/egresos y sus etiquetas. Sin nuevas rutas, formularios, permisos UI ni pantallas de cartera. Se agregan tres pruebas y se ejecuta toda la suite frontend.

Comandos de validación desde la solución:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Desde `heladeria-web`:

```powershell
npm run lint
npm run build
npm test
```

Resultados finales verificados el 2026-10-09:

- `dotnet build --configuration Release`: 0 errores, 0 advertencias.
- Suite completa Release: **488 aprobadas, 0 fallidas, 0 omitidas** (428 previas + 60 de Fase 9), incluidos tests SQL Server LocalDB. Evidencia local: `Heladeria.Pruebas/TestResults/fase9-final.trx`.
- Frontend: `npm run lint` y `npm run build` correctos; `npm test`: **319 aprobadas en 22 archivos** (316 previas + 3 de compatibilidad).
- Hubo un intento intermedio de build mientras testhost mantenía DLLs abiertas en Windows (MSB3026/MSB3021). Fue un conflicto de ejecución local, no una regresión de código; después de finalizar la suite se repitieron build y suite completa secuencialmente con los resultados anteriores.
- Lista para validación manual; no equivale a aprobación funcional en la instalación real.

## Checklist manual pendiente

- Aplicar script en copia de la BD, repetir, comprobar rollback ante una incompatibilidad preparada solo en esa copia; revisar columnas/índices y ninguna operación creada.
- Ejecutar preparador explícito en el ambiente elegido, confirmar texto, revisar registros creados y repetir sin duplicados. Renovar sesión/token/permisos según el flujo existente.
- Crear venta crédito con cliente y vencimiento; confirmar/completar; comprobar una CxC, original igual al total, saldo íntegro. Venta contado no genera CxC y sigue siendo cobrable por Caja POS.
- Crédito sin cliente/vencimiento: 400. Verificar misma empresa/sucursal y versiones actuales.
- Abrir Caja; abonar parcialmente; verificar saldo, PARCIAL, usuario, UTC Z, aplicación y movimiento CxC. Revisar cambio de versión e invalidación de arqueo.
- Aplicar saldo final: PAGADA/saldo cero. Exceso: conflicto sin cambios. Repetir UUID/payload: reintento sin doble ingreso; cambiar payload con misma UUID: conflicto.
- Probar tarjeta CxC: total de Caja aumenta, efectivo no. CxP tarjeta se rechaza. Referencia sensible no se acepta.
- Crear compra a crédito; confirmar/recibir completamente; comprobar que aún no existe CxP. Registrar la factura real de total coincidente: una factura y una cuenta, sin alterar recepción ni inventario.
- Pagar proveedor parcialmente y luego total; verificar egresos diferenciados, saldo exacto y que Caja no quede negativa. Compra contado/desconocida no genera CxP.
- Consultar vigentes/vencidas y resumen por moneda; comprobar cambio de día según zona y cuentas PAGADAS excluidas de pendiente. Revisar filtros, paginación e historial.
- Intentar anular venta/compra con cuenta: `OPERACION_FINANCIERA_EXISTENTE`. No ejecutar reversión manual como parte de esta fase.
- Probar cada permiso por separado y usuario CONSULTA: 403. Otra sucursal/empresa: no expone ni modifica cuentas ajenas.
- Arqueo actualizado y cierre: totales/efectivo coinciden con movimientos POS, manuales y financieros, sin contar apertura como ingreso operativo.
- Ejecutar regresión manual de contado POS, compras, inventario y Caja anterior. No avanzar a Fase Visual 7 hasta aprobación.

## Archivos de este cierre

- `Heladeria.Aplicacion/Cartera/ContratosCartera.cs`
- `Heladeria.Aplicacion/Compras/ContratosCompras.cs`
- `Heladeria.Aplicacion/Ventas/ContratosVentas.cs`
- `Heladeria.Aplicacion/Seguridad/Autorizacion/ContratosAutorizacion.cs`
- `Heladeria.Aplicacion/Seguridad/Bootstrap/IPreparadorFase9.cs`
- `Heladeria.Api/Controllers/CuentasPorCobrarController.cs`
- `Heladeria.Api/Controllers/CuentasPorPagarController.cs`
- `Heladeria.Infraestructura/Cartera/ReglasCartera.cs`
- `Heladeria.Infraestructura/Cartera/ServicioCartera.Consultas.cs`
- `Heladeria.Infraestructura/Cartera/ServicioCartera.Operaciones.cs`
- `Heladeria.Infraestructura/Caja/ServicioCajaBase.cs`
- `Heladeria.Infraestructura/Caja/ServicioCaja.Consultas.cs`
- `Heladeria.Infraestructura/Caja/ServicioCaja.Operaciones.cs`
- `Heladeria.Infraestructura/Compras/ServicioCompras.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Consultas.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Operaciones.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentasBase.cs`
- `Heladeria.Infraestructura/Persistencia/Entidades/Cartera.Fase9.cs`
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Fase9.cs`
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Fase7.cs`
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Tiempo.cs`
- `Heladeria.Infraestructura/Seguridad/Bootstrap/PreparadorFase9.cs`
- `Heladeria.Infraestructura/InyeccionDependencias.cs`
- `Heladeria.Bootstrap/Program.cs`
- `BaseDatos/Reparaciones/20261009_Fase9_CxC_CxP.sql`
- `BaseDatos/Reparaciones/20261007_Fase8_Caja.sql`
- `BaseDatos/Heladeria.sql`
- `Heladeria.Pruebas/Fase9/EscenarioCartera.cs`
- `Heladeria.Pruebas/Fase9/CarteraPruebas.cs`
- `Heladeria.Pruebas/Fase9/CarteraHttpPruebas.cs`
- `Heladeria.Pruebas/Fase9/CarteraSqlPruebas.cs`
- `Heladeria.Pruebas/Fase8/CajaSqlPruebas.cs`
- `Heladeria.Pruebas/Integracion/Persistencia/BaseSqlServerAislada.cs`
- `heladeria-web/src/features/caja/utilidadesCaja.ts`
- `heladeria-web/src/features/caja/ComponentesCaja.tsx`
- `heladeria-web/src/features/caja/FormulariosSesionCaja.tsx`
- `heladeria-web/src/features/caja/Caja.test.tsx`
- `docs/FASE9_CXC_CXP.md`

También se generaron salidas de compilación, bundle y resultados TRX locales. No son scripts para la BD real ni contienen credenciales reales.
