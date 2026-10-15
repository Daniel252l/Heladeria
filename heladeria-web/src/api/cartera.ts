import { solicitar } from './clienteHttp'
import type { Paginado } from '../types/api'
import type { DetalleCartera, TipoCartera, SolicitudPagoCartera, SolicitudFacturaCartera, ResultadoPagoCartera, ResultadoFacturaCartera } from '../types/cartera'

export const rutaCartera = (tipo: TipoCartera) => `/cuentas-por-${tipo}`
export const apiCartera = (tipo: TipoCartera) => `/api${rutaCartera(tipo)}`
export const carteraApi = {
  obtener: (tipo: TipoCartera, id: number, signal?: AbortSignal) => solicitar<DetalleCartera>(`${apiCartera(tipo)}/${id}`, { signal }),
  aplicar: (tipo: TipoCartera, id: number, datos: SolicitudPagoCartera) => solicitar<ResultadoPagoCartera>(`${apiCartera(tipo)}/${id}/${tipo === 'cobrar' ? 'abonos' : 'pagos'}`, { method: 'POST', body: JSON.stringify(datos) }),
  facturar: (datos: SolicitudFacturaCartera) => solicitar<ResultadoFacturaCartera>('/api/cuentas-por-pagar/desde-compra', { method: 'POST', body: JSON.stringify(datos) }),
}
// Para exclusiones/selecciones que necesitan el conjunto completo, nunca solo la primera página.
export async function leerPaginas<T>(ruta: string, signal?: AbortSignal): Promise<T[]> {
  const elementos: T[] = []
  for (let pagina = 1; ; pagina++) {
    signal?.throwIfAborted()
    const r = await solicitar<Paginado<T>>(`${ruta}${ruta.includes('?') ? '&' : '?'}pagina=${pagina}&tamanioPagina=100`, { signal })
    elementos.push(...r.elementos)
    if (pagina >= r.totalPaginas) return elementos
  }
}
