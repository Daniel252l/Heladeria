import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { respuestaJson } from '../../test/datosAutenticacion'
import { compraDetalle, pagina, presentacionesCompra, productoCompra, proveedorPrueba, referenciasCompras } from '../../test/datosFaseVisual3'
import { renderSeguridad } from '../../test/renderSeguridad'
import { PaginaDetalleCompra } from './PaginaDetalleCompra'
import { PaginaFormularioCompra } from './PaginaFormularioCompra'

// Se prueba el JSON entregado a fetch; solicitar no se simula y no hay acceso a la BD.
const transporte = vi.fn<typeof fetch>()
const crearCompra = (esCredito: boolean | null = true) => {
  const compra = compraDetalle('BORRADOR', 10)
  return { ...compra, encabezado: { ...compra.encabezado, sucursalId: 4, esCredito } }
}
let compra = crearCompra()
let referencias = structuredClone(referenciasCompras)
let errorGuardado = false
const escrituras = () => transporte.mock.calls.filter(([, o]) => ['POST', 'PUT'].includes(o?.method ?? ''))
const cuerpo = () => JSON.parse(String(escrituras().at(-1)?.[1]?.body))
const cambiar = (etiqueta: string, valor: string) => fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } })
const guardar = () => fireEvent.click(screen.getByRole('button', { name: /Guardar borrador/ }))

beforeEach(() => {
  transporte.mockReset(); compra = crearCompra(); referencias = structuredClone(referenciasCompras); errorGuardado = false
  transporte.mockImplementation(async (entrada, opciones) => {
    const path = new URL(entrada instanceof Request ? entrada.url : String(entrada)).pathname
    if (path === '/api/compras/referencias') return respuestaJson(referencias)
    if (path === '/api/compras/proveedores') return respuestaJson(pagina([proveedorPrueba]))
    if (path === '/api/productos') return respuestaJson(pagina([productoCompra]))
    if (path === '/api/compras/productos/17/presentaciones') return respuestaJson(presentacionesCompra)
    if ((path === '/api/compras' || path === '/api/compras/51') && ['POST', 'PUT'].includes(opciones?.method ?? '')) {
      if (errorGuardado) return respuestaJson({ mensaje: 'Orden rechazada por validación.', codigo: 'VALIDACION_COMPRAS' }, 400)
      const datos = JSON.parse(String(opciones?.body))
      // Misma semántica del contrato: null conserva modalidad existente en PUT.
      compra.encabezado.esCredito = datos.esCredito ?? (opciones?.method === 'POST' ? false : compra.encabezado.esCredito)
      return respuestaJson(compra)
    }
    if (path === '/api/compras/51/confirmar' && opciones?.method === 'POST') {
      compra.encabezado.estado = 'CONFIRMADA'; return respuestaJson(compra)
    }
    if (path === '/api/compras/51/estado' && opciones?.method === 'PUT') {
      compra.encabezado.estado = 'ANULADA'; return respuestaJson(compra)
    }
    if (path === '/api/compras/51') return respuestaJson(compra)
    throw new Error(`Petición no prevista: ${opciones?.method ?? 'GET'} ${path}`)
  })
  vi.stubGlobal('fetch', transporte)
})
afterEach(() => vi.unstubAllGlobals())

function navegar(ruta = '/compras/nueva') {
  return renderSeguridad(<Routes>
    <Route path="/compras/nueva" element={<PaginaFormularioCompra />} />
    <Route path="/compras/:id/editar" element={<PaginaFormularioCompra />} />
    <Route path="/compras/:id" element={<PaginaDetalleCompra />} />
  </Routes>, Object.values(PERMISOS), ruta)
}
async function datosNueva() {
  fireEvent.click(screen.getByLabelText('Proveedor *'))
  fireEvent.click(await screen.findByRole('option', { name: /PROV-007.*Lácteos del Valle/ }))
  fireEvent.click(screen.getByLabelText('Producto *'))
  fireEvent.click(await screen.findByRole('option', { name: 'Base de vainilla' }))
  await waitFor(() => expect((screen.getByLabelText('Presentación *') as HTMLButtonElement).disabled).toBe(false))
  fireEvent.click(screen.getByLabelText('Presentación *'))
  fireEvent.click(await screen.findByRole('option', { name: /Litro.*1,000/ }))
  cambiar('Cantidad *', '4'); cambiar('Precio unitario *', '25')
  cambiar('Fecha de emisión *', '2026-10-01'); cambiar('Fecha de entrega esperada', '2026-10-10')
}
async function editar(esCredito: boolean | null) {
  compra = crearCompra(esCredito)
  navegar('/compras/51/editar')
  await screen.findByRole('heading', { name: 'Editar orden de compra' })
}
async function guardada() {
  await screen.findByRole('heading', { name: 'Orden OC-00000051' })
  expect(escrituras()).toHaveLength(1)
}

