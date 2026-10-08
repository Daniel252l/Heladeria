# Corrección transversal: tiempo y orden transaccional

Validación: 2026-10-08. Alcance limitado a fechas/horas, presentación y orden estable.
No se avanza Caja ni Visual 6; no se continúa la validación manual de cobro con tarjeta.

## 1–5. Diagnóstico de las seis horas

El recorrido verificado en código y reproducido mediante HTTP + SQL Server aislado es:

| Paso | Comportamiento anterior | Corrección |
| --- | --- | --- |
| POST `/api/ventas` | `ServicioVentas.CrearAsync` asigna `FechaVenta = Ahora` | Se conserva. El body no proporciona esta hora. |
| Reloj | `ServicioVentasBase.Ahora` usa `TimeProvider.GetUtcNow().UtcDateTime`, Kind UTC | Se conserva la única autoridad de tiempo. |
| Persistencia | `datetime2(3)` guarda, por ejemplo, `2026-10-08 20:42:15.376`, sin metadatos de zona | Mismo esquema y mismos ticks. No hay desplazamiento en SQL. |
| GET / materialización | SqlClient devuelve `DateTimeKind.Unspecified`; EF no restauraba Kind | Conversión explícita en el modelo parcial EF para timestamps auditados: Kind UTC. |
| JSON | `2026-10-08T20:42:15.376`, sin Z | `2026-10-08T20:42:15.376Z`. Mantiene milisegundos. |
| TypeScript / pantalla | `new Date(valor)` interpretaba el ISO sin offset como hora local; `Intl` tampoco especificaba zona | Los instantes deben incluir zona; `Intl` usa la zona efectiva comunicada por la sesión. |

La diferencia no era una suma de seis horas en el servicio ni un cálculo comercial incorrecto: se perdió la semántica UTC en la frontera SQL → EF → JSON. La pantalla mostraba 20:42 como hora local en vez de convertir ese instante UTC a 14:42 Guatemala.

Ubicaciones del recorrido: `ServicioVentas.Operaciones.cs` (asignación), `ServicioVentasBase.cs` (reloj), `HeladeriaDbContext.cs` (precisión SQL), `ServicioVentas.Consultas.cs` (consulta y DTO), `types/ventas.ts` (string sin transformación), `utilidadesVentas.ts` → `utils/formato.ts` → pantalla `PaginaVentas.tsx`.

No se consultó la venta real VT-0000000006 ni el turno real 1. La causa del recorrido está comprobada mediante una reproducción controlada; la procedencia de cada registro real no se puede certificar sin esa revisión.

## 6. Política final de tiempo

- Operaciones: `TimeProvider` en UTC; los servicios existentes ya lo utilizaban. No se agregó otro reloj ni se tomó la hora del navegador para operaciones.
- Persistencia: mismos `datetime2` y precisión existente. `HeladeriaDbContext.Tiempo.cs` restaura Kind UTC al leer únicamente las entidades de las fases implementadas cuya semántica fue revisada. No cambia valores ni ejecuta una migración.
- Transporte: timestamps operativos con `Z`; las entradas con offset se normalizan a UTC sin pasar por la zona del host. No se permite emitir `DateTimeKind.Local` dependiente del servidor.
- Presentación: única política en `utils/fechas.ts`, consumida por Ventas, Compras/recepciones, Inventario y Producción/Recetas. Los wrappers conservan el estilo visual anterior.
- Zona IANA efectiva: configuración específica de sucursal → empresa → `Seguridad:ZonaHoraria`, actualmente `America/Guatemala`. La sesión incluye `usuario.sucursalActiva.zonaHoraria` y la zona de cada sucursal disponible. Se actualiza al iniciar/restaurar/renovar sesión o seleccionar otra sucursal; al cerrar se limpia a UTC.
- Sin cambiar esquema se pueden configurar `Seguridad:ZonasHorariasEmpresas:<empresaId>` y `Seguridad:ZonasHorariasSucursales:<sucursalId>`. Son claves de configuración operativa, no IDs incrustados en código. Las zonas se validan al iniciar la API. No se modificó la política existente de horarios de acceso de seguridad.
- Filtros de Ventas/Producción: una fecha/hora sin offset es civil de la zona efectiva del contexto autenticado; se convierte a UTC antes de comparar. Un instante UTC ya explícito se conserva. Horas civiles ambiguas/inexistentes por cambio estacional se rechazan con validación controlada, no se adivinan.

### Fechas civiles y planificación: no confundir con ejecución

`OrdenesCompra.FechaEmision` y `FechaEntregaEsperada` son fechas editables de documento/entrega; no se etiquetan como UTC por EF ni se desplazan al presentarlas. Las vigencias de recetas/impuestos siguen siendo `DateOnly`. No se alteraron sus cálculos ni reglas de vigencia.

