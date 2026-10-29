import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { Route, Routes } from 'react-router-dom'
import { permisosOperativosPos, soloOperacionPos } from '../../auth/inicioOperativo'
import { PERMISOS } from '../../auth/permisos'
import { renderSeguridad } from '../../test/renderSeguridad'
import { respuestaJson } from '../../test/datosAutenticacion'
import { paginaVentas, productoPos, referenciasVentas, ventaBorrador } from '../../test/datosFaseVisual5'
import { referenciasCaja, sesionCaja, paginaCaja } from '../../test/datosFaseVisual6'
import { EnrutadorAplicacion } from '../../rutas/EnrutadorAplicacion'
import { LayoutAutenticado } from '../../layouts/LayoutAutenticado'
import type { ProductoPos, VentaConsulta } from '../../types/ventas'
import { PaginaPos } from './PaginaPos'

const transporte = vi.fn<typeof fetch>()
let productos: ProductoPos[], venta: VentaConsulta, cajaAbierta: boolean, fallaCierre: boolean
const paths = () => transporte.mock.calls.map(([u]) => new URL(String(u)))
const posts = (ruta: string) => transporte.mock.calls.filter(([u, o]) => new URL(String(u)).pathname === ruta && o?.method === 'POST')
const cliente = { id: 27, codigo: 'CLIENTE', nombre: 'Cliente válido' }
beforeEach(() => {
  sessionStorage.clear(); transporte.mockReset(); cajaAbierta = true; fallaCierre = false
  productos = [structuredClone(productoPos)]; venta = structuredClone(ventaBorrador)
  transporte.mockImplementation(async (u, o) => {
    const url = new URL(String(u)), ruta = url.pathname
    if (ruta === '/api/ventas/referencias') return respuestaJson({ ...referenciasVentas, clientes: [cliente] })
    if (ruta === '/api/ventas/productos') return respuestaJson(paginaVentas(productos.filter((p) =>
      (!url.searchParams.get('categoriaId') || String(p.categoriaId) === url.searchParams.get('categoriaId')) &&
      (!url.searchParams.get('busqueda') || p.nombre.toLowerCase().includes(url.searchParams.get('busqueda')!.toLowerCase())))))
    if (ruta === '/api/caja/referencias') return respuestaJson(referenciasCaja)
    if (ruta === '/api/caja/sesiones') return respuestaJson(paginaCaja(cajaAbierta ? [sesionCaja.encabezado] : []))
    if (ruta === '/api/ventas' && o?.method === 'POST') {
      const p = JSON.parse(String(o.body))
      venta = { ...venta, encabezado: { ...venta.encabezado, esCredito: p.esCredito, fechaVencimientoCredito: p.fechaVencimientoCredito,
        clienteId: p.clienteId, cliente: p.clienteId ? cliente.nombre : null, total: 25, subtotal: 25 },
        detalles: [{ ...venta.detalles[0], cantidad: 1, cantidadBase: 1000, totalLinea: 25, subtotalNeto: 25 }] }
      return respuestaJson(venta, 201)
    }
    if (ruta.endsWith('/confirmar')) { venta = { ...venta, encabezado: { ...venta.encabezado, estado: 'CONFIRMADA', versionFila: 'V2' } }; return respuestaJson(venta) }
    if (ruta.endsWith('/completar')) {
      if (fallaCierre) return respuestaJson({ codigo: 'EXISTENCIA_INSUFICIENTE', mensaje: 'Stock insuficiente para el cierre.' }, 409)
      const p = JSON.parse(String(o?.body))
      venta = { ...venta, encabezado: { ...venta.encabezado, estado: 'COMPLETADA', claveOperacion: p.claveOperacion },
        cobro: p.cobro ? { pagoId: 99, sesionCajaId: p.cobro.sesionCajaId, claveOperacion: p.claveOperacion, pagos: p.cobro.pagos } : null }
      return respuestaJson(venta)
    }
    if (ruta === '/api/ventas/2') return respuestaJson(venta)
    throw new Error(`Petición inesperada ${ruta}`)
  })
  vi.stubGlobal('fetch', transporte)
})
afterEach(() => vi.unstubAllGlobals())
function renderPos() { return renderSeguridad(<PaginaPos />, [...permisosOperativosPos], '/pos') }
async function agregar() {
  const p = await screen.findByRole('article', { name: 'Helado Chocolate' })
  fireEvent.change(within(p).getByLabelText('Presentación · Helado Chocolate'), { target: { value: '1' } })
  fireEvent.click(within(p).getByRole('button', { name: /Agregar/ }))
}
async function caja() {
  fireEvent.change(await screen.findByLabelText('Forma de pago 1'), { target: { value: String(referenciasCaja.formasPago.find((f) => f.esEfectivo)!.id) } })
  await waitFor(() => expect((screen.getByRole('button', { name: 'Confirmar y finalizar' }) as HTMLButtonElement).disabled).toBe(false))
}
it('catálogo, búsqueda y chips envían filtros reales juntos y Todos elimina categoría', async () => {
  renderPos(); await screen.findByRole('article')
  fireEvent.click(screen.getByRole('button', { name: 'Helados artesanales' }))
  fireEvent.change(screen.getByPlaceholderText('Buscar producto'), { target: { value: 'Chocolate' } })
  await waitFor(() => expect(paths().some((u) => u.searchParams.get('categoriaId') === '20' && u.searchParams.get('busqueda') === 'Chocolate')).toBe(true))
  fireEvent.click(screen.getByRole('button', { name: 'Todos' }))
  await waitFor(() => expect(paths().at(-1)?.searchParams.get('categoriaId')).toBeNull())
  expect(paths().at(-1)?.searchParams.get('busqueda')).toBe('Chocolate')
}, 15000)
it('carrito vacío bloquea finalizar y conserva acceso móvil a la orden', async () => {
  renderPos(); await screen.findByRole('article')
  expect((screen.getByRole('button', { name: 'Finalizar venta' }) as HTMLButtonElement).disabled).toBe(true)
  expect(screen.getByRole('link', { name: /Ver carrito/ }).getAttribute('href')).toBe('#carrito-pos')
  expect(screen.getByRole('heading', { name: 'Orden actual' })).toBeDefined()
})
it('incrementar, disminuir y eliminar conservan una sola línea y total real de presentación', async () => {
  renderPos(); await agregar()
  const cantidad = screen.getByLabelText('Cantidad Helado Chocolate Litro') as HTMLInputElement
  fireEvent.click(screen.getByRole('button', { name: 'Incrementar Helado Chocolate Litro' })); expect(cantidad.value).toBe('2')
  expect(screen.getByText('Total línea: GTQ 50.00')).toBeDefined()
  fireEvent.click(screen.getByRole('button', { name: 'Disminuir Helado Chocolate Litro' })); expect(cantidad.value).toBe('1')
  expect((screen.getByRole('button', { name: 'Disminuir Helado Chocolate Litro' }) as HTMLButtonElement).disabled).toBe(true)
  fireEvent.click(screen.getByRole('button', { name: 'Eliminar Helado Chocolate Litro' }))
  expect(screen.getByText('Su carrito está vacío')).toBeDefined()
})
it.each([0, 500])('existencia %s menor a Litro mantiene tarjeta visible y bloqueada', async (stock) => {
  productos[0].disponibleBase = stock; renderPos()
  const p = await screen.findByRole('article', { name: 'Helado Chocolate' })
  fireEvent.change(within(p).getByLabelText('Presentación · Helado Chocolate'), { target: { value: '1' } })
  expect(within(p).getByText('Sin existencias')).toBeDefined()
  expect((within(p).getByRole('button', { name: /Agregar/ }) as HTMLButtonElement).disabled).toBe(true)
})
it('compuesto permanece una línea comercial sin inventar stock ni expandir insumos', async () => {
  productos = [{ ...productoPos, nombre: 'Vaca Loca', manejaExistencia: false, disponibleBase: null,
    presentaciones: [{ ...productoPos.presentaciones[0], nombre: 'Unidad', precio: 28 }] }]
  renderPos(); const p = await screen.findByRole('article', { name: 'Vaca Loca' })
  fireEvent.click(within(p).getByRole('button', { name: /Agregar/ }))
  expect(screen.getAllByRole('listitem')).toHaveLength(1)
  expect(screen.getByText('Total línea: GTQ 28.00')).toBeDefined()
  expect(screen.queryByText('Sin existencias')).toBeNull()
})
it('imagen fallida cambia al icono local', async () => {
  productos[0].imagen = '/producto-prueba.png'; const r = renderPos(); await screen.findByRole('article')
  fireEvent.error(r.container.querySelector('img')!)
  expect(r.container.querySelector('img')).toBeNull()
})
it('sin turno deshabilita finalizar contado, mantiene guardar borrador', async () => {
  cajaAbierta = false; renderPos(); await agregar()
  await screen.findByText('Se requiere un turno de caja abierto para cobrar.')
  expect((screen.getByRole('button', { name: 'Finalizar venta' }) as HTMLButtonElement).disabled).toBe(true)
  expect((screen.getByRole('button', { name: 'Guardar borrador' }) as HTMLButtonElement).disabled).toBe(false)
})
it('contado usa guardar → confirmar → completar con cobro y muestra comprobante/nueva venta', async () => {
  renderPos(); await agregar()
  fireEvent.click(screen.getByRole('button', { name: 'Finalizar venta' }))
  await caja(); fireEvent.click(screen.getByRole('button', { name: 'Confirmar y finalizar' }))
  await screen.findByRole('heading', { name: 'Venta finalizada' })
  expect(posts('/api/ventas')).toHaveLength(1); expect(posts('/api/ventas/2/confirmar')).toHaveLength(1)
  const body = JSON.parse(String(posts('/api/ventas/2/completar')[0][1]?.body))
  expect(body.versionFila).toBe('V2'); expect(body.cobro.pagos[0].montoAplicado).toBe(25)
  expect(body.claveOperacion).toMatch(/^[0-9a-f-]{36}$/)
  expect(screen.getByRole('link', { name: 'Ver comprobante' }).getAttribute('href')).toBe('/ventas/2/comprobante')
  fireEvent.click(screen.getByRole('button', { name: 'Nueva venta' }))
  await screen.findByText('Su carrito está vacío')
})
it('crédito exige cliente/fecha y completa sin cobro ni turno', async () => {
  cajaAbierta = false; renderPos(); await agregar()
  fireEvent.click(screen.getByRole('button', { name: 'Crédito' }))
  const finalizar = () => screen.getByRole('button', { name: 'Finalizar venta' }) as HTMLButtonElement
  expect(finalizar().disabled).toBe(true)
  fireEvent.click(screen.getByRole('button', { name: /^Cliente/ })); fireEvent.click(await screen.findByRole('option', { name: /Cliente válido/ }))
  expect(finalizar().disabled).toBe(true)
  fireEvent.change(screen.getByLabelText(/Fecha de vencimiento/), { target: { value: '2099-12-31' } })
  fireEvent.click(finalizar()); await screen.findByRole('dialog')
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar y finalizar' }))
  await screen.findByRole('heading', { name: 'Venta finalizada' })
  const creado = JSON.parse(String(posts('/api/ventas')[0][1]?.body))
  expect(creado.esCredito).toBe(true); expect(creado.fechaVencimientoCredito).toBe('2099-12-31'); expect(creado.clienteId).toBe(27)
  expect(JSON.parse(String(posts('/api/ventas/2/completar')[0][1]?.body)).cobro).toBeUndefined()
})
it('fallo después de confirmar conserva venta/UUID y reintenta sin otro borrador ni confirmación', async () => {
  fallaCierre = true; renderPos(); await agregar()
  fireEvent.click(screen.getByRole('button', { name: 'Finalizar venta' })); await caja()
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar y finalizar' })); await screen.findByText('Stock insuficiente para el cierre.')
  fallaCierre = false; fireEvent.click(screen.getByRole('button', { name: 'Confirmar y finalizar' }))
  await screen.findByRole('heading', { name: 'Venta finalizada' })
  expect(posts('/api/ventas')).toHaveLength(1); expect(posts('/api/ventas/2/confirmar')).toHaveLength(1)
  const cierres = posts('/api/ventas/2/completar').map(([, o]) => JSON.parse(String(o?.body)))
  expect(cierres).toHaveLength(2); expect(cierres[1]).toEqual(cierres[0])
})
it('redirect y sidebar dependen de permisos aunque el fixture se llame admin', async () => {
  renderSeguridad(<EnrutadorAplicacion />, [...permisosOperativosPos], '/dashboard')
  await screen.findByRole('heading', { name: 'Punto de venta' })
  const nav = within(screen.getByRole('navigation', { name: 'Navegación principal' }))
  fireEvent.click(nav.getByRole('button', { name: 'Ventas' }))
  expect(nav.getByRole('link', { name: 'Punto de venta' })).toBeDefined()
  expect(nav.getByRole('link', { name: 'Ventas' })).toBeDefined()
  for (const name of ['Dashboard', 'Auditoría', 'Cajas', 'Cuentas por pagar', 'Productos']) expect(nav.queryByRole('link', { name })).toBeNull()
})
it('ADMIN conserva navegación habitual y CONSULTA no gana capacidades', () => {
  expect(soloOperacionPos(Object.values(PERMISOS))).toBe(false); expect(soloOperacionPos([])).toBe(false)
  renderSeguridad(<Routes><Route element={<LayoutAutenticado />}><Route path="/" element={<p>Inicio</p>} /></Route></Routes>, Object.values(PERMISOS))
  expect(screen.getByRole('link', { name: 'Dashboard' })).toBeDefined()
  fireEvent.click(screen.getByRole('button', { name: 'Auditoría' }))
  expect(screen.getByRole('link', { name: 'Auditoría' })).toBeDefined()
})
