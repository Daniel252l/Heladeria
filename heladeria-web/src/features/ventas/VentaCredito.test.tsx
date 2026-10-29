import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { respuestaJson } from '../../test/datosAutenticacion'
import { paginaVentas, productoPos, referenciasVentas, ventaBorrador } from '../../test/datosFaseVisual5'
import { renderSeguridad } from '../../test/renderSeguridad'
import type { VentaConsulta } from '../../types/ventas'
import { configurarZonaHoraria, mostrarFechaCivil } from '../../utils/fechas'
import { PaginaDetalleVenta } from './PaginaDetalleVenta'
import { PaginaPos } from './PaginaPos'

// La UI y el cliente HTTP son reales; solo fetch se sustituye para no tocar datos reales.
const transporte = vi.fn<typeof fetch>()
const cliente = { id: 27, codigo: 'CLI-27', nombre: 'Cliente de crédito' }
const vencimiento = '2099-10-31'
const conCredito = (): VentaConsulta => ({ ...structuredClone(ventaBorrador), encabezado: {
  ...ventaBorrador.encabezado, clienteId: cliente.id, cliente: cliente.nombre,
  esCredito: true, fechaVencimientoCredito: vencimiento,
} })
let venta = conCredito()
let errorGuardado = false
const escrituras = () => transporte.mock.calls.filter(([, opciones]) => ['POST', 'PUT'].includes(opciones?.method ?? ''))
const payload = () => JSON.parse(String(escrituras().at(-1)?.[1]?.body))
const cambiar = (etiqueta: string | RegExp, valor: string) => fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } })
const modalidad = (valor: 'CONTADO' | 'CREDITO') => fireEvent.click(screen.getByRole('button', { name: valor === 'CREDITO' ? 'Crédito' : 'Contado' }))
const guardar = () => fireEvent.click(screen.getByRole('button', { name: /^Guardar (borrador|cambios del borrador)$/ }))

beforeEach(() => {
  transporte.mockReset()
  errorGuardado = false
  venta = conCredito()
  configurarZonaHoraria('America/Guatemala')
  transporte.mockImplementation(async (entrada, opciones) => {
    const path = new URL(entrada instanceof Request ? entrada.url : String(entrada)).pathname
    if (path === '/api/ventas/referencias') return respuestaJson({ ...referenciasVentas, clientes: [cliente] })
    if (path === '/api/ventas/productos') return respuestaJson(paginaVentas([productoPos]))
    if (path === '/api/ventas/productos/1/presentaciones') return respuestaJson(productoPos)
    if (['POST', 'PUT'].includes(opciones?.method ?? '') && (path === '/api/ventas' || path === '/api/ventas/2')) {
      if (errorGuardado) return respuestaJson({ mensaje: 'Revise los datos del crédito.', codigo: 'VALIDACION_CXC' }, 400)
      const datos = JSON.parse(String(opciones?.body))
      venta = { ...venta, encabezado: { ...venta.encabezado, ...datos, versionFila: 'VERSION-2', cliente: datos.clienteId ? cliente.nombre : null } }
      return respuestaJson(venta)
    }
    if (path === '/api/ventas/2') return respuestaJson(venta)
    throw new Error(`Petición no prevista: ${opciones?.method ?? 'GET'} ${path}`)
  })
  vi.stubGlobal('fetch', transporte)
})
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers(); configurarZonaHoraria() })

function navegar(ruta = '/ventas/nueva') {
  return renderSeguridad(<Routes>
    <Route path="/ventas/nueva" element={<PaginaPos nueva />} />
    <Route path="/ventas/:id/editar" element={<PaginaPos />} />
    <Route path="/ventas/:id" element={<PaginaDetalleVenta />} />
    <Route path="/pos" element={<PaginaPos />} />
  </Routes>, Object.values(PERMISOS), ruta)
}
async function nueva(ruta = '/ventas/nueva') {
  navegar(ruta)
  const producto = await screen.findByRole('article', { name: 'Helado Chocolate' })
  fireEvent.change(within(producto).getByLabelText('Presentación · Helado Chocolate'), { target: { value: '1' } })
  fireEvent.click(within(producto).getByRole('button', { name: 'Agregar Helado Chocolate Litro' }))
}
async function elegirCliente() {
  fireEvent.click(screen.getByRole('button', { name: /^Cliente/ }))
  fireEvent.click(await screen.findByRole('option', { name: /Cliente de crédito/ }))
}
async function guardada() {
  await screen.findByRole('table', { name: 'Detalle de venta' })
  expect(escrituras()).toHaveLength(1)
}

