import type { Paginado } from '../types/api'
import type { RespuestasReportes, SesionCajaReporte } from '../types/reportes'

export const paginaReporte = <T,>(elementos: T[]): Paginado<T> => ({ elementos, pagina: 1, tamanioPagina: 20, totalRegistros: elementos.length, totalPaginas: elementos.length ? 1 : 0 })
export const periodoDemo = { fechaDesde: '2026-10-01', fechaHasta: '2026-10-19', zonaHoraria: 'America/Guatemala', sucursalId: 4 }
const venta = { id: 21, numero: 'VT-0000000021', fecha: '2026-10-19T02:31:00Z', estado: 'COMPLETADA', clienteId: 7, cliente: 'Cafetería del Parque', esCredito: true, monedaId: 1, moneda: 'GTQ',
  subtotal: 100, descuentos: 5, impuestos: 12, total: 107, costo: 25, margenBruto: 70, cobrado: 50, estadoCobro: 'PARCIAL' }
const compra = { id: 31, numero: 'OC-0000000031', fecha: '2026-10-10', proveedorId: 8, proveedor: 'Lácteos del Valle', esCredito: true,
  estado: 'RECIBIDA', monedaId: 1, moneda: 'GTQ', total: 200, lineas: 2, lineasRecibidasCompletas: 2, recepciones: 3, cuentaPorPagarId: 11, saldoPorPagar: 150 }
const sesion: SesionCajaReporte = { id: 41, caja: 'Caja principal', apertura: '2026-10-19T14:00:00Z', cierre: '2026-10-19T22:00:00Z', usuario: 'admin',
  estado: 'CERRADA', monedaId: 1, moneda: 'GTQ', saldoInicial: 50, ingresosOperativos: 200, egresosOperativos: 20, saldoTeorico: 230,
  efectivoEsperado: 130, efectivoContado: 135, diferencia: 5, conciliable: true }
const cuenta = { id: 51, terceroId: 7, tercero: 'Cafetería del Parque', documento: 'VT-0000000021', origenId: 21, compraId: null,
  emision: '2026-10-10', vencimiento: '2026-10-30', monedaId: 1, moneda: 'GTQ', importeOriginal: 107, aplicado: 50, saldo: 57, estado: 'PARCIAL', clasificacion: 'VIGENTE' }
