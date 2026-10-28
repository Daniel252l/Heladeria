# Ajustes finales

## Ajuste A — Layout colapsado

Fecha: 23 de octubre de 2026 (America/Guatemala).

Alcance original exclusivo: frontend/layout. El propietario aprobó A antes de autorizar el Ajuste B documentado más abajo.

### Diagnóstico

`LayoutAutenticado` ya iniciaba con el sidebar compacto, pero eso solo controlaba su anchura. Cada `GrupoSidebar` inicializaba `abierto` con `true`. Además, la rama compacta renderizaba todos los enlaces secundarios sin consultar ese estado. Por tanto, colapsar el sidebar no equivalía a cerrar sus grupos.

No existía apertura por ruta activa ni persistencia de los grupos en `localStorage` o `sessionStorage`.

### Comportamiento nuevo

- Todos los grupos autorizados comienzan cerrados, tanto en modo compacto como ancho y en el drawer móvil, incluso al cargar directamente una ruta secundaria.
- Cada grupo utiliza un botón explícito. Clic, Enter o Espacio abre/cierra únicamente ese grupo. El botón mantiene `aria-expanded`, nombre accesible y foco visible; en modo compacto tiene también una etiqueta emergente nativa (`title`).
- Los enlaces secundarios solo se montan cuando el usuario abre su grupo. En modo compacto conservan sus iconos y nombres accesibles existentes.
- Dashboard continúa como enlace directo. No se alteran destinos, orden alfabético ni filtrado por permisos.
- Cambiar la anchura, navegar o cerrar/reabrir el drawer conserva la elección manual mientras el layout siga montado. Un nuevo montaje comienza nuevamente con todos los grupos cerrados. No se incorpora persistencia.
- La selección de un enlace sigue cerrando el drawer móvil.

### Archivos afectados

- `heladeria-web/src/layouts/LayoutAutenticado.tsx`: estado inicial cerrado y representación común de grupos para ambos anchos.
- `heladeria-web/src/layouts/LayoutAutenticado.test.tsx`: 19 pruebas nuevas.
- `heladeria-web/src/rutas/EnrutadorAplicacion.test.tsx`: apertura explícita antes de comprobar el listado completo y su orden; conserva todas las expectativas previas.
- `heladeria-web/src/features/reportes/Reportes.test.tsx`: apertura de Reportes/Auditoría antes de comprobar sus accesos.
- `heladeria-web/src/features/cartera/Cartera.test.tsx`: apertura de Cartera antes de comprobar que solo aparece la cuenta autorizada.
- `heladeria-web/scripts/verificar-visual-reportes.mjs`: mismo smoke aislado de Visual 8, con interacción explícita de apertura y comprobaciones adicionales del sidebar. No fuerza estado React ni modifica el DOM para hacer pasar pruebas. Las capturas esperan a que terminen las transiciones CSS reales.
- `docs/AJUSTES_FINALES.md`: esta evidencia.

### Pruebas

Las 19 pruebas nuevas cubren montaje en 11 rutas, apertura/cierre en ambos anchos, independencia entre grupos, navegación, resaltado del enlace activo, nuevo montaje, drawer, ADMIN con permisos, CONSULTA sin permisos y permisos individuales dentro de un grupo. Antes de corregir el componente, 18 fallaron y una pasó: reproducían el comportamiento anterior.

Las pruebas existentes de rutas, Reportes y Cartera solo incorporan la interacción ahora requerida. No se eliminan ni relajan comprobaciones de permisos, destinos ni orden.

La revisión en navegador mantiene las 36 combinaciones de Visual 8 (9 vistas × 4 anchos), e incorpora 4 verificaciones del sidebar y 8 capturas adicionales. Comprueba estado inicial, clic y segundo clic, Enter/Espacio, foco visible, apertura independiente, navegación y cierre del drawer. Autenticación y API completamente simuladas; no utiliza sesiones ni datos reales.

### Validación

- `npm run lint`: salida 0, sin errores ni advertencias.
- `npm run build`: salida 0; TypeScript y Vite 8.2.4 correctos, 1.942 módulos transformados; build Vite final de 1,01 s.
- Suite dirigida de Layout/rutas/Reportes: 122 aprobadas, 0 fallidas.
- `npm test` completo: salida 0; **540 aprobadas, 0 fallidas, 0 omitidas**, 28 archivos aprobados, duración 258,42 s. Se conservan las 521 pruebas anteriores y se agregan 19.
- Smoke Visual 8 reutilizado: salida 0; 36 escenarios existentes + 4 verificaciones responsive del sidebar, 44 capturas, 0 errores. Anchos: 320, 390, 768 y 1366 px. Sin overflow horizontal de página/sidebar, foco de teclado visible, Enter/Espacio operativos, navegación y restricciones de CONSULTA conservadas. Capturas revisadas visualmente en `heladeria-web/test-results/visual-reportes/`; resultado estructurado en `resultado.json` dentro de esa carpeta ignorada.
- El preview local temporal se cerró al terminar. No se dejó un servidor adicional ejecutándose.

Durante la validación se corrigieron únicamente expectativas de interacción de pruebas: el primer recorrido completo registró 539 aprobadas y una falla en la prueba de navegación de Cartera que todavía no abría su grupo. Se añadió ese clic sin cambiar sus aserciones de permisos. También se retiró una opción `exact` no admitida por los tipos de Testing Library en las pruebas nuevas (el nombre de rol con cadena ya compara exactamente); no se modificó configuración ni se silenciaron errores.

Una comparación SHA-256 de los 542 archivos fuente/configuración/documentación iniciales detectó únicamente los cinco archivos existentes descritos arriba, más los dos nuevos (prueba del layout y este documento), sin eliminaciones. `Heladeria.sql` conserva SHA-256 `BEA99F04F971FA394B04A86C02A6CFAAE2AA3C9D692B5B12ED1A966A0C2ABB0A`.

### Límites

Sin cambios en backend, SQL, contratos API, autenticación, permisos, rutas o reglas financieras. No se ejecuta bootstrap, no se consulta la BD real y no se regeneran los artefactos de entrega. Solo se genera `heladeria-web/dist` para comprobar el build y ejecutar el preview local aislado.

Checklist del Ajuste A: iniciar sesión con ADMIN y CONSULTA; comprobar grupos cerrados al entrar, abrir/cerrar uno y navegar; repetir en móvil y teclado. El propietario confirmó posteriormente la aprobación de A, antes de autorizar B. No se declara terminado el conjunto de ajustes finales.

## Ajuste B — Caja y flujo financiero

Fecha: 23 de octubre de 2026 (America/Guatemala). Ajuste A aprobado por el propietario antes de iniciar B.

Estado: implementado, pendiente de validación manual del propietario. No constituye aprobación del Ajuste B, del cierre del proyecto ni autorización para C/D.

### Diagnóstico previo a los cambios

La acción «Cobrar venta» de Caja no era una aprobación vacía: capturaba el turno, las formas de pago, el importe aplicado, el recibido, el vuelto y las referencias. Ventas completaba primero el inventario; posteriormente, otra operación de Caja creaba `Pagos`, `PagoDetalles` y `MovimientosCaja`. Una venta de contado completada podía quedar legítimamente pendiente de ese cobro, incluso sin turno abierto.

Por tanto, eliminar solamente el botón habría perdido el lugar donde se informaban los pagos reales. Tampoco era correcto asumir efectivo automáticamente. El ajuste mueve esa captura al cierre de la venta y reutiliza el cobro existente dentro de la misma transacción. No se elimina el historial ni se presume que ventas anteriores fueron cobradas.

Antes y después: BORRADOR y CONFIRMADA no producen ingresos ni descuentan inventario. La venta a crédito completada genera CxC, no un ingreso anticipado. Los abonos CxC y los pagos CxP conservan sus operaciones y su tratamiento financiero existentes.

### B1 — Saldo inicial heredado

- Consulta reutilizada: `GET /api/caja/referencias?cajaId=...`; se añade el dato opcional `saldoUltimoCierre`, sin endpoint ni tabla nuevos.
- Fuente exacta: `TurnosCaja.MontoFisico`, equivalente al `efectivoContadoCierre` del detalle de sesión. No se utiliza saldo teórico, ingresos ni efectivo esperado como sustituto.
- El backend limita la caja por empresa y sucursal autorizadas. Selecciona el último turno con fecha de cierre de esa caja, ordenando por fecha de cierre e Id descendentes; ignora turnos abiertos.
- Valida flujo CAJA_SESION, estado CERRADA, moneda, número/usuario de cierre, cronología, montos válidos y correspondencia con el arqueo de cierre de ese turno. Si el último cerrado es incompatible, devuelve null: no retrocede silenciosamente a uno anterior.
- El cero es válido. Contado superior o inferior al esperado se hereda por su valor físico real.
- La UI muestra «Saldo del último cierre», importe, documento y fecha; precarga el saldo pero permite corregirlo. Sin cierre válido mantiene la entrada manual, inicialmente en cero.
- El backend sigue validando el importe enviado. La bitácora CAJA_ABIERTA registra el saldo realmente abierto y su moneda, no una cantidad calculada por el navegador.
- Cambiar de caja cancela/descarta la consulta anterior y reinicia el formulario con los datos de la caja elegida. Una respuesta tardía no sustituye el saldo ni una corrección manual.

