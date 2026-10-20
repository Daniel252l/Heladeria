import type { TipoReporte } from '../../types/reportes'

export const reportes: { tipo: Exclude<TipoReporte, 'auditoria'>; nombre: string }[] = [
  { tipo: 'ventas', nombre: 'Ventas' }, { tipo: 'productos-vendidos', nombre: 'Productos vendidos' },
  { tipo: 'inventario', nombre: 'Inventario' }, { tipo: 'kardex', nombre: 'Kardex' },
  { tipo: 'compras', nombre: 'Compras' }, { tipo: 'caja', nombre: 'Caja' },
  { tipo: 'cuentas-por-cobrar', nombre: 'Cuentas por cobrar' }, { tipo: 'cuentas-por-pagar', nombre: 'Cuentas por pagar' },
]
export const tituloReporte = (tipo: TipoReporte) => tipo === 'auditoria' ? 'Auditoría' : `Reporte de ${reportes.find((r) => r.tipo === tipo)!.nombre.toLocaleLowerCase('es')}`
export const descripcionReporte: Record<TipoReporte, string> = {
  ventas: 'Importes oficiales por moneda y estado. Los cobros aplicados se muestran por separado.',
  'productos-vendidos': 'Ventas completadas por producto, presentación y moneda. Sin mezclar unidades ni recalcular costos.',
  inventario: 'Existencias actuales y valor oficial del inventario por sucursal, bodega y ubicación.',
  kardex: 'Movimientos y saldos históricos registrados. Los costos se presentan sin recalcular.',
  compras: 'Órdenes, recepción y vínculo financiero. Una compra recibida no significa una cuenta pagada.',
  caja: 'Sesiones por fecha de apertura. Los importes incluyen los movimientos de esas sesiones; la apertura no es ingreso operativo.',
  'cuentas-por-cobrar': 'Cuentas por fecha de emisión. Los saldos son actuales, no saldos históricos al cierre del período.',
  'cuentas-por-pagar': 'Facturas por fecha de emisión y compra origen. Los saldos son actuales, no saldos históricos al cierre del período.',
  auditoria: 'Metadatos de bitácora. REGISTRADA indica un registro de auditoría, no certifica éxito financiero.',
}
export const estadosReporte: Partial<Record<TipoReporte, string[]>> = {
  ventas: ['BORRADOR', 'CONFIRMADA', 'COMPLETADA', 'ANULADA'],
  compras: ['BORRADOR', 'CONFIRMADA', 'PARCIALMENTE_RECIBIDA', 'RECIBIDA', 'ANULADA'],
  caja: ['ABIERTA', 'CERRADA'],
  'cuentas-por-cobrar': ['PENDIENTE', 'PARCIAL', 'PAGADA', 'ANULADA'],
  'cuentas-por-pagar': ['PENDIENTE', 'PARCIAL', 'PAGADA', 'ANULADA'],
}