describe('modalidad de compra sin cambios financieros', () => {
  it.each([false, true])('nueva compra esCredito=%s envía el JSON completo sin añadir vencimiento ni factura', async (credito) => {
    navegar(); await screen.findByRole('heading', { name: 'Nueva orden de compra' })
    expect((screen.getByLabelText('Modalidad') as HTMLSelectElement).value).toBe('CONTADO')
    if (credito) cambiar('Modalidad', 'CREDITO')
    await datosNueva(); guardar(); await guardada()
    expect(escrituras()[0][1]?.method).toBe('POST')
    expect(new URL(String(escrituras()[0][0])).pathname).toBe('/api/compras')
    expect(cuerpo()).toEqual({ sucursalId: 4, proveedorId: 7, fechaEmision: '2026-10-01', fechaEntregaEsperada: '2026-10-10', monedaId: 1, tipoCambio: 1,
      esCredito: credito, detalles: [{ productoId: 17, productoPresentacionId: 23, cantidad: 4, precioUnitario: 25, porcentajeDescuento: 0 }] })
    expect(screen.getByText(credito ? 'Crédito' : 'Contado')).toBeDefined()
  })
  it('conserva crédito al fallar validaciones locales y permite corregir sin volver a elegir', async () => {
    navegar(); await screen.findByRole('heading', { name: 'Nueva orden de compra' })
    cambiar('Modalidad', 'CREDITO'); guardar()
    await screen.findByText('Complete los datos generales obligatorios.')
    expect(escrituras()).toHaveLength(0)
    expect((screen.getByLabelText('Modalidad') as HTMLSelectElement).value).toBe('CREDITO')
    await datosNueva(); guardar(); await guardada()
    expect(cuerpo().esCredito).toBe(true)
  })
  it('conserva modalidad y líneas después de un 400', async () => {
    errorGuardado = true
    navegar(); await screen.findByRole('heading', { name: 'Nueva orden de compra' })
    cambiar('Modalidad', 'CREDITO'); await datosNueva(); guardar()
    await screen.findByText('Orden rechazada por validación.')
    expect((screen.getByLabelText('Modalidad') as HTMLSelectElement).value).toBe('CREDITO')
    expect((screen.getByLabelText('Cantidad *') as HTMLInputElement).value).toBe('4')
    expect((screen.getByLabelText('Precio unitario *') as HTMLInputElement).value).toBe('25')
    errorGuardado = false; guardar(); await screen.findByRole('heading', { name: 'Orden OC-00000051' })
    expect(escrituras()).toHaveLength(2)
    expect(JSON.parse(String(escrituras()[0][1]?.body))).toEqual(cuerpo())
  })
  it.each([true, false, null])('BORRADOR esCredito=%s conserva modalidad al cambiar solo cantidades', async (credito) => {
    await editar(credito)
    expect((screen.getByLabelText('Modalidad') as HTMLSelectElement).value).toBe(credito === null ? '' : credito ? 'CREDITO' : 'CONTADO')
    cambiar('Cantidad *', '6'); guardar(); await guardada()
    expect(escrituras()[0][1]?.method).toBe('PUT')
    expect(new URL(String(escrituras()[0][0])).pathname).toBe('/api/compras/51')
    expect(cuerpo()).toMatchObject({ esCredito: credito, detalles: [{ cantidad: 6 }] })
    if (credito === null) {
      expect(screen.getByText('Sin definir')).toBeDefined()
      expect(screen.queryByText('Contado')).toBeNull()
    }
  })
  it.each([[true, false], [false, true], [null, true], [null, false]] as const)('cambio explícito %s → %s envía el valor elegido', async (previo, elegido) => {
    await editar(previo)
    cambiar('Modalidad', elegido ? 'CREDITO' : 'CONTADO')
    guardar(); await guardada()
    expect(cuerpo().esCredito).toBe(elegido)
  })
  it.each([[true, 'Crédito'], [false, 'Contado'], [null, 'Sin definir']] as const)('detalle esCredito=%s muestra %s', async (credito, texto) => {
    compra = crearCompra(credito); navegar('/compras/51')
    await screen.findByRole('heading', { name: 'Orden OC-00000051' })
    expect(screen.getByText('Modalidad')).toBeDefined()
    expect(screen.getByText(texto)).toBeDefined()
    if (credito === null) expect(screen.queryByText('Contado')).toBeNull()
  })
  it.each(['CONFIRMADA', 'PARCIALMENTE_RECIBIDA', 'RECIBIDA', 'ANULADA'])('%s no permite editar modalidad por URL', async (estado) => {
    compra.encabezado.estado = estado
    navegar('/compras/51/editar')
    await screen.findByText(/Solo.*BORRADOR/)
    expect(screen.queryByLabelText('Modalidad')).toBeNull()
    expect(screen.queryByRole('button', { name: /Guardar borrador/ })).toBeNull()
    expect(escrituras()).toHaveLength(0)
  })
  it.each(['sin permiso de edición del estado', 'con recepciones'])('BORRADOR %s no habilita cambios', async (caso) => {
    if (caso === 'con recepciones') compra.recepciones = compraDetalle('BORRADOR', 7).recepciones
    else referencias.estados[0].permiteEdicion = false
    navegar('/compras/51/editar')
    await screen.findByText(/Solo.*BORRADOR/)
    expect(screen.queryByRole('button', { name: /Guardar borrador/ })).toBeNull()
    expect(escrituras()).toHaveLength(0)
  })
  it.each([true, false, null])('confirmar y anular esCredito=%s no envían ni alteran modalidad', async (credito) => {
    compra = crearCompra(credito); navegar('/compras/51')
    fireEvent.click(await screen.findByRole('button', { name: 'Confirmar' }))
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar' }))
    await screen.findByText('CONFIRMADA')
    expect(escrituras()[0][1]?.body).toBeUndefined()
    fireEvent.click(screen.getByRole('button', { name: 'Anular' }))
    fireEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Anular orden' }))
    await screen.findByText('ANULADA')
    expect(cuerpo()).toEqual({ estado: 'ANULADA' })
    expect(compra.encabezado.esCredito).toBe(credito)
    expect(screen.getByText(credito === null ? 'Sin definir' : credito ? 'Crédito' : 'Contado')).toBeDefined()
  })
})