### B2 — Venta, pago y Caja en una operación

Flujo de contado con total positivo:

1. Crear borrador y confirmar, como antes.
2. En «Completar venta», elegir sesión abierta y formas de pago reales. No se selecciona efectivo de forma presunta.
3. Un único `POST /api/ventas/{id}/completar` contiene la versión de venta, UUID y `cobro`: `sesionCajaId`, `versionCaja`, `pagos[]` con los campos ya existentes de cada forma.
4. Backend valida estado, alcance, versiones, configuración, stock y datos de cobro. Descuenta inventario, completa la venta y registra pago/detalles/movimientos y bitácora.
5. Todos los cambios se confirman juntos. No hay otro POST de cobro desde POS ni aprobación posterior en Caja.

`ServicioCaja.CobrarEnTransaccionAsync` contiene el mismo núcleo de cobro que reutiliza el endpoint histórico. Exige una transacción activa en proveedores relacionales y no abre otra, no limpia el tracker y no vuelve a adquirir el bloqueo. El servicio de Ventas conserva su transacción Serializable, coordinador empresarial y bloqueo de aplicación SQL; comparte DbContext, reloj y coordinador con Caja. Los SaveChanges intermedios no son commits: son necesarios para IDs y rowversion. El único commit es el de la operación exterior.

Se conserva:

- Efectivo: movimiento por importe aplicado/retendido; el vuelto no se suma al ingreso.
- Tarjeta y otros medios admitidos: forman parte del saldo teórico, no aumentan efectivo esperado.
- Mixto: un pago con detalles y movimientos por cada forma, sin duplicar el total.
- Crédito: solo CxC al completar. No admite `cobro` de contado; los abonos continúan por su flujo aprobado.
- Total cero de contado, permitido por el descuento del 100 % existente: completa inventario sin pago, movimiento financiero ni turno requerido. Se rechaza adjuntar un pago ficticio. No cambia la regla existente que rechaza crédito sin importe.
- Egresos: siguen siendo los conceptos ya existentes (manual/CxP). No se inventan reembolsos.
- Una venta ya cobrada no se anula desde POS: la protección backend ya existía y la UI ahora evita ofrecer esa operación. Las anulaciones permitidas y la reversión de inventario de históricos sin cobro siguen cubiertas.

Sin sesión válida, con versión de Caja obsoleta o con un cobro inválido, no se confirma una venta financiera a medias. La venta permanece CONFIRMADA; stock, costos, pagos, movimientos y correlativos se conservan. La bitácora de rechazo puede registrarse separadamente conforme al mecanismo previo.

### Idempotencia, concurrencia y permisos

- La misma UUID se usa en venta y pago. Un reintento exacto devuelve lo registrado sin repetir inventario ni movimientos, incluso después de cerrar la sesión.
- Misma UUID con otras formas/importes/recibidos/referencias/sesión se rechaza; UUID diferente sobre una venta completada también se rechaza.
- Se reutilizan los índices únicos de pago por venta, UUID y movimiento por detalle de pago. No se introduce una segunda huella financiera ni se reutiliza incorrectamente la huella reservada a Cartera.
- Las versiones de venta y Caja siguen protegiendo operaciones nuevas. La idempotencia exacta se resuelve antes de rechazar versiones antiguas de una operación ya aplicada.
- Frontend bloquea doble envío, conserva únicamente la UUID técnica en sessionStorage y consulta el resultado ante incertidumbre; nunca hace un segundo POST automático. La conciliación compara el cobro persistido, no solamente el estado COMPLETADA.
- Completar con cobro exige VENTAS.VENDER + POS.OPERAR y los permisos existentes CAJA.OPERAR + CAJA.COBRAR. Las consultas visuales usan CAJA.CONSULTAR. No se crean permisos ni se amplían roles. Crédito y contado de importe cero no requieren un cobro ni permisos adicionales de Caja.
- No se guardan tarjetas completas, CVV ni tokens; se mantiene la validación existente de referencias seguras.

### B3/B4 — Interfaz operativa

Caja prioriza estado, caja, sucursal, número de apertura, efectivo esperado y saldo teórico. Si no hay turno, destaca Abrir caja. Con turno operativo muestra Registrar ingreso, Registrar egreso, Arqueo y Cerrar caja según permisos; el cierre mantiene su requisito de arqueo vigente.

El resumen conserva saldo inicial, ingresos operativos y egresos. Apertura, ingresos y egresos se distinguen con insignias del sistema visual y texto; apertura nunca se etiqueta como ingreso operativo. Se conservan fecha, documento, forma, referencia, usuario, importe y vínculo de venta en los movimientos, así como contado, diferencia y documento de cierre.

«Cobrar venta» deja de ser una acción principal. Se conserva dentro de «Cobros anteriores pendientes», inicialmente cerrada, como «Cobrar venta anterior». Es necesaria para ventas completadas antes del ajuste cuyo medio de pago todavía no fue capturado; no aprueba ventas nuevas ni genera cobros inventados. El historial permanece íntegro.

Los campos de pago se comparten entre POS y el cobro histórico, sin duplicar validación ni agregar librerías. Botones principales de al menos 48 px, distribución adaptable, foco y navegación por teclado; tablas/listados se adaptan mediante los componentes existentes.

### Compatibilidad y límites

- Sin cambios SQL, EF Database First, estructura, preparación ni bootstrap. Se reutilizan columnas y constraints existentes. No se consulta ni escribe la BD real.
- La ampliación de referencias/detalle es aditiva. El cierre de contado positivo ahora exige `cobro`: un cliente anterior que solo enviaba UUID/versión recibe COBRO_REQUERIDO y no completa. API y frontend deben actualizarse juntos. No se intenta mantener el comportamiento inseguro mediante efectivo o turno inferidos.
- Este ajuste requiere la configuración Fase 8 ya aprobada y permisos de cobro existentes para el operador. No prepara datos automáticamente.
- El saldo anterior es una sugerencia consultada, no una reserva de efectivo: ante un cambio concurrente el backend conserva validaciones de apertura única, alcance e importe real. No se fuerza a usar un cierre obsoleto.
- No se implementan tickets, reportes nuevos, productos compuestos, recetas, empaques, devolución financiera ni Ajustes C/D.
- No se regeneran `artifacts/api`, `artifacts/web`, base de datos ni bootstrap. Solo se genera `heladeria-web/dist` para validar el build y el preview aislado.

### Pruebas y clasificación de cambios de regresión

28 casos backend nuevos en `AjusteFinalBPruebas`:

- Cierre físico mayor/menor/igual al esperado, cero, corrección manual auditada, sin cierre, turno abierto ignorado, última caja correcta, otra caja/sucursal/empresa, históricos incompatibles.
- Contado efectivo/tarjeta/mixto; CxC sin turno ni ingreso; importe cero e intento de pago ficticio.
- Cobro ausente, sesión cerrada, rowversion obsoleta, suma incorrecta.
- Reintento exacto incluso tras cerrar Caja, payload distinto, UUID diferente, dos solicitudes con UUID igual/distinta.
- Fallos tardíos de bitácora mediante triggers en SQLite aislado: rollback completo y reintento seguro.
- HTTP 403 al faltar CAJA.OPERAR o CAJA.COBRAR.

20 casos frontend nuevos en `AjusteFinalB.test.tsx`: sugerencia 100/98/0, corrección, falta de histórico, respuesta tardía de otra caja; efectivo/tarjeta/mixto en un solo POST; selección vacía; sin turno; crédito; total cero; permisos; conciliación de pago distinto; cuatro acciones principales, sección histórica secundaria, egreso preseleccionado, estado cerrado, apertura por permisos y distinción visual de tipos.

No se eliminan pruebas anteriores. Las adaptaciones de Fase 7/8 suministran el cobro real al nuevo cierre y mantienen fixtures explícitas de ventas históricas sin cobro para probar las reglas anteriores aplicables. No se fabrican históricos eliminando pagos de una operación nueva. Las expectativas de anular una venta recién completada se actualizan porque ahora está cobrada; la protección contra anular pagos no cambia.

La primera suite backend completa detectó dos expectativas legítimamente obsoletas en Reportes: una venta nueva de contado ya no es PENDIENTE sino COBRADA; el fixture SQL de cobro mixto debe completar con el pago, no intentar cobrar otra vez. Solo se actualizaron esas expectativas/preparaciones. Los asserts de importes, Kardex, inventario y costos se conservaron. El test SQL mixto ejecuta la integración real en una base LocalDB temporal aislada.

Las pruebas existentes de Visual 5/6 se adaptaron a permisos, captura integrada y acceso secundario de históricos, conservando verificaciones de importes, UUID, stock, incertidumbre y versiones. Los scripts visuales también abren explícitamente los grupos del sidebar, conforme al Ajuste A aprobado.

### Validación automática final

