# Fase Visual 6 — Caja

Implementada el 2026-10-08. Pendiente de aprobación mediante validación manual.
No incluye CxC, CxP, FEL, impresión, delivery, conciliación bancaria, contabilidad ni reversión financiera.

## 1. Rutas

| Ruta | Pantalla | Acceso |
| --- | --- | --- |
| `/caja` | Resumen de sesiones abiertas | CAJA.CONSULTAR |
| `/caja/cajas` | Catálogo de cajas y registro | CAJA.CONSULTAR |
| `/caja/sesiones` | Sesiones e histórico filtrado/paginado | CAJA.CONSULTAR |
| `/caja/sesiones/:id` | Detalle operativo | CAJA.CONSULTAR |
| `/caja/abrir` | Apertura | CAJA.CONSULTAR + CAJA.ABRIR |

Consulta es necesaria para cargar cajas, referencias y detalle: no se amplían permisos del backend.
Menú Caja ordenado con el resto del sidebar. El resumen se activa únicamente en su ruta exacta.

## 2. Pantallas

Resumen sin asumir una sola caja por sucursal; ausencia de sesión explícita. Catálogo con código/nombre/estado/sucursal/sesión. Histórico con búsqueda, caja, abierta/cerrada y paginación.
El detalle concentra operación, saldos, totales por tipo/forma, movimientos, cobros, arqueos y snapshots de cierre.

## 3. Componentes

`PaginaCaja`, `PaginaCajas`, `PaginaSesionesCaja`, `PaginaSesionCaja`, `PaginaAbrirCaja`, `CobrarVentaCaja`, `FormularioSesionCaja` y `ComponentesCaja`.
Se reutilizan controles, selector de entidades, paginación, modal, avisos y tablas responsivas existentes; no se duplica un sistema visual.

## 4. Hooks y servicios

`api/caja.ts` usa el cliente HTTP existente con autenticación/renovación. `useDatosCaja` reutiliza las lecturas cancelables y el bloqueo síncrono de doble envío ya probados.
Las lecturas descartan respuestas obsoletas. Al cambiar usuario/empresa/sucursal se desmonta la vista operativa y sus formularios. No se acepta un resultado de otra sucursal como estado actual.

## 5. Permisos

- CONSULTAR: todas las lecturas y rutas.
- ABRIR: apertura y enlaces correspondientes.
- OPERAR: creación de caja y arqueo.
- OPERAR + COBRAR: cobros.
- OPERAR + MOVIMIENTO_MANUAL: ingresos/egresos manuales.
- CERRAR: cierre, además de exigir arqueo vigente.
- ANULAR: reservado, sin botón ni endpoint inventado.

Los permisos se verifican por código, nunca por nombre del rol. Backend sigue siendo autoridad.

## 6. Contratos consumidos

GET `/api/caja/cajas`, `/sesiones`, `/sesiones/{id}`, `/referencias`, `/ventas-pendientes`.
POST `/api/caja/cajas`, `/sesiones/abrir`, `/sesiones/{id}/movimientos`, `/cobrar-venta`, `/arqueo`, `/cerrar`.
Las rutas abreviadas de la última lista cuelgan de `/api/caja/sesiones/{id}`.
Tipos camelCase en `types/caja.ts`, respetando campos nullable, resultados paginados y respuesta `{ cobro, sesion, reintento }`.
Empresa/sucursal las determina el contexto del servidor. Caja, moneda, venta y formas de pago provienen de respuestas reales, sin IDs identity fijos en implementación.

## 7. Versiones

Cada mutación usa la `versionFila` actual; el cobro envía también `versionVenta` del pendiente elegido. Tras respuesta válida se reemplaza la sesión completa, incluidos arqueos, saldos, movimientos y nueva versión. No se parchean saldos suponiendo éxito.
`CONCURRENCIA_CAJA` explica que la sesión cambió y ofrece recarga; no reintenta automáticamente. La versión es interna, no una cifra visual.

