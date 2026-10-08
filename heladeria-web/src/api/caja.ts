import { solicitar } from './clienteHttp'
import type { Caja, SesionCaja, ResultadoCobroCaja, SolicitudCobro } from '../types/caja'

const post = <T>(ruta: string, datos: object) => solicitar<T>(`/api/caja/${ruta}`, { method: 'POST', body: JSON.stringify(datos) })
export const cajaApi = {
  obtener: (id: number, signal?: AbortSignal) => solicitar<SesionCaja>(`/api/caja/sesiones/${id}`, { signal }),
  crear: (codigo: string, nombre: string) => post<Caja>('cajas', { codigo, nombre }),
  abrir: (cajaId: number, monedaId: number, saldoInicial: number) => post<SesionCaja>('sesiones/abrir', { cajaId, monedaId, saldoInicial }),
  cobrar: (id: number, datos: SolicitudCobro) => post<ResultadoCobroCaja>(`sesiones/${id}/cobrar-venta`, datos),
  movimiento: (id: number, datos: { versionFila: string; tipo: string; monto: number; motivo: string; observacion: string }) => post<SesionCaja>(`sesiones/${id}/movimientos`, datos),
  arqueo: (id: number, versionFila: string, efectivoContado: number, observaciones: string) => post<SesionCaja>(`sesiones/${id}/arqueo`, { versionFila, efectivoContado, observaciones }),
  cerrar: (id: number, versionFila: string, arqueoId: number) => post<SesionCaja>(`sesiones/${id}/cerrar`, { versionFila, arqueoId }),
}