- `dotnet restore`: correcto, proyectos actualizados.
- `dotnet build --configuration Release`: correcto, **0 errores / 0 advertencias**, 11,66 s.
- `dotnet test --configuration Release`: **650 aprobadas / 0 fallidas / 0 omitidas** (622 previas + 28 nuevas), 3 min 6 s. Evidencia: `Heladeria.Pruebas/TestResults/ajuste-b-final.trx`. El aviso de sobrescritura corresponde solo a ese informe de pruebas local.
- `npm run lint`: correcto, sin errores ni advertencias.
- Suite frontend final completa: **560 aprobadas / 0 fallidas / 0 omitidas**, 29 archivos (540 previas + 20 nuevas), 91,05 s. Evidencia: `heladeria-web/test-results/ajuste-b-completa.json`.
- `npm run build`: TypeScript + Vite 8.2.4 correctos; 1.945 módulos, build Vite de 531 ms.
- Un recorrido frontend aprobó las 560 pruebas pero emitió un aviso de timeout al terminar un worker de ComponentesVisuales. La repetición aislada de ese archivo aprobó sus 7 casos en 2,02 s, sin aviso. Se repitió además TODA la suite: 560 aprobadas, sin reproducir el aviso del worker. No se modificó la prueba ni la configuración para ocultarlo.
- Smoke Caja: 85 combinaciones/capturas, 0 errores.
- Smoke Ventas: 45 combinaciones/capturas, 0 errores. La revisión ampliada comprueba también entrada con Enter y retención de foco con Tab/Shift+Tab en el cobro integrado.
- Anchos: **320, 390, 768, 1366 y 1600 px**. Sin overflow horizontal de página; modales con scroll interno, acciones accesibles y foco dentro del diálogo. API/autenticación completamente simuladas, navegador en Asia/Tokyo y sucursal America/Guatemala. No usan credenciales ni datos reales.
- Capturas y resultados: `heladeria-web/test-results/visual-caja/` y `heladeria-web/test-results/visual-ventas/`. Revisión visual de apertura móvil, resumen escritorio, detalle/movimientos y cobro integrado.
- El preview temporal propio del puerto 5193 se cerró al terminar; se verificó que no quedó escuchando. No se detuvieron procesos del usuario.

### Inventario de archivos

Creados:

- `Heladeria.Pruebas/Fase8/AjusteFinalBPruebas.cs`
- `heladeria-web/src/features/ventas/CobroAlCompletar.tsx`
- `heladeria-web/src/features/caja/AjusteFinalB.test.tsx`
- `heladeria-web/src/features/caja/CamposPago.tsx`
- `heladeria-web/src/features/caja/pagosFormulario.ts`

Modificados:

- `Heladeria.Pruebas/Fase10/ReportesSqlPruebas.cs`
- `Heladeria.Pruebas/Fase10/ReportesServicioPruebas.cs`
- `Heladeria.Pruebas/Fase8/EscenarioCaja.cs`
- `Heladeria.Pruebas/Fase8/CajaPruebas.cs`
- `Heladeria.Pruebas/Fase7/VentasRollbackPruebas.cs`
- `Heladeria.Pruebas/Fase7/VentasPruebas.cs`
- `Heladeria.Pruebas/Fase7/VentasHttpPruebas.cs`
- `Heladeria.Pruebas/Fase7/EscenarioFase7.cs`
- `Heladeria.Api/Controllers/CajaController.cs`
- `Heladeria.Api/Controllers/VentasController.cs`
- `Heladeria.Infraestructura/Caja/ServicioCaja.Operaciones.cs`
- `Heladeria.Infraestructura/Caja/ServicioCaja.Consultas.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentasBase.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Operaciones.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Consultas.cs`
- `Heladeria.Aplicacion/Ventas/ContratosVentas.cs`
- `Heladeria.Aplicacion/Caja/ContratosCaja.cs`
- `heladeria-web/scripts/verificar-visual-ventas.mjs`
- `heladeria-web/scripts/verificar-visual-caja.mjs`
- `heladeria-web/src/api/ventas.ts`
- `heladeria-web/src/features/ventas/Ventas.test.tsx`
- `heladeria-web/src/features/ventas/useDatosVentas.ts`
- `heladeria-web/src/features/ventas/PaginaDetalleVenta.tsx`
- `heladeria-web/src/features/caja/PaginaSesionCaja.tsx`
- `heladeria-web/src/features/caja/PaginaCaja.tsx`
- `heladeria-web/src/features/caja/PaginaAbrirCaja.tsx`
- `heladeria-web/src/features/caja/FormulariosSesionCaja.tsx`
- `heladeria-web/src/features/caja/ComponentesCaja.tsx`
- `heladeria-web/src/features/caja/CobrarVentaCaja.tsx`
- `heladeria-web/src/features/caja/Caja.test.tsx`
- `heladeria-web/src/features/caja/caja.css`
- `heladeria-web/src/types/ventas.ts`
- `heladeria-web/src/types/caja.ts`
- `docs/AJUSTES_FINALES.md`

Sin archivos eliminados. No se modificaron paquetes npm, scripts SQL, entidades EF, preparación/Bootstrap, reglas CxC/CxP, reportes de producción ni artefactos publicados. La comparación SHA-256 contra los 536 archivos de la captura inicial confirma el alcance; los informes/capturas/builds locales son salidas de validación. `BaseDatos/Heladeria.sql` conserva SHA-256 `BEA99F04F971FA394B04A86C02A6CFAAE2AA3C9D692B5B12ED1A966A0C2ABB0A`.

### Checklist de validación manual del propietario

1. Reiniciar API y frontend con este código; no ejecutar bootstrap ni una reparación SQL.
2. Cerrar una caja con esperado 98 / contado 100. Reabrir la misma: sugerencia 100, origen visible; corregir manualmente y comprobar importe real/bitácora. Repetir contado cero, primera apertura y otra caja/sucursal.
3. Confirmar una venta de contado y completarla en efectivo, tarjeta y mixto. Verificar un único pago y sus movimientos en Caja, con importe retenido y vuelto correctos. Tarjeta no aumenta efectivo esperado.
4. Repetir UUID exacta/doble clic: no debe duplicarse stock ni pago. Intento con monto o forma diferentes bajo la misma UUID debe rechazarse.
5. Sin turno válido o con versión obsoleta: venta CONFIRMADA y sin efectos parciales. Recargar, corregir la causa y reintentar con la misma UUID.
6. Completar crédito: CxC, sin cobro anticipado; registrar abono por Cartera y comprobar el movimiento habitual. Probar contado con descuento total: inventario sí, pago ficticio no.
7. Comprobar históricos, ingreso/egreso, arqueo y cierre; restricciones de permisos, anulación cobrada bloqueada y acceso secundario para un cobro antiguo realmente pendiente.
8. Revisar en 320/390/768/escritorio: botones, scroll, Tab/Shift+Tab/Enter/Escape, detalles y origen del saldo.

El propietario aprobó posteriormente el Ajuste B y autorizó el Ajuste C descrito a continuación. No se declara finalizado el proyecto.

## Ajuste C — Comprobantes y reporte diario

Fecha: 27 de octubre de 2026 (America/Guatemala). El propietario aprobó Backend 1–10, Visuales 1–8 y Ajustes A/B antes de iniciar C. No se implementa D.

### Diagnóstico previo

- Venta y sus detalles ya guardan cantidades, precios, descuentos, impuestos y costos históricos. `GET /api/ventas/{id}` conserva la fuente de verdad. Faltaban datos comerciales para el encabezado, código de cliente, usuario de finalización, nombres de formas y recibido/vuelto.
- Clientes es una entidad directa; no interviene una entidad Persona en esta consulta. Empresa y Sucursal tienen razón social/nombre comercial, dirección y teléfono; se usan únicamente los datos existentes.
- Nombres de producto/presentación, empresa y cliente no tienen snapshot comercial histórico. El ticket advierte que esos nombres/datos son actuales; los importes son los guardados. No se inventan datos fiscales ni recibido/vuelto para pagos históricos incompletos.
- Los reportes Fase 10 tienen semánticas distintas: ventas por creación, caja por apertura de turno y cartera por emisión. Unir sus respuestas como si fueran el mismo día produciría un informe incoherente. Se reutiliza el cálculo existente de costos/margen y la clasificación de movimientos; se agrega una única consulta de lectura para el diario.

### Comprobante

Ruta `/ventas/:id/comprobante`, fuera del layout con sidebar y dentro de sesión/permisos. Reutiliza `GET /api/ventas/{id}`; se añade `comprobante` al DTO sin cambiar los campos existentes ni crear tabla/endpoint de tickets.

Se habilita con fecha de finalización, flujo VENTA_POS, estado COMPLETADA o ANULADA y detalle histórico completo. BORRADOR, CONFIRMADA e históricos sin importes de línea suficientes no producen comprobante definitivo. El acceso desde el detalle compartido también sirve para el resultado del POS.

Incluye empresa/razón social, sucursal/dirección/teléfono, documento, fecha de finalización en zona efectiva, usuario finalizador (creador si no está registrado), moneda, cliente/código o «Venta sin cliente identificado», cantidades, presentaciones, precios capturados, descuentos, totales de línea, subtotal, impuesto y total persistidos.

- Contado: formas, aplicado, recibido y vuelto real de `Pagos/PagoDetalles`. Recibido desconocido se muestra como «No registrado». Sin pago histórico se indica explícitamente; total cero no crea pago ficticio.
- Crédito: modalidad, total e importe original de CxC como saldo inicial, incluso después de abonos. No se presenta ese saldo como saldo pendiente actual ni se inventa pago al emitir.
- Anulada tras realización: encabezado ANULADA, fecha/motivo y valores originales. No se altera el histórico.