`OrdenesProduccion.FechaInicio` es la fecha **planificada**, no el instante de ejecución. El formulario envía la hora civil elegida, sin convertirla con la zona del navegador; el backend la resuelve según la sucursal y persiste UTC. También admite ISO con Z/offset. Consumos, finalización y bitácora siguen tomando exclusivamente el reloj del servidor. No se sustituyó la planificación por `Ahora`.

### Auditoría de alcance

| Área | Fechas operativas / autoridad revisada |
| --- | --- |
| Ventas | creación, modificación, completar, anular: `ServicioVentasBase.Ahora` |
| Compras | recepción e histórico de precio: `ServicioComprasBase.Ahora`; creación/confirmación documentadas en bitácora |
| Inventario | movimientos/aplicación/anulación, Kardex, último movimiento, costos históricos: `ServicioFase4Base.AhoraUtc` |
| Producción | consumo, finalización, costos, bitácora: `ServicioProduccionBase.Ahora` |
| Recetas | vigencias civiles; acciones auditadas con reloj del servidor |
| Caja | apertura/cierre/modificación de turno, pago, movimiento, arqueo: `ServicioCajaBase.Ahora` |
| Seguridad/configuración/catálogos | sesiones, accesos, bitácoras, creación/modificación: TimeProvider UTC |

La búsqueda en fuentes productivas `.cs` y SQL no encontró llamadas directas a `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, `GETDATE`, `GETUTCDATE`, `SYSDATETIME` ni `SYSUTCDATETIME`. Los usos de reloj directo en fixtures de pruebas no constituyen autoridad de la aplicación. No se activaron ni reinterpretaron entidades de módulos todavía no implementados.

## 7–8. Archivos modificados

Rutas relativas a la solución `Heladeria/`.

Backend:

- `Heladeria.Aplicacion/Tiempo/TiempoEmpresarial.cs` (nuevo).
- `Heladeria.Aplicacion/Seguridad/Configuracion/OpcionesSeguridad.cs`.
- `Heladeria.Aplicacion/Seguridad/Autenticacion/ModelosAutenticacion.cs`.
- `Heladeria.Api/Configuracion/ConfiguracionSeguridad.cs`.
- `Heladeria.Api/Configuracion/ConvertidorFechaApi.cs` (nuevo).
- `Heladeria.Api/Program.cs`.
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Tiempo.cs` (nuevo).
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Fase7.cs` (conecta la configuración parcial de tiempo).
- `Heladeria.Infraestructura/Seguridad/Autenticacion/ServicioAutenticacion.cs`.
- `Heladeria.Infraestructura/Ventas/ServicioVentasBase.cs`.
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Consultas.cs`.
- `Heladeria.Infraestructura/Produccion/ServicioProduccionBase.cs`.
- `Heladeria.Infraestructura/Produccion/ServicioProduccion.cs`.

Frontend, bajo `heladeria-web/src/`:

- `utils/fechas.ts` (nuevo).
- `utils/formato.ts`.
- `types/auth.ts`.
- `auth/ProveedorAutenticacion.tsx`.
- `features/compras/utilidadesCompras.tsx`.
- `features/compras/PaginaFormularioCompra.tsx`.
- `features/produccion/utilidadesProduccion.ts`.
- `features/produccion/PaginaFormularioProduccion.tsx`.

Pruebas:

- `Heladeria.Pruebas/Tiempo/PoliticaTiempoPruebas.cs` (nuevo).
- `Heladeria.Pruebas/Tiempo/VentasConcurrentesSqlPruebas.cs` (nuevo).
- `Heladeria.Pruebas/Seguridad/ServicioAutenticacionPruebas.cs` (agrega prueba, sin debilitar las existentes).
- `heladeria-web/src/utils/fechas.test.ts` (nuevo).
- `heladeria-web/src/auth/ProveedorAutenticacion.test.tsx`.
- `heladeria-web/src/features/ventas/Ventas.test.tsx`.
- `heladeria-web/src/features/produccion/Produccion.test.tsx` (verifica que el formulario envía la hora planificada civil).

Documentación: este archivo. Los artefactos generados de build y `TestResults` no son cambios de fuente.

## 9–10. SQL e históricos

No se modificó ningún SQL, el esquema maestro ni el esquema real. No se ejecutó SQL ni bootstrap contra la BD real. Los tests crean una instancia y base LocalDB privadas con nombres únicos, preparan únicamente fixtures desechables y eliminan exclusivamente esas bases propias al finalizar.

No hay UPDATE masivo ni reparación histórica. Los registros creados por los escritores revisados ya contienen UTC: restaurar Kind solo permite interpretarlos correctamente. Los registros importados/manualmente insertados o de versiones ajenas a estos escritores **no pueden asumirse verificados**. Como `datetime2` no conserva procedencia ni offset, la aplicación interpreta las columnas operativas auditadas como UTC; si contienen valores locales/mixtos, eso requiere una revisión controlada y reparación separada, autorizada, con respaldo y evidencia por origen. No se puede deducir la zona a partir del número de la hora.

