import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cajaApi } from '../../api/caja'
import { ErrorApi } from '../../api/clienteHttp'
import { pendienteCaja, respuestaCobro, sesionCaja, sesionCerrada } from '../../test/datosFaseVisual6'
import { claveIntentoCobro, CobroIncierto, cobrarConReconciliacion } from './useDatosCaja'

vi.mock('../../api/caja', () => ({ cajaApi: { obtener: vi.fn(), cobrar: vi.fn() } }))
const leer = vi.mocked(cajaApi.obtener)
const cobrar = vi.mocked(cajaApi.cobrar)
const clave = claveIntentoCobro(1, 4, 4, 7, 12)
const uuid = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee'
const pagos = [{ formaPagoId: 34, montoAplicado: 16, montoRecibido: 16, referencia: 'AUTH-32' }]
const ejecutar = () => cobrarConReconciliacion(sesionCaja, pendienteCaja, pagos, clave)
beforeEach(() => { vi.resetAllMocks(); sessionStorage.clear(); leer.mockResolvedValue(sesionCaja); cobrar.mockResolvedValue(respuestaCobro(pagos, uuid)) })
afterEach(() => vi.restoreAllMocks())

describe('idempotencia de caja y reconciliación sin datos sensibles persistidos', () => {
  it('genera una UUID, solo guarda esa UUID y transmite versiones exactas', async () => {
    const random = vi.spyOn(crypto, 'randomUUID').mockReturnValue(uuid)
    await ejecutar()
    expect(random).toHaveBeenCalledTimes(1); expect(sessionStorage.getItem(clave)).toBe(uuid); expect(sessionStorage.length).toBe(1)
    expect(cobrar).toHaveBeenCalledWith(7, { claveOperacion: uuid, versionFila: 'CAJA-V1', versionVenta: 'VENTA-V1', ventaId: 12, pagos })
    expect(leer).not.toHaveBeenCalled()
  })
  it('consulta antes de reintentar y usa la misma UUID', async () => {
    sessionStorage.setItem(clave, uuid); const random = vi.spyOn(crypto, 'randomUUID')
    await ejecutar(); expect(leer.mock.invocationCallOrder[0]).toBeLessThan(cobrar.mock.invocationCallOrder[0]); expect(random).not.toHaveBeenCalled()
    expect(cobrar.mock.calls[0][1].claveOperacion).toBe(uuid)
  })
  it('reconcilia cobro existente incluso después del cierre sin otro POST', async () => {
    sessionStorage.setItem(clave, uuid); const resultado = respuestaCobro(pagos, uuid)
    leer.mockResolvedValue({ ...sesionCerrada, cobros: [resultado.cobro] })
    expect((await ejecutar()).reintento).toBe(true); expect(cobrar).not.toHaveBeenCalled()
  })
  it('recupera respuesta perdida con consulta, sin repetir POST', async () => {
    cobrar.mockRejectedValue(new TypeError('Fallo de red')); leer.mockResolvedValue(respuestaCobro(pagos, uuid).sesion)
    expect((await ejecutar()).reintento).toBe(true); expect(cobrar).toHaveBeenCalledTimes(1); expect(leer).toHaveBeenCalledTimes(1)
  })
  it('resultado aún desconocido conserva UUID y no repite hasta una nueva acción', async () => {
    cobrar.mockRejectedValueOnce(new TypeError('Fallo de red')); leer.mockRejectedValueOnce(new TypeError('Lectura no disponible'))
    await expect(ejecutar()).rejects.toBeInstanceOf(CobroIncierto)
    const guardada = sessionStorage.getItem(clave); expect(guardada).toBeTruthy(); expect(cobrar).toHaveBeenCalledTimes(1)
    await ejecutar(); expect(cobrar).toHaveBeenCalledTimes(2); expect(cobrar.mock.calls[1][1].claveOperacion).toBe(guardada)
    expect(leer.mock.invocationCallOrder[1]).toBeLessThan(cobrar.mock.invocationCallOrder[1])
  })
  it.each([408, 500, 503])('HTTP %s incierto consulta y nunca reenvía automáticamente', async (estado) => {
    cobrar.mockRejectedValue(new ErrorApi('Error temporal', estado))
    await expect(ejecutar()).rejects.toBeInstanceOf(CobroIncierto); expect(leer).toHaveBeenCalledTimes(1); expect(cobrar).toHaveBeenCalledTimes(1)
  })
  it.each([400, 403, 409])('HTTP %s definitivo conserva el error original sin repetir', async (estado) => {
    const error = new ErrorApi('Rechazo explícito', estado); cobrar.mockRejectedValue(error)
    await expect(ejecutar()).rejects.toBe(error); expect(leer).not.toHaveBeenCalled(); expect(cobrar).toHaveBeenCalledTimes(1)
  })
  it('lectura previa rechazada prohíbe reenviar', async () => {
    sessionStorage.setItem(clave, uuid); leer.mockRejectedValue(new ErrorApi('Prohibido', 403))
    await expect(ejecutar()).rejects.toMatchObject({ estado: 403 }); expect(cobrar).not.toHaveBeenCalled()
  })
  it('cambio de versión durante reconciliación exige recarga', async () => {
    sessionStorage.setItem(clave, uuid); leer.mockResolvedValue({ ...sesionCaja, encabezado: { ...sesionCaja.encabezado, versionFila: 'OTRA' } })
    await expect(ejecutar()).rejects.toMatchObject({ codigo: 'CONCURRENCIA_CAJA' }); expect(cobrar).not.toHaveBeenCalled()
  })
  it('sesión cerrada sin cobro no admite reintento', async () => {
    sessionStorage.setItem(clave, uuid); leer.mockResolvedValue({ ...sesionCerrada, encabezado: { ...sesionCerrada.encabezado, versionFila: sesionCaja.encabezado.versionFila } })
    await expect(ejecutar()).rejects.toMatchObject({ codigo: 'CAJA_NO_ABIERTA' }); expect(cobrar).not.toHaveBeenCalled()
  })
  it.each(['getItem', 'setItem'] as const)('almacenamiento bloqueado (%s) impide envío inseguro', async (metodo) => {
    vi.spyOn(Storage.prototype, metodo).mockImplementation(() => { throw new Error('Bloqueado') })
    await expect(ejecutar()).rejects.toThrow(); expect(cobrar).not.toHaveBeenCalled()
  })
  it('separa intentos por usuario, empresa, sucursal, sesión y venta', () => {
    const claves = [clave, claveIntentoCobro(2, 4, 4, 7, 12), claveIntentoCobro(1, 5, 4, 7, 12), claveIntentoCobro(1, 4, 5, 7, 12), claveIntentoCobro(1, 4, 4, 8, 12), claveIntentoCobro(1, 4, 4, 7, 13)]
    expect(new Set(claves).size).toBe(6)
  })
})
