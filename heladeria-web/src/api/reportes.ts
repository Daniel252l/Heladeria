import { solicitar } from './clienteHttp'
import type { FiltrosPorReporte, RespuestasReportes, TipoReporte } from '../types/reportes'

const comunes = ['sucursalId', 'busqueda', 'pagina', 'tamanioPagina'] as const
const periodo = [...comunes, 'fechaDesde', 'fechaHasta'] as const
export const parametrosReporte = {
  ventas: [...periodo, 'clienteId', 'estado', 'monedaId'],
  'productos-vendidos': [...periodo, 'productoId', 'clienteId', 'monedaId'],
  inventario: [...comunes, 'productoId', 'bodegaId', 'ubicacionId'],
  kardex: [...periodo, 'productoId', 'bodegaId', 'concepto', 'tipo'],
  compras: [...periodo, 'proveedorId', 'monedaId', 'estado', 'esCredito'],
  caja: [...periodo, 'cajaId', 'usuarioId', 'estado'],
  'cuentas-por-cobrar': [...periodo, 'terceroId', 'monedaId', 'estado', 'vencimiento'],
  'cuentas-por-pagar': [...periodo, 'terceroId', 'monedaId', 'estado', 'vencimiento'],
  auditoria: [...periodo, 'usuarioId', 'modulo', 'accion', 'entidad'],
} as const satisfies { [K in TipoReporte]: readonly (keyof FiltrosPorReporte[K])[] }

export function rutaReporte<K extends TipoReporte>(tipo: K, filtros: FiltrosPorReporte[K]) {
  const permitidos: readonly string[] = parametrosReporte[tipo]
  const query = new URLSearchParams()
  for (const [nombre, valor] of Object.entries(filtros)) {
    if (permitidos.includes(nombre) && valor !== undefined && valor !== null && valor !== '') query.set(nombre, String(valor))
  }
  return `${tipo === 'auditoria' ? '/api/auditoria' : `/api/reportes/${tipo}`}?${query}`
}

export const consultarReporte = <K extends TipoReporte>(tipo: K, filtros: FiltrosPorReporte[K], signal?: AbortSignal) =>
  solicitar<RespuestasReportes[K]>(rutaReporte(tipo, filtros), { signal })
