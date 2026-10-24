import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { solicitar, ErrorApi } from '../../api/clienteHttp'
import { PERMISOS } from '../../auth/permisos'
import { renderSeguridad } from '../../test/renderSeguridad'
import { referenciasCaja, sesionCaja, sesionCerrada, paginaCaja, cajaDemo } from '../../test/datosFaseVisual6'
import { ventaConfirmada, ventaCompletada } from '../../test/datosFaseVisual5'
import type { VentaConsulta } from '../../types/ventas'
import { PaginaAbrirCaja } from './PaginaAbrirCaja'
import { PaginaCaja } from './PaginaCaja'
import { PaginaSesionCaja } from './PaginaSesionCaja'
import { PaginaDetalleVenta } from '../ventas/PaginaDetalleVenta'
import { completarConReconciliacion } from '../ventas/useDatosVentas'
import { TipoMovimientoCaja } from './ComponentesCaja'

vi.mock('../../api/clienteHttp', async (original) => ({ ...(await original<typeof import('../../api/clienteHttp')>()), solicitar: vi.fn() }))
const api = vi.mocked(solicitar)
const permisos = [PERMISOS.cajaConsultar, PERMISOS.cajaAbrir, PERMISOS.cajaOperar, PERMISOS.cajaCobrar,
  PERMISOS.cajaMovimientoManual, PERMISOS.cajaCerrar, PERMISOS.ventasConsultar, PERMISOS.ventasVender, PERMISOS.posOperar, PERMISOS.ventasAnular]