HTML blanco de 80 mm aproximados, importes alineados y texto legible en móvil. Leyenda obligatoria: **Comprobante interno — no constituye documento tributario**. `window.print()` solo ante acción; diálogo estándar y Guardar como PDF del navegador. El papel de 80 mm se selecciona en el diálogo/controlador; CSS conserva 72 mm de contenido impreso y márgenes. Sin hardware, drivers ni dependencia PDF nueva.

### Reporte diario y contrato

Ruta `/reportes/diario`, opción en el grupo Reportes. Nuevo `GET /api/reportes/diario` con binding normal `[FromQuery] FiltroDiario`.

Parámetros: `fecha` (DateOnly), `sucursalId`, `cajaId`, `formaPagoId`, `esCredito`, `pagina`, `paginaOperaciones`, `paginaCajas`, `tamanioPagina` (1–100; 20 por defecto). El `busqueda` heredado, si se usa por API, limita únicamente las ventas por número/cliente; la UI no lo ofrece como filtro global. Tres páginas independientes evitan colecciones de detalle ilimitadas; los resúmenes no dependen de la página. Las agrupaciones mantienen el límite defensivo de los reportes existentes.

El backend calcula el día local por `TimeProvider` y zona efectiva empresa/sucursal de `TiempoEmpresarial`, incluida America/Guatemala. Rango UTC semiabierto: medianoche local inclusiva hasta la siguiente medianoche exclusiva. Sin restas fijas ni asumir días siempre de 24 horas. Se devuelve el período y la hora de generación. La UI deja que el servidor elija inicialmente el día y lo fija al paginar para no cruzarlo silenciosamente a medianoche.

Filtros editables y aplicados separados; solo Aplicar/Limpiar/paginar consultan. Caja/forma son filtros opcionales por ID, coherentes con los filtros de reportes existentes y sin exigir permisos adicionales de catálogos. IDs fuera de empresa/sucursal se rechazan. La elección de otra sucursal autorizada no cambia la sesión; para abrir ventas de ella se informa que debe elegirse como sucursal activa, respetando el alcance del detalle de Ventas.

### Semántica y prevención de doble conteo

| Dimensión | Fuente / fecha | Interpretación |
|---|---|---|
| Ventas realizadas | Ventas/VentasDetalle, FechaCompletada | Estado actual COMPLETADA; total, descuentos, costo y margen según Fase 10. No fecha de creación del borrador. |
| Anulaciones | Ventas, FechaAnulacion | Estado actual ANULADA; separado de ventas completadas. Sin inventar costo/margen ni restarlo dos veces. |
| Cobros por forma | Pagos/PagoDetalles, Fecha del pago | Aplicado por forma/moneda, incluye abonos CxC aunque la venta sea de otro día. No dinero entregado antes del vuelto. |
| Movimientos del día | MovimientosCaja, Fecha | Apertura separada; ingresos, egresos, cobros ventas, cobros CxC y pagos CxP. Estos últimos son desgloses, no ingresos/egresos adicionales. |
| Crédito emitido | CuentasPorCobrar, FechaEmision | Cantidad e importe ORIGINAL por moneda, no saldo actual. Excluye anuladas. |
| Turnos | TurnosCaja que cruzan el día | Saldo inicial y movimientos completos del turno anteriores al corte; efectivo esperado, contado y diferencia si hubo cierre. No se limita a turnos abiertos ese día. |

No hay total que sume ventas + cobros + caja. Monedas separadas; no conversión ficticia. Costos/margen desconocidos permanecen nulos. Movimientos históricos sin clasificación completa se señalan; no se inventa conciliación.

Caja/forma atribuyen ventas mediante un pago directo asociado: el crédito sin cobro no tiene esa atribución y no aparece al aplicar tales filtros. Forma limita también el detalle de pago y movimientos a esa forma. Modalidad filtra movimientos a COBRO_VENTA (contado) o COBRO_CUENTA_POR_COBRAR (crédito); aperturas, manuales y pagos CxP no tienen esa modalidad. Los saldos de turnos siempre conservan **todas** sus operaciones/formas, aunque se filtre el detalle financiero; la UI lo explica. Caja/sucursal sí limitan los turnos.

El reporte es una consulta operativa del estado actual, no una reconstrucción contable «tal como se conocía en el pasado»: una venta anulada posteriormente figura como anulada en el día de anulación. No se sustituyen reportes analíticos existentes.

### Consistencia, seguridad y ausencia de escrituras

- Lectura de comprobante y diario bajo transacción SERIALIZABLE en proveedores relacionales, ejecutada con la estrategia de reintentos EF. Si hay transacción exterior (detalle devuelto al completar venta), se reutiliza. No requiere habilitar SNAPSHOT en la BD.
- El bloqueo de lectura dura la consulta; puede añadir contención frente a escrituras concurrentes. Consultas paginadas y SQL agregado; no se materializan miles de detalles para calcular en frontend.
- Comprobante: sesión + VENTAS.CONSULTAR y alcance empresa/sucursal del endpoint existente. Diario: REPORTES.CONSULTAR y asignación activa a la sucursal solicitada. Sin permisos nuevos ni ampliación de CONSULTA.
- 401/403, validación 400, IDs y `Cache-Control: no-store` conservados. No devolver contenido de bitácoras ni secretos.
- Consultar/imprimir no llama SaveChanges, no cambia rowversion, existencias, Kardex, pagos, caja, correlativos ni estado; no agrega bitácoras de impresión.
- Ningún SQL, esquema, entidad EF, paquete npm, bootstrap ni script de publicación modificados. No SQL/bootstrap contra BD real. Integración SQL solo en bases LocalDB aisladas creadas/eliminadas por las fixtures existentes.

### Impresión del diario y responsive

Impresión A4 horizontal con fecha, sucursal, generación, filtros aplicados, resúmenes y tablas; oculta sidebar, navegación, botones y filtros editables. Incluye únicamente las páginas visibles de ventas/operaciones/turnos, con número de página y total de registros expresos. No afirma exportar todos los detalles.

Tarjetas apiladas en móvil, tablas con scroll interno por teclado y sin overflow de página. Script `heladeria-web/scripts/verificar-ajuste-c.mjs`: APIs/autenticación interceptadas, navegador con zona Asia/Tokyo para comprobar que se muestra Guatemala, tickets contado/mixto, crédito y anulado, diario, impresión y anchos 320/390/768/1366/1600. Sin conexiones a la API real. Capturas, PDF de prueba e informe en `heladeria-web/test-results/ajuste-c-visual/`.

### Pruebas y ajustes de expectativas

24 casos backend nuevos: efectivo/tarjeta/mixto con vuelto, cliente nullable, histórico ante cambio de precio de catálogo, crédito con abono conservando importe original, anulada, borrador, sucursal ajena, día vacío/default local, límites exactos de finalización, paginación/orden/totales, estados separados, zonas con DST de 23/25 horas, 401/403/no-store, entradas 400 y binding HTTP real/defaults. Se amplían además las tres integraciones SQL existentes para diario, filtros combinados, crédito emitido, CxC/CxP, saldos y ausencia de escrituras.

21 casos frontend nuevos: render histórico, crédito, anulada, restricciones, impresión explícita, ruta limpia, acceso desde detalle/POS, día del servidor, URL real de filtros, limpiar, paginar con filtros aplicados, vacío, 400/401/403/500 y permisos.

La primera suite completa detectó tres expectativas antiguas de Fase 8: comparar todo el DTO antes/después de cobrar ya no era válido porque ahora expone pagos del comprobante. Se valida explícitamente aplicado/vuelto nuevo y se conserva la comparación de todos los demás datos, además de la huella de inventario original. La prueba de menú agrega exclusivamente la nueva opción Reporte diario, manteniendo orden y demás enlaces. Una aserción SQL nueva asumía emisión actual, pero la fixture existente registra FechaEmision = 09/10/2026: ahora consulta esa fecha guardada y comprueba que no se atribuye al día actual de cierre/cobro. No se eliminan pruebas ni se ocultan regresiones.

Resultados finales de ejecución:

- `dotnet restore`: correcto, proyectos actualizados.
- `dotnet build --configuration Release`: correcto, **0 errores / 0 advertencias**.
- `dotnet test --configuration Release`: **674 aprobadas / 0 fallidas / 0 omitidas**, 2 min 46 s (650 previas + 24 nuevas). Evidencia: `Heladeria.Pruebas/TestResults/ajuste-c-final.trx`. El aviso del logger indica únicamente sobrescritura de ese informe local.
- `npm run lint`: correcto, sin errores.
- `npm test`: **581 aprobadas / 0 fallidas / 0 omitidas**, 30 archivos, 168,23 s (560 previas + 21 nuevas). Evidencia: `heladeria-web/test-results/ajuste-c-completa.json`.
- `npm run build`: TypeScript y Vite correctos. Sin dependencias nuevas.
- Validación visual final: **20 escenarios / 0 errores** (4 vistas × 5 anchos). Sin overflow de página, sin solicitudes a API real ni escrituras de negocio, impresión explícita y navegación oculta al imprimir. Evidencia: `heladeria-web/test-results/ajuste-c-visual/resultado.json`.
- Muestras de PDF del navegador: tickets de 80 × 297 mm (una página cada muestra) y diario A4 horizontal (tres páginas para la muestra). Son salidas de QA, no un nuevo generador PDF del producto. La impresora real y su configuración quedan para validación manual.
- `BaseDatos/Heladeria.sql` conserva SHA-256 `BEA99F04F971FA394B04A86C02A6CFAAE2AA3C9D692B5B12ED1A966A0C2ABB0A`. Sin cambios de BD real/bootstrap real ni nueva publicación de artifacts.
- Preview temporal propio del puerto 5193 cerrado y ausencia de listener verificada. No se detuvo el servidor del usuario del puerto 5173.

