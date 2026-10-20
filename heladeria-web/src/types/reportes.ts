import type { Paginado } from './api'

export type TipoReporte = 'ventas' | 'productos-vendidos' | 'inventario' | 'kardex' | 'compras' | 'caja' | 'cuentas-por-cobrar' | 'cuentas-por-pagar' | 'auditoria'
export interface FiltroReporteBase { sucursalId?: number; busqueda?: string; pagina: number; tamanioPagina: number }
export interface FiltroReportePeriodo extends FiltroReporteBase { fechaDesde?: string; fechaHasta?: string }
export interface FiltroVentasReporte extends FiltroReportePeriodo { clienteId?: number; estado?: string; monedaId?: number }
export interface FiltroProductosVendidos extends FiltroReportePeriodo { productoId?: number; clienteId?: number; monedaId?: number }
export interface FiltroInventarioReporte extends FiltroReporteBase { productoId?: number; bodegaId?: number; ubicacionId?: number }
export interface FiltroKardexReporte extends FiltroReportePeriodo { productoId?: number; bodegaId?: number; concepto?: string; tipo?: 'ENTRADA' | 'SALIDA' }
export interface FiltroComprasReporte extends FiltroReportePeriodo { proveedorId?: number; monedaId?: number; estado?: string; esCredito?: boolean }
export interface FiltroCajaReporte extends FiltroReportePeriodo { cajaId?: number; usuarioId?: number; estado?: string }
export interface FiltroCarteraReporte extends FiltroReportePeriodo { terceroId?: number; monedaId?: number; estado?: string; vencimiento?: string }
export interface FiltroAuditoria extends FiltroReportePeriodo { usuarioId?: number; modulo?: string; accion?: string; entidad?: string }
export interface FiltrosPorReporte {
  ventas: FiltroVentasReporte
  'productos-vendidos': FiltroProductosVendidos
  inventario: FiltroInventarioReporte
  kardex: FiltroKardexReporte
  compras: FiltroComprasReporte
  caja: FiltroCajaReporte
  'cuentas-por-cobrar': FiltroCarteraReporte
  'cuentas-por-pagar': FiltroCarteraReporte
  auditoria: FiltroAuditoria
}
export interface PeriodoReporte { fechaDesde: string; fechaHasta: string; zonaHoraria: string; sucursalId: number }
export interface Reporte<T, R> { datos: Paginado<T>; totales: R[]; periodo: PeriodoReporte }
export interface VentaReporte {
  id: number; numero: string; fecha: string; estado: string; clienteId: number | null; cliente: string | null
  esCredito: boolean | null; monedaId: number; moneda: string; subtotal: number; descuentos: number
  impuestos: number; total: number; costo: number | null; margenBruto: number | null; cobrado: number; estadoCobro: string
}
export interface TotalVentasReporte {
  monedaId: number; moneda: string; estado: string; cantidad: number; subtotal: number; descuentos: number
  impuestos: number; total: number; costo: number | null; margenBruto: number | null
}
export interface CobrosFormaReporte { monedaId: number; moneda: string; formaPagoId: number; formaPago: string; aplicado: number }
export interface ReporteVentas { ventas: Reporte<VentaReporte, TotalVentasReporte>; cobrosPorForma: CobrosFormaReporte[] }
export interface ProductoVendidoReporte {
  productoId: number; sku: string; producto: string; productoPresentacionId: number | null; presentacion: string | null
  unidad: string | null; unidadBase: string | null; monedaId: number; moneda: string; cantidad: number
  cantidadBase: number | null; importe: number | null; costo: number | null; margenBruto: number | null
}
export interface InventarioReporte {
  id: number; productoId: number; sku: string; producto: string; bodegaId: number; bodega: string; ubicacionId: number
  ubicacion: string; unidadBase: string; existencia: number; reservada: number; disponible: number; costoPromedio: number; valorInventario: number
}
export interface ReporteInventario { datos: Paginado<InventarioReporte>; valorInventario: number; monedaBase: string | null; sucursalId: number }
export interface KardexReporte {
  id: number; fecha: string; concepto: string; tipo: string; productoId: number; sku: string; producto: string; unidadBase: string
  bodegaId: number; bodega: string; ubicacion: string; cantidad: number; costoUnitario: number; costo: number
  saldoCantidad: number; saldoCosto: number; movimientoId: number; moduloOrigen: string | null; documentoOrigenId: number | null; documento: string; usuario: string
}
export interface CompraReporte {
  id: number; numero: string; fecha: string; proveedorId: number; proveedor: string; esCredito: boolean | null; estado: string
  monedaId: number; moneda: string; total: number; lineas: number; lineasRecibidasCompletas: number; recepciones: number; cuentaPorPagarId: number | null; saldoPorPagar: number | null
}
export interface TotalComprasReporte { monedaId: number; moneda: string; estado: string; cantidad: number; total: number }
export interface SesionCajaReporte {
  id: number; caja: string; apertura: string; cierre: string | null; usuario: string; estado: string; monedaId: number | null; moneda: string | null
  saldoInicial: number; ingresosOperativos: number | null; egresosOperativos: number | null; saldoTeorico: number | null
  efectivoEsperado: number | null; efectivoContado: number | null; diferencia: number | null; conciliable: boolean
}
export interface FormaCajaReporte {
  monedaId: number | null; moneda: string | null; formaPagoId: number | null; formaPago: string | null
  esEfectivo: boolean | null; apertura: number; ingresosOperativos: number; egresosOperativos: number
}
export interface CuentaReporte {
  id: number; terceroId: number; tercero: string; documento: string; origenId: number; compraId: number | null
  emision: string; vencimiento: string; monedaId: number; moneda: string; importeOriginal: number; aplicado: number; saldo: number; estado: string; clasificacion: string
}
export interface TotalCarteraReporte { monedaId: number; moneda: string; cantidad: number; cantidadPendientes: number; pendiente: number; vigente: number; vencido: number }
// Lista explícita de metadatos permitidos: nunca modelar ni renderizar el payload de una bitácora.
export interface AuditoriaReporte {
  id: number; fecha: string; usuarioId: number | null; usuario: string | null; modulo: string | null; entidad: string
  entidadId: string | null; accion: string; empresaId: number; sucursalId: number; resultado: string
}
export interface RespuestasReportes {
  ventas: ReporteVentas
  'productos-vendidos': Paginado<ProductoVendidoReporte>
  inventario: ReporteInventario
  kardex: Paginado<KardexReporte>
  compras: Reporte<CompraReporte, TotalComprasReporte>
  caja: Reporte<SesionCajaReporte, FormaCajaReporte>
  'cuentas-por-cobrar': Reporte<CuentaReporte, TotalCarteraReporte>
  'cuentas-por-pagar': Reporte<CuentaReporte, TotalCarteraReporte>
  auditoria: Paginado<AuditoriaReporte>
}
export type ResultadoReporte = { [K in TipoReporte]: { tipo: K; respuesta: RespuestasReportes[K] } }[TipoReporte]
