import { fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { respuestaJson } from '../../test/datosAutenticacion'
import { cuentaCartera } from '../../test/datosFaseVisual7'
import { renderSeguridad } from '../../test/renderSeguridad'
import type { TipoCartera } from '../../types/cartera'
import { PaginaCartera } from './PaginaCartera'

// No sustituir solicitar: verificamos la URL final que recibe fetch.
const transporte = vi.fn<typeof fetch>()
const urlDe = (entrada: RequestInfo | URL) => new URL(entrada instanceof Request ? entrada.url : String(entrada))
const peticiones = (tipo: TipoCartera, resumen = false) => transporte.mock.calls
  .map(([entrada]) => urlDe(entrada))
  .filter((u) => u.pathname === `/api/cuentas-por-${tipo}${resumen ? '/resumen' : ''}`)
const parametros = (tipo: TipoCartera, resumen = false) => Object.fromEntries(peticiones(tipo, resumen).at(-1)!.searchParams)
const editar = (etiqueta: string, valor: string) => fireEvent.change(screen.getByLabelText(etiqueta), { target: { value: valor } })
const buscar = () => fireEvent.click(screen.getByRole('button', { name: 'Buscar cuentas' }))
const base = { sucursalId: '4', pagina: '1', tamanioPagina: '10' }
const combinados = { busqueda: 'Café & factura #2', terceroId: '27', estado: 'PAGADA', vencimiento: 'VIGENTE', fechaDesde: '2026-10-01', fechaHasta: '2026-10-14' }

beforeEach(() => {
  transporte.mockReset()
  transporte.mockImplementation(async (entrada) => {
    const u = urlDe(entrada)
    if (u.pathname.endsWith('/resumen')) return respuestaJson([])
    if (u.pathname === '/api/clientes' || u.pathname === '/api/compras/proveedores') {
      return respuestaJson({ elementos: [{ id: 27, nombre: 'Tercero de prueba' }], pagina: 1, totalPaginas: 1, totalRegistros: 1 })
    }
    if (!/^\/api\/cuentas-por-(cobrar|pagar)$/.test(u.pathname)) throw new Error(`Petición no prevista: ${u.pathname}`)
    return respuestaJson({ elementos: [cuentaCartera], pagina: Number(u.searchParams.get('pagina')), tamanioPagina: Number(u.searchParams.get('tamanioPagina')), totalRegistros: 40, totalPaginas: 4 })
  })
  vi.stubGlobal('fetch', transporte)
})
afterEach(() => vi.unstubAllGlobals())

async function abrir(tipo: TipoCartera, conCatalogo = true) {
  renderSeguridad(<PaginaCartera tipo={tipo} />, conCatalogo ? Object.values(PERMISOS) : [PERMISOS.cxcConsultar, PERMISOS.cxpConsultar])
  await screen.findByRole('table', { name: `Cuentas por ${tipo}` })
}
async function elegirTercero(tipo: TipoCartera) {
  fireEvent.click(screen.getByRole('button', { name: new RegExp(`^${tipo === 'cobrar' ? 'Cliente' : 'Proveedor'} Todos`) }))
  fireEvent.click(await screen.findByRole('option', { name: 'Tercero de prueba' }))
}
async function editarTodos(tipo: TipoCartera) {
  editar('Búsqueda', `  ${combinados.busqueda}  `)
  editar('Estado', combinados.estado)
  editar('Vencimiento', combinados.vencimiento)
  editar('Fecha desde', combinados.fechaDesde)
  editar('Fecha hasta', combinados.fechaHasta)
  await elegirTercero(tipo)
}
async function esperarListado(tipo: TipoCartera, esperados: Record<string, string>) {
  await waitFor(() => expect(parametros(tipo)).toEqual(esperados))
  await screen.findByRole('table', { name: `Cuentas por ${tipo}` })
}

describe.each(['cobrar', 'pagar'] as const)('URL real de cartera por %s', (tipo) => {
  it.each([
    ['Estado', 'PAGADA', 'estado'],
    ['Vencimiento', 'VIGENTE', 'vencimiento'],
    ['Búsqueda', 'Café & factura #2', 'busqueda'],
    ['Fecha desde', '2026-10-01', 'fechaDesde'],
    ['Fecha hasta', '2026-10-14', 'fechaHasta'],
  ])('envía %s como %s al buscar, no al editar', async (etiqueta, valor, clave) => {
    await abrir(tipo)
    editar(etiqueta, valor)
    expect(peticiones(tipo)).toHaveLength(1)
    buscar()
    await esperarListado(tipo, { ...base, [clave]: valor })
    await waitFor(() => expect(parametros(tipo, true)[clave]).toBe(valor))
  })
  it('envía terceroId del catálogo de cliente/proveedor', async () => {
    await abrir(tipo)
    await elegirTercero(tipo)
    buscar()
    await esperarListado(tipo, { ...base, terceroId: '27' })
  })
  it('envía terceroId introducido sin permiso de catálogo', async () => {
    await abrir(tipo, false)
    fireEvent.change(screen.getByLabelText(new RegExp(`^${tipo === 'cobrar' ? 'Cliente' : 'Proveedor'} \\(ID\\)`)), { target: { value: '35' } })
    buscar()
    await esperarListado(tipo, { ...base, terceroId: '35' })
  })
  it('coexisten todos los filtros, sin perder acentos ni caracteres reservados', async () => {
    await abrir(tipo)
    await editarTodos(tipo)
    buscar()
    await esperarListado(tipo, { ...base, ...combinados })
  })
  it('limpiar elimina todos los parámetros opcionales de ambas peticiones', async () => {
    await abrir(tipo)
    await editarTodos(tipo)
    buscar()
    await esperarListado(tipo, { ...base, ...combinados })
    fireEvent.click(screen.getByRole('button', { name: 'Limpiar filtros' }))
    await esperarListado(tipo, base)
    await waitFor(() => {
      const p = parametros(tipo, true)
      for (const clave of Object.keys(combinados)) expect(p).not.toHaveProperty(clave)
    })
    expect((screen.getByLabelText('Estado') as HTMLSelectElement).value).toBe('')
  })
  it('paginar conserva los filtros aplicados, no los editados sin buscar', async () => {
    await abrir(tipo)
    await editarTodos(tipo)
    buscar()
    await esperarListado(tipo, { ...base, ...combinados })
    editar('Estado', 'PARCIAL')
    editar('Búsqueda', 'Todavía sin aplicar')
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }))
    await esperarListado(tipo, { ...base, ...combinados, pagina: '2' })
    fireEvent.change(screen.getByLabelText('Registros por página'), { target: { value: '20' } })
    await esperarListado(tipo, { ...base, ...combinados, tamanioPagina: '20' })
  })
  it('buscar desde otra página vuelve a página 1 con los filtros nuevos', async () => {
    await abrir(tipo)
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }))
    await esperarListado(tipo, { ...base, pagina: '2' })
    editar('Estado', 'PAGADA')
    buscar()
    await esperarListado(tipo, { ...base, estado: 'PAGADA' })
  })
  it('resumen recibe todos los criterios soportados sin paginación y no se recarga al paginar', async () => {
    await abrir(tipo)
    expect(parametros(tipo, true)).toEqual({ sucursalId: '4' })
    await editarTodos(tipo)
    buscar()
    await esperarListado(tipo, { ...base, ...combinados })
    await waitFor(() => expect(parametros(tipo, true)).toEqual({ sucursalId: '4', ...combinados }))
    const cantidadResumen = peticiones(tipo, true).length
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }))
    await esperarListado(tipo, { ...base, ...combinados, pagina: '2' })
    fireEvent.change(screen.getByLabelText('Registros por página'), { target: { value: '20' } })
    await esperarListado(tipo, { ...base, ...combinados, tamanioPagina: '20' })
    expect(peticiones(tipo, true)).toHaveLength(cantidadResumen)
  })
})