### Inventario de archivos de este ajuste

Creados:
- `heladeria-web/src/types/diario.ts`
- `heladeria-web/src/test/datosAjusteC.ts`
- `Heladeria.Infraestructura/Reportes/ServicioReportes.Diario.cs`
- `Heladeria.Infraestructura/Persistencia/LecturaConsistente.cs`
- `heladeria-web/src/api/diario.ts`
- `heladeria-web/scripts/verificar-ajuste-c.mjs`
- `heladeria-web/src/features/ventas/PaginaComprobanteVenta.tsx`
- `heladeria-web/src/features/ventas/comprobante.css`
- `heladeria-web/src/features/reportes/PaginaReporteDiario.tsx`
- `heladeria-web/src/features/reportes/diario.css`
- `heladeria-web/src/features/reportes/AjusteFinalC.test.tsx`
- `Heladeria.Pruebas/AjusteFinalC/DiarioPruebas.cs`
- `Heladeria.Pruebas/AjusteFinalC/ComprobantesPruebas.cs`
- `Heladeria.Aplicacion/Reportes/ContratosDiario.cs`

Modificados:
- `Heladeria.Api/Controllers/ReportesController.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Consultas.cs`
- `heladeria-web/src/types/ventas.ts`
- `Heladeria.Infraestructura/Reportes/ServicioReportes.Ventas.cs`
- `Heladeria.Pruebas/Fase8/CajaPruebas.cs`
- `heladeria-web/src/rutas/EnrutadorAplicacion.tsx`
- `heladeria-web/src/rutas/EnrutadorAplicacion.test.tsx`
- `heladeria-web/src/layouts/LayoutAutenticado.tsx`
- `heladeria-web/src/features/ventas/PaginaDetalleVenta.tsx`
- `Heladeria.Aplicacion/Ventas/ContratosVentas.cs`
- `Heladeria.Pruebas/Fase10/ReportesSqlPruebas.cs`
- `Heladeria.Pruebas/Fase10/ReportesOpenApiBindingPruebas.cs`
- `Heladeria.Aplicacion/Reportes/ContratosReportes.cs`
- `docs/AJUSTES_FINALES.md`

Sin archivos eliminados. La comparación SHA-256 se realiza contra la captura de 533 archivos anterior a C. Las salidas locales de compilación/tests/capturas no son una nueva publicación de `artifacts`.

### Checklist manual

1. Reiniciar API/frontend con la compilación actual. No ejecutar bootstrap ni SQL para C.
2. Abrir una venta completada de contado: Ver comprobante desde detalle y resultado POS. Verificar documento, empresa/sucursal, cliente nullable, cantidades, descuentos, precio capturado, pagos y vuelto; repetir efectivo/tarjeta/mixto.
3. Abrir una venta a crédito con cliente: saldo original CxC y ausencia de pago ficticio. Tras un abono, confirmar que saldo inicial no se reemplaza por pendiente.
4. Comprobar ANULADA en una venta histórica realizada y posteriormente anulada. BORRADOR/CONFIRMADA no tienen comprobante definitivo.
5. Imprimir a 80 mm o Guardar como PDF desde el diálogo: contenido legible, sin controles/sidebar, leyenda no tributaria y cantidades históricas.
6. Consultar Reporte diario vacío y con operaciones. Contrastar por separado ventas, cobros aplicados y movimientos; apertura no aumenta ingreso operativo, tarjeta no aumenta efectivo, CxC/CxP aparecen en sus dimensiones.
7. Aplicar fecha, sucursal autorizada, caja, forma y modalidad; verificar límites del día, páginas y Limpiar. Caja/forma y modalidad tienen el alcance documentado. Para abrir ventas de otra sucursal, cambiar la sucursal activa.
8. Revisar turno abierto en día anterior y cerrado durante el día, efectivo contado/diferencia, moneda y costos desconocidos. No sumar entre sí las tres dimensiones.
9. Imprimir reporte: fecha/sucursal/generación, resumen y páginas visibles señaladas, sin navegación. Revisar 320/390/768/1366/1600, teclado y scroll interno.
10. Probar usuario sin permiso y URL de venta ajena; comprobar 401/403/alcance. La mera consulta/impresión no genera pagos, movimientos ni bitácoras financieras.

Ajuste C y corrección C.1 aprobados según el estado oficial comunicado por el propietario al iniciar D.

## Ajuste D — Productos compuestos y consumo automático

Implementado el 27/10/2026; pendiente de validación manual del propietario. La aprobación comunicada de A, B, C y C.1 se conserva. Este ajuste no declara el cierre final ni publica un nuevo conjunto de artifacts.

### Diagnóstico y decisión de modelo

Antes de editar se revisaron Productos, ProductoPresentaciones, UnidadesMedida, CombosDetalle, Existencias, Movimientos/MovimientoDetalles, Kardex, Ventas/VentasDetalle, Recetas/RecetaDetalles y Producción, sus servicios, contratos, UI y pruebas.

- Ya existía **CombosDetalle**, con empresa, producto padre, producto incluido y Cantidad entera. No tenía presentación del componente ni una descarga integrada en POS. Es la estructura reutilizada.
- Productos ya permite combinar EsVendible con un tipo que maneja existencia; también tiene EsCombo. POS excluía todos los EsCombo.
- La producción existente transforma insumos y registra rendimiento/consumo en Recetas, RecetaDetalles y órdenes de producción. **No se modifica esa semántica ni se usan esas recetas para vender.**
- La composición de venta consume productos inventariables al completar una venta comercial. Los empaques no necesitan ser vendibles o comprables individualmente.
- Las existencias se conservan por empresa, producto, bodega y ubicación; ProductoPresentaciones convierte cantidad de presentación a unidad base. Se reutilizan la precisión de unidades y el costo promedio existente.
- Una venta simple conserva su descarga habitual. Una compuesta consume sus componentes, **sin descargar además stock propio del padre**, incluso si su tipo maneja existencia.
- La existencia de filas de composición determina el comportamiento compuesto. Se sincroniza el EsCombo ya existente; no se crea un nuevo tipo/enum de producto. Un EsCombo antiguo sin componentes sigue sin ofrecerse como vendible en POS hasta configurar o quitar expresamente esa marca mediante el formulario.

### SQL y Database First

No se crean tablas ni catálogos. Cambios mínimos:

| Tabla existente | Cambio |
|---|---|
| CombosDetalle | Cantidad pasa de int a decimal(19,6); ProductoPresentacionId bigint nullable; FK compuesta empresa/producto/presentación; unicidad de empresa/padre/componente/presentación; checks de cantidad positiva y no autorreferencia. |
| VentasDetalle | EsCompuesto bit nullable como **dato histórico de la línea**, no clasificación nueva del catálogo. NULL conserva el tratamiento histórico simple. |
| MovimientoDetalles | VentaDetalleId bigint nullable y UnidadBaseId int nullable, con FKs e índice de línea, para identificar la línea comercial y la unidad del consumo original. |

Script incremental: `BaseDatos/Reparaciones/20261027_AjusteD_ComposicionVenta.sql`. El mismo bloque se incorpora al final de `BaseDatos/Heladeria.sql`, separado por GO de los bloques anteriores.

El incremental usa una única transacción, XACT_ABORT y rollback en CATCH. Las sentencias que consumen columnas nuevas se compilan mediante SQL dinámico controlado después del ALTER; no se intercalan GO dentro de esa transacción. Detecta columnas, tipos/nullabilidad, índices, constraints y definiciones de FKs; permite preparación parcial compatible y repetición. Aborta con 51110 si encuentra estructura incompatible, datos duplicados, cantidades inválidas, autorreferencias o anidación histórica. No inventa conversiones ni limpia datos automáticamente.

La prueba sobre el esquema previo detectó el DEFAULT original de Cantidad como dependencia del ALTER. Se resolvió conservando su nombre real obtenido del catálogo y su valor 1, eliminándolo/recreándolo dentro de la misma transacción; una definición distinta aborta. No se asumen nombres autogenerados ni IDs.

EF se actualiza mediante partials y el tipo decimal de CombosDetalle.Cantidad. Una regeneración Database First futura debe revisar estas configuraciones parciales para no duplicar relaciones o perder la trazabilidad añadida.

**Despliegue pendiente:** el propietario debe respaldar y seleccionar explícitamente su BD, detener la API, ejecutar el incremental completo con su herramienta SQL habitual y después iniciar el backend nuevo. No ejecutar el maestro sobre una instalación existente ni bootstrap para este ajuste. El script rechaza una transacción externa para controlar su propio rollback. Mensaje de éxito:

```text
AJUSTE D: composición de venta y trazabilidad listas. Sin datos demo ni cambios financieros.
```

No se ejecutó incremental, maestro ni bootstrap contra la BD real. Los productos VACA-LOCA/HEL-VAI de las pruebas existen exclusivamente en fixtures aisladas.

