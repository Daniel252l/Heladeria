import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { StrictMode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { ContextoAutenticacion } from '../../auth/contextoAutenticacion'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { configurarRenovacion, establecerAccessToken } from '../../api/clienteHttp'
import { consultarReporte } from '../../api/reportes'
import { EnrutadorAplicacion } from '../../rutas/EnrutadorAplicacion'
import { respuestaJson } from '../../test/datosAutenticacion'
import { datosReportes } from '../../test/datosFaseVisual8'
import { renderSeguridad } from '../../test/renderSeguridad'
import type { TipoReporte } from '../../types/reportes'
import { configurarZonaHoraria, mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { PaginaReportes } from './PaginaReportes'
import { consultaReportes, type FiltrosEditables } from './utilidadesReportes'

const permisos = [PERMISOS.reportesConsultar, PERMISOS.auditoriaConsultar]
const ruta = (tipo: TipoReporte) => tipo === 'auditoria' ? '/api/auditoria' : `/api/reportes/${tipo}`
function simular(tipo: TipoReporte, respuesta: unknown = datosReportes[tipo], estado = 200) {
  const peticiones: URL[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: URL, opciones?: RequestInit) => {
    const u = new URL(String(url))
    peticiones.push(u)
    expect(opciones?.method ?? 'GET').toBe('GET')
    expect(u.pathname).toBe(ruta(tipo))
    return respuestaJson(respuesta, estado)
  }))
  return peticiones
}
const mostrar = (tipo: TipoReporte) => renderSeguridad(<PaginaReportes tipo={tipo} />, permisos)
const cambiar = (nombre: string, valor: string) => fireEvent.change(screen.getByLabelText(nombre), { target: { value: valor } })
const aplicar = () => fireEvent.click(screen.getByRole('button', { name: 'Aplicar filtros' }))
const tabla = (nombre: string) => screen.getByRole('table', { name: nombre })
const fila = (nombreTabla: string, texto: string) => within(tabla(nombreTabla)).getAllByRole('row').slice(1).find((r) => r.textContent?.includes(texto))!
const celdas = (r: HTMLElement) => within(r).getAllByRole('cell').map((c) => c.textContent)
afterEach(() => { vi.unstubAllGlobals(); establecerAccessToken(null); configurarZonaHoraria('UTC') })

describe('Visual 8: navegación por permisos existentes', () => {
  it('ADMIN con permisos ve las nueve opciones, agrupadas', () => {
    renderSeguridad(<EnrutadorAplicacion />, permisos, '/dashboard')
    fireEvent.click(screen.getByRole('button', { name: 'Expandir menú lateral' }))
    expect(screen.getByRole('button', { name: 'Reportes' })).toBeDefined()
    fireEvent.click(screen.getByRole('button', { name: 'Reportes' }))
    fireEvent.click(screen.getByRole('button', { name: 'Auditoría' }))
    expect(screen.getByRole('link', { name: 'Auditoría' })).toBeDefined()
    for (const tipo of Object.keys(datosReportes).filter((v) => v !== 'auditoria')) {
      expect(document.querySelector(`a[href="/reportes/${tipo}"]`)).not.toBeNull()
    }
  })
  it('CONSULTA sin permisos no ve los grupos ni enlaces', () => {
    renderSeguridad(<EnrutadorAplicacion />, [PERMISOS.productosConsultar], '/dashboard')
    fireEvent.click(screen.getByRole('button', { name: 'Expandir menú lateral' }))
    expect(screen.queryByRole('button', { name: 'Reportes' })).toBeNull()
    expect(screen.queryByRole('link', { name: 'Auditoría' })).toBeNull()
    expect(document.querySelector('a[href^="/reportes"]')).toBeNull()
  })
  it.each(['/reportes/ventas', '/auditoria'])('bloquea acceso directo %s sin consultar API', async (url) => {
    const fetch = vi.fn()
    vi.stubGlobal('fetch', fetch)
    renderSeguridad(<EnrutadorAplicacion />, [], url)
    await screen.findByText('Acceso no autorizado')
    expect(fetch).not.toHaveBeenCalled()
  })
  it.each([
    ['/reportes/ventas', PERMISOS.auditoriaConsultar], ['/auditoria', PERMISOS.reportesConsultar],
  ])('los permisos son independientes para %s', async (url, permiso) => {
    renderSeguridad(<EnrutadorAplicacion />, [permiso], url)
    await screen.findByText('Acceso no autorizado')
  })
})

