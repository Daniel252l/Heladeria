import { useCallback } from 'react'
import { consultarReporte, rutaReporte } from '../../api/reportes'
import type { ResultadoReporte, TipoReporte } from '../../types/reportes'
import { useCargaVentas } from '../ventas/useDatosVentas'
import { consultaReportes, type FiltrosEditables } from './utilidadesReportes'

export function useReporte(tipo: TipoReporte, filtros: FiltrosEditables, pagina: number, tamanio: number) {
  const ruta = rutaReporte(tipo, consultaReportes(filtros, pagina, tamanio))
  const cargar = useCallback(async (signal: AbortSignal): Promise<ResultadoReporte> => {
    // StrictMode monta/limpia el efecto en el mismo turno. No iniciar un GET ya cancelado.
    await Promise.resolve()
    signal.throwIfAborted()
    const f = consultaReportes(filtros, pagina, tamanio)
    // La discriminación conserva el tipo concreto de cada respuesta, sin casts ni arrays raíz inventados.
    switch (tipo) {
      case 'ventas': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'productos-vendidos': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'inventario': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'kardex': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'compras': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'caja': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'cuentas-por-cobrar': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'cuentas-por-pagar': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
      case 'auditoria': return { tipo, respuesta: await consultarReporte(tipo, f, signal) }
    }
  }, [tipo, filtros, pagina, tamanio])
  return useCargaVentas(ruta, cargar)
}
export function paginadoReporte(r: ResultadoReporte) {
  switch (r.tipo) {
    case 'ventas': return r.respuesta.ventas.datos
    case 'productos-vendidos': case 'kardex': case 'auditoria': return r.respuesta
    default: return r.respuesta.datos
  }
}
export function periodoReporte(r: ResultadoReporte) {
  switch (r.tipo) {
    case 'ventas': return r.respuesta.ventas.periodo
    case 'compras': case 'caja': case 'cuentas-por-cobrar': case 'cuentas-por-pagar': return r.respuesta.periodo
    default: return null
  }
}
