import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ErrorApi, solicitar } from '../../api/clienteHttp'
import { PERMISOS } from '../../auth/permisos'
import { renderSeguridad } from '../../test/renderSeguridad'
import { arqueoCaja, cajaDemo, paginaCaja, pendienteCaja, referenciasCaja, respuestaCobro, sesionArqueada, sesionCaja, sesionCerrada } from '../../test/datosFaseVisual6'
import type { SesionCaja } from '../../types/caja'
import { configurarZonaHoraria } from '../../utils/fechas'
import { PaginaCaja } from './PaginaCaja'
import { PaginaCajas } from './PaginaCajas'
import { PaginaAbrirCaja } from './PaginaAbrirCaja'
import { PaginaSesionesCaja } from './PaginaSesionesCaja'
import { PaginaSesionCaja } from './PaginaSesionCaja'
import { centavos, ingresosCaja, egresosCaja } from './utilidadesCaja'

vi.mock('../../api/clienteHttp', async (original) => ({ ...(await original<typeof import('../../api/clienteHttp')>()), solicitar: vi.fn() }))
const api = vi.mocked(solicitar)
const permisos = [PERMISOS.cajaConsultar, PERMISOS.cajaAbrir, PERMISOS.cajaOperar, PERMISOS.cajaCobrar, PERMISOS.cajaMovimientoManual, PERMISOS.cajaCerrar]
let actual: SesionCaja
const escrituras = () => api.mock.calls.filter(([, o]) => o?.method === 'POST')
const ultimoBody = () => JSON.parse(String(escrituras().at(-1)?.[1]?.body))
function navegar(ruta = '/caja/sesiones/7', autorizados: string[] = permisos) {
  return renderSeguridad(<Routes><Route path="/caja" element={<PaginaCaja />} /><Route path="/caja/cajas" element={<PaginaCajas />} /><Route path="/caja/sesiones" element={<PaginaSesionesCaja />} /><Route path="/caja/sesiones/:id" element={<PaginaSesionCaja />} /><Route path="/caja/abrir" element={<PaginaAbrirCaja />} /></Routes>, autorizados, ruta)
}
async function abrirModal(nombre: string) { if (nombre === 'Cobrar venta anterior') { const resumen = await screen.findByText('Cobros anteriores pendientes'); if (!resumen.closest('details')?.open) fireEvent.click(resumen) } fireEvent.click(await screen.findByRole('button', { name: nombre })); return screen.findByRole('dialog') }
async function elegirVenta() { await abrirModal('Cobrar venta anterior'); fireEvent.click(await screen.findByRole('button', { name: 'Cobrar VT-0000000012' })); return screen.findByLabelText('Monto aplicado 1') }
async function confirmarCobro() { fireEvent.click(screen.getByRole('button', { name: 'Revisar cobro' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar cobro' })); await waitFor(() => expect(escrituras()).toHaveLength(1)) }
function fallaOperacion(codigo: string, mensaje = 'Mensaje claro del servidor', estado = 409) {
  const previo = api.getMockImplementation()!; api.mockImplementation(async (ruta, opciones) => { if (opciones?.method) throw new ErrorApi(mensaje, estado, { codigo }); return previo(ruta, opciones) })
}
beforeEach(() => {
  sessionStorage.clear(); configurarZonaHoraria('America/Guatemala'); actual = structuredClone(sesionCaja); api.mockReset()
  api.mockImplementation(async (ruta, opciones) => {
    const body = opciones?.body ? JSON.parse(String(opciones.body)) : null
    if (ruta === '/api/caja/cajas' && body) return { ...cajaDemo, codigo: body.codigo, nombre: body.nombre } as never
    if (ruta.startsWith('/api/caja/cajas')) return paginaCaja([cajaDemo]) as never
    if (ruta.startsWith('/api/caja/referencias')) return referenciasCaja as never
    if (ruta.startsWith('/api/caja/ventas-pendientes')) return paginaCaja(actual.cobros.length ? [] : [pendienteCaja]) as never
    if (ruta === '/api/caja/sesiones/abrir') return actual as never
    if (ruta.startsWith('/api/caja/sesiones?')) return paginaCaja([actual.encabezado]) as never
    if (ruta.endsWith('/cobrar-venta')) { const r = respuestaCobro(body.pagos, body.claveOperacion); actual = r.sesion; return r as never }
    if (ruta.endsWith('/arqueo')) { actual = { ...actual, encabezado: { ...actual.encabezado, versionFila: 'ARQUEO-NUEVO' }, arqueos: [{ ...arqueoCaja, contado: body.efectivoContado, diferencia: body.efectivoContado - actual.efectivoEsperado!, observaciones: body.observaciones }] }; return actual as never }
    if (ruta.endsWith('/cerrar')) { actual = structuredClone(sesionCerrada); return actual as never }
    if (ruta.endsWith('/movimientos')) { actual = { ...actual, encabezado: { ...actual.encabezado, versionFila: 'MOVIMIENTO-NUEVO' }, arqueos: actual.arqueos.map((a) => ({ ...a, vigente: false })) }; return actual as never }
    if (ruta === '/api/caja/sesiones/7') return actual as never
    throw new Error(`Ruta no prevista: ${ruta}`)
  })
})
afterEach(() => { configurarZonaHoraria(); vi.restoreAllMocks() })

describe('compatibilidad de conceptos financieros Fase 9', () => {
  it('incluye cobros CxC y pagos CxP en los totales sin contar la apertura', () => {
    actual.totalesPorTipo = [{ tipo: 'APERTURA_CAJA', total: 100 }, { tipo: 'COBRO_VENTA', total: 15.25 },
      { tipo: 'INGRESO_CAJA', total: 2.50 }, { tipo: 'COBRO_CUENTA_POR_COBRAR', total: 30.10 },
      { tipo: 'EGRESO_CAJA', total: 1.50 }, { tipo: 'PAGO_CUENTA_POR_PAGAR', total: 20.25 }]
    expect(ingresosCaja(actual)).toBe(47.85); expect(egresosCaja(actual)).toBe(21.75)
  })
  it('conserva los importes de Caja cuando no existen operaciones financieras', () => {
    actual.totalesPorTipo = [{ tipo: 'INGRESO_CAJA', total: 7 }, { tipo: 'EGRESO_CAJA', total: 3 }]
    expect(ingresosCaja(actual)).toBe(7); expect(egresosCaja(actual)).toBe(3)
  })
  it('renderiza los nuevos totales en el resumen existente sin nuevas operaciones UI', async () => {
    actual.totalesPorTipo = [{ tipo: 'COBRO_CUENTA_POR_COBRAR', total: 30 }, { tipo: 'PAGO_CUENTA_POR_PAGAR', total: 20 }]
    navegar(); const resumen = await screen.findByRole('region', { name: 'Resumen financiero' })
    expect(resumen.textContent).toContain('GTQ 30.00'); expect(resumen.textContent).toContain('GTQ 20.00')
    expect(escrituras()).toHaveLength(0)
  })
})

describe('cajas y sesiones', () => {
  it('lista código, nombre, estado, sucursal y sesión', async () => {
    navegar('/caja/cajas'); const t = await screen.findByRole('table', { name: 'Listado de cajas' }); expect(within(t).getByText('CAJA-CENTRAL')).toBeDefined(); expect(within(t).getByText('Sucursal principal')).toBeDefined(); expect(within(t).getByRole('link', { name: 'Ver sesión' }).getAttribute('href')).toBe('/caja/sesiones/7')
  })
  it('registra una caja sin enviar empresa, sucursal ni IDs', async () => {
    navegar('/caja/cajas'); await abrirModal('Registrar caja'); fireEvent.change(screen.getByLabelText('Código'), { target: { value: 'CAJA-2' } }); fireEvent.change(screen.getByLabelText('Nombre'), { target: { value: 'Mostrador' } }); fireEvent.click(screen.getByRole('button', { name: 'Guardar caja' })); await waitFor(() => expect(escrituras()).toHaveLength(1)); expect(ultimoBody()).toEqual({ codigo: 'CAJA-2', nombre: 'Mostrador' }); await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
  })
  it('consulta no concede creación ni apertura', async () => {
    navegar('/caja/cajas', [PERMISOS.cajaConsultar]); await screen.findByRole('table'); expect(screen.queryByRole('button', { name: 'Registrar caja' })).toBeNull(); expect(screen.queryByRole('link', { name: 'Abrir caja' })).toBeNull()
  })
  it('muestra ausencia de sesión y apertura autorizada', async () => {
    api.mockResolvedValue(paginaCaja([])); navegar('/caja'); await screen.findByText('No hay una sesión de caja abierta.'); expect(screen.getByRole('link', { name: 'Abrir caja' })).toBeDefined(); expect(escrituras()).toHaveLength(0)
  })
  it('resumen operativo carga importes del detalle', async () => {
    navegar('/caja'); const resumen = await screen.findByRole('region', { name: 'Resumen financiero' }); expect(resumen.textContent).toContain('GTQ 50.00'); expect(resumen.textContent).toContain('Efectivo esperado')
  })
  it('lista y filtra sesiones con paginación', async () => {
    navegar('/caja/sesiones'); await screen.findByRole('table', { name: 'Listado de sesiones' }); fireEvent.change(screen.getByLabelText('Buscar sesión'), { target: { value: 'CA-7' } }); fireEvent.change(screen.getByLabelText('Estado'), { target: { value: 'false' } }); fireEvent.click(screen.getByRole('button', { name: 'Buscar sesiones' })); await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes('busqueda=CA-7&abierta=false'))).toBe(true)); fireEvent.click(screen.getByRole('button', { name: 'Limpiar filtros' })); expect((screen.getByLabelText('Estado') as HTMLSelectElement).value).toBe('')
  })
  it('abre con IDs entregados por referencias y selección', async () => {
    navegar('/caja/abrir?cajaId=9'); await screen.findByLabelText('Saldo inicial'); fireEvent.change(screen.getByLabelText('Saldo inicial'), { target: { value: '60' } }); fireEvent.click(screen.getByRole('button', { name: 'Confirmar apertura' })); await waitFor(() => expect(escrituras()).toHaveLength(1)); expect(ultimoBody()).toEqual({ cajaId: 9, monedaId: 6, saldoInicial: 60 }); await screen.findByRole('table', { name: 'Movimientos de caja' })
  })
  it('muestra CAJA_YA_ABIERTA sin segunda apertura automática', async () => {
    fallaOperacion('CAJA_YA_ABIERTA', 'La caja ya tiene una sesión abierta.'); navegar('/caja/abrir?cajaId=9'); fireEvent.click(await screen.findByRole('button', { name: 'Confirmar apertura' })); await screen.findByText(/CAJA_YA_ABIERTA/); expect(escrituras()).toHaveLength(1)
    const aviso = screen.getByRole('alert')
    expect(within(aviso).getByText('No se pudo abrir la caja')).toBeDefined()
    expect(within(aviso).getByText(/La caja ya tiene una sesión abierta\./)).toBeDefined()
    expect(screen.queryByText('No pudimos cargar la información')).toBeNull()
    expect(screen.getByLabelText('Saldo inicial')).toBeDefined()
  })
  it('conserva el mensaje de carga si fallan las referencias de apertura', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, opciones) => ruta === '/api/caja/referencias' ? Promise.reject(new ErrorApi('Referencias no disponibles', 500)) : previo(ruta, opciones))
    navegar('/caja/abrir'); await screen.findByText('No pudimos cargar la información')
    expect(screen.queryByText('No se pudo abrir la caja')).toBeNull(); expect(escrituras()).toHaveLength(0)
  })
  it('no abre con saldo inválido', async () => {
    navegar('/caja/abrir?cajaId=9'); const input = await screen.findByLabelText('Saldo inicial'); fireEvent.change(input, { target: { value: '-1' } }); fireEvent.submit(input.closest('form')!); await screen.findByRole('alert'); expect(escrituras()).toHaveLength(0)
  })
  it('muestra detalle sin revelar RowVersion y separa apertura de ingresos', async () => {
    navegar(); await screen.findByRole('table', { name: 'Movimientos de caja' }); expect(screen.queryByText('CAJA-V1')).toBeNull(); expect(screen.getByText('El saldo teórico incluye todas las formas de pago.', { exact: false })).toBeDefined(); expect(screen.getAllByText('Ingresos operativos').length).toBeGreaterThan(0); expect(screen.getByRole('region', { name: 'Resumen financiero' }).textContent).toContain('GTQ 0.00')
  })
  it('cerrada es solo lectura y muestra snapshots y usuario cierre', async () => {
    actual = structuredClone(sesionCerrada); navegar(); await screen.findByText('Cierre confirmado'); for (const nombre of ['Cobrar venta anterior', 'Registrar ingreso', 'Registrar arqueo', 'Cerrar sesión de caja']) expect(screen.queryByRole('button', { name: nombre })).toBeNull(); expect(screen.getByText('CC-0000000007')).toBeDefined(); expect(screen.getByText('Usuario cierre')).toBeDefined()
  })
  it('histórica sin semántica Fase 8 permanece solo lectura', async () => {
    actual.encabezado.esFase8 = false; actual.saldoTeorico = null; actual.efectivoEsperado = null; navegar(); await screen.findByText(/Sesión histórica: solo lectura/); expect(screen.queryByRole('button', { name: 'Cobrar venta anterior' })).toBeNull()
  })
  it.each([[403, 'Acceso no autorizado'], [404, 'Página no encontrada']])('distingue respuesta %s', async (estado, titulo) => { api.mockRejectedValue(new ErrorApi('Rechazada', estado as number)); navegar(); await screen.findByText(titulo as string) })
  it('id no válido produce 404 sin consultar API', async () => { navegar('/caja/sesiones/invalido'); await screen.findByText('Página no encontrada'); expect(api).not.toHaveBeenCalled() })
})