describe('Visual 8: filtros inspeccionando la petición HTTP real', () => {
  const casos: [TipoReporte, [string, string][], Record<string, string>][] = [
    ['ventas', [['Estado', 'CONFIRMADA'], ['Cliente (ID)', '7'], ['Moneda (ID)', '2']], { estado: 'CONFIRMADA', clienteId: '7', monedaId: '2' }],
    ['productos-vendidos', [['Producto (ID)', '1'], ['Cliente (ID)', '7'], ['Moneda (ID)', '2']], { productoId: '1', clienteId: '7', monedaId: '2' }],
    ['inventario', [['Producto (ID)', '1'], ['Bodega (ID)', '2'], ['Ubicación (ID)', '3']], { productoId: '1', bodegaId: '2', ubicacionId: '3' }],
    ['kardex', [['Producto (ID)', '1'], ['Bodega (ID)', '2'], ['Concepto', 'VENTA'], ['Tipo de movimiento', 'SALIDA']], { productoId: '1', bodegaId: '2', concepto: 'VENTA', tipo: 'SALIDA' }],
    ['compras', [['Proveedor (ID)', '8'], ['Moneda (ID)', '2'], ['Estado', 'RECIBIDA'], ['Modalidad', 'false']], { proveedorId: '8', monedaId: '2', estado: 'RECIBIDA', esCredito: 'false' }],
    ['caja', [['Caja (ID)', '9'], ['Usuario (ID)', '1'], ['Estado', 'CERRADA']], { cajaId: '9', usuarioId: '1', estado: 'CERRADA' }],
    ['cuentas-por-cobrar', [['Cliente (ID)', '7'], ['Moneda (ID)', '2'], ['Estado', 'PAGADA'], ['Clasificación de vencimiento', 'PAGADA']], { terceroId: '7', monedaId: '2', estado: 'PAGADA', vencimiento: 'PAGADA' }],
    ['cuentas-por-pagar', [['Proveedor (ID)', '8'], ['Moneda (ID)', '2'], ['Estado', 'PARCIAL'], ['Clasificación de vencimiento', 'VENCIDA']], { terceroId: '8', monedaId: '2', estado: 'PARCIAL', vencimiento: 'VENCIDA' }],
    ['auditoria', [['Usuario (ID)', '1'], ['Módulo', 'VENTAS'], ['Acción', 'COMPLETAR'], ['Entidad', 'Ventas']], { usuarioId: '1', modulo: 'VENTAS', accion: 'COMPLETAR', entidad: 'Ventas' }],
  ]
  it.each(casos)('%s envía todos sus filtros solo al aplicar; limpiar los elimina', async (tipo, campos, esperado) => {
    const llamadas = simular(tipo)
    mostrar(tipo)
    await screen.findByLabelText('Registros por página')
    expect(llamadas).toHaveLength(1)
    cambiar('Búsqueda', '  chocolate  ')
    for (const [nombre, valor] of campos) cambiar(nombre, valor)
    if (tipo !== 'inventario') { cambiar('Fecha desde', '2026-10-01'); cambiar('Fecha hasta', '2026-10-19') }
    else expect(screen.queryByLabelText('Fecha desde')).toBeNull()
    expect(llamadas).toHaveLength(1)
    aplicar()
    await waitFor(() => expect(llamadas).toHaveLength(2))
    const params = Object.fromEntries(llamadas[1].searchParams)
    expect(params).toEqual({ sucursalId: '4', busqueda: 'chocolate', pagina: '1', tamanioPagina: '20', ...esperado, ...(tipo === 'inventario' ? {} : { fechaDesde: '2026-10-01', fechaHasta: '2026-10-19' }) })
    await screen.findByLabelText('Registros por página')
    fireEvent.click(screen.getByRole('button', { name: 'Limpiar' }))
    await waitFor(() => expect(llamadas).toHaveLength(3))
    expect(Object.fromEntries(llamadas[2].searchParams)).toEqual({ sucursalId: '4', pagina: '1', tamanioPagina: '20' })
  })
  it('paginación conserva filtros aplicados, no borradores; tamaño limita opciones y vuelve a página 1', async () => {
    const datos = structuredClone(datosReportes.kardex)
    datos.totalRegistros = 60; datos.totalPaginas = 3
    const llamadas: URL[] = []
    vi.stubGlobal('fetch', vi.fn(async (url: URL) => {
      llamadas.push(new URL(String(url)))
      return respuestaJson({ ...datos, pagina: Number(llamadas.at(-1)!.searchParams.get('pagina')) })
    }))
    mostrar('kardex')
    await screen.findByRole('table')
    cambiar('Concepto', 'VENTA'); aplicar()
    await waitFor(() => expect(llamadas).toHaveLength(2))
    await screen.findByRole('table')
    cambiar('Concepto', 'COMPRA')
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }))
    await waitFor(() => expect(llamadas).toHaveLength(3))
    expect(llamadas[2].searchParams.get('concepto')).toBe('VENTA')
    expect(llamadas[2].searchParams.get('pagina')).toBe('2')
    await screen.findByRole('table')
    const tamanio = screen.getByLabelText('Registros por página')
    expect(within(tamanio).getAllByRole('option').every((o) => Number((o as HTMLOptionElement).value) <= 100)).toBe(true)
    cambiar('Registros por página', '50')
    await waitFor(() => expect(llamadas).toHaveLength(4))
    expect(llamadas[3].searchParams.get('pagina')).toBe('1')
    expect(llamadas[3].searchParams.get('tamanioPagina')).toBe('50')
    expect(llamadas[3].searchParams.get('concepto')).toBe('VENTA')
  })
  it('el cliente inventario descarta fechas y campos de otros contratos', async () => {
    const llamadas = simular('inventario')
    const extras: FiltrosEditables = { sucursalId: '4', fechaDesde: '2026-10-01', usuarioId: '1', esCredito: 'true' }
    await consultarReporte('inventario', consultaReportes(extras, 1, 20))
    expect(Object.fromEntries(llamadas[0].searchParams)).toEqual({ sucursalId: '4', pagina: '1', tamanioPagina: '20' })
  })
  it('compras conserva true para crédito sin confundirlo con false ni omitirlo', async () => {
    const llamadas = simular('compras')
    mostrar('compras'); await screen.findByRole('table')
    cambiar('Modalidad', 'true'); aplicar()
    await waitFor(() => expect(llamadas).toHaveLength(2))
    expect(llamadas[1].searchParams.get('esCredito')).toBe('true')
  })
  it('la sucursal se aplica explícitamente y se conserva al paginar', async () => {
    const llamadas: URL[] = []
    vi.stubGlobal('fetch', vi.fn(async (url: URL) => {
      const u = new URL(String(url)); llamadas.push(u)
      return respuestaJson({ ...datosReportes.inventario, sucursalId: Number(u.searchParams.get('sucursalId')),
        datos: { ...datosReportes.inventario.datos, pagina: Number(u.searchParams.get('pagina')), totalRegistros: 40, totalPaginas: 2 } })
    }))
    function DosSucursales() {
      const auth = useAutenticacion()
      return <ContextoAutenticacion.Provider value={{ ...auth, sucursales: [...auth.sucursales, { ...auth.sucursales[0], id: 9, nombre: 'Sucursal norte' }] }}>
        <PaginaReportes tipo="inventario" />
      </ContextoAutenticacion.Provider>
    }
    renderSeguridad(<DosSucursales />, permisos)
    await screen.findByRole('table')
    cambiar('Sucursal', '9')
    expect(llamadas).toHaveLength(1)
    aplicar()
    await waitFor(() => expect(llamadas).toHaveLength(2))
    await screen.findByRole('table')
    expect(screen.getByRole('region', { name: 'Valor oficial del inventario' }).textContent).toContain('Sucursal norte')
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }))
    await waitFor(() => expect(llamadas).toHaveLength(3))
    expect(llamadas[2].searchParams.get('sucursalId')).toBe('9')
  })
  it.each([
    ['ventas', PERMISOS.ventasConsultar, '/api/clientes', 'Cliente', 'clienteId'],
    ['productos-vendidos', PERMISOS.productosConsultar, '/api/productos', 'Producto', 'productoId'],
    ['compras', PERMISOS.proveedoresConsultar, '/api/compras/proveedores', 'Proveedor', 'proveedorId'],
    ['caja', PERMISOS.cajaConsultar, '/api/caja/cajas', 'Caja', 'cajaId'],
  ] as const)('%s reutiliza catálogo autorizado y envía el ID elegido, sin consultas por fila', async (tipo, permiso, catalogo, etiqueta, parametro) => {
    const llamadas: URL[] = []
    vi.stubGlobal('fetch', vi.fn(async (url: URL) => {
      const u = new URL(String(url)); llamadas.push(u)
      if (u.pathname === catalogo) return respuestaJson({ elementos: [{ id: 17, nombre: 'Registro elegido' }], pagina: 1, tamanioPagina: 30, totalPaginas: 1, totalRegistros: 1 })
      expect(u.pathname).toBe(ruta(tipo))
      return respuestaJson(datosReportes[tipo])
    }))
    renderSeguridad(<PaginaReportes tipo={tipo} />, [...permisos, permiso])
    await screen.findByLabelText('Registros por página')
    fireEvent.click(screen.getByRole('button', { name: `${etiqueta} Todos` }))
    fireEvent.click(await screen.findByRole('option', { name: 'Registro elegido' }))
    expect(llamadas.filter((u) => u.pathname === ruta(tipo))).toHaveLength(1)
    aplicar()
    await waitFor(() => expect(llamadas.filter((u) => u.pathname === ruta(tipo))).toHaveLength(2))
    expect(llamadas.at(-1)!.searchParams.get(parametro)).toBe('17')
    expect(llamadas.filter((u) => u.pathname === catalogo)).toHaveLength(1)
  })
})

