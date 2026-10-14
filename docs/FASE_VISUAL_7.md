# Fase Visual 7 — Cuentas por cobrar y por pagar

## Diagnóstico previo (2026-10-14)

Se inspeccionaron `EnrutadorAplicacion`, `LayoutAutenticado`, `RutaProtegida`, el contexto de autenticación, `clienteHttp`, los componentes UI, Caja, Compras, Ventas, sus tipos y pruebas. La autenticación mantiene el access token en memoria y centraliza refresh/401; los permisos se verifican con `tienePermiso` y `RutaConPermiso`. Empresa/sucursal proceden exclusivamente de la sesión. No se cambian roles ni asignaciones.

Se reutilizan EncabezadoPagina, PanelFiltros, SelectorEntidad, Paginacion, Entrada/Seleccion/AreaTexto/Boton, Insignia, estados cargando/vacío/error, ModalCaja (foco/teclado), TablaVentas (tarjetas móviles), useCargaVentas/useRecursoVentas (cancelación/contexto) y useOperacionVenta (exclusión síncrona). Los importes usan dineroCaja; fechas civiles y operaciones usan respectivamente mostrarFechaCivil/mostrarInstante, con zona de sesión. No se incorpora una librería.

Contratos verificados en ContratosCartera.cs, CuentasPorCobrarController, CuentasPorPagarController, ServicioCartera.Consultas/Operaciones, ContratosCompras y CajaController. Las listas son paginadas; resumen devuelve un array por moneda, referencias devuelve ReferenciasCajaDto. Historial ya incluye nombre de usuario y movimientos. Operable=false es histórico de solo consulta.

Permisos: consultar cada cartera para navegar; CXC.COBRAR + CAJA.OPERAR + CAJA.COBRAR para abonar; CXP.PAGAR + CAJA.OPERAR para pagar. La UI necesita además CAJA.CONSULTAR para recuperar sesiones/versiones. Facturación requiere CXP.ADMINISTRAR y COMPRAS.CONSULTAR para seleccionar/leer la compra. Los filtros auxiliares de clientes/proveedores respetan sus permisos existentes; sin ellos se permite filtrar por ID o buscar por nombre, sin conceder acceso adicional.

No existe endpoint de compras pendientes de factura. CompraResumenDto ya expone EsCredito/VersionFila, aún ausentes en los tipos frontend. Se consultan compras RECIBIDA del flujo ORDENES_COMPRA, sus detalles y todas las páginas CxP para excluir compras vinculadas (también pagadas). La consulta actual no expone una factura histórica sin CxP ni todos los indicadores operativos del proveedor/estado. El servidor conserva la comprobación definitiva, duplicados y concurrencia. No se cambia backend para duplicar esta regla.

La huella idempotente backend incluye ambas versiones, referencia y observación. Un reintento no puede actualizar el payload: se conservará exactamente en memoria, con marcador UUID en sessionStorage. Nunca se persiste el formulario, referencia o información de tarjeta. Tras recargar completamente una pestaña con resultado incierto, solo se permite consultar/reconciliar; no se inventa un nuevo payload o UUID. La incertidumbre no autoriza un segundo pago.

Alcance: frontend y documentación/pruebas solamente; sin SQL, bootstrap, datos reales ni reglas financieras nuevas.

## Implementación entregada

Rutas nuevas:

- `/cuentas-por-cobrar` y `/cuentas-por-cobrar/:id`.
- `/cuentas-por-pagar` y `/cuentas-por-pagar/:id`.
- `/cuentas-por-pagar/registrar-factura`.

Listado y resumen usan los mismos filtros remotos y datos de moneda del servidor. Un resumen vacío no genera tarjetas artificiales. Los detalles muestran historial real, usuario, estado y movimientos; no exponen UUID/RowVersion. Las cuentas pagadas, anuladas o no operables no ofrecen nuevos pagos. Cambiar empresa/sucursal/usuario desmonta la vista y descarta lecturas obsoletas. Si el contexto desaparece durante una lectura previa a una operación, no se inicia el POST.

