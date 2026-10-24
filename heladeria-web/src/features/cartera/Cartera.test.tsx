import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ErrorApi, solicitar } from '../../api/clienteHttp'
import { PERMISOS } from '../../auth/permisos'
import { renderSeguridad } from '../../test/renderSeguridad'
import { paginaCaja, referenciasCaja, sesionCaja } from '../../test/datosFaseVisual6'
import { aplicarPagoDemo, compraCredito, detalleCartera, resumenCartera } from '../../test/datosFaseVisual7'
import type { DetalleCartera, DetalleCompraCredito, SolicitudPagoCartera, TipoCartera } from '../../types/cartera'
import type { SesionCaja } from '../../types/caja'
import { configurarZonaHoraria, mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { EnrutadorAplicacion } from '../../rutas/EnrutadorAplicacion'
import { PaginaCartera } from './PaginaCartera'
import { PaginaDetalleCartera } from './PaginaDetalleCartera'
import { PaginaFacturaProveedor } from './PaginaFacturaProveedor'
import { mensajesCartera, referenciaInsegura } from './utilidadesCartera'
import { comprasFacturables } from './comprasFacturables'

vi.mock('../../api/clienteHttp', async (original) => ({ ...(await original<typeof import('../../api/clienteHttp')>()), solicitar: vi.fn() }))
const api = vi.mocked(solicitar)
const permisos = Object.values(PERMISOS)
let cuenta: DetalleCartera; let caja: SesionCaja; let compra: DetalleCompraCredito
let cuentasPagar: DetalleCartera['encabezado'][]
const escrituras = () => api.mock.calls.filter(([, o]) => o?.method === 'POST')
const body = () => JSON.parse(String(escrituras().at(-1)?.[1]?.body)) as SolicitudPagoCartera
function navegar(ruta = '/cuentas-por-cobrar/1', autorizados: string[] = permisos) {
  return renderSeguridad(<Routes>
    {(['cobrar', 'pagar'] as const).map((tipo) => <Route key={tipo} path={`/cuentas-por-${tipo}`} element={<PaginaCartera key={tipo} tipo={tipo} />} />)}
    {(['cobrar', 'pagar'] as const).map((tipo) => <Route key={tipo} path={`/cuentas-por-${tipo}/:id`} element={<PaginaDetalleCartera tipo={tipo} />} />)}
    <Route path="/cuentas-por-pagar/registrar-factura" element={<PaginaFacturaProveedor />} />
  </Routes>, autorizados, ruta)
}
async function prepararPago(tipo: TipoCartera = 'cobrar', monto = '3', forma = '21') {
  fireEvent.click(await screen.findByRole('button', { name: tipo === 'cobrar' ? 'Registrar abono' : 'Registrar pago' }))
  fireEvent.change(await screen.findByLabelText('Sesión de caja'), { target: { value: '7' } })
  fireEvent.change(screen.getByLabelText('Forma de pago'), { target: { value: forma } })
  fireEvent.change(screen.getByLabelText('Monto'), { target: { value: monto } })
  await waitFor(() => expect((screen.getByRole('button', { name: 'Revisar operación' }) as HTMLButtonElement).disabled).toBe(false))
}
async function confirmarPago() {
  fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' }))
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' }))
  await waitFor(() => expect(escrituras()).toHaveLength(1))
}
async function prepararFactura() {
  navegar('/cuentas-por-pagar/registrar-factura')
  fireEvent.click(await screen.findByRole('button', { name: /Compra recibida a crédito/ }))
  fireEvent.click(await screen.findByRole('option', { name: /OC-00000051/ }))
  fireEvent.change(screen.getByLabelText('Número de factura'), { target: { value: 'F-2026-17' } })
  fireEvent.change(screen.getByLabelText('Fecha documento'), { target: { value: '2026-10-10' } })
  fireEvent.change(screen.getByLabelText('Fecha vencimiento'), { target: { value: '2026-10-30' } })
  await waitFor(() => expect((screen.getByRole('button', { name: 'Revisar factura' }) as HTMLButtonElement).disabled).toBe(false))
}
function fallo(codigo: string, estado = 409) {
  const previo = api.getMockImplementation()!
  api.mockImplementation((ruta, opciones) => opciones?.method === 'POST' ? Promise.reject(new ErrorApi('Datos rechazados', estado, { codigo })) : previo(ruta, opciones))
}
beforeEach(() => {
  api.mockReset(); sessionStorage.clear(); configurarZonaHoraria('America/Guatemala')
  cuenta = structuredClone(detalleCartera); caja = structuredClone(sesionCaja); compra = structuredClone(compraCredito); cuentasPagar = []
  api.mockImplementation(async (ruta, opciones) => {
    const u = new URL(ruta, 'http://pruebas.local'); const path = u.pathname
    const datos = opciones?.body ? JSON.parse(String(opciones.body)) : null
    if (path === '/api/compras/referencias') return { monedas: [{ id: 6, codigoISO: 'GTQ' }], estados: [{ id: 93, flujo: 'ORDENES_COMPRA', codigo: 'RECIBIDA' }], series: [] } as never
    if (path === '/api/compras') return paginaCaja([compra.encabezado]) as never
    if (path === '/api/compras/51') return structuredClone(compra) as never
    if (path === '/api/clientes') return paginaCaja([{ id: 27, nombreCliente: 'Cafetería del Parque' }]) as never
    if (path === '/api/compras/proveedores') return paginaCaja([{ id: 27, nombreProveedor: 'Lácteos del Valle' }]) as never
    if (path === '/api/caja/sesiones') return paginaCaja([caja.encabezado]) as never
    if (path === '/api/caja/sesiones/7') return structuredClone(caja) as never
    if (path.endsWith('/referencias')) return { ...referenciasCaja, formasPago: path.includes('por-pagar') ? referenciasCaja.formasPago.filter((f) => f.esEfectivo) : referenciasCaja.formasPago } as never
    if (path.endsWith('/resumen')) return cuenta.encabezado.saldoPendiente ? [{ ...resumenCartera, totalPendiente: cuenta.encabezado.saldoPendiente }] as never : [] as never
    if (path.endsWith('/desde-compra')) {
      cuenta = { encabezado: { ...detalleCartera.encabezado, id: 5, tipoDocumentoOrigen: 'FACTURA_PROVEEDOR', numeroDocumentoOrigen: datos.numeroDocumento, compraId: 51 }, historial: [] }
      cuentasPagar.push(cuenta.encabezado)
      return { facturaId: 18, cuenta: structuredClone(cuenta), reintento: false } as never
    }
    if (path.endsWith('/abonos') || path.endsWith('/pagos')) {
      cuenta = aplicarPagoDemo(cuenta, datos.monto, datos.claveOperacion, datos.formaPagoId === 21 ? 'Efectivo' : 'Tarjeta')
      caja.encabezado.versionFila = 'CAJA-V2'; caja.efectivoEsperado! += path.endsWith('/abonos') ? datos.monto : -datos.monto
      return { pagoId: 70, cuenta: structuredClone(cuenta), reintento: false } as never
    }
    if (/\/cuentas-por-(cobrar|pagar)\/\d+$/.test(path)) return structuredClone(cuenta) as never
    if (path === '/api/cuentas-por-pagar' && u.searchParams.get('tamanioPagina') === '100') return paginaCaja(cuentasPagar) as never
    if (/\/cuentas-por-(cobrar|pagar)$/.test(path)) return paginaCaja([cuenta.encabezado], Number(u.searchParams.get('pagina') ?? 1)) as never
    throw new Error(`Ruta no prevista: ${ruta}`)
  })
})
afterEach(() => { configurarZonaHoraria(); vi.restoreAllMocks() })

describe('consulta, permisos y presentación', () => {
  it.each(['/cuentas-por-cobrar', '/cuentas-por-cobrar/1', '/cuentas-por-pagar', '/cuentas-por-pagar/1', '/cuentas-por-pagar/registrar-factura'])('protege ruta %s sin consultas remotas', async (ruta) => {
    renderSeguridad(<EnrutadorAplicacion />, [], ruta); await screen.findByText('Acceso no autorizado'); expect(api).not.toHaveBeenCalled()
    expect(screen.queryByRole('link', { name: 'Cuentas por cobrar' })).toBeNull(); expect(screen.queryByRole('link', { name: 'Cuentas por pagar' })).toBeNull()
  })
  it('muestra navegación solo de la cartera autorizada', async () => {
    renderSeguridad(<EnrutadorAplicacion />, [PERMISOS.cxcConsultar], '/cuentas-por-cobrar')
    fireEvent.click(screen.getByRole('button', { name: 'Cartera' }))
    await screen.findByRole('table', { name: 'Cuentas por cobrar' }); expect(screen.getByRole('link', { name: 'Cuentas por cobrar' })).toBeDefined(); expect(screen.queryByRole('link', { name: 'Cuentas por pagar' })).toBeNull()
  })
  it.each(['cobrar', 'pagar'] as const)('lista %s con resumen real y datos responsivos', async (tipo) => {
    navegar(`/cuentas-por-${tipo}`); const tabla = await screen.findByRole('table', { name: `Cuentas por ${tipo}` })
    expect(screen.getByRole('region', { name: 'Resumen GTQ' }).textContent).toContain('GTQ 8.00')
    expect(within(tabla).getByText('Cafetería del Parque')).toBeDefined()
    expect(within(tabla).getByText(mostrarFechaCivil('2026-10-10'))).toBeDefined()
    const fila = within(tabla).getAllByRole('row')[1]; const cabeceras = within(tabla).getAllByRole('columnheader')
    within(fila).getAllByRole('cell').forEach((celda, i) => expect(celda.getAttribute('data-etiqueta')).toBe(cabeceras[i].textContent))
  })
  it('resumen vacío no inventa tarjetas', async () => {
    cuenta.encabezado.saldoPendiente = 0; navegar('/cuentas-por-cobrar'); await screen.findByText('Sin saldos pendientes para estos filtros'); expect(screen.queryByRole('region', { name: 'Resumen de cartera' })).toBeNull()
  })
  it('filtros se envían a listado y resumen, mantienen sucursal y permiten limpiar', async () => {
    navegar('/cuentas-por-cobrar'); await screen.findByRole('table')
    fireEvent.change(screen.getByLabelText('Búsqueda'), { target: { value: 'Café' } }); fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'PARCIAL' } })
    fireEvent.change(screen.getByLabelText('Vencimiento'), { target: { value: 'VENCIDA' } }); fireEvent.change(screen.getByLabelText('Fecha desde'), { target: { value: '2026-10-01' } })
    fireEvent.click(screen.getByRole('button', { name: /Cliente Todos/ })); fireEvent.click(await screen.findByRole('option', { name: 'Cafetería del Parque' }))
    fireEvent.click(screen.getByRole('button', { name: 'Buscar cuentas' }))
    await waitFor(() => expect(api.mock.calls.filter(([r]) => r.includes('terceroId=27') && r.includes('estado=PARCIAL') && r.includes('vencimiento=VENCIDA') && r.includes('sucursalId=4') && r.includes('fechaDesde=2026-10-01'))).toHaveLength(2))
    fireEvent.click(screen.getByRole('button', { name: 'Limpiar filtros' })); expect((screen.getByLabelText('Estado') as HTMLSelectElement).value).toBe('')
    fireEvent.change(await screen.findByLabelText('Registros por página'), { target: { value: '20' } }); await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes('tamanioPagina=20'))).toBe(true))
  })
  it('sin permiso auxiliar conserva filtro ID y no consulta clientes', async () => {
    navegar('/cuentas-por-cobrar', [PERMISOS.cxcConsultar]); await screen.findByLabelText(/^Cliente \(ID\)/); expect(api.mock.calls.some(([r]) => r.startsWith('/api/clientes'))).toBe(false)
  })
  it('historial usa nombre real, fecha zonificada y enlaces; oculta UUID/versiones', async () => {
    cuenta = aplicarPagoDemo(cuenta, 3, 'uuid-oculta'); navegar(); const tabla = await screen.findByRole('table', { name: 'Historial financiero' })
    for (const texto of ['Pago #70', 'admin', 'Movimiento #170', 'Abono confirmado', 'Efectivo']) expect(within(tabla).getByText(texto)).toBeDefined()
    expect(tabla.textContent).toContain(mostrarInstante('2026-10-14T20:31:27.275Z')); expect(screen.queryByText('uuid-oculta')).toBeNull(); expect(screen.queryByText('CUENTA-V2')).toBeNull()
  })
  it.each(['PAGADA', 'ANULADA', 'HISTORICA', 'SALDO_CERO'])('cuenta %s no ofrece pago', async (estado) => {
    if (estado === 'HISTORICA') cuenta.encabezado.operable = false
    else if (estado === 'SALDO_CERO') cuenta.encabezado.saldoPendiente = 0
    else cuenta.encabezado.estado = estado
    navegar(); await screen.findByText('VT-0000000012', { selector: 'h2' }); expect(screen.queryByRole('button', { name: 'Registrar abono' })).toBeNull()
  })
  it.each([PERMISOS.cxcCobrar, PERMISOS.cajaCobrar, PERMISOS.cajaOperar, PERMISOS.cajaConsultar])('abono no ofrecido sin %s', async (faltante) => {
    navegar(undefined, permisos.filter((p) => p !== faltante)); await screen.findByText('Sin pagos registrados'); expect(screen.queryByRole('button', { name: 'Registrar abono' })).toBeNull()
  })
  it.each([403, 404])('maneja HTTP %s sin stack', async (estado) => {
    api.mockRejectedValue(new ErrorApi('Interno', estado)); navegar(); await screen.findByText(estado === 403 ? 'Acceso no autorizado' : 'Página no encontrada')
  })
  it('ID inválido no consulta API', async () => { navegar('/cuentas-por-cobrar/no-valido'); await screen.findByText('Página no encontrada'); expect(api).not.toHaveBeenCalled() })
})