let venta: VentaConsulta
const posts = () => api.mock.calls.filter(([, o]) => o?.method === 'POST')
const body = () => JSON.parse(String(posts().at(-1)?.[1]?.body))
const navegar = (ruta: string, roles = permisos) => renderSeguridad(<Routes>
  <Route path="/caja" element={<PaginaCaja />} /><Route path="/caja/abrir" element={<PaginaAbrirCaja />} />
  <Route path="/caja/sesiones/:id" element={<PaginaSesionCaja />} /><Route path="/ventas/:id" element={<PaginaDetalleVenta />} />
</Routes>, roles, ruta)
beforeEach(() => {
  api.mockReset(); sessionStorage.clear(); venta = structuredClone(ventaConfirmada)
  api.mockImplementation(async (ruta, opciones) => {
    if (ruta.startsWith('/api/caja/referencias')) return referenciasCaja as never
    if (ruta.startsWith('/api/caja/cajas')) return paginaCaja([cajaDemo]) as never
    if (ruta.startsWith('/api/caja/sesiones?')) return paginaCaja([sesionCaja.encabezado]) as never
    if (ruta === '/api/caja/sesiones/7' || ruta === '/api/caja/sesiones/abrir') return sesionCaja as never
    if (ruta === '/api/ventas/2/completar') {
      const enviado = JSON.parse(String(opciones?.body))
      venta = { ...structuredClone(ventaCompletada), encabezado: { ...venta.encabezado, estado: 'COMPLETADA', claveOperacion: enviado.claveOperacion },
        cobro: enviado.cobro ? { pagoId: 91, sesionCajaId: enviado.cobro.sesionCajaId, claveOperacion: enviado.claveOperacion, pagos: enviado.cobro.pagos } : null }
      return venta as never
    }
    if (ruta === '/api/ventas/2') return venta as never
    throw new Error('Petición no prevista: ' + ruta)
  })
})
async function iniciarCobro() {
  navegar('/ventas/2'); fireEvent.click(await screen.findByRole('button', { name: 'Completar venta' }))
  return screen.findByLabelText('Forma de pago 1')
}
describe('saldo inicial del último cierre', () => {
  it.each([100, 98, 0])('carga automáticamente contado %s y permite editar sin calcularlo en UI', async (contado) => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, o) => ruta === '/api/caja/referencias?cajaId=9'
      ? Promise.resolve({ ...referenciasCaja, saldoUltimoCierre: { sesionId: 3, cajaId: 9, numeroCierre: 'CC-ANTERIOR', fechaCierre: '2026-10-22T21:00:00Z', efectivoContado: contado } }) as never : previo(ruta, o))
    navegar('/caja/abrir?cajaId=9')
    const saldo = await screen.findByLabelText('Saldo inicial') as HTMLInputElement
    expect(saldo.value).toBe(contado.toFixed(2)); expect(screen.getByText(/Saldo del último cierre/)).toBeDefined()
    expect(screen.getByText(/CC-ANTERIOR/)).toBeDefined()
    fireEvent.change(saldo, { target: { value: '23.50' } }); fireEvent.click(screen.getByRole('button', { name: 'Confirmar apertura' }))
    await waitFor(() => expect(posts()).toHaveLength(1)); expect(body()).toEqual({ cajaId: 9, monedaId: referenciasCaja.monedaId, saldoInicial: 23.5 })
  })
  it('sin histórico permite apertura manual y no inventa un cierre', async () => {
    navegar('/caja/abrir?cajaId=9'); await screen.findByLabelText('Saldo inicial')
    expect(screen.getByText(/Sin cierre anterior válido/)).toBeDefined(); expect(screen.queryByText(/Saldo del último cierre:/)).toBeNull()
  })
  it('una respuesta anterior no sustituye el saldo de otra caja seleccionada', async () => {
    const previo = api.getMockImplementation()!; let resolver!: (v: unknown) => void
    api.mockImplementation((ruta, o) => {
      if (ruta === '/api/caja/referencias?cajaId=9') return new Promise((r) => { resolver = r }) as never
      if (ruta.startsWith('/api/caja/cajas')) return Promise.resolve(paginaCaja([{ ...cajaDemo, id: 10, nombre: 'Otra caja' }])) as never
      return previo(ruta, o)
    })
    navegar('/caja/abrir?cajaId=9')
    fireEvent.click(await screen.findByRole('button', { name: /^Caja/ }))
    fireEvent.click(await screen.findByRole('option', { name: /Otra caja/ }))
    const saldo = await screen.findByLabelText('Saldo inicial') as HTMLInputElement
    fireEvent.change(saldo, { target: { value: '12' } })
    await act(async () => resolver({ ...referenciasCaja, saldoUltimoCierre: { sesionId: 1, cajaId: 9, numeroCierre: 'ANTERIOR', fechaCierre: '2026-10-20T12:00:00Z', efectivoContado: 999 } }))
    expect(saldo.value).toBe('12'); expect(screen.queryByText(/Saldo del último cierre/)).toBeNull()
    expect(api.mock.calls.find(([ruta]) => ruta === '/api/caja/referencias?cajaId=9')?.[1]?.signal?.aborted).toBe(true)
    expect(posts()).toHaveLength(0)
  })
})
describe('cierre y cobro único en Ventas', () => {
  it.each(['efectivo', 'tarjeta', 'mixto'])('%s envía cobro en el mismo POST completar, nunca otro POST de Caja', async (tipo) => {
    const forma = await iniciarCobro(); const efectivo = referenciasCaja.formasPago.find((f) => f.esEfectivo)!
    const tarjeta = referenciasCaja.formasPago.find((f) => !f.esEfectivo)!
    fireEvent.change(forma, { target: { value: String(tipo === 'tarjeta' ? tarjeta.id : efectivo.id) } })
    if (tipo === 'efectivo') fireEvent.change(screen.getByLabelText('Monto recibido 1'), { target: { value: String(venta.encabezado.total + 20) } })
    if (tipo === 'tarjeta') fireEvent.change(screen.getByLabelText(/Referencia\/autorización.*1/), { target: { value: 'AUT-123' } })
    if (tipo === 'mixto') {
      fireEvent.change(screen.getByLabelText('Monto aplicado 1'), { target: { value: '10' } })
      fireEvent.change(screen.getByLabelText('Monto recibido 1'), { target: { value: '20' } })
      fireEvent.click(screen.getByRole('button', { name: 'Agregar forma de pago' }))
      fireEvent.change(screen.getByLabelText('Monto aplicado 2'), { target: { value: String(venta.encabezado.total - 10) } })
      fireEvent.change(screen.getByLabelText(/Referencia\/autorización.*2/), { target: { value: 'AUT-456' } })
    }
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar venta' })); await screen.findByText('COMPLETADA')
    expect(posts()).toHaveLength(1); expect(posts()[0][0]).toBe('/api/ventas/2/completar')
    expect(body().cobro).toMatchObject({ sesionCajaId: sesionCaja.encabezado.id, versionCaja: sesionCaja.encabezado.versionFila })
    expect(body().cobro.pagos).toHaveLength(tipo === 'mixto' ? 2 : 1)
    expect(screen.queryByRole('button', { name: 'Anular venta' })).toBeNull()
    expect(sessionStorage.length).toBe(1); expect(sessionStorage.getItem(sessionStorage.key(0)!)).toMatch(/^[\da-f-]{36}$/i)
  })
  it('no presupone efectivo ni permite completar con formas vacías', async () => {
    const forma = await iniciarCobro(); expect((forma as HTMLSelectElement).value).toBe('')
    fireEvent.submit(forma.closest('form')!); await screen.findByText(/Seleccione una sesión abierta y complete/)
    expect(posts()).toHaveLength(0)
  })
  it('sin sesión abierta no crea venta financiera parcial', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, o) => ruta.startsWith('/api/caja/sesiones?') ? Promise.resolve(paginaCaja([])) as never : previo(ruta, o))
    navegar('/ventas/2'); fireEvent.click(await screen.findByRole('button', { name: 'Completar venta' }))
    await screen.findByText(/No hay una sesión abierta para cobrar/); fireEvent.click(screen.getByRole('button', { name: 'Cerrar venta' }))
    expect(posts()).toHaveLength(0); expect(screen.getByText('CONFIRMADA')).toBeDefined()
  })
  it('crédito no carga Caja ni adjunta cobro', async () => {
    venta.encabezado.esCredito = true
    navegar('/ventas/2', [PERMISOS.ventasConsultar, PERMISOS.ventasVender, PERMISOS.posOperar])
    fireEvent.click(await screen.findByRole('button', { name: 'Completar venta' }))
    expect(screen.getByText(/se genera CxC/)).toBeDefined(); fireEvent.click(screen.getByRole('button', { name: 'Cerrar venta' }))
    await screen.findByText('COMPLETADA'); expect(body().cobro).toBeUndefined()
    expect(api.mock.calls.some(([ruta]) => ruta.startsWith('/api/caja'))).toBe(false)
  })
  it('contado de total cero conserva cierre sin pagos, turno ni permisos adicionales de Caja', async () => {
    venta.encabezado.total = 0
    navegar('/ventas/2', [PERMISOS.ventasConsultar, PERMISOS.ventasVender, PERMISOS.posOperar])
    fireEvent.click(await screen.findByRole('button', { name: 'Completar venta' }))
    expect(screen.getByText(/Sin importe a cobrar/)).toBeDefined()
    fireEvent.click(screen.getByRole('button', { name: 'Cerrar venta' }))
    await screen.findByText('COMPLETADA'); expect(body().cobro).toBeUndefined(); expect(posts()).toHaveLength(1)
    expect(api.mock.calls.some(([ruta]) => ruta.startsWith('/api/caja'))).toBe(false)
  })
  it.each([PERMISOS.cajaOperar, PERMISOS.cajaCobrar])('sin %s no ofrece completar contado', async (falta) => {
    navegar('/ventas/2', permisos.filter((p) => p !== falta)); await screen.findByText('CONFIRMADA')
    expect(screen.queryByRole('button', { name: 'Completar venta' })).toBeNull(); expect(posts()).toHaveLength(0)
  })
  it('reconciliación rechaza cobro distinto bajo UUID conservada sin repetir POST', async () => {
    const uuid = crypto.randomUUID(); sessionStorage.setItem('cierre', uuid)
    api.mockResolvedValue({ ...ventaCompletada, encabezado: { ...ventaCompletada.encabezado, claveOperacion: uuid },
      cobro: { pagoId: 1, sesionCajaId: 7, claveOperacion: uuid, pagos: [{ formaPagoId: 21, montoAplicado: 50, montoRecibido: 50 }] } })
    await expect(completarConReconciliacion(ventaConfirmada, 'cierre', { sesionCajaId: 7, versionCaja: 'V1', pagos: [{ formaPagoId: 21, montoAplicado: 50, montoRecibido: 100 }] })).rejects.toBeInstanceOf(ErrorApi)
    expect(posts()).toHaveLength(0)
  })
})
describe('operación de Caja sin segunda aprobación', () => {
  it('distingue apertura, ingreso y egreso sin etiquetar apertura como ingreso operativo', () => {
    renderSeguridad(<><TipoMovimientoCaja tipo="APERTURA_CAJA" /><TipoMovimientoCaja tipo="INGRESO_CAJA" /><TipoMovimientoCaja tipo="EGRESO_CAJA" /></>, permisos)
    const apertura = screen.getByText('Apertura'), ingreso = screen.getByText('Ingresos manuales'), egreso = screen.getByText('Egresos manuales')
    expect(new Set([apertura.className, ingreso.className, egreso.className]).size).toBe(3)
  })
  it('prioriza cuatro acciones y mantiene cobros históricos en sección secundaria cerrada', async () => {
    navegar('/caja/sesiones/7'); await screen.findByRole('table', { name: 'Movimientos de caja' })
    for (const nombre of ['Registrar ingreso', 'Registrar egreso', 'Registrar arqueo', 'Cerrar sesión de caja']) expect(screen.getByRole('button', { name: nombre })).toBeDefined()
    expect(screen.queryByRole('button', { name: 'Cobrar venta' })).toBeNull()
    expect(screen.getByText('Cobros anteriores pendientes').closest('details')?.open).toBe(false)
    expect(screen.queryByRole('button', { name: /Validar venta|Aprobar cobro|Confirmar venta/ })).toBeNull()
  })
  it('acceso directo a egreso preselecciona EGRESO sin escribir hasta confirmar', async () => {
    navegar('/caja/sesiones/7?accion=egreso')
    expect((await screen.findByLabelText('Tipo de movimiento') as HTMLSelectElement).value).toBe('EGRESO'); expect(posts()).toHaveLength(0)
  })
  it('Caja cerrada conserva contado, diferencia e histórico sin acciones de escritura', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, o) => ruta === '/api/caja/sesiones/7' ? Promise.resolve(sesionCerrada) as never : previo(ruta, o))
    navegar('/caja/sesiones/7'); await screen.findByText('Cierre confirmado')
    expect(screen.getByText('Efectivo contado final')).toBeDefined(); expect(screen.queryByRole('button', { name: 'Registrar ingreso' })).toBeNull()
  })
  it('sin turno prioriza abrir caja y mantiene permisos', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, o) => ruta.startsWith('/api/caja/sesiones?') ? Promise.resolve(paginaCaja([])) as never : previo(ruta, o))
    const vista = navegar('/caja'); expect(await screen.findByRole('link', { name: 'Abrir caja ahora' })).toBeDefined(); vista.unmount()
    navegar('/caja', [PERMISOS.cajaConsultar]); await screen.findByText('CERRADA'); expect(screen.queryByRole('link', { name: 'Abrir caja ahora' })).toBeNull()
  })
})