describe('cobros', () => {
  it.each([[1, 'admin'], [27, 'cajera.turno']])('muestra el usuario real del cobro %s: %s', async (usuarioId, usuario) => {
    actual = respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]).sesion
    actual.cobros = actual.cobros.map((c) => ({ ...c, usuarioId: Number(usuarioId) }))
    actual.movimientos = actual.movimientos.map((m) => m.pagoId === actual.cobros[0].id ? { ...m, usuarioId: Number(usuarioId), usuario: String(usuario) } : m)
    navegar(); const cobro = await screen.findByRole('region', { name: 'Cobro VT-0000000012' })
    expect(within(cobro).getByText(new RegExp(` · ${usuario}$`))).toBeDefined()
    expect(cobro.textContent).not.toContain('Usuario #'); expect(escrituras()).toHaveLength(0)
  })
  it('no cruza los usuarios de dos cobros aunque sus movimientos estén en otro orden', async () => {
    actual = respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]).sesion
    const otro = { ...actual.cobros[0], id: 150, ventaId: 13, numeroVenta: 'VT-0000000013', usuarioId: 27 }
    actual.cobros.push(otro)
    actual.movimientos.unshift({ ...actual.movimientos[1], id: 151, pagoId: otro.id, ventaId: otro.ventaId, numeroVenta: otro.numeroVenta, usuarioId: otro.usuarioId, usuario: 'cajera.turno' })
    navegar(); const primero = await screen.findByRole('region', { name: 'Cobro VT-0000000012' }); const segundo = screen.getByRole('region', { name: 'Cobro VT-0000000013' })
    expect(within(primero).getByText(/ · admin$/)).toBeDefined(); expect(primero.textContent).not.toContain('cajera.turno')
    expect(within(segundo).getByText(/ · cajera.turno$/)).toBeDefined(); expect(segundo.textContent).not.toContain('admin')
  })
  it.each(['pagoId', 'usuarioId'] as const)('no inventa nombre si no coincide %s del movimiento', async (campo) => {
    actual = respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]).sesion
    actual.movimientos = actual.movimientos.map((m) => m.pagoId ? { ...m, [campo]: 999 } : m)
    navegar(); const cobro = await screen.findByRole('region', { name: 'Cobro VT-0000000012' })
    expect(within(cobro).getByText(/Nombre de usuario no disponible/)).toBeDefined()
    expect(cobro.textContent).not.toContain('admin'); expect(cobro.textContent).not.toContain('Usuario #')
  })
  it('muestra solo pendientes devueltos y cobra efectivo con vuelto', async () => {
    navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Monto recibido 1'), { target: { value: '20' } }); expect(screen.getByText(/Vuelto estimado: GTQ 4.00/)).toBeDefined(); await confirmarCobro(); expect(ultimoBody()).toMatchObject({ ventaId: 12, versionFila: 'CAJA-V1', versionVenta: 'VENTA-V1', pagos: [{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 20 }] }); expect(ultimoBody().claveOperacion).toMatch(/^[\da-f-]{36}$/i); await screen.findByText(/Cobro registrado para/); expect(screen.getByRole('region', { name: 'Resumen financiero' }).textContent).toContain('GTQ 66.00')
  })
  it('tarjeta iguala recibido a aplicado y no pide datos sensibles', async () => {
    navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Forma de pago 1'), { target: { value: '34' } }); expect(screen.queryByLabelText('Monto recibido 1')).toBeNull(); expect(screen.queryByLabelText(/PAN|CVV|vencimiento/i)).toBeNull(); expect(screen.getByText('No ingresar datos de tarjeta.')).toBeDefined(); fireEvent.change(screen.getByLabelText('Referencia/autorización opcional 1'), { target: { value: 'AUTH-32' } }); await confirmarCobro(); expect(ultimoBody().pagos).toEqual([{ formaPagoId: 34, montoAplicado: 16, montoRecibido: 16, referencia: 'AUTH-32' }]); expect(sessionStorage.length).toBe(1); expect(sessionStorage.getItem(sessionStorage.key(0)!)).not.toContain('AUTH-32')
  })
  it('validación de referencia muestra backend y borra el valor, sin transformarlo', async () => {
    fallaOperacion('VALIDACION_CAJA', 'La referencia debe ser un identificador seguro; nunca datos de tarjeta.', 400); navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Forma de pago 1'), { target: { value: '34' } }); fireEvent.change(screen.getByLabelText('Referencia/autorización opcional 1'), { target: { value: 'referencia-no-permitida' } }); await confirmarCobro(); await screen.findByText(/VALIDACION_CAJA/); expect((screen.getByLabelText('Referencia/autorización opcional 1') as HTMLInputElement).value).toBe(''); expect(ultimoBody().pagos[0].referencia).toBe('referencia-no-permitida'); expect(sessionStorage.getItem(sessionStorage.key(0)!)).not.toContain('referencia')
  })
  it('mixto valida suma exacta y separa teórico del efectivo', async () => {
    navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Monto aplicado 1'), { target: { value: '10' } }); fireEvent.change(screen.getByLabelText('Monto recibido 1'), { target: { value: '10' } }); expect((screen.getByRole('button', { name: 'Revisar cobro' }) as HTMLButtonElement).disabled).toBe(true); fireEvent.click(screen.getByRole('button', { name: 'Agregar forma de pago' })); fireEvent.change(screen.getByLabelText('Monto aplicado 2'), { target: { value: '6' } }); expect(screen.queryByRole('button', { name: 'Agregar forma de pago' })).toBeNull(); await confirmarCobro(); expect(ultimoBody().pagos).toEqual([{ formaPagoId: 21, montoAplicado: 10, montoRecibido: 10 }, { formaPagoId: 34, montoAplicado: 6, montoRecibido: 6 }]); await screen.findByText(/Cobro registrado/); const r = screen.getByRole('region', { name: 'Resumen financiero' }); expect(r.textContent).toContain('GTQ 66.00'); expect(r.textContent).toContain('GTQ 60.00')
  })
  it('no envía si recibido es menor o importes tienen más de dos decimales', async () => {
    navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Monto recibido 1'), { target: { value: '10' } }); expect((screen.getByRole('button', { name: 'Revisar cobro' }) as HTMLButtonElement).disabled).toBe(true); fireEvent.change(screen.getByLabelText('Monto aplicado 1'), { target: { value: '16.001' } }); expect((screen.getByRole('button', { name: 'Revisar cobro' }) as HTMLButtonElement).disabled).toBe(true); expect(escrituras()).toHaveLength(0)
  })
  it('doble clic produce un único POST', async () => {
    let resolver!: (v: unknown) => void; const previo = api.getMockImplementation()!; api.mockImplementation((ruta, opciones) => ruta.endsWith('/cobrar-venta') ? new Promise((r) => { resolver = r }) as never : previo(ruta, opciones)); navegar(); await elegirVenta(); fireEvent.click(screen.getByRole('button', { name: 'Revisar cobro' })); const boton = screen.getByRole('button', { name: 'Confirmar cobro' }); fireEvent.click(boton); fireEvent.click(boton); expect(escrituras()).toHaveLength(1); expect((boton as HTMLButtonElement).disabled).toBe(true); await act(async () => resolver(respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]))); await screen.findByText(/Cobro registrado/)
  })
  it('VENTA_YA_COBRADA bloquea nuevo intento y permite actualizar pendientes', async () => {
    fallaOperacion('VENTA_YA_COBRADA', 'La venta ya está cobrada.'); navegar(); await elegirVenta(); await confirmarCobro(); await screen.findByText(/VENTA_YA_COBRADA/); expect(screen.queryByRole('button', { name: 'Revisar cobro' })).toBeNull(); actual = respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]).sesion; fireEvent.click(screen.getByRole('button', { name: 'Consultar resultado y pendientes' })); await screen.findByText('La venta ya tiene un cobro registrado en esta sesión.'); await abrirModal('Cobrar venta anterior'); await screen.findByText('No hay ventas pendientes'); expect(escrituras()).toHaveLength(1)
  })
  it('después de cobro el arqueo envía la nueva versionFila', async () => {
    navegar(); await elegirVenta(); await confirmarCobro(); await screen.findByText(/Cobro registrado/); await abrirModal('Registrar arqueo'); fireEvent.change(screen.getByLabelText('Efectivo contado'), { target: { value: '66' } }); fireEvent.click(screen.getByRole('button', { name: 'Guardar arqueo' })); await waitFor(() => expect(escrituras()).toHaveLength(2)); expect(ultimoBody().versionFila).toBe('CAJA-COBRADA')
  })
  it('respuesta perdida obliga a consultar antes de ofrecer un reintento controlado', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, opciones) => ruta.endsWith('/cobrar-venta') ? Promise.reject(new TypeError('Respuesta perdida')) : previo(ruta, opciones))
    navegar(); await elegirVenta(); await confirmarCobro(); await screen.findByText(/No se pudo confirmar el cobro/)
    expect(screen.queryByRole('button', { name: 'Revisar cobro' })).toBeNull()
    const uuid = ultimoBody().claveOperacion
    fireEvent.click(screen.getByRole('button', { name: 'Consultar resultado y pendientes' })); await screen.findByText(/Sesión actualizada. Consulte los pendientes/)
    api.mockImplementation(previo); await elegirVenta(); fireEvent.click(screen.getByRole('button', { name: 'Revisar cobro' })); fireEvent.click(screen.getByRole('button', { name: 'Confirmar cobro' }))
    await screen.findByText(/Cobro registrado para/); expect(escrituras()).toHaveLength(2); expect(ultimoBody().claveOperacion).toBe(uuid)
  })
  it('venta ya cobrada recarga automáticamente los pendientes sin ofrecer nueva UUID', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta.endsWith('/cobrar-venta')) { actual = respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 16 }]).sesion; throw new ErrorApi('Ya cobrada', 409, { codigo: 'VENTA_YA_COBRADA' }) }
      return previo(ruta, opciones)
    })
    navegar(); await elegirVenta(); await confirmarCobro(); await screen.findByText(/0 ventas pendientes/)
    expect(api.mock.calls.filter(([r]) => r.startsWith('/api/caja/ventas-pendientes'))).toHaveLength(2)
    expect(screen.queryByRole('button', { name: 'Revisar cobro' })).toBeNull(); expect(escrituras()).toHaveLength(1)
  })
  it('respeta referencia requerida por configuración sin asumir que toda tarjeta la exige', async () => {
    const previo = api.getMockImplementation()!
    api.mockImplementation((ruta, opciones) => ruta === '/api/caja/referencias' ? Promise.resolve({ ...referenciasCaja, formasPago: referenciasCaja.formasPago.map((f) => ({ ...f, requiereReferencia: !f.esEfectivo })) }) as never : previo(ruta, opciones))
    navegar(); await elegirVenta(); fireEvent.change(screen.getByLabelText('Forma de pago 1'), { target: { value: '34' } })
    expect((screen.getByRole('button', { name: 'Revisar cobro' }) as HTMLButtonElement).disabled).toBe(true)
    fireEvent.change(screen.getByLabelText('Referencia/autorización requerida 1'), { target: { value: 'AUTH-32' } }); await confirmarCobro()
    expect(ultimoBody().pagos[0].referencia).toBe('AUTH-32')
  })
})