### API, DTO y permisos

Se amplían los contratos existentes de Productos:

- POST `/api/productos` y PUT `/api/productos/{id}`: `componentes`, con productoId, productoPresentacionId nullable y cantidad numérica.
- GET `/api/productos/{id}`: componentes con nombre/SKU, presentación, factor, cantidad base y unidades.
- GET `/api/productos?soloComponentesVenta=true`: catálogo paginado y buscable de componentes terminales válidos de la empresa.
- Se reutiliza GET `/api/productos/{id}/presentaciones`; su DTO agrega la abreviatura de unidad para la equivalencia visual.

No hay endpoints nuevos. En un cliente anterior, componentes omitido/null **preserva** la composición existente; [] la elimina expresamente. Producto y composición se guardan atómicamente, con la auditoría de producto existente y su VersionFila.

Administración usa PRODUCTOS.ADMINISTRAR y consulta PRODUCTOS.CONSULTAR. Ventas, inventario, caja y CxC mantienen sus permisos y alcances; no se amplía CONSULTA ni se crean permisos. Los productos se configuran por empresa; el stock se valida en la sucursal/bodega/ubicación autorizadas de la venta.

### UI, conversiones y validaciones

El formulario existente de Producto incorpora **Composición de venta**: buscar producto/insumo, escoger unidad base o presentación, cantidad, equivalencia, agregar y eliminar. Se sustituye el switch independiente Combo por la composición efectiva. Mantiene estilos y controles existentes, guardado del formulario y consulta sin edición para quien no administra.

Texto: «El inventario de estos componentes se descontará automáticamente al completar la venta».

Las cantidades de composición corresponden a **una unidad base del producto comercial padre**. Fórmula:

```text
cantidad base del padre = cantidad vendida × factor de su presentación
consumo base = cantidad base del padre × cantidad del componente × factor de su presentación
```

Sin presentación, factor = 1 y la cantidad ya está en unidad base. Dos BOLA de factor 60 consumen 120 ml por unidad del padre; vender dos unidades consume 240 ml. Una presentación comercial del padre que contiene tres unidades multiplica también por tres.

Hasta 100 filas; cantidades positivas con hasta seis decimales y precisión admitida por cada unidad. Sin productos/presentaciones inactivos, empresa ajena, conversión inválida, autorreferencia o duplicado exacto producto/presentación. Se rechazan componentes compuestos y convertir en compuesto un producto que ya sirve como componente: evita ciclos directos e indirectos sin recursión. Se permite el mismo producto con presentaciones diferentes; la demanda se acumula por producto antes de validar stock.

### Venta, stock, atomicidad, concurrencia e idempotencia

Completar conserva validación de estado, RowVersion, UUID, alcance, referencias y flujo financiero existentes. Resuelve todas las líneas y componentes, acumula demanda compartida con productos simples, toma los bloqueos existentes y comprueba disponible = cantidad menos reservada antes de insertar salidas.

Una insuficiencia devuelve HTTP 409 / EXISTENCIA_INSUFICIENTE, identificando el componente. No completa la venta ni deja movimientos, cambios de saldo/costo, pagos, caja, CxC, correlativos o una UUID de cierre exitosa. Fallos posteriores en movimiento, pago o CxC revierten igualmente toda la transacción.

Se reutilizan SERIALIZABLE, el coordinador existente, sp_getapplock de inventario por empresa y bloqueos por producto en orden estable. La edición de composición usa el mismo bloqueo de empresa. No se agrega un sistema de reservas. Se conserva la idempotencia del cierre: misma operación retorna el resultado aplicado; otra UUID incompatible se rechaza.

La composición vigente se resuelve **al completar**, no se congela al crear un borrador. Editar cantidades afecta ventas aún no completadas; nunca ventas completadas. Si entre confirmación y cierre cambia el carácter simple/compuesto, se rechaza la configuración incompatible para que se actualice el borrador.

### Costos, Kardex, anulación, ticket y reportes

Cada componente genera VENTA-SALIDA mediante el servicio actual de inventario. Movimiento/Detalle/Kardex identifican producto consumido, cantidad de presentación/base, factor, costo unitario base, usuario y documento de venta; VentaDetalleId enlaza la línea del padre y UnidadBaseId conserva la unidad histórica.

Costo de línea = suma de costos reales de los componentes consumidos al costo promedio de esa ubicación. Se captura el total con seis decimales y se deriva el unitario del padre, sin recalcular después el total multiplicando un unitario ya redondeado. El precio de venta de componentes no interviene. El ejemplo de cinco componentes da Q7.65 por unidad, Q15.30 por dos.

La anulación permitida por las reglas actuales usa **los movimientos originales**, no la composición vigente. Devuelve exactamente las cantidades base y costos originales; verifica trazabilidad completa y unidad histórica antes de devolver. No cambia las restricciones financieras que impiden anular ventas con pagos/CxC. Las pruebas de reversión usan ventas sin importe exigible, y las de efectivo/tarjeta/crédito verifican cierre y rollback por separado.

El ticket y los reportes siguen mostrando solo el padre comercial. Costos/margen proceden de VentasDetalle y reflejan la suma consumida. No se editaron comprobantes, reportes ni su formato; se conserva C.1.

### Pruebas y resultados

36 casos backend nuevos:

- 15 de producto/composición: crear/editar/eliminar, omisión compatible, validaciones, terminales no vendibles, empresa, versiones, anidación/ciclos y rollback de alta/auditoría.
- 14 de ventas: uno/varios componentes, cantidades 1/2/3, presentación del padre/componente, stock exacto e insuficiente, demanda compartida, costo/Kardex/documento, fallos tardíos de movimiento/pago/CxC, efectivo/tarjeta/crédito, UUID y reversión histórica/doble anulación.
- 4 SQL Server: esquema previo/parcial, repetición, rollback ante incompatibilidad e índice/FK incorrectos; venta integral y concurrencia con DbContexts/coordinadores independientes tanto para ventas distintas como misma UUID.
- 3 HTTP: permisos de crear/editar y contrato real de alta/consulta/validación.

La integración LocalDB configura HEL-VAI (BOLA=60 ml), COCA-LATA, VASO-12, CUCHARA y SERVILLETA. Vender dos VACA-LOCA consume 240 ml y dos de cada otro insumo, cinco salidas, una línea comercial, costo Q15.30 y ninguna sobreventa concurrente. Tras cambiar la composición, anular devuelve los consumos originales. Reporte de productos y diario contrastan costo/margen sin vender componentes separados.

14 casos frontend nuevos: 13 de composición (alta, edición, búsqueda/presentación/equivalencia, payload, quitar, validaciones, conservación tras error y permisos) y uno de POS que muestra el componente insuficiente y mantiene la venta confirmada. La expectativa antigua de cinco switches comerciales pasa a cuatro porque Combo se deriva ahora de la sección; no se elimina la prueba.

Una aserción nueva de agregación de reportes se trasladó de SQLite a SQL Server porque SQLite no admite el SUM decimal generado por EF. No se modificaron reportes ni se sustituyó la validación por valores ficticios.

Resultados finales:

| Validación | Resultado |
|---|---|
| dotnet restore | Correcto; proyectos actualizados. |
| dotnet build --configuration Release | **0 errores / 0 advertencias**. |
| dotnet test --configuration Release | **710 aprobadas / 0 fallidas / 0 omitidas**, 2 min 56 s; 674 previas + 36 nuevas. |
| npm run lint | Correcto; 0 errores. |
| npm test | **608 aprobadas / 0 fallidas / 0 omitidas**, 32 archivos; 594 previas + 14 nuevas. |
| npm run build | TypeScript y Vite correctos; sin paquetes nuevos. |
| Navegador responsive | **5 escenarios / 0 fallidos**, 320/390/768/1366/1600 px; sin overflow horizontal; equivalencia y payload reales del formulario comprobados. |

Evidencias locales: `Heladeria.Pruebas/TestResults/ajuste-d-completa.trx`, `heladeria-web/test-results/ajuste-d-completa.json` y `heladeria-web/test-results/ajuste-d-visual/resultado.json`, más capturas. El script `heladeria-web/scripts/verificar-ajuste-d.mjs` intercepta todas las APIs; no se conecta a datos reales. Preview temporal propio del puerto 5193 cerrado; no se detuvo el servidor del usuario. Los builds generan salidas locales, no una nueva publicación de artifacts.

### Límites y riesgos residuales

- Componentes terminales activos, costo promedio, sin lotes/series/vencimiento. No se implementan selección de lote, composición anidada ni reservas.
- Todos se consumen en la misma bodega/ubicación de la venta. No hay reparto automático entre almacenes.
- La reversión requiere productos/unidades válidos según las restricciones de inventario existentes. Una inconsistencia histórica se rechaza; no se reconstruye inventando datos.
- El padre con composición no consume stock propio. Debe elegirse una unidad base comercial apropiada; la UI explica que las cantidades son por esa unidad.
- La configuración vigente aplica al cierre de ventas pendientes. No hay versionado de recetas comerciales por fecha ni congelación al confirmar.
- El bloqueo por empresa ya usado en inventario prioriza integridad y puede producir contención bajo carga. No se amplió a un sistema de reservas.
- Antes de usar esta compilación con una BD existente es necesario aplicar explícitamente el incremental. Ninguna BD real fue modificada. No se regeneraron artefactos de distribución.