describe('pagos, concurrencia e idempotencia', () => {
  it.each(['cobrar', 'pagar'] as const)('registra %s con versiones actuales y refresca detalle', async (tipo) => {
    navegar(`/cuentas-por-${tipo}/1`); await prepararPago(tipo); expect(screen.getByText('GTQ 5.00')).toBeDefined(); await confirmarPago()
    await screen.findByRole('table', { name: 'Historial financiero' }); expect(body()).toMatchObject({ versionFila: 'CUENTA-V1', versionCaja: 'CAJA-V1', sesionCajaId: 7, formaPagoId: 21, monto: 3 })
    expect(body().claveOperacion).toMatch(/^[\da-f-]{36}$/i); expect(sessionStorage.length).toBe(0)
    fireEvent.click(screen.getByRole('link', { name: 'Volver a cuentas' })); await screen.findByRole('table', { name: `Cuentas por ${tipo}` }); expect(screen.getByRole('region', { name: 'Resumen GTQ' }).textContent).toContain('GTQ 5.00')
  })
  it('CxC permite tarjeta, referencia segura y no solicita datos sensibles', async () => {
    navegar(); await prepararPago('cobrar', '3', '34'); expect(screen.queryByLabelText(/^(PAN|CVV|Número de tarjeta|Expiración)$/)).toBeNull()
    fireEvent.change(screen.getByLabelText(/^Referencia de autorización/), { target: { value: 'AUTH-17' } }); await confirmarPago(); await screen.findByText('Abono registrado correctamente.'); expect(body().referencia).toBe('AUTH-17')
  })
  it('CxP no ofrece tarjeta y muestra efectivo esperado', async () => {
    navegar('/cuentas-por-pagar/1'); await prepararPago('pagar'); expect(screen.queryByRole('option', { name: 'Tarjeta' })).toBeNull(); expect(screen.getByText(/Efectivo esperado: GTQ 50.00/)).toBeDefined()
  })
  it.each(['0', '-1', '8.01', '0.001'])('no envía monto inválido %s', async (valor) => {
    navegar(); await prepararPago(); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: valor } }); expect((screen.getByRole('button', { name: 'Revisar operación' }) as HTMLButtonElement).disabled).toBe(true); expect(escrituras()).toHaveLength(0)
  })
  it('doble clic no duplica el POST', async () => {
    let resolver!: (r: unknown) => void; const previo = api.getMockImplementation()!
    api.mockImplementation((r, o) => o?.method ? new Promise((resolve) => { resolver = resolve }) as never : previo(r, o))
    navegar(); await prepararPago(); fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); const b = screen.getByRole('button', { name: 'Confirmar operación' }); fireEvent.click(b); fireEvent.click(b)
    await waitFor(() => expect(escrituras()).toHaveLength(1)); await act(async () => resolver({ pagoId: 70, cuenta, reintento: false }))
  })
  it('timeout conserva EXACTAMENTE UUID y payload aunque cuenta y caja cambien; reintento true es éxito', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((r, o) => o?.method ? Promise.reject(new TypeError('Timeout')) : previo(r, o))
    navegar(); await prepararPago(); await confirmarPago(); await screen.findByRole('button', { name: 'Reintentar misma operación' })
    const original = body(); expect(sessionStorage.getItem(sessionStorage.key(0)!)).toBe(original.claveOperacion)
    cuenta = aplicarPagoDemo(cuenta, 8, original.claveOperacion); caja.encabezado.versionFila = 'VERSION-NUEVA'
    api.mockImplementation((r, o) => o?.method ? Promise.resolve({ pagoId: 70, cuenta, reintento: true }) as never : previo(r, o))
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar misma operación' })); await screen.findByText('Operación ya registrada. Resultado recuperado sin duplicarla.')
    expect(escrituras()).toHaveLength(2); expect(body()).toEqual(original); expect(sessionStorage.length).toBe(0)
  })
  it('resultado incierto puede conciliar por UUID exacta sin otro POST', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation(async (r, o) => { if (o?.method) { const d = JSON.parse(String(o.body)); cuenta = aplicarPagoDemo(cuenta, d.monto, d.claveOperacion); throw new TypeError('Respuesta perdida') } return previo(r, o) })
    navegar(); await prepararPago(); await confirmarPago(); fireEvent.click(await screen.findByRole('button', { name: 'Consultar resultado' })); await screen.findByText('Operación localizada en el historial. No se envió otro pago.'); expect(escrituras()).toHaveLength(1)
  })
  it.each(['CONCURRENCIA_CXC', 'CONCURRENCIA_CAJA', 'CUENTA_YA_PAGADA', 'ABONO_SUPERA_SALDO', 'CLAVE_OPERACION_CONFLICTIVA', 'CAJA_NO_ABIERTA'])('expone %s y recarga sin reenvío', async (codigo) => {
    fallo(codigo); navegar(); await prepararPago(); await confirmarPago(); await screen.findByText(new RegExp(`\\(${codigo}\\)`)); expect(escrituras()).toHaveLength(1); expect(api.mock.calls.filter(([r]) => r === '/api/cuentas-por-cobrar/1').length).toBeGreaterThanOrEqual(4)
  })
  it.each(['PAGO_SUPERA_SALDO', 'SALDO_INSUFICIENTE', 'CONCURRENCIA_CXP'])('CxP muestra %s sin nuevo pago', async (codigo) => {
    fallo(codigo); navegar('/cuentas-por-pagar/1'); await prepararPago('pagar'); await confirmarPago(); await screen.findByText(new RegExp(`\\(${codigo}\\)`)); expect(escrituras()).toHaveLength(1)
  })
  it('detecta caja cerrada en lectura previa al POST', async () => {
    navegar(); await prepararPago(); caja.encabezado.estado = 'CERRADA'; fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' })); await screen.findByText(/\(CAJA_NO_ABIERTA\)/); expect(escrituras()).toHaveLength(0)
  })
  it('detecta versión cambiada antes de POST', async () => {
    navegar(); await prepararPago(); cuenta.encabezado.versionFila = 'OTRA'; fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' })); await screen.findByText(/\(CONCURRENCIA_CXC\)/); expect(escrituras()).toHaveLength(0)
  })
  it('marcador recuperado sin formulario bloquea nueva UUID y admite solo consulta', async () => {
    sessionStorage.setItem('heladeria:cartera:1:4:4:cobrar:1', 'uuid-recarga'); navegar(); fireEvent.click(await screen.findByRole('button', { name: 'Revisar operación pendiente' })); await screen.findByText(/La pestaña fue recargada/); expect(screen.queryByRole('button', { name: 'Revisar operación' })).toBeNull(); expect(screen.queryByRole('button', { name: 'Reintentar misma operación' })).toBeNull(); expect(escrituras()).toHaveLength(0)
  })
  it.each([403, 429])('rechazo HTTP %s en reintento no borra la incertidumbre anterior', async (estado) => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((r, o) => o?.method ? Promise.reject(new TypeError('Timeout')) : previo(r, o))
    navegar(); await prepararPago(); await confirmarPago(); await screen.findByRole('button', { name: 'Reintentar misma operación' })
    const original = body()
    api.mockImplementation((r, o) => o?.method ? Promise.reject(new ErrorApi('Rechazo anterior al servicio', estado)) : previo(r, o))
    fireEvent.click(screen.getByRole('button', { name: 'Reintentar misma operación' }))
    await waitFor(() => expect(escrituras()).toHaveLength(2)); expect(body()).toEqual(original)
    await waitFor(() => expect((screen.getByRole('button', { name: 'Reintentar misma operación' }) as HTMLButtonElement).disabled).toBe(false))
    expect(sessionStorage.getItem(sessionStorage.key(0)!)).toBe(original.claveOperacion)
  })
  it('salir durante la lectura previa no envía un POST fuera de contexto', async () => {
    const vista = navegar(); await prepararPago()
    let resolver!: (r: unknown) => void; const previo = api.getMockImplementation()!
    api.mockImplementation((r, o) => r === '/api/cuentas-por-cobrar/1' ? new Promise((resolve) => { resolver = resolve }) as never : previo(r, o))
    fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' }))
    vista.unmount(); await act(async () => resolver(cuenta)); expect(escrituras()).toHaveLength(0); expect(sessionStorage.length).toBe(0)
  })
  it('sin almacenamiento no envía un pago cuya UUID no puede recuperar', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('Storage no disponible') })
    navegar(); await prepararPago(); fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' }))
    await screen.findByText('No se pudo completar la operación'); expect(escrituras()).toHaveLength(0)
  })
  it('después de éxito otro abono consulta nuevamente la versión de Caja', async () => {
    navegar(); await prepararPago(); await confirmarPago(); await screen.findByText('Abono registrado correctamente.')
    await prepararPago(); fireEvent.click(screen.getByRole('button', { name: 'Revisar operación' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar operación' }))
    await waitFor(() => expect(escrituras()).toHaveLength(2)); expect(body().versionCaja).toBe('CAJA-V2'); expect(body().versionFila).toBe('CUENTA-V2')
  })
})

describe('factura de proveedor', () => {
  it('registra con versión fresca y muestra CxP creada', async () => {
    await prepararFactura(); fireEvent.click(screen.getByRole('button', { name: 'Revisar factura' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar factura' })); await screen.findByText('Factura registrada. Cuenta por pagar creada.')
    expect(JSON.parse(String(escrituras()[0][1]?.body))).toMatchObject({ ordenCompraId: 51, versionCompra: 'COMPRA-V1', numeroDocumento: 'F-2026-17', fechaDocumento: '2026-10-10', fechaVencimiento: '2026-10-30' }); expect(screen.getByRole('link', { name: 'Ver cuenta por pagar' }).getAttribute('href')).toBe('/cuentas-por-pagar/5')
  })
  it.each(['contado', 'parcial', 'otra-empresa', 'otra-sucursal', 'facturada'])('excluye compra %s', async (caso) => {
    if (caso === 'contado') compra.encabezado.esCredito = false
    if (caso === 'parcial') compra.encabezado.estado = 'PARCIALMENTE_RECIBIDA'
    if (caso === 'otra-empresa') compra.encabezado.empresaId = 99
    if (caso === 'otra-sucursal') compra.encabezado.sucursalId = 99
    if (caso === 'facturada') cuentasPagar = [{ ...cuenta.encabezado, compraId: 51 }]
    navegar('/cuentas-por-pagar/registrar-factura'); await screen.findByText('No hay compras candidatas'); expect(escrituras()).toHaveLength(0)
  })
  it('exclusión de facturas recorre todas las páginas', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((r, o) => r.startsWith('/api/cuentas-por-pagar?') ? Promise.resolve({ ...paginaCaja(new URL(r, 'http://test').searchParams.get('pagina') === '2' ? [{ ...cuenta.encabezado, compraId: 51 }] : []), totalPaginas: 2 }) as never : previo(r, o))
    const r = await comprasFacturables(4, 4); expect(r.compras).toHaveLength(0); expect(api.mock.calls.some(([r]) => r.includes('pagina=2'))).toBe(true)
  })
  it('duplicado aparecido después de selección no envía factura', async () => {
    await prepararFactura(); cuentasPagar = [{ ...cuenta.encabezado, compraId: 51 }]; fireEvent.click(screen.getByRole('button', { name: 'Revisar factura' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar factura' })); await screen.findByText(/DOCUMENTO_FINANCIERO_DUPLICADO/); expect(escrituras()).toHaveLength(0)
  })
  it.each(['DOCUMENTO_FINANCIERO_DUPLICADO', 'VALIDACION_CXP'])('maneja rechazo backend %s', async (codigo) => {
    fallo(codigo); await prepararFactura(); fireEvent.click(screen.getByRole('button', { name: 'Revisar factura' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar factura' })); await screen.findByText(new RegExp(`\\(${codigo}\\)`)); expect(escrituras()).toHaveLength(1)
  })
  it('timeout de factura conserva misma solicitud y UUID', async () => {
    const previo = api.getMockImplementation()!; api.mockImplementation((r, o) => o?.method ? Promise.reject(new TypeError('Timeout')) : previo(r, o))
    await prepararFactura(); fireEvent.click(screen.getByRole('button', { name: 'Revisar factura' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar factura' })); await screen.findByRole('button', { name: 'Reintentar misma factura' })
    const original = escrituras()[0][1]?.body; compra.encabezado.versionFila = 'OTRA'; api.mockImplementation(previo); fireEvent.click(screen.getByRole('button', { name: 'Reintentar misma factura' })); await screen.findByText('Factura registrada. Cuenta por pagar creada.'); expect(escrituras()[1][1]?.body).toBe(original)
  })
})

describe('utilidades financieras visuales', () => {
  it.each(Object.keys(mensajesCartera))('código %s tiene explicación legible', (codigo) => { expect(mensajesCartera[codigo].length).toBeGreaterThan(25); expect(mensajesCartera[codigo]).not.toBe(codigo) })
  it.each(['4111111111111111', '4111-1111-1111-1111', 'CVV-123', 'exp 10/29'])('rechaza referencia sensible %s', (r) => expect(referenciaInsegura(r)).toBe(true))
  it('acepta autorización corta sin almacenarla', () => expect(referenciaInsegura('AUTH-17')).toBe(false))
})