Los pagos consultan cuenta, sesiones abiertas, referencias de su cartera y detalle de Caja. Antes del primer POST se vuelven a leer las versiones; un cambio exige revisión, nunca reenvío automático. Se muestran saldo anterior/posterior estimado y efectivo esperado. CxC ofrece efectivo/tarjeta según referencias; CxP solo efectivo. La referencia es únicamente un identificador corto de autorización, validado también por backend. No se solicitan datos de tarjeta.

El payload completo de un intento incierto permanece inmutable en memoria. Reintentar conserva UUID, importes, referencias y versiones. `reintento:true` es éxito. Un 403/429 recibido al reintentar tampoco borra el intento incierto. Se puede reconciliar el pago por la UUID del historial, sin otro POST. Se conserva solo UUID en sessionStorage; ni credenciales ni formularios se persisten. Un nuevo pago exitoso obliga a recuperar las nuevas versiones antes del siguiente.

Registrar factura vuelve a consultar la compra y todas las CxP para detectar duplicados aparecidos después de la selección. El servidor verifica definitivamente proveedor, estado operativo, recepción, moneda, importes, documento y unicidad. El resultado muestra la cuenta creada; al volver al listado se consultan listado y resumen nuevamente.

## Archivos

En `heladeria-web`, creados:

- `src/api/cartera.ts`.
- `src/types/cartera.ts` (incluye la extensión de lectura de Compras ya existente en backend).
- `src/features/cartera/PaginaCartera.tsx`.
- `src/features/cartera/PaginaDetalleCartera.tsx`.
- `src/features/cartera/FormularioPagoCartera.tsx`.
- `src/features/cartera/PaginaFacturaProveedor.tsx`.
- `src/features/cartera/ComponentesCartera.tsx`.
- `src/features/cartera/utilidadesCartera.ts`.
- `src/features/cartera/comprasFacturables.ts`.
- `src/features/cartera/useIntentoCartera.ts`.
- `src/features/cartera/Cartera.test.tsx`.
- `src/test/datosFaseVisual7.ts`.
- `scripts/verificar-visual-cartera.mjs`.

Modificados:

- `src/auth/permisos.ts`: declara permisos Fase 9 existentes, sin concederlos.
- `src/layouts/LayoutAutenticado.tsx`: navegación Cartera y etiquetas de rutas.
- `src/rutas/EnrutadorAplicacion.tsx`: rutas protegidas y aislamiento por contexto.
- `src/components/ui/SelectorEntidad.tsx`: reconoce `nombreCliente` real; conserva el mapeo anterior.
- `src/rutas/EnrutadorAplicacion.test.tsx`: incorpora las dos entradas de Cartera en la expectativa del menú; conserva y amplía la validación alfabética. La primera corrida completa falló únicamente por esta expectativa estructural antigua.

Documentación creada: `docs/FASE_VISUAL_7.md`. No se cambió ningún archivo backend/SQL, ni package.json ni dependencias.

## Pruebas y verificación visual

Validación final ejecutada desde `heladeria-web`:

- `npm run lint`: correcto, sin diagnósticos.
- `npm run build`: correcto (TypeScript + Vite), sin errores ni advertencias.
- `npm test`: **402 aprobadas, 0 fallidas, 0 omitidas**, 23 archivos de pruebas.
- 83 pruebas nuevas de Cartera; 319 pruebas anteriores conservadas, con la expectativa del menú actualizada como se documenta arriba.
- No se ejecutó build/test backend porque no hubo cambios backend.

83 pruebas nuevas: rutas/permisos/navegación, listados, filtros/paginación, resumen vacío/con datos, historial/fechas/usuario, estados de solo lectura, efectivo/tarjeta CxC, efectivo CxP, montos inválidos, doble clic, UUID/payload exactos tras timeout, conciliación sin POST, errores financieros, versiones y caja cerrada, abandono de contexto, fallo de almacenamiento, segunda operación con versiones nuevas, facturación, exclusión paginada de compras, duplicados y reintentos de factura.

La verificación visual reutiliza el patrón Playwright existente, sin instalar dependencias. Intercepta todas las API antes de navegar, simula autenticación y rechaza destinos no locales/no previstos. Verifica 12 estados por ancho: 320, 390, 768 y 1366 px (48 comprobaciones), overflow de página/modal, foco dentro del modal, interacción completa y consola. Navegador Asia/Tokyo, sucursal America/Guatemala. Resultado: 0 errores de consola, 0 llamadas no previstas y 0 desbordamientos. Capturas e informe regenerables en `heladeria-web/test-results/visual-cartera/` (ignorado).