## 8. UUID y resultados inciertos

`crypto.randomUUID()` antes del primer cobro. En `sessionStorage` se conserva SOLO esa UUID, separada por usuario/empresa/sucursal/sesión/venta. No se guardan pagos, referencias ni datos sensibles.
Si almacenamiento falla, no se envía. Clave existente exige consultar la sesión antes de cualquier nuevo POST. Cobro ya existente se muestra sin duplicarlo, incluso si la sesión fue cerrada. Versión cambiada o consulta fallida bloquean reenvío.
Red/408/5xx: consulta de reconciliación; nunca otro POST automático. Si el resultado sigue desconocido se exige consultar desde UI. Reabrir el formulario conserva la UUID.
La persistencia es de la pestaña, no una promesa de coordinación entre navegadores; el backend mantiene el control final de idempotencia.

## 9. Efectivo y vuelto

Monto aplicado, recibido y vuelto preview; recibido debe cubrir aplicado. Importes de hasta dos decimales se comparan en centavos. Confirmación en dos pasos y botón bloqueado durante envío. Tras éxito se usa vuelto y sesión del servidor.

## 10. Tarjeta y referencia segura

Recibido coincide con aplicado, sin vuelto. Referencia opcional salvo configuración que la exija, con aviso “No ingresar datos de tarjeta.” No hay campos PAN, CVV, número completo ni vencimiento.
El error backend se muestra sin transformar la referencia; el campo se vacía después del error. No se persiste en almacenamiento local ni se escribe a consola.

## 11. Mixto

Formas dinámicas, sin duplicar forma de pago; máximo hasta el catálogo soportado y límite contractual de diez. Total aplicado debe coincidir exactamente con total de venta.
Saldo teórico y efectivo esperado se muestran por separado; tarjeta no se suma al efectivo. Apertura se separa de ingresos operativos.

## 12. Movimientos

Ingreso/egreso, monto positivo, motivo obligatorio y observación. No se presentan como venta/pago.
Preview rechaza egreso superior al efectivo esperado y cualquier error servidor conserva las cifras confirmadas. No se realizan ajustes locales ni reversión financiera.

## 13. Arqueo

Efectivo esperado de solo lectura; contado admite cero. Diferencia preview = contado − esperado, sin ajustes automáticos. Resultado final e historial vienen del backend. Vigente se destaca mediante insignia, no se infiere localmente.

## 14. Cierre

Solo con arqueo vigente. Confirmación de esperado/contado/diferencia/ingresos/egresos. Al cerrar se muestran número, fecha, usuario y snapshots. Sesión cerrada o histórica sin semántica Fase 8 queda en solo lectura.

## 15. Errores

403 y 404 reutilizan sus pantallas diferenciadas. Códigos de validación, doble apertura, saldo y concurrencia se muestran claramente.
VENTA_YA_COBRADA bloquea reintento, recarga pendientes automáticamente y permite consultar el resultado completo; nunca ofrece una UUID nueva.

## 16. Fechas

Exclusivamente `utils/fechas.ts`, mediante `fechaCaja`: UTC con Z hacia zona efectiva del contexto (`America/Guatemala` actualmente). Sin `new Date` en componentes, restas fijas ni Z añadida.
Se comprobaron apertura, cobro, movimiento, arqueo y cierre, incluso con navegador en Asia/Tokyo.

## 17. Responsive

Prueba de navegador Edge headless con API íntegramente simulada: 16 estados de UI × 5 anchos (320, 390, 768, 1366, 1600) = 80 verificaciones.
Sin desbordamiento horizontal de documento/modal; tablas con tarjetas en móvil y scroll de tabla en tamaños intermedios. Modales con scroll vertical interno, campos apilados y acciones alcanzables. Cero errores de consola y cero solicitudes API fuera de los mocks.
Evidencias en `heladeria-web/test-results/visual-caja/`; script reproducible `heladeria-web/scripts/verificar-visual-caja.mjs` (instrucciones al inicio).