describe('Visual 8: presentación fiel al contrato', () => {
  it('la zona explícita de un reporte no cambia la zona global usada por otros módulos', () => {
    const instante = '2026-10-19T02:31:00Z'
    configurarZonaHoraria('Asia/Tokyo')
    const anterior = mostrarInstante(instante)
    const esperado = new Intl.DateTimeFormat('es-GT', { timeZone: 'America/Guatemala', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(instante))
    expect(mostrarInstante(instante, true, 'America/Guatemala')).toBe(esperado)
    expect(mostrarInstante(instante)).toBe(anterior)
    expect(anterior).not.toBe(esperado)
  })
  it('ventas separa monedas, totales oficiales, cobros, modalidad, estados y nulls', async () => {
    simular('ventas'); configurarZonaHoraria('Asia/Tokyo'); mostrar('ventas')
    await screen.findByRole('table', { name: 'Ventas' })
    expect(screen.getByLabelText('Período consultado').textContent).toContain('America/Guatemala')
    expect(screen.getByLabelText('Período consultado').textContent).toContain(mostrarFechaCivil('2026-10-01'))
    expect(screen.getByRole('region', { name: 'Totales · GTQ · COMPLETADA' }).textContent).toContain('GTQ 214.00')
    expect(screen.getByRole('region', { name: 'Totales · USD · COMPLETADA' }).textContent).toContain('USD 20.00')
    expect(tabla('Cobros aplicados').textContent).toContain('Tarjeta')
    expect(tabla('Cobros aplicados').textContent).toContain('GTQ 57.00')
    expect(fila('Ventas', 'VT-0000000021').textContent).toContain('Crédito')
    expect(fila('Ventas', 'VT-0000000021').textContent).toContain('COMPLETADA')
    expect(fila('Ventas', 'VT-0000000021').textContent).toContain('PARCIAL')
    expect(fila('Ventas', 'VT-0000000021').textContent).toContain(mostrarFechaCivil('2026-10-18'))
    expect(fila('Ventas', 'VT-0000000022').textContent).toContain('Contado')
    expect(fila('Ventas', 'VT-0000000022').textContent).toContain('COBRADA')
    expect(celdas(fila('Ventas', 'VT-0000000022')).slice(11, 13)).toEqual(['—', '—'])
    expect(fila('Ventas', 'VT-0000000023').textContent).toContain('Sin definir')
    expect(fila('Ventas', 'VT-0000000023').textContent).toContain('PENDIENTE')
  })
  it('productos conserva presentaciones y cantidades comerciales/base separadas; no suma ni inventa costo', async () => {
    simular('productos-vendidos'); mostrar('productos-vendidos'); await screen.findByRole('table')
    expect(celdas(fila('Productos vendidos', 'Litro'))).toEqual(['HEL-CHO', 'Helado Chocolate', 'Litro', 'L', 'ml', '4', '4,000', 'GTQ', 'GTQ 100.00', '—', '—'])
    expect(celdas(fila('Productos vendidos', 'Vaso')).slice(5, 8)).toEqual(['2', '—', 'USD'])
    expect(screen.getByLabelText('Rango solicitado').textContent).toContain('no devuelve el período efectivo')
    expect(screen.queryByLabelText('Período consultado')).toBeNull()
  })
  it('inventario usa existencia/reserva/disponible/valor oficiales y costo de 6 decimales sin fechas', async () => {
    const datos = structuredClone(datosReportes.inventario)
    datos.datos.elementos[0].disponible = 777
    datos.valorInventario = 23.99 // Detectar un recálculo indebido de frontend.
    simular('inventario', datos); mostrar('inventario'); await screen.findByRole('table')
    expect(celdas(fila('Inventario actual', 'HEL-CHO')).slice(5)).toEqual(['1,000', '50', '777', 'GTQ 0.0245', 'GTQ 24.50'])
    expect(screen.getByRole('region', { name: 'Valor oficial del inventario' }).textContent).toContain('GTQ 23.99')
    expect(screen.queryByLabelText('Fecha desde')).toBeNull()
  })
  it('inventario sin moneda no inventa GTQ', async () => {
    simular('inventario', { ...datosReportes.inventario, monedaBase: null }); mostrar('inventario'); await screen.findByRole('table')
    expect(tabla('Inventario actual').textContent).not.toContain('GTQ')
    expect(screen.getByText('Sin moneda definida')).toBeDefined()
  })
  it('Kardex distingue entrada/salida, origen y saldos sin recalcular ni inventar moneda', async () => {
    simular('kardex'); mostrar('kardex'); await screen.findByRole('table')
    expect(celdas(fila('Kardex', 'OC-0000000031')).slice(9, 16)).toEqual(['0.0245', '24.50 (sin moneda)', '5,000', '122.50 (sin moneda)', 'COMPRAS', 'OC-0000000031', 'admin'])
    expect(tabla('Kardex').textContent).toContain('ENTRADA')
    expect(tabla('Kardex').textContent).toContain('SALIDA')
    expect(tabla('Kardex').textContent).not.toContain('GTQ')
  })
  it('compras representa las tres modalidades y CxP/saldo null sin decir pagada', async () => {
    simular('compras'); mostrar('compras'); await screen.findByRole('table')
    expect(celdas(fila('Compras', 'OC-0000000031'))).toContain('Crédito')
    expect(celdas(fila('Compras', 'OC-0000000032'))).toContain('Contado')
    expect(celdas(fila('Compras', 'OC-0000000033'))).toContain('Sin definir')
    expect(celdas(fila('Compras', 'OC-0000000033')).slice(-2)).toEqual(['Sin cuenta vinculada', '—'])
    expect(tabla('Compras').textContent).not.toContain('PAGADA')
    expect(fila('Compras', 'OC-0000000031').textContent).toContain(mostrarFechaCivil('2026-10-10'))
  })
  it('caja separa apertura, ingreso, efectivo/tarjeta y las diferencias sin alterar cifras', async () => {
    simular('caja'); mostrar('caja'); await screen.findByRole('table', { name: 'Sesiones de caja' })
    expect(celdas(fila('Resumen por forma de pago y moneda', 'Efectivo'))).toEqual(['Efectivo', 'GTQ', 'Sí', 'GTQ 50.00', 'GTQ 100.00', 'GTQ 20.00'])
    expect(celdas(fila('Resumen por forma de pago y moneda', 'Tarjeta'))).toEqual(['Tarjeta', 'GTQ', 'No', 'GTQ 0.00', 'GTQ 100.00', 'GTQ 0.00'])
    expect(fila('Sesiones de caja', 'Caja principal').textContent).toContain('Sobrante: +GTQ 5.00')
    expect(fila('Sesiones de caja', 'Caja sin diferencia').textContent).toContain('Sin diferencia: GTQ 0.00')
    expect(fila('Sesiones de caja', 'Caja faltante').textContent).toContain('Faltante: GTQ -3.00')
    expect(celdas(fila('Sesiones de caja', 'Caja histórica')).slice(7)).toEqual(['—', '—', '—', '—', '—', '—', 'No'])
  })
  it.each(['cuentas-por-cobrar', 'cuentas-por-pagar'] as const)('%s conserva resumen, cero pagado y fechas civiles', async (tipo) => {
    simular(tipo); mostrar(tipo); await screen.findByRole('table')
    expect(screen.getByRole('region', { name: 'Resumen · GTQ' }).textContent).toContain('GTQ 157.00')
    const t = tipo === 'cuentas-por-cobrar' ? 'Cuentas por cobrar' : 'Cuentas por pagar'
    const pagada = within(tabla(t)).getAllByRole('row').find((r) => r.textContent?.includes('PAGADA'))!
    expect(celdas(pagada).slice(-3)).toEqual(['GTQ 0.00', 'PAGADA', 'PAGADA'])
    expect(pagada.textContent).toContain(mostrarFechaCivil('2026-10-30'))
    if (tipo === 'cuentas-por-cobrar') {
      expect(tabla(t).textContent).toContain('REQUIERE_REVISION')
      expect(tabla(t).textContent).toContain('VENCIDA')
      expect(tabla(t).textContent).toContain('ANULADA')
    } else expect(tabla(t).textContent).toContain('Compra #31')
  })
  it('auditoría solo renderiza metadatos explícitos incluso si recibe propiedades sensibles extra', async () => {
    const datos = structuredClone(datosReportes.auditoria)
    const extra = { valorAnterior: 'SECRETO_ANTERIOR', valorNuevo: 'SECRETO_NUEVO', comentario: 'SECRETO_COMENTARIO', equipo: 'SECRETO_EQUIPO',
      ip: 'SECRETO_IP', token: 'SECRETO_TOKEN', cookie: 'SECRETO_COOKIE', hash: 'SECRETO_HASH', request: 'SECRETO_BODY' }
    simular('auditoria', { ...datos, elementos: [{ ...datos.elementos[0], ...extra }] })
    mostrar('auditoria'); await screen.findByRole('table')
    expect(tabla('Registros de auditoría').textContent).toContain('REGISTRADA')
    expect(tabla('Registros de auditoría').textContent).toContain('COMPLETAR')
    expect(tabla('Registros de auditoría').textContent).not.toContain('exitosa')
    for (const secreto of Object.values(extra)) expect(document.body.textContent).not.toContain(secreto)
    expect(within(tabla('Registros de auditoría')).getAllByRole('columnheader')).toHaveLength(9)
  })
})

describe('Visual 8: carga, errores y aislamiento de respuestas', () => {
  it('StrictMode no duplica la consulta inicial ni consulta por cada render', async () => {
    const llamadas = simular('inventario')
    renderSeguridad(<StrictMode><PaginaReportes tipo="inventario" /></StrictMode>, permisos)
    await screen.findByRole('table')
    cambiar('Búsqueda', 'sin aplicar')
    fireEvent.click(screen.getByRole('button', { name: 'Contraer filtros' }))
    fireEvent.click(screen.getByRole('button', { name: 'Expandir filtros' }))
    expect(llamadas).toHaveLength(1)
  })
  it('carga, vacío y paginación sin registros', async () => {
    let resolver!: (r: Response) => void
    vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>((resolve) => { resolver = resolve })))
    mostrar('auditoria')
    expect(screen.getByRole('status').textContent).toContain('Consultando reporte')
    await waitFor(() => expect(resolver).toBeDefined())
    await act(async () => resolver(respuestaJson({ elementos: [], pagina: 1, tamanioPagina: 20, totalRegistros: 0, totalPaginas: 0 })))
    expect(screen.getByText('No hay registros para estos filtros')).toBeDefined()
    expect(screen.getByRole('button', { name: 'Página siguiente' }).hasAttribute('disabled')).toBe(true)
  })
  it.each([400, 403, 500])('maneja HTTP %s sin detalles internos, conserva filtros y traceId útil', async (estado) => {
    const mensaje = estado === 400 ? 'El período no puede superar 366 días.' : 'SQL Server: connection-secret / stack-trace'
    simular('ventas', { mensaje, traceId: 'trace-prueba' }, estado)
    mostrar('ventas')
    await screen.findByRole('alert')
    if (estado === 400) expect(screen.getByRole('alert').textContent).toContain(mensaje)
    else expect(document.body.textContent).not.toContain(mensaje)
    if (estado === 403) expect(screen.getByRole('alert').textContent).toContain('No tiene permisos para consultar este reporte.')
    expect(screen.getByText('Referencia de soporte: trace-prueba')).toBeDefined()
    cambiar('Estado', 'CONFIRMADA'); aplicar()
    await screen.findByRole('alert')
    expect((screen.getByLabelText('Estado') as HTMLSelectElement).value).toBe('CONFIRMADA')
  })
  it('401 usa la renovación global existente sin lógica de sesión nueva', async () => {
    const renovar = vi.fn(async () => 'token-renovado')
    configurarRenovacion(renovar, vi.fn())
    const fetch = vi.fn().mockResolvedValueOnce(respuestaJson({}, 401)).mockResolvedValueOnce(respuestaJson(datosReportes.ventas))
    vi.stubGlobal('fetch', fetch)
    mostrar('ventas'); await screen.findByRole('table', { name: 'Ventas' })
    expect(renovar).toHaveBeenCalledTimes(1)
    expect(fetch).toHaveBeenCalledTimes(2)
    expect(new Headers(fetch.mock.calls[1][1].headers).get('Authorization')).toBe('Bearer token-renovado')
  })
  it('descarta una respuesta atrasada de filtros anteriores', async () => {
    let primera!: (r: Response) => void
    const fetch = vi.fn().mockImplementationOnce(() => new Promise<Response>((r) => { primera = r }))
      .mockResolvedValueOnce(respuestaJson({ ...datosReportes.auditoria, elementos: [{ ...datosReportes.auditoria.elementos[0], accion: 'RESPUESTA_ACTUAL' }] }))
    vi.stubGlobal('fetch', fetch)
    mostrar('auditoria')
    await waitFor(() => expect(primera).toBeDefined())
    cambiar('Acción', 'RESPUESTA_ACTUAL'); aplicar()
    await screen.findByText('RESPUESTA_ACTUAL')
    await act(async () => primera(respuestaJson(datosReportes.auditoria)))
    expect(screen.getByText('RESPUESTA_ACTUAL')).toBeDefined()
    expect(screen.queryByText('COMPLETAR')).toBeNull()
  })
})
