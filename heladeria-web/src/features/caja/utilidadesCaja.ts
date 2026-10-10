import { ErrorApi } from '../../api/clienteHttp'
import type { SesionCaja } from '../../types/caja'
export { mostrarFecha as fechaCaja } from '../../utils/fechas'
export { enlaceVenta as enlaceCaja, enlaceVentaPrimario as enlaceCajaPrimario } from '../ventas/ComponentesVentas'
import { dineroVenta } from '../ventas/utilidadesVentas'
export const dineroCaja = (valor: number, moneda: string | null) => moneda ? dineroVenta(valor, moneda)
  : `${new Intl.NumberFormat('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(valor)} (sin moneda)`

export function centavos(valor: string): number | null {
  if (!/^\d{1,10}(\.\d{1,2})?$/.test(valor)) return null
  const [entero, decimal = ''] = valor.split('.')
  const n = Number(entero) * 100 + Number(decimal.padEnd(2, '0'))
  return n <= 999999999999 ? n : null
}
export const operable = (s: SesionCaja) => s.encabezado.esFase8 && s.encabezado.estado === 'ABIERTA' && s.encabezado.fechaCierre === null
export const tiposCaja: Record<string, string> = { APERTURA_CAJA: 'Apertura', COBRO_VENTA: 'Cobros', INGRESO_CAJA: 'Ingresos manuales', EGRESO_CAJA: 'Egresos manuales', COBRO_CUENTA_POR_COBRAR: 'Cobros CxC', PAGO_CUENTA_POR_PAGAR: 'Pagos CxP' }
export const totalTipo = (s: SesionCaja, tipo: string) => s.totalesPorTipo.find((t) => t.tipo === tipo)?.total ?? 0
export const ingresosCaja = (s: SesionCaja) => (Math.round(totalTipo(s, 'COBRO_VENTA') * 100) + Math.round(totalTipo(s, 'INGRESO_CAJA') * 100) + Math.round(totalTipo(s, 'COBRO_CUENTA_POR_COBRAR') * 100)) / 100
export const egresosCaja = (s: SesionCaja) => (Math.round(totalTipo(s, 'EGRESO_CAJA') * 100) + Math.round(totalTipo(s, 'PAGO_CUENTA_POR_PAGAR') * 100)) / 100
export const mensajeCaja = (error: Error) => error instanceof ErrorApi && error.codigo === 'CONCURRENCIA_CAJA'
  ? 'La sesión cambió desde que la abriste. Recarga la información.' : error.message