describe('integración mínima de crédito Ventas/POS', () => {
  it('contado por defecto mantiene todos los campos anteriores y explicita false/null', async () => {
    await nueva()
    expect(screen.getByRole('button', { name: 'Contado' }).getAttribute('aria-pressed')).toBe('true')
    expect(screen.queryByLabelText(/Fecha de vencimiento/)).toBeNull()
    guardar(); await guardada()
    expect(escrituras()[0][1]?.method).toBe('POST')
    expect(payload()).toEqual({ sucursalId: 4, bodegaId: 8, ubicacionId: 16, clienteId: null, monedaId: 6,
      esCredito: false, fechaVencimientoCredito: null,
      detalles: [{ productoId: 1, productoPresentacionId: 1, cantidad: 1, porcentajeDescuento: 0 }] })
  })
  it.each(['/ventas/nueva', '/pos'])('%s envía crédito, cliente y vencimiento civil al endpoint existente', async (ruta) => {
    await nueva(ruta)
    modalidad('CREDITO'); await elegirCliente(); cambiar(/Fecha de vencimiento/, vencimiento)
    guardar(); await guardada()
    expect(new URL(String(escrituras()[0][0])).pathname).toBe('/api/ventas')
    expect(payload()).toMatchObject({ clienteId: 27, esCredito: true, fechaVencimientoCredito: '2099-10-31' })
    expect(screen.getByText('Crédito')).toBeDefined()
    expect(screen.getByText(mostrarFechaCivil(vencimiento))).toBeDefined()
  })
  it('crédito sin cliente bloquea el POST y conserva modalidad y fecha', async () => {
    await nueva(); modalidad('CREDITO'); cambiar(/Fecha de vencimiento/, vencimiento); guardar()
    expect(await screen.findByText('Seleccione un cliente para la venta a crédito.')).toBeDefined()
    expect(escrituras()).toHaveLength(0)
    expect(screen.getByRole('button', { name: 'Crédito' }).getAttribute('aria-pressed')).toBe('true')
    expect((screen.getByLabelText(/Fecha de vencimiento/) as HTMLInputElement).value).toBe(vencimiento)
    await elegirCliente()
    expect(screen.queryByText('Seleccione un cliente para la venta a crédito.')).toBeNull()
  })
  it('crédito sin vencimiento bloquea el POST y conserva el cliente', async () => {
    await nueva(); modalidad('CREDITO'); await elegirCliente(); guardar()
    expect(await screen.findByText('Indique la fecha de vencimiento del crédito.')).toBeDefined()
    expect(escrituras()).toHaveLength(0)
    expect(screen.getByRole('button', { name: /Cliente.*Cliente de crédito/ })).toBeDefined()
    cambiar(/Fecha de vencimiento/, vencimiento)
    expect(screen.queryByText('Indique la fecha de vencimiento del crédito.')).toBeNull()
  })
  it('fecha pasada respeta la validación backend sin enviar el POST', async () => {
    await nueva(); modalidad('CREDITO'); await elegirCliente(); cambiar(/Fecha de vencimiento/, '2000-01-01'); guardar()
    expect(await screen.findByText(/El vencimiento no puede ser anterior a hoy/)).toBeDefined()
    expect(escrituras()).toHaveLength(0)
  })
  it('contado con cliente sigue siendo válido y no exige fecha', async () => {
    await nueva(); await elegirCliente(); guardar(); await guardada()
    expect(payload()).toMatchObject({ clienteId: 27, esCredito: false, fechaVencimientoCredito: null })
  })
  it('BORRADOR crédito renderiza los valores persistidos y los preserva al editar solo cantidad', async () => {
    navegar('/ventas/2/editar'); await screen.findByLabelText('Cantidad Helado Chocolate Litro')
    expect(screen.getByRole('button', { name: 'Crédito' }).getAttribute('aria-pressed')).toBe('true')
    expect((screen.getByLabelText(/Fecha de vencimiento/) as HTMLInputElement).value).toBe(vencimiento)
    expect(screen.getByRole('button', { name: /Cliente.*Cliente de crédito/ })).toBeDefined()
    cambiar('Cantidad Helado Chocolate Litro', '2'); guardar(); await guardada()
    expect(new URL(String(escrituras()[0][0])).pathname).toBe('/api/ventas/2')
    expect(escrituras()[0][1]?.method).toBe('PUT')
    expect(payload()).toMatchObject({ esCredito: true, fechaVencimientoCredito: vencimiento, clienteId: 27, versionFila: 'AQ==', detalles: [{ cantidad: 2 }] })
  })
  it('BORRADOR contado conserva su modalidad al editar', async () => {
    venta.encabezado.esCredito = false; venta.encabezado.fechaVencimientoCredito = null
    navegar('/ventas/2/editar'); await screen.findByLabelText('Cantidad Helado Chocolate Litro')
    guardar(); await guardada()
    expect(payload()).toMatchObject({ esCredito: false, fechaVencimientoCredito: null, versionFila: 'AQ==' })
  })
  it('edición cambia explícitamente crédito a contado, oculta y limpia vencimiento en PUT', async () => {
    navegar('/ventas/2/editar'); await screen.findByLabelText('Cantidad Helado Chocolate Litro')
    modalidad('CONTADO')
    expect(screen.queryByLabelText(/Fecha de vencimiento/)).toBeNull()
    guardar(); await guardada()
    expect(payload()).toMatchObject({ esCredito: false, fechaVencimientoCredito: null, clienteId: 27, versionFila: 'AQ==' })
  })
  it('crédito → contado → crédito no recupera un vencimiento descartado', async () => {
    await nueva(); modalidad('CREDITO'); await elegirCliente(); cambiar(/Fecha de vencimiento/, vencimiento)
    modalidad('CONTADO'); modalidad('CREDITO')
    expect((screen.getByLabelText(/Fecha de vencimiento/) as HTMLInputElement).value).toBe('')
    guardar(); expect(escrituras()).toHaveLength(0)
    expect(await screen.findByText('Indique la fecha de vencimiento del crédito.')).toBeDefined()
  })
  it('un 400 conserva crédito, cliente, vencimiento y carrito para corregir/reintentar', async () => {
    errorGuardado = true
    await nueva(); modalidad('CREDITO'); await elegirCliente(); cambiar(/Fecha de vencimiento/, vencimiento)
    guardar(); await screen.findByText('Revise los datos del crédito.')
    expect(screen.getByRole('button', { name: 'Crédito' }).getAttribute('aria-pressed')).toBe('true')
    expect((screen.getByLabelText(/Fecha de vencimiento/) as HTMLInputElement).value).toBe(vencimiento)
    expect((screen.getByLabelText('Cantidad Helado Chocolate Litro') as HTMLInputElement).value).toBe('1')
    expect(screen.getByRole('button', { name: /Cliente.*Cliente de crédito/ })).toBeDefined()
    errorGuardado = false; guardar()
    await screen.findByRole('table', { name: 'Detalle de venta' })
    await waitFor(() => expect(escrituras()).toHaveLength(2))
    expect(JSON.parse(String(escrituras()[0][1]?.body))).toEqual(payload())
  })
  it.each(['CONFIRMADA', 'COMPLETADA', 'ANULADA'])('venta de crédito %s no se puede editar por URL', async (estado) => {
    venta.encabezado.estado = estado
    navegar('/ventas/2/editar'); await screen.findByText(/Solo pueden editarse borradores/)
    expect(screen.queryByRole('group', { name: 'Modalidad' })).toBeNull()
    expect(screen.queryByRole('button', { name: /Guardar/ })).toBeNull()
    expect(escrituras()).toHaveLength(0)
  })
  it.each(['America/Guatemala', 'Pacific/Kiritimati', 'America/Los_Angeles'])('la fecha civil no cambia con zona %s', async (zona) => {
    configurarZonaHoraria(zona)
    navegar('/ventas/2/editar'); await screen.findByLabelText('Cantidad Helado Chocolate Litro')
    guardar(); await guardada()
    expect(payload().fechaVencimientoCredito).toBe('2099-10-31')
    expect(screen.getByText(mostrarFechaCivil('2099-10-31'))).toBeDefined()
  })
  it('detalle de una venta existente muestra modalidad y vencimiento', async () => {
    navegar('/ventas/2'); await screen.findByRole('table', { name: 'Detalle de venta' })
    expect(screen.getByText('Crédito')).toBeDefined()
    expect(screen.getByText(mostrarFechaCivil(vencimiento))).toBeDefined()
  })
  it('vencimiento de hoy usa el día de la sucursal, no el día UTC', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-10-15T02:30:00Z')) // Guatemala: todavía 14 de octubre.
    await nueva(); modalidad('CREDITO'); await elegirCliente()
    expect(screen.getByLabelText(/Fecha de vencimiento/).getAttribute('min')).toBe('2026-10-14')
    cambiar(/Fecha de vencimiento/, '2026-10-14'); guardar(); await guardada()
    expect(payload().fechaVencimientoCredito).toBe('2026-10-14')
  })
  it('editar desde el detalle recién guardado conserva crédito; nueva venta vuelve a contado', async () => {
    await nueva(); modalidad('CREDITO'); await elegirCliente(); cambiar(/Fecha de vencimiento/, vencimiento)
    guardar(); await guardada()
    fireEvent.click(screen.getByRole('button', { name: 'Editar borrador' }))
    await screen.findByLabelText('Cantidad Helado Chocolate Litro')
    expect(screen.getByRole('button', { name: 'Crédito' }).getAttribute('aria-pressed')).toBe('true')
    expect((screen.getByLabelText(/Fecha de vencimiento/) as HTMLInputElement).value).toBe(vencimiento)
    guardar(); await screen.findByRole('table', { name: 'Detalle de venta' })
    expect(payload()).toMatchObject({ esCredito: true, fechaVencimientoCredito: vencimiento, versionFila: 'VERSION-2' })
    fireEvent.click(screen.getByRole('button', { name: 'Nueva venta' }))
    await screen.findByRole('group', { name: 'Modalidad' })
    expect(screen.getByRole('button', { name: 'Contado' }).getAttribute('aria-pressed')).toBe('true')
    expect(screen.queryByLabelText(/Fecha de vencimiento/)).toBeNull()
    expect(screen.getByRole('button', { name: /Cliente.*Venta sin cliente identificado/ })).toBeDefined()
    expect(screen.getByText('Su carrito está vacío')).toBeDefined()
  })
})
