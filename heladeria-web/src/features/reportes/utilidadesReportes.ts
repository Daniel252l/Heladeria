import { parametrosReporte } from '../../api/reportes'
import type { FiltrosPorReporte, TipoReporte } from '../../types/reportes'
import { formatoCosto, formatoNumero } from '../../utils/formato'
import { dineroCaja } from '../caja/utilidadesCaja'

export const importeReporte = (n: number | null, moneda: string | null) => n == null ? '—' : dineroCaja(n, moneda)
export const costoReporte = (n: number | null, moneda?: string | null) => n == null ? '—' : `${moneda ? `${moneda} ` : ''}${formatoCosto.format(n)}${moneda === null ? ' (sin moneda)' : ''}`
export const cantidadReporte = (n: number | null) => n == null ? '—' : formatoNumero.format(n)
export const modalidadReporte = (credito: boolean | null) => credito === true ? 'Crédito' : credito === false ? 'Contado' : 'Sin definir'

export type CampoFiltro = Exclude<(typeof parametrosReporte)[TipoReporte][number], 'pagina' | 'tamanioPagina'>
export type FiltrosEditables = Partial<Record<CampoFiltro, string>>
// Superconjunto únicamente local; el cliente HTTP admite solo las claves del contrato de cada reporte.
type ConsultaReportes = FiltrosPorReporte['ventas'] & FiltrosPorReporte['inventario'] & FiltrosPorReporte['kardex']
  & FiltrosPorReporte['compras'] & FiltrosPorReporte['caja'] & FiltrosPorReporte['cuentas-por-cobrar'] & FiltrosPorReporte['auditoria']
const id = (valor?: string) => valor ? Number(valor) : undefined
const texto = (valor?: string) => valor?.trim() || undefined
export function consultaReportes(f: FiltrosEditables, pagina: number, tamanioPagina: number): ConsultaReportes {
  return {
    sucursalId: id(f.sucursalId), busqueda: texto(f.busqueda), pagina, tamanioPagina,
    fechaDesde: texto(f.fechaDesde), fechaHasta: texto(f.fechaHasta), clienteId: id(f.clienteId),
    productoId: id(f.productoId), monedaId: id(f.monedaId), estado: texto(f.estado), bodegaId: id(f.bodegaId),
    ubicacionId: id(f.ubicacionId), concepto: texto(f.concepto),
    tipo: f.tipo === 'ENTRADA' || f.tipo === 'SALIDA' ? f.tipo : undefined,
    proveedorId: id(f.proveedorId), esCredito: f.esCredito === 'true' ? true : f.esCredito === 'false' ? false : undefined,
    cajaId: id(f.cajaId), usuarioId: id(f.usuarioId), terceroId: id(f.terceroId), vencimiento: texto(f.vencimiento),
    modulo: texto(f.modulo), accion: texto(f.accion), entidad: texto(f.entidad),
  }
}
