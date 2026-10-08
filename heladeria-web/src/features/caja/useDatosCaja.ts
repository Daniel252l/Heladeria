import { ErrorApi } from '../../api/clienteHttp'
import { cajaApi } from '../../api/caja'
import type { FormaCobro, ResultadoCobroCaja, SesionCaja, VentaPendienteCaja } from '../../types/caja'
// Reutilizar la carga cancelable y la exclusión síncrona ya probadas; no duplicar sus mecanismos.
export { useRecursoVentas as useRecursoCaja, useOperacionVenta as useOperacionCaja } from '../ventas/useDatosVentas'

export const claveIntentoCobro = (usuario: number, empresa: number, sucursal: number, sesion: number, venta: number) =>
  `heladeria:caja:cobro:${usuario}:${empresa}:${sucursal}:${sesion}:${venta}`

export class CobroIncierto extends Error {}
export async function cobrarConReconciliacion(sesion: SesionCaja, venta: VentaPendienteCaja, pagos: FormaCobro[], clave: string): Promise<ResultadoCobroCaja> {
  let uuid: string | null
  try { uuid = sessionStorage.getItem(clave) }
  catch { throw new Error('No se pudo recuperar la clave del cobro. No se enviará otra operación.') }
  const conciliar = async () => {
    const actual = await cajaApi.obtener(sesion.encabezado.id)
    const cobro = actual.cobros.find((c) => c.ventaId === venta.id)
    return { actual, cobro }
  }
  if (uuid) {
    const { actual, cobro } = await conciliar() // Nunca reenviar si esta lectura falla.
    if (cobro) return { sesion: actual, cobro, reintento: true }
    if (actual.encabezado.versionFila !== sesion.encabezado.versionFila)
      throw new ErrorApi('La sesión cambió desde que la abriste. Recarga la información.', 409, { codigo: 'CONCURRENCIA_CAJA' })
    if (actual.encabezado.estado !== 'ABIERTA') throw new ErrorApi('La sesión de caja no está abierta.', 409, { codigo: 'CAJA_NO_ABIERTA' })
  } else {
    uuid = crypto.randomUUID()
    try { sessionStorage.setItem(clave, uuid) }
    catch { throw new Error('No es posible conservar la clave de cobro. Habilite el almacenamiento de sesión antes de cobrar.') }
  }
  // Persistimos SOLO UUID. Nunca pagos, referencias, credenciales ni datos de tarjeta.
  try {
    return await cajaApi.cobrar(sesion.encabezado.id, { versionFila: sesion.encabezado.versionFila,
      versionVenta: venta.versionFila, ventaId: venta.id, claveOperacion: uuid, pagos })
  } catch (error) {
    if (error instanceof ErrorApi && error.estado < 500 && error.estado !== 408) throw error
    try {
      const { actual, cobro } = await conciliar()
      if (cobro) return { sesion: actual, cobro, reintento: true }
    } catch { /* Resultado desconocido: conservar UUID, no repetir POST automáticamente. */ }
    throw new CobroIncierto('No se pudo confirmar el cobro. La clave se conservó. Consulta el resultado antes de reintentar; nunca se enviará automáticamente un segundo cobro.')
  }
}
