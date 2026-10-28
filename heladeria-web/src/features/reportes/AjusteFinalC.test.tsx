import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { EnrutadorAplicacion } from '../../rutas/EnrutadorAplicacion'
import { respuestaJson } from '../../test/datosAutenticacion'
import { diarioDemo, ticketDemo } from '../../test/datosAjusteC'
import { renderSeguridad } from '../../test/renderSeguridad'
import { ComprobanteVenta } from '../ventas/PaginaComprobanteVenta'
import { DetalleVenta } from '../ventas/PaginaDetalleVenta'
import { PaginaReporteDiario } from './PaginaReporteDiario'
import { mostrarInstante } from '../../utils/fechas'

const permisos = [PERMISOS.reportesConsultar, PERMISOS.ventasConsultar]
afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks() })
function simular(datos: unknown = diarioDemo, estado = 200) {
  const peticiones: URL[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: URL, opciones?: RequestInit) => {
    expect(opciones?.method ?? 'GET').toBe('GET'); peticiones.push(new URL(String(url)))
    return respuestaJson(datos, estado)
  }))
  return peticiones
}
const cambiar = (nombre: string, value: string) => fireEvent.change(screen.getByLabelText(nombre), { target: { value } })
describe('Ajuste C · comprobante', () => {
  it('datos históricos, cliente nullable y pagos mixtos con vuelto real', () => {
    const fetch = vi.fn(); vi.stubGlobal('fetch', fetch)
    renderSeguridad(<ComprobanteVenta venta={ticketDemo} />, permisos)
    const ticket = screen.getByRole('article', { name: 'Comprobante de venta' })
    expect(ticket.textContent).toContain('Heladería Inicial'); expect(ticket.textContent).toContain('Venta sin cliente identificado')
    expect(ticket.textContent).toContain('2 L × GTQ 25.00'); expect(ticket.textContent).toContain('VueltoGTQ 5.00')
    expect(ticket.textContent).toContain('TarjetaGTQ 30.00'); expect(ticket.textContent).toContain('Comprobante interno — no constituye documento tributario')
    expect(ticket.textContent).toContain(mostrarInstante(ticketDemo.encabezado.fechaCompletada, true, 'America/Guatemala'))
    expect(fetch).not.toHaveBeenCalled()
  })
  it('crédito muestra saldo original y cliente sin pago ficticio', () => {
    const venta = structuredClone(ticketDemo)
    Object.assign(venta.encabezado, { esCredito: true, cliente: 'Cliente identificado' })
    Object.assign(venta.comprobante!, { codigoCliente: 'C-01', saldoInicialCredito: 50, cuentaPorCobrarId: 3, pagos: [] })
    renderSeguridad(<ComprobanteVenta venta={venta} />, permisos)
    expect(screen.getByText('Crédito')).toBeDefined(); expect(screen.getByText('C-01')).toBeDefined()
    expect(screen.getByText('Saldo inicial CxC')).toBeDefined(); expect(screen.queryByText('Vuelto')).toBeNull()
    expect(screen.queryByLabelText('Pagos del comprobante')).toBeNull()
  })
  it('anulada es evidente y conserva importes', () => {
    const venta = structuredClone(ticketDemo); Object.assign(venta.encabezado, { estado: 'ANULADA', fechaAnulacion: '2026-10-08T17:00:00Z', motivoAnulacion: 'Error en pedido' })
    renderSeguridad(<ComprobanteVenta venta={venta} />, permisos)
    expect(screen.getByText('ANULADA').className).toBe('comprobante-anulada'); expect(screen.getByText(/Error en pedido/)).toBeDefined()
    expect(screen.getAllByText('GTQ 50.00')).toHaveLength(3)
  })
  it.each(['BORRADOR', 'CONFIRMADA'])('%s no se imprime como definitivo', estado => {
    renderSeguridad(<ComprobanteVenta venta={{ ...ticketDemo, encabezado: { ...ticketDemo.encabezado, estado } }} />, permisos)
    expect(screen.getByText('Comprobante no disponible')).toBeDefined(); expect(screen.queryByText('Imprimir comprobante')).toBeNull()
  })
  it('histórico sin datos suficientes no inventa ticket', () => {
    renderSeguridad(<ComprobanteVenta venta={{ ...ticketDemo, comprobante: null }} />, permisos)
    expect(screen.getByText('Comprobante no disponible')).toBeDefined()
  })
  it('imprime únicamente por acción y sin llamadas de escritura', () => {
    const imprimir = vi.spyOn(window, 'print').mockImplementation(() => {}); const fetch = vi.fn(); vi.stubGlobal('fetch', fetch)
    renderSeguridad(<ComprobanteVenta venta={ticketDemo} />, permisos)
    expect(imprimir).not.toHaveBeenCalled(); fireEvent.click(screen.getByRole('button', { name: 'Imprimir comprobante' }))
    expect(imprimir).toHaveBeenCalledOnce(); expect(fetch).not.toHaveBeenCalled()
  })
  it('ruta limpia reutiliza GET sin navegación lateral', async () => {
    const llamadas = simular(ticketDemo)
    renderSeguridad(<EnrutadorAplicacion />, permisos, '/ventas/2/comprobante')
    await screen.findByRole('article'); expect(llamadas.map(x => x.pathname)).toEqual(['/api/ventas/2'])
    expect(screen.queryByRole('navigation', { name: 'Navegación principal' })).toBeNull()
  })
  it('detalle compartido con resultado POS ofrece enlace', () => {
    renderSeguridad(<DetalleVenta inicial={ticketDemo} />, permisos)
    expect(screen.getByRole('link', { name: 'Ver comprobante' }).getAttribute('href')).toBe('/ventas/2/comprobante')
  })
})
describe('Ajuste C · diario', () => {
  it('día predeterminado del backend, resumen y dimensiones independientes', async () => {
    const llamadas = simular(); renderSeguridad(<PaginaReporteDiario />, permisos)
    await screen.findByRole('table', { name: 'Ventas del día' })
    expect(llamadas[0].searchParams.has('fecha')).toBe(false)
    expect((screen.getByLabelText('Fecha') as HTMLInputElement).value).toBe('2026-10-07')
    expect(screen.getByLabelText('Día consultado').textContent).toContain('America/Guatemala')
    expect(screen.getByText(/Estas dimensiones no se suman/)).toBeDefined()
    expect(screen.getByRole('table', { name: 'Operaciones financieras del día' })).toBeDefined()
    expect(screen.queryByText('Total consolidado')).toBeNull()
  })
  it('aplica todos los filtros en URL, no consulta al escribir y limpiar los elimina', async () => {
    const llamadas = simular(); renderSeguridad(<PaginaReporteDiario />, permisos)
    await screen.findByRole('table', { name: 'Ventas del día' })
    cambiar('Fecha', '2026-10-09'); cambiar('Caja (ID opcional)', '7'); cambiar('Forma de pago (ID opcional)', '2'); cambiar('Modalidad', 'contado')
    expect(llamadas).toHaveLength(1); fireEvent.click(screen.getByRole('button', { name: 'Aplicar' }))
    await waitFor(() => expect(llamadas).toHaveLength(2))
    expect(Object.fromEntries(llamadas[1].searchParams)).toEqual({ fecha: '2026-10-09', sucursalId: '4', cajaId: '7', formaPagoId: '2', esCredito: 'false', pagina: '1', tamanioPagina: '20', paginaOperaciones: '1', paginaCajas: '1' })
    await screen.findByRole('table', { name: 'Ventas del día' }); fireEvent.click(screen.getByRole('button', { name: 'Limpiar' }))
    await waitFor(() => expect(llamadas).toHaveLength(3)); expect(llamadas[2].searchParams.has('fecha')).toBe(false); expect(llamadas[2].searchParams.has('esCredito')).toBe(false)
  })
  it('páginas independientes conservan filtros aplicados y día efectivo', async () => {
    const datos = structuredClone(diarioDemo); datos.ventas.totalPaginas = 2; datos.ventas.totalRegistros = 40; datos.operaciones.totalPaginas = 2; datos.operaciones.totalRegistros = 40
    const llamadas = simular(datos); renderSeguridad(<PaginaReporteDiario />, permisos)
    await screen.findByRole('table', { name: 'Ventas del día' }); cambiar('Modalidad', 'credito')
    fireEvent.click(within(screen.getByLabelText('Paginación de ventas')).getByRole('button', { name: /siguiente/i }))
    await waitFor(() => expect(llamadas).toHaveLength(2)); expect(llamadas[1].searchParams.get('pagina')).toBe('2'); expect(llamadas[1].searchParams.get('fecha')).toBe('2026-10-07'); expect(llamadas[1].searchParams.has('esCredito')).toBe(false)
    await screen.findByRole('table', { name: 'Ventas del día' }); fireEvent.click(within(screen.getByLabelText('Paginación de operaciones')).getByRole('button', { name: /siguiente/i }))
    await waitFor(() => expect(llamadas).toHaveLength(3)); expect(llamadas[2].searchParams.get('pagina')).toBe('2'); expect(llamadas[2].searchParams.get('paginaOperaciones')).toBe('2')
  })
  it('vacío sin totales ficticios y sin enlaces', async () => {
    const datos = structuredClone(diarioDemo); datos.ventasTotales = []; datos.cobrosPorForma = []; datos.creditosGenerados = []; datos.finanzas = []
    for (const k of ['ventas', 'operaciones', 'cajas'] as const) Object.assign(datos[k], { elementos: [], totalRegistros: 0, totalPaginas: 0 })
    simular(datos); renderSeguridad(<PaginaReporteDiario />, permisos)
    await screen.findByText('Sin ventas realizadas para este día y filtros.'); expect(screen.queryByText('Ver comprobante')).toBeNull()
  })
  it('impresión incluye solo páginas visibles y no vuelve a consultar', async () => {
    const llamadas = simular(), imprimir = vi.spyOn(window, 'print').mockImplementation(() => {})
    renderSeguridad(<PaginaReporteDiario />, permisos); await screen.findByRole('table', { name: 'Ventas del día' })
    expect(screen.getByText(/solo las páginas de detalle visibles/)).toBeDefined(); expect(imprimir).not.toHaveBeenCalled()
    fireEvent.click(screen.getByRole('button', { name: 'Imprimir reporte' })); expect(imprimir).toHaveBeenCalledOnce(); expect(llamadas).toHaveLength(1)
  })
  it.each([400, 401, 403, 500])('error %s no ofrece imprimir datos previos', async estado => {
    simular({ exito: false, mensaje: 'Filtros inválidos', codigo: 'ERROR_CONTROLADO' }, estado)
    renderSeguridad(<PaginaReporteDiario />, permisos); await screen.findByRole('alert')
    expect(screen.queryByRole('table')).toBeNull(); expect((screen.getByRole('button', { name: 'Imprimir reporte' }) as HTMLButtonElement).disabled).toBe(true)
  })
  it('consulta de reporte no concede enlaces de ventas', async () => {
    simular(); renderSeguridad(<PaginaReporteDiario />, [PERMISOS.reportesConsultar]); await screen.findByRole('table', { name: 'Ventas del día' })
    expect(screen.queryByRole('link', { name: 'Ver comprobante' })).toBeNull(); expect(screen.queryByRole('link', { name: 'VT-0000000002' })).toBeNull()
  })
  it.each(['/reportes/diario', '/ventas/2/comprobante'])('protege ruta %s antes de consultar', async ruta => {
    const fetch = vi.fn(); vi.stubGlobal('fetch', fetch); renderSeguridad(<EnrutadorAplicacion />, [], ruta)
    await screen.findByText('Acceso no autorizado'); expect(fetch).not.toHaveBeenCalled()
  })
})