## 18. Accesibilidad

Botones reales, labels, ayuda de referencia separada mediante aria-describedby, nombres de tabla/región, estados en texto y aria-live.
Modal captura/restaura foco, contiene Tab/Shift+Tab y admite Escape cuando no hay escritura en curso. Se revisaron capturas móviles y escritorio.

## 19. Tests

`Caja.test.tsx`: 58 casos de UI, permisos, caja/sesión, efectivo/tarjeta/mixto, referencias, doble clic, resultado incierto, actualización de pendientes, manuales, arqueo, cierre, versiones, 403/404, fechas y foco.
`useDatosCaja.test.ts`: 17 casos de idempotencia/reconciliación, errores definidos/inciertos, UUID por contexto y almacenamiento bloqueado.
Router: 5 nuevas rutas sin permiso y 2 combinaciones insuficientes para apertura; se actualiza expectativa de menú por el grupo agregado.
Suite frontend completa: 310 aprobadas, 0 fallidas, 0 omitidas en 22 archivos; incluye las regresiones anteriores y 82 casos adicionales de esta fase.

## 20. Validación técnica

Comandos frontend: `npm run lint`, `npm run build`, `npm test` desde `heladeria-web`.
Backend: `dotnet build --configuration Release` y `dotnet test --configuration Release --no-build` desde la solución.
Backend verificado: 0 advertencias, 0 errores; 428 aprobadas, 0 fallidas, 0 omitidas.
Frontend verificado: lint sin diagnósticos, build TypeScript/Vite correcto, 310 pruebas aprobadas. Última pasada visual: 80 verificaciones aprobadas y cero errores de consola. El servidor temporal de comprobación fue detenido al terminar.

## 21. Cambio backend mínimo READ

El contrato de sesión solo exponía UsuarioCierreId, no su nombre, requerido para mostrar el cierre. Se agregó `SesionCajaDto.UsuarioCierre` nullable y consulta `AsNoTracking` por ese ID en `LeerSesionAsync`. No se altera ninguna regla ni operación de escritura.
Archivos: `Heladeria.Aplicacion/Caja/ContratosCaja.cs`, `Heladeria.Infraestructura/Caja/ServicioCaja.Consultas.cs`, `Heladeria.Pruebas/Fase8/CajaPruebas.cs` (assert del nombre tras cierre).
No se modifican SQL, EF generado ni esquema. No se ejecutaron SQL/bootstrap contra BD real. Las pruebas backend usan sus fixtures aislados; el navegador usa exclusivamente mocks.

## 22. Validación manual pendiente

La fase NO está aprobada sin validación manual del usuario. No se crearon datos reales automáticamente.

1. Con permisos reales, revisar cajas y crear una caja de validación autorizada.
2. Abrir sesión, comprobar saldo inicial y rechazo de doble apertura.
3. Revisar únicamente ventas elegibles pendientes.
4. Cobrar efectivo con vuelto, tarjeta sin datos sensibles y mixto; comprobar saldos por forma.
5. Probar referencia rechazada: mensaje y campo vacío.
6. Simular respuesta perdida/doble clic y comprobar cobro único, misma UUID y consulta previa.
7. Desde otra pestaña, cobrar una venta y comprobar actualización de pendientes en el conflicto.
8. Ingreso/egreso, rechazo de egreso sin efectivo y cifras sin modificación visual ficticia.
9. Arqueos cero, sobrante, faltante; registrar movimiento y comprobar invalidación del anterior.
10. Cerrar con arqueo vigente; verificar snapshots y sesión cerrada sin operaciones.
11. Concurrencia entre pestañas, permisos separados, 403/404, cambio de sucursal y horas configuradas.
12. Revisar teclado, móvil/tablet/escritorio y consola con datos reales autorizados.

Detenerse aquí: no iniciar otra fase.