### Archivos creados

- `BaseDatos/Reparaciones/20261027_AjusteD_ComposicionVenta.sql`
- `Heladeria.Infraestructura/Persistencia/Entidades/ComposicionVenta.AjusteD.cs`
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.AjusteD.cs`
- `Heladeria.Infraestructura/Productos/ReglasComposicionVenta.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Composicion.cs`
- `Heladeria.Pruebas/AjusteFinalD/ComposicionHttpPruebas.cs`
- `Heladeria.Pruebas/AjusteFinalD/ComposicionPruebas.cs`
- `Heladeria.Pruebas/AjusteFinalD/ComposicionSqlPruebas.cs`
- `Heladeria.Pruebas/AjusteFinalD/EscenarioComposicion.cs`
- `Heladeria.Pruebas/AjusteFinalD/VentasCompuestasPruebas.cs`
- `heladeria-web/scripts/verificar-ajuste-d.mjs`
- `heladeria-web/src/features/productos/ComposicionVenta.test.tsx`
- `heladeria-web/src/features/productos/ComposicionVenta.tsx`
- `heladeria-web/src/features/productos/utilidadesComposicion.ts`

### Archivos modificados

- `BaseDatos/Heladeria.sql`
- `Heladeria.Aplicacion/Productos/ContratosProductos.cs`
- `Heladeria.Infraestructura/Inventario/ServicioInventario.cs`
- `Heladeria.Infraestructura/Persistencia/Entidades/CombosDetalle.cs`
- `Heladeria.Infraestructura/Persistencia/HeladeriaDbContext.Fase7.cs`
- `Heladeria.Infraestructura/Productos/ServicioProductos.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Consultas.cs`
- `Heladeria.Infraestructura/Ventas/ServicioVentas.Operaciones.cs`
- `Heladeria.Pruebas/Integracion/Persistencia/BaseSqlServerAislada.cs`
- `docs/AJUSTES_FINALES.md`
- `heladeria-web/src/features/productos/PaginaProductos.test.tsx`
- `heladeria-web/src/features/ventas/Ventas.test.tsx`
- `heladeria-web/src/pages/PaginaProductos.tsx`
- `heladeria-web/src/types/productos.ts`

Sin archivos eliminados. Inventario comparado por SHA-256 contra los 548 archivos capturados antes de D; no incluye salidas locales de build/test.

### Checklist manual del propietario

1. Respaldar y seleccionar la BD destino; con API detenida, aplicar explícitamente el incremental, confirmar su mensaje y reiniciar backend/frontend. No ejecutar bootstrap.
2. En Productos, configurar los insumos activos y sus unidades/presentaciones reales. Mantener vaso/cuchara/servilleta no vendibles y comprobar que pueden seleccionarse como componentes.
3. Crear Vaca Loca con base unidad y las cinco filas del ejemplo; verificar «2 BOLA = 120 ml», guardar, reabrir y comprobar cantidades/presentaciones.
4. Editar/quitar/agregar componentes. Probar cantidad cero, duplicado, autorreferencia, producto inactivo y producto compuesto; comprobar rechazo sin pérdida del formulario.
5. Con existencias suficientes, vender dos unidades: descontar 240 ml de helado y dos de cada otro insumo. Verificar cinco salidas/Kardex y una sola línea comercial; costo esperado Q15.30 si se usan los costos del ejemplo.
6. Repetir efectivo, tarjeta y crédito con las configuraciones reales requeridas. Revisar caja/CxC y reintento UUID sin duplicaciones.
7. Dejar insuficiente un solo componente, por ejemplo servilleta: debe aparecer EXISTENCIA_INSUFICIENTE; verificar venta confirmada, stock, movimientos y finanzas intactos.
8. Revisar una venta simple y dos ventas concurrentes con stock limitado; no permitir saldo negativo. No deducir disponibilidad del compuesto solo desde frontend.
9. En una venta que las reglas existentes permitan anular, cambiar luego la composición y anular la anterior: devolver exclusivamente cantidades/costos originalmente consumidos. Una segunda anulación debe rechazarse.
10. Revisar ticket y reporte diario: padre comercial, costo/margen correctos y ausencia de ingredientes vendidos como líneas independientes; mantener omisión de placeholders de C.1.
11. Probar consulta sin administración, alcance de empresa/sucursal y formulario en 320/390/768/1366/1600 px.

**Ajuste D implementado; pendiente de validación manual del propietario. No se declara aprobado ni se realiza cierre final.**

## Datos demostrativos opcionales

El cargador independiente y sus instrucciones están en [DATOS_DEMO.md](DATOS_DEMO.md). No forma parte del bootstrap estructural ni del Ajuste D funcional; su carga en la BD de desarrollo queda a decisión manual del propietario.

## Ajuste E — Usuario vendedor y rediseño POS

### Alcance y diagnóstico

Solicitud posterior a la carga demo de Development. Solo se preparan el usuario operativo y la experiencia del POS. No se cambian servicios financieros, composición del Ajuste D, ticket, SQL, contratos, compras, producción, reportes ni CxC/CxP. La cuenta real **no se crea durante la implementación**.

El esquema y EF usan `Usuarios.RoleId` (un rol), `Roles`, `RolesPermisos`, `Permisos` y `UsuariosSucursales`. No existe `UsuariosRoles` ni una FK obligatoria de Usuarios a Personas. `AsociadoId` es opcional y no se inventa una Persona. El rol exige `NivelDeAccesoId`, `TipoId`, un período de contraseña entre 1 y 48 meses y la bandera de configuración; el usuario requiere identidad, hash, horario, estado, tipo de login, imagen y Soundex. La asignación exige usuario/sucursal/estado y `EsPrincipal`.

Las políticas consultan los permisos activos en BD y la sucursal del usuario; el nivel y el nombre del rol no sustituyen la autorización. ADMIN conserva su configuración; CONSULTA conserva sus permisos. El horario `00:00`–`00:00` ya significa acceso durante todo el día. La contraseña temporal bloquea operaciones hasta cambiarla mediante el flujo existente.

| Operación | Autorización existente reutilizada |
|---|---|
| Login, sesión y selección de sucursal | Autenticación; asignación activa a la sucursal y empresa; cambio obligatorio de contraseña |
| Referencias POS, categorías, clientes y almacenes | `POS.OPERAR` |
| Catálogo y presentaciones POS | `POS.OPERAR` (no necesita `PRODUCTOS.CONSULTAR`) |
| Listado, detalle y datos del comprobante; consulta de clientes | `VENTAS.CONSULTAR` |
| Crear/editar borrador y confirmar | `VENTAS.ADMINISTRAR` |
| Completar venta | `VENTAS.VENDER` + `POS.OPERAR` |
| Cobrar contado al completar | Además `CAJA.OPERAR` + `CAJA.COBRAR` |
| Consultar sesiones abiertas y formas de pago | `CAJA.CONSULTAR` |

**Permisos exactos:** `VENTAS.CONSULTAR`, `VENTAS.ADMINISTRAR`, `VENTAS.VENDER`, `POS.OPERAR`, `CAJA.CONSULTAR`, `CAJA.OPERAR`, `CAJA.COBRAR`.

Limitación explícita del modelo actual: `CAJA.OPERAR` también permite crear cajas y registrar arqueos; `CAJA.CONSULTAR` permite consultar las sesiones/cobros de la sucursal, y `VENTAS.ADMINISTRAR` permite alta de clientes. No existe un permiso más estrecho que pueda sustituirlos en el cobro integrado. La navegación operativa no muestra Caja, pero ocultarla no revoca esas capacidades HTTP. No se cambian las reglas ni se crean permisos duplicados. El vendedor NO recibe abrir/cerrar caja, movimiento manual, anular ventas, administración de configuración/usuarios/roles/inventario/compras/CxP, auditoría ni reportes. Las lecturas de configuración que desde Fase 3 requieren solamente autenticación conservan esa política; su escritura devuelve 403.

### Preparación segura e idempotente

Desde la carpeta de la solución, ejecutar manualmente **solo cuando el propietario decida crear la cuenta**:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project Heladeria.Bootstrap -- --crear-usuario-ventas
```

Usa la conexión/configuración y User Secrets existentes. El comando muestra ambiente, servidor/base de destino y permisos, solicita `CREAR VENTAS` y lee/confirmará la contraseña temporal **interactivamente sin eco**, reutilizando `LectorContrasena`. No acepta contraseñas como argumentos ni las registra en logs, documentación o archivos; no se introduce otra convención de secretos por variable de entorno. El valor lo proporciona el operador, nunca el código.

El provisionador se construye con el `IPasswordHasher<Usuarios>` registrado por `AgregarInfraestructura`, usa `HashPassword(usuario, contrasena)` y la política de contraseña existente. Genera un SecurityStamp criptográfico, almacena `ContraseniaTemporal=true`, `UltimoCambioDeContrasenia=null` y `PasswordTemporal=null`. La API no registra ni ejecuta este provisionador al iniciar.