El frontend no agrega Z arbitrariamente a cadenas ambiguas recibidas de una API antigua: indica «Fecha sin zona horaria». Esto no detecta un dato local que el servidor haya leído de una columna bajo política UTC; ese caso exige verificar procedencia. Desplegar backend y frontend juntos y renovar la sesión.

Los ejemplos reales de Caja proporcionados serían 14:16:27.675, 14:22:45.501, 14:22:45.618, 14:30:45.629 y 14:37:15.338 Guatemala **si su origen es el escritor UTC revisado**. No se alteraron esos registros ni se construyó una UI nueva de Caja.

## 11–15. Concurrencia y orden

Se conserva `SeriesDocumentos`/`CorrelativosDocumentos`. `DocumentoAsync` incrementa el correlativo dentro de la misma transacción que inserta la venta. `EscribirAsync` usa aislamiento Serializable y `sp_getapplock` exclusivo con propietario Transaction sobre `Heladeria.Inventario.<empresa>`, compartido por los servicios operativos, antes de leer el correlativo. Se conserva RowVersion y los índices existentes. El reloj no participa en asignar números. No se añadió una segunda reserva, reintentos ni un generador paralelo.

Nuevo orden del listado: `FechaVenta DESC, NumeroFactura DESC, Id DESC`. El contrato expone `NumeroFactura` como `NumeroVenta`. La serie VT preparada exige prefijo `VT-` y longitud numérica fija de 10; el orden textual corresponde al correlativo dentro de esa serie. Id es desempate final. No se usa solamente FechaVenta. No hay garantía de orden de llegada por red: el orden comercial es el de asignación transaccional.

Pruebas `PostVentasSimultaneasConContextosYCoordinadoresIndependientes(cantidad: 2)` y `(cantidad: 5)`: ambas aprobadas. Lanzan POST HTTP paralelos con una barrera de inicio, un DbContext por petición y coordinadores de memoria distintos; la exclusión depende de SQL Server real aislado. Verifican:

- todas las respuestas 201, IDs/VT distintos y correlativos consecutivos sin duplicados;
- todas las ventas con el mismo timestamp fijo del servidor;
- lectura SQL cruda Kind Unspecified y lectura EF UTC con los mismos ticks;
- POST y GET emiten exactamente `2026-10-08T20:42:15.376Z`;
- auditoría con reloj UTC autoritativo;
- listado repetido tres veces idéntico y ordenado por VT;
- filtro civil 14:42 Guatemala encuentra el timestamp UTC 20:42.

## 16–18. Resultados

- `dotnet build --configuration Release`: 0 errores, 0 advertencias.
- `dotnet test --configuration Release --no-build`: **428 aprobadas, 0 fallidas, 0 omitidas** (399 previas + 29 nuevas). Incluye seguridad, configuración, inventario/Kardex/costos, compras, recetas, producción, ventas y caja.
- Resultado backend conservado en `Heladeria.Pruebas/TestResults/validacion-tiempo-orden.trx` (ejecución completa con logger adicional).
- `npm run lint`: correcto, sin diagnósticos.
- `npm run build`: correcto.
- `npm test`: **228 aprobadas**, 20 archivos (218 previas + 10 nuevas).
- Tests de tiempo cubren política central, offsets, cambios de día, horario estacional, fechas civiles, instantes sin zona, render real de Ventas y actualización de zona en la sesión. No se cambió diseño ni cálculos financieros.
- Comprobación adicional con proceso de pruebas en `TZ=Asia/Tokyo`: 54 pruebas aprobadas (formateador, sesión y pantalla Ventas), manteniendo Guatemala como zona de presentación configurada. La variable se restauró al finalizar; no se modificó la zona del sistema operativo.

## 19. Pendientes manuales

1. Desplegar/reiniciar API y servir el nuevo build del frontend; restaurar/renovar sesión y comprobar `zonaHoraria`.
2. Comprobar en Network que un GET de Ventas y de Caja devuelve timestamps operativos con Z. Contrastar VT-0000000006 con su procedencia; no modificar su valor para arreglar la visualización.
3. Verificar `/ventas`: UTC 20:42 → 14:42 Guatemala; repetir con navegador en otra zona. Verificar filtros cerca de medianoche y que compra/vigencia conservan su día civil.
4. Clasificar por origen los históricos de desarrollo cargados externamente antes de proponer cualquier script de reparación. No hay autorización ni ejecución de tal script en esta corrección.
5. Esta entrega no reanuda la validación manual de Caja/tarjeta ni habilita Visual 6.