describe('movimientos, arqueos, cierre y permisos', () => {
  it.each(['INGRESO', 'EGRESO'])('registra %s manual con motivo y versión', async (tipo) => {
    navegar(); await abrirModal('Registrar ingreso'); fireEvent.change(screen.getByLabelText('Tipo de movimiento'), { target: { value: tipo } }); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: '5' } }); fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Fondo operativo' } }); fireEvent.click(screen.getByRole('button', { name: 'Registrar movimiento' })); await waitFor(() => expect(escrituras()).toHaveLength(1)); expect(ultimoBody()).toMatchObject({ tipo, monto: 5, motivo: 'Fondo operativo', versionFila: 'CAJA-V1' })
  })
  it('no permite egreso superior al efectivo ni importes cero', async () => {
    navegar(); await abrirModal('Registrar ingreso'); fireEvent.change(screen.getByLabelText('Tipo de movimiento'), { target: { value: 'EGRESO' } }); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: '51' } }); fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Retiro' } }); fireEvent.click(screen.getByRole('button', { name: 'Registrar movimiento' })); await screen.findByText(/supera el efectivo/); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: '0' } }); fireEvent.submit(screen.getByLabelText('Monto').closest('form')!); await screen.findByText(/importe positivo/); expect(escrituras()).toHaveLength(0)
  })
  it('rechazo backend por saldo no cambia cifras', async () => {
    fallaOperacion('CONFLICTO_CAJA', 'El egreso supera el efectivo disponible.'); navegar(); await abrirModal('Registrar ingreso'); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: '5' } }); fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Retiro' } }); fireEvent.click(screen.getByRole('button', { name: 'Registrar movimiento' })); await screen.findByText(/El egreso supera/); expect(screen.getByRole('region', { name: 'Resumen financiero' }).textContent).toContain('GTQ 50.00')
  })
  it.each([[50, 'Sin diferencia'], [52, 'Sobrante'], [48, 'Faltante']])('arqueo contado %s y diferencia %s', async (contado, etiqueta) => {
    navegar(); await abrirModal('Registrar arqueo'); fireEvent.change(screen.getByLabelText('Efectivo contado'), { target: { value: String(contado) } }); expect(within(screen.getByRole('dialog')).getByText(new RegExp(String(etiqueta)))).toBeDefined(); fireEvent.click(screen.getByRole('button', { name: 'Guardar arqueo' })); await screen.findByText('Vigente'); expect(ultimoBody()).toMatchObject({ efectivoContado: contado, versionFila: 'CAJA-V1' }); expect((screen.getByRole('button', { name: 'Cerrar sesión de caja' }) as HTMLButtonElement).disabled).toBe(false)
  })
  it('arqueo admite contado cero', async () => {
    navegar(); await abrirModal('Registrar arqueo'); fireEvent.change(screen.getByLabelText('Efectivo contado'), { target: { value: '0' } }); fireEvent.click(screen.getByRole('button', { name: 'Guardar arqueo' })); await waitFor(() => expect(escrituras()).toHaveLength(1)); expect(ultimoBody().efectivoContado).toBe(0)
  })
  it('no permite cerrar sin arqueo vigente', async () => { navegar(); const boton = await screen.findByRole('button', { name: 'Cerrar sesión de caja' }); expect((boton as HTMLButtonElement).disabled).toBe(true); expect(escrituras()).toHaveLength(0) })
  it('cierra con arqueo vigente, confirma snapshots y queda solo lectura', async () => {
    actual = structuredClone(sesionArqueada); navegar(); const d = await abrirModal('Cerrar sesión de caja'); expect(within(d).getByText(/Faltante/)).toBeDefined(); fireEvent.click(within(d).getByRole('button', { name: 'Confirmar cierre' })); await screen.findByText('Cierre confirmado'); expect(ultimoBody()).toEqual({ versionFila: 'CAJA-V2', arqueoId: 80 }); expect(screen.queryByRole('button', { name: 'Cobrar venta anterior' })).toBeNull()
  })
  it('un movimiento invalida arqueo y bloquea cierre hasta otro conteo', async () => {
    actual = structuredClone(sesionArqueada); navegar(); await abrirModal('Registrar ingreso'); fireEvent.change(screen.getByLabelText('Monto'), { target: { value: '1' } }); fireEvent.change(screen.getByLabelText('Motivo'), { target: { value: 'Ingreso' } }); fireEvent.click(screen.getByRole('button', { name: 'Registrar movimiento' })); await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull()); expect((screen.getByRole('button', { name: 'Cerrar sesión de caja' }) as HTMLButtonElement).disabled).toBe(true)
  })
  it('conflicto de versión explica recarga sin reintento automático', async () => {
    fallaOperacion('CONCURRENCIA_CAJA'); navegar(); await abrirModal('Registrar arqueo'); fireEvent.change(screen.getByLabelText('Efectivo contado'), { target: { value: '50' } }); fireEvent.click(screen.getByRole('button', { name: 'Guardar arqueo' })); await screen.findByText(/La sesión cambió desde que la abriste/); expect(escrituras()).toHaveLength(1)
  })
  it.each([
    [[], []], [[PERMISOS.cajaAbrir], []], [[PERMISOS.cajaCobrar], []], [[PERMISOS.cajaMovimientoManual], []],
    [[PERMISOS.cajaOperar], ['Registrar arqueo']], [[PERMISOS.cajaCerrar], ['Cerrar sesión de caja']],
    [[PERMISOS.cajaOperar, PERMISOS.cajaCobrar], ['Cobrar venta anterior', 'Registrar arqueo']],
    [[PERMISOS.cajaOperar, PERMISOS.cajaMovimientoManual], ['Registrar ingreso', 'Registrar arqueo']],
    [[PERMISOS.cajaAnular], []],
  ])('acciones corresponden exactamente a permisos %s', async (extras, visibles) => {
    navegar('/caja/sesiones/7', [PERMISOS.cajaConsultar, ...extras]); await screen.findByRole('table', { name: 'Movimientos de caja' }); const anteriores = screen.queryByText('Cobros anteriores pendientes'); if (anteriores) fireEvent.click(anteriores); for (const nombre of ['Cobrar venta anterior', 'Registrar ingreso', 'Registrar arqueo', 'Cerrar sesión de caja']) expect(Boolean(screen.queryByRole('button', { name: nombre }))).toBe((visibles as string[]).includes(nombre)); expect(screen.queryByRole('button', { name: /Anular/ })).toBeNull()
  })
  it('todos los timestamps operativos Z usan Guatemala, no hora del navegador', async () => {
    actual = { ...respuestaCobro([{ formaPagoId: 21, montoAplicado: 16, montoRecibido: 20 }]).sesion, encabezado: sesionCerrada.encabezado, arqueos: [arqueoCaja], usuarioCierre: 'admin' }; navegar(); await screen.findByText('Cierre confirmado'); expect(screen.getAllByText(/2:16/).length).toBeGreaterThan(0); expect(screen.getAllByText(/2:42/).length).toBeGreaterThan(0); expect(screen.getAllByText(/2:30/).length).toBeGreaterThan(0); expect(screen.getAllByText(/2:37/).length).toBeGreaterThan(0); expect(screen.queryByText(/8:42/)).toBeNull()
  })
  it('modal captura foco, admite Escape y devuelve foco al botón', async () => {
    navegar(); const boton = await screen.findByRole('button', { name: 'Registrar arqueo' }); boton.focus(); fireEvent.click(boton); const d = await screen.findByRole('dialog'); expect(d.contains(document.activeElement)).toBe(true); fireEvent.keyDown(document, { key: 'Escape' }); await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull()); expect(document.activeElement).toBe(boton)
  })
  it('Tab y Shift+Tab permanecen dentro del modal', async () => {
    navegar(); const d = await abrirModal('Registrar arqueo'); const primero = within(d).getByRole('button', { name: 'Cerrar ventana' }); const ultimo = within(d).getByRole('button', { name: 'Guardar arqueo' })
    ultimo.focus(); fireEvent.keyDown(ultimo, { key: 'Tab' }); expect(document.activeElement).toBe(primero)
    fireEvent.keyDown(primero, { key: 'Tab', shiftKey: true }); expect(document.activeElement).toBe(ultimo)
  })
  it('tablas conservan etiquetas para tarjetas responsivas', async () => {
    navegar(); const t = await screen.findByRole('table', { name: 'Movimientos de caja' }); expect(t.querySelector('td[data-etiqueta="Fecha"]')).not.toBeNull(); expect(t.querySelector('td[data-etiqueta="Observación"]')).not.toBeNull()
  })
})
it.each([['0.10', 10], ['0', 0], ['9999999999.99', 999999999999], ['0.001', null], ['-1', null], ['', null], ['1e3', null]])('parsea %s sin errores de coma flotante', (valor, esperado) => { expect(centavos(String(valor))).toBe(esperado) })