Una transacción SERIALIZABLE y `sp_getapplock` protegen toda la preparación. Se valida antes de escribir: instalación única, ADMIN original activo, asignación principal activa, empresa/sucursal activas y los siete permisos activos. Se resuelven empresa y sucursal desde el marcador `BOOTSTRAP_ADMIN_COMPLETADO` y sus relaciones, nunca desde IDs fijos ni nombres comerciales susceptibles de cambiar. El destino esperado es la instalación Heladeria Inicial/Central; si dejó de ser inequívoco, se rechaza la operación en vez de elegir otra sucursal.

Se busca primero `VENDEDOR`, o un único rol operativo equivalente con exactamente esos permisos y sin configuración administrativa por rol. Si no existe, se crea `VENDEDOR` / `Vendedor`, con nivel `Operativo POS` y tipo `Rol operativo POS` reutilizables. Esos dos catálogos tienen claves no identity; se obtienen mediante MAX+1 dentro de la transacción. Los IDs identity de usuario/asignación/auditoría los genera SQL Server. No se copian las asignaciones ni permisos de ADMIN.

Se crea `ventas` / `ventas@heladeria.local` y exactamente una asignación principal a la sucursal inicial. La repetición compatible no escribe registros ni cambia contraseña, contador de fallos, sesiones o asignaciones. Una colisión de usuario/correo/rol o permisos inesperados se rechaza sin reparar ni ampliar silenciosamente la cuenta existente. Cualquier fallo revierte la operación. Los cambios se auditan con `USUARIO_VENTAS_PREPARADO`, sin secretos.

### Experiencia operativa

La ruta real continúa siendo `/pos`. Tras login/cambio obligatorio de contraseña, `/dashboard` redirige al POS si el usuario posee `POS.OPERAR` y únicamente capacidades del conjunto operativo. No se compara username ni rol. ADMIN conserva el dashboard y la navegación habitual; un usuario CONSULTA no recibe capacidades nuevas.

El sidebar operativo muestra únicamente el grupo Ventas (Punto de venta e histórico, según permisos), conserva selección de sucursal y cierre de sesión. La marca lleva al POS. Las rutas siguen protegidas por los permisos existentes.

Antes: selector de categoría, configuración arriba, tarjetas compactas y guardar borrador como única acción del carrito. Después:

- Cabecera Heladería con sucursal/usuario; buscador destacado y chips horizontales de categorías reales, incluyendo Todos; búsqueda por nombre/SKU y paginación conservadas.
- Tarjetas con imagen configurada o icono local por categoría; fallback ante error de imagen, precio y presentación seleccionables. Una presentación con stock informado insuficiente se mantiene visible y no se puede agregar. Stock desconocido no equivale a cero.
- Panel **Orden actual**, cliente, botones Contado/Crédito, fecha civil de vencimiento solo para crédito, líneas comerciales con cantidad +/−, eliminación, precio/subtotal/total. Los compuestos siguen siendo una sola línea; su disponibilidad y componentes se validan en backend.
- Guardar borrador se conserva como opción secundaria. **Finalizar venta** abre el cierre guiado después de guardar; utiliza el total persistido. Se seleccionan las formas reales de pago (efectivo/tarjeta/mixto) mediante `CobroAlCompletar` y `CamposPago` existentes. El cierre confirma y completa mediante las mismas APIs, sin retornar al cobro separado en Caja. Crédito conserva cliente/vencimiento y las validaciones de límite/días/CxC del backend.
- Cierre deshabilitado ante datos conocidos incompletos, carrito vacío o falta de turno requerido. Errores de guardado conservan el carrito. Si completar falla después de confirmar, se conserva esa venta y su UUID, sin otro POST de creación; se reutiliza `completarConReconciliacion`. Éxito: Ver comprobante / Nueva venta, sin impresión automática.
- Desktop: catálogo y panel sticky con scroll propio. Tablet/móvil: una columna y acceso fijo a Orden actual; categorías con scroll local. Sin overflow horizontal de página en los anchos verificados. Labels, foco visible, `aria-pressed`, controles de cantidad accesibles y foco retenido en el diálogo; botones táctiles.

### Verificación y limitaciones

Las pruebas SQL usan instancias LocalDB privadas y desechables creadas por `BaseSqlServerAislada`, nunca `VictusPabloE / Heladeria`. Cubren idempotencia, hash, contraseña temporal, alcance, permisos exactos, rechazo/rollback, rol equivalente, login real, cambio obligatorio, sesión, POS, cobro/comprobante y 403 administrativos. La integración real usa los catálogos demo solo dentro de su BD aislada.

Pruebas frontend con fetch interceptado: filtros enviados realmente, catálogo, agotados, fallback de imagen, compuesto como línea única, carrito/cantidades/total, turno, cierre contado/crédito, reintento, comprobante, nueva venta, redirect y navegación por permisos. Las pruebas anteriores de categoría y modalidad se adaptan a botones/chips manteniendo sus aserciones de payload y validación.

Script visual nuevo `heladeria-web/scripts/verificar-ajuste-e.mjs`: 320, 390, 768, 1366 y 1600; 12 estados por ancho, API completamente interceptada y toda petición inesperada rechazada. El script previo de Ventas conserva su regresión y adapta solo los selectores de modalidad.

Resultados finales (28 de octubre de 2026):

| Validación | Resultado |
|---|---|
| `dotnet restore` | Correcto; proyectos actualizados. |
| `dotnet build --configuration Release` | **0 errores / 0 advertencias**, 45.06 s. |
| `dotnet test --configuration Release` | **726 aprobadas / 0 fallidas / 0 omitidas**, 4 min 22 s; 722 previas + 4 nuevas. |
| `npm run lint` | Correcto; 0 errores. |
| `npm test` | **621 aprobadas / 0 fallidas / 0 omitidas**, 33 archivos, 134.34 s; 608 previas + 13 nuevas. |
| `npm run build` | TypeScript y Vite correctos; compilación Vite 2.09 s. Sin dependencias nuevas. |
| Visual Ajuste E | **60 verificaciones / 0 errores**, 12 estados en 320/390/768/1366/1600 px; sin overflow horizontal de página. |
| Regresión visual Ventas | **45 verificaciones / 0 errores**, en los mismos cinco anchos. |

Evidencias locales: `Heladeria.Pruebas/TestResults/ajuste-e-build.log`, `Heladeria.Pruebas/TestResults/ajuste-e-completa.trx`, `heladeria-web/test-results/ajuste-e-completa-final.json`, `heladeria-web/test-results/ajuste-e-visual/resultado.json` y `heladeria-web/test-results/visual-ventas/resultado.json`, más logs y capturas.

La primera ejecución frontend completa terminó con nueve fallos por tiempos de espera bajo carga simultánea; la ejecución final completa pasó sin aumentar los tiempos de espera de pruebas anteriores ni eliminar sus aserciones. Se corrigió el uso de la opción `exact` de Playwright en consultas de Testing Library, detectado por TypeScript. La revisión visual también permitió corregir el foco después del cierre exitoso y mantener visibles el total y Finalizar en el panel de escritorio. Los dos scripts visuales se ejecutaron nuevamente con esos cambios. El servidor temporal propio del puerto 5194 se cerró al finalizar; no se detuvo ningún servidor del propietario.

Archivos nuevos:

- `Heladeria.Bootstrap/Seguridad/ProvisionadorUsuarioVentas.cs`
- `Heladeria.Bootstrap/Seguridad/ComandoUsuarioVentas.cs`
- `Heladeria.Pruebas/AjusteFinalE/UsuarioVentasPruebas.cs`
- `heladeria-web/src/auth/inicioOperativo.ts`
- `heladeria-web/src/features/ventas/CierrePos.tsx`
- `heladeria-web/src/features/ventas/AjusteFinalE.test.tsx`
- `heladeria-web/scripts/verificar-ajuste-e.mjs`

Archivos modificados:

- `Heladeria.Bootstrap/Program.cs`: despacho explícito, sin modificar las otras preparaciones.
- `heladeria-web/src/rutas/EnrutadorAplicacion.tsx`: entrada según permisos.
- `heladeria-web/src/layouts/LayoutAutenticado.tsx`: navegación operativa.
- `heladeria-web/src/features/ventas/PaginaPos.tsx` y `ventas.css`: composición visual y cierre guiado.
- `heladeria-web/src/features/ventas/CobroAlCompletar.tsx`: notificación opcional de validez al nuevo cierre, sin cambiar el payload ni sus reglas.
- `heladeria-web/src/features/ventas/Ventas.test.tsx` y `VentaCredito.test.tsx`: interacción con chips/botones en lugar de selects; mismas verificaciones anteriores.
- `heladeria-web/scripts/verificar-visual-ventas.mjs`: selectores de modalidad actualizados.
- `docs/AJUSTES_FINALES.md`: esta sección.

No se crean turnos abiertos ni se concede abrir Caja: un operador autorizado debe abrir el turno previamente. Stock mostrado es orientativo; el backend conserva validación definitiva, especialmente para compuestos. Límites/días de crédito no se recalculan ni se inventan en frontend. El cierre conserva las etapas persistidas del flujo existente: guardar/confirmar no constituyen una transacción conjunta con completar. Si el cierre falla, la venta permanece confirmada; puede consultarse y reintentarse según las reglas existentes.

No se ejecutó creación sobre la BD real, no se requiere SQL adicional y no se regeneran los artefactos de publicación en este ajuste. Pendiente creación autorizada y validación manual del propietario.