const resumenCartera = { monedaId: 1, moneda: 'GTQ', cantidad: 4, cantidadPendientes: 3, pendiente: 157, vigente: 57, vencido: 100 }
export const datosReportes: RespuestasReportes = {
  ventas: {
    ventas: { periodo: periodoDemo, datos: paginaReporte([venta,
      { ...venta, id: 22, numero: 'VT-0000000022', esCredito: false, costo: null, margenBruto: null, clienteId: null, cliente: null, estadoCobro: 'COBRADA' },
      { ...venta, id: 23, numero: 'VT-0000000023', monedaId: 2, moneda: 'USD', esCredito: null, estadoCobro: 'PENDIENTE', total: 20, cobrado: 0 },
    ]), totales: [
      { monedaId: 1, moneda: 'GTQ', estado: 'COMPLETADA', cantidad: 2, subtotal: 200, descuentos: 10, impuestos: 24, total: 214, costo: null, margenBruto: null },
      { monedaId: 2, moneda: 'USD', estado: 'COMPLETADA', cantidad: 1, subtotal: 20, descuentos: 0, impuestos: 0, total: 20, costo: 5, margenBruto: 15 },
    ] }, cobrosPorForma: [
      { monedaId: 1, moneda: 'GTQ', formaPagoId: 1, formaPago: 'Efectivo', aplicado: 100 },
      { monedaId: 1, moneda: 'GTQ', formaPagoId: 2, formaPago: 'Tarjeta', aplicado: 57 },
    ],
  },
  'productos-vendidos': paginaReporte([
    { productoId: 1, sku: 'HEL-CHO', producto: 'Helado Chocolate', productoPresentacionId: 1, presentacion: 'Litro', unidad: 'L', unidadBase: 'ml', monedaId: 1, moneda: 'GTQ', cantidad: 4, cantidadBase: 4000, importe: 100, costo: null, margenBruto: null },
    { productoId: 1, sku: 'HEL-CHO', producto: 'Helado Chocolate', productoPresentacionId: 2, presentacion: 'Vaso', unidad: 'vaso', unidadBase: 'ml', monedaId: 2, moneda: 'USD', cantidad: 2, cantidadBase: null, importe: 5, costo: null, margenBruto: null },
  ]),
  inventario: { datos: paginaReporte([
    { id: 61, productoId: 1, sku: 'HEL-CHO', producto: 'Helado Chocolate', bodegaId: 2, bodega: 'Bodega principal', ubicacionId: 3, ubicacion: 'Congelador', unidadBase: 'ml', existencia: 1000, reservada: 50, disponible: 950, costoPromedio: 0.0245, valorInventario: 24.5 },
  ]), valorInventario: 24.5, monedaBase: 'GTQ', sucursalId: 4 },
  kardex: paginaReporte([
    { id: 71, fecha: '2026-10-19T02:31:00Z', concepto: 'COMPRA', tipo: 'ENTRADA', productoId: 1, sku: 'HEL-CHO', producto: 'Helado Chocolate', unidadBase: 'ml', bodegaId: 2, bodega: 'Bodega principal', ubicacion: 'Congelador',
      cantidad: 1000, costoUnitario: 0.0245, costo: 24.5, saldoCantidad: 5000, saldoCosto: 122.5, movimientoId: 71, moduloOrigen: 'COMPRAS', documentoOrigenId: 31, documento: 'OC-0000000031', usuario: 'admin' },
    { id: 72, fecha: '2026-10-19T02:32:00Z', concepto: 'VENTA', tipo: 'SALIDA', productoId: 1, sku: 'HEL-CHO', producto: 'Helado Chocolate', unidadBase: 'ml', bodegaId: 2, bodega: 'Bodega principal', ubicacion: 'Congelador',
      cantidad: 100, costoUnitario: 0.0245, costo: 2.45, saldoCantidad: 4900, saldoCosto: 120.05, movimientoId: 72, moduloOrigen: 'VENTAS', documentoOrigenId: 21, documento: 'VT-0000000021', usuario: 'admin' },
  ]),
  compras: { periodo: periodoDemo, datos: paginaReporte([compra,
    { ...compra, id: 32, numero: 'OC-0000000032', esCredito: false, cuentaPorPagarId: null, saldoPorPagar: null },
    { ...compra, id: 33, numero: 'OC-0000000033', esCredito: null, cuentaPorPagarId: null, saldoPorPagar: null },
  ]), totales: [{ monedaId: 1, moneda: 'GTQ', estado: 'RECIBIDA', cantidad: 3, total: 600 }] },
  caja: { periodo: periodoDemo, datos: paginaReporte([sesion,
    { ...sesion, id: 42, caja: 'Caja sin diferencia', diferencia: 0, efectivoContado: 130 },
    { ...sesion, id: 43, caja: 'Caja faltante', diferencia: -3, efectivoContado: 127 },
    { ...sesion, id: 44, caja: 'Caja histórica', cierre: null, monedaId: null, moneda: null, saldoTeorico: null, ingresosOperativos: null, egresosOperativos: null,
      efectivoEsperado: null, efectivoContado: null, diferencia: null, conciliable: false },
  ]), totales: [
    { monedaId: 1, moneda: 'GTQ', formaPagoId: 1, formaPago: 'Efectivo', esEfectivo: true, apertura: 50, ingresosOperativos: 100, egresosOperativos: 20 },
    { monedaId: 1, moneda: 'GTQ', formaPagoId: 2, formaPago: 'Tarjeta', esEfectivo: false, apertura: 0, ingresosOperativos: 100, egresosOperativos: 0 },
  ] },
  'cuentas-por-cobrar': { periodo: periodoDemo, datos: paginaReporte([cuenta,
    { ...cuenta, id: 52, documento: 'VT-0000000022', estado: 'PAGADA', clasificacion: 'PAGADA', saldo: 0, aplicado: 107 },
    { ...cuenta, id: 53, documento: 'VT-0000000023', estado: 'PENDIENTE', clasificacion: 'VENCIDA' },
    { ...cuenta, id: 54, documento: 'VT-HISTORICA', estado: 'ANULADA', clasificacion: 'REQUIERE_REVISION' },
  ]), totales: [resumenCartera] },
  'cuentas-por-pagar': { periodo: periodoDemo, datos: paginaReporte([
    { ...cuenta, terceroId: 8, tercero: 'Lácteos del Valle', documento: 'FACT-001', origenId: 8, compraId: 31 },
    { ...cuenta, id: 52, terceroId: 8, tercero: 'Lácteos del Valle', documento: 'FACT-002', origenId: 9, compraId: null, estado: 'PAGADA', clasificacion: 'PAGADA', saldo: 0, aplicado: 107 },
  ]), totales: [resumenCartera] },
  auditoria: paginaReporte([
    { id: 81, fecha: '2026-10-19T02:31:00Z', usuarioId: 1, usuario: 'admin', modulo: 'VENTAS', entidad: 'Ventas', entidadId: '21', accion: 'COMPLETAR', empresaId: 4, sucursalId: 4, resultado: 'REGISTRADA' },
  ]),
}