## Checklist de validación manual del usuario

Usar un entorno de validación autorizado y datos controlados. No se realizaron estos movimientos sobre la BD real durante la implementación.

1. Iniciar sesión con ADMIN preparado y elegir la sucursal. Abrir Cartera/Cuentas por cobrar y Cartera/Cuentas por pagar. Verificar que documentos/terceros/saldos coincidan con los GET correspondientes.
2. Filtrar por nombre/documento, cliente/proveedor, estado, vencimiento y fechas. Cambiar tamaño/página y limpiar. Revisar que el resumen se corresponda con filtros y moneda. Un resumen `[]` debe mostrar el estado vacío.
3. Abrir una cuenta pendiente. Comparar emisión/vencimiento civiles con API y el historial con la hora local de la sucursal. Revisar nombre de usuario y enlaces a movimientos/sesión; no deben mostrarse UUID ni versiones.
4. En CxC, registrar un abono parcial en efectivo desde una Caja abierta compatible. Revisar saldo previo/posterior; confirmar. Verificar historial, saldo, listado y resumen. Probar tarjeta usando solo referencia corta de autorización, nunca datos de tarjeta.
5. Completar el saldo restante. La cuenta PAGADA no debe ofrecer Registrar abono. Verificar también una cuenta histórica de solo consulta y una sesión cerrada/no compatible.
6. En CxP, registrar un pago parcial y uno final usando efectivo. Confirmar que no se ofrece tarjeta y que se informa el efectivo esperado. Probar un importe superior al saldo (bloqueo UX) y un efectivo insuficiente (respuesta controlada backend).
7. En dos sesiones de navegador del entorno de prueba, modificar cuenta/Caja después de preparar el formulario. Confirmar que se avisa de concurrencia, se recarga y no hay envío automático con datos nuevos.
8. Solo en entorno de pruebas con respuesta simulada/perdida, comprobar Reintentar misma operación: Network debe conservar byte a byte el body original. Si devuelve `reintento:true`, tratarlo como éxito sin duplicar pago/movimiento. Consultar resultado debe conciliar por UUID. No repetir operaciones reales para simular incertidumbre.
9. Registrar factura de proveedor: elegir una compra a crédito RECIBIDA sin CxP, ingresar serie opcional/número/emisión/vencimiento reales y confirmar. Verificar CxP creada y su vínculo. Una compra al contado, parcial, ajena a la sucursal o ya vinculada no debe ofrecerse. Verificar respuesta controlada de documento duplicado.
10. Con un usuario sin permisos, revisar navegación oculta y acceso directo 403. CXC.ADMINISTRAR/CXP.ADMINISTRAR no sustituyen los permisos de consulta/pago; no ampliar CONSULTA. Sin permiso auxiliar de catálogo, usar filtro por ID/nombre.
11. Cambiar sucursal y confirmar que no persisten formularios/datos del contexto anterior. Revisar a 320, 390, 768 y escritorio: tarjetas móviles, scroll de tabla en tablet, foco/tabulación/escape de modal y acciones utilizables.

## Limitaciones reales

- Los contratos existentes no permiten garantizar desde la selección que no exista una factura histórica sin CxP, ni comprobar toda la operatividad interna del proveedor/estado. El backend sigue siendo la autoridad y puede rechazar una candidata. No se inventó un endpoint ni se relajaron reglas.
- Carga de candidatos: requiere recorrer compras recibidas y todas las cuentas de la sucursal para exclusión completa; en volúmenes grandes puede ser más lenta que un endpoint específico. No se ocultan páginas ni se asume que la primera contiene todo.
- Un timeout conserva payload solo mientras vive la pestaña. Tras recarga completa se conserva UUID, pero no datos del formulario; se bloquea una nueva operación y se exige consulta/conciliación si no se encuentra el resultado. No se guarda información financiera sensible para facilitar un reenvío.
- No se ha ejecutado validación financiera contra datos reales. La revisión manual del usuario sigue pendiente; esta entrega no declara la fase aprobada.

**Lista para validación manual del usuario.**
