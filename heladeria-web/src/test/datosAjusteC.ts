import type { VentaConsulta } from '../types/ventas'
import type { ReporteDiario } from '../types/diario'
import type { Paginado } from '../types/api'
const pagina = <T,>(elementos: T[]): Paginado<T> => ({ elementos, pagina: 1, tamanioPagina: 20, totalRegistros: elementos.length, totalPaginas: elementos.length ? 1 : 0 })
export const ticketDemo: VentaConsulta = {
  encabezado: { id: 2, empresaId: 4, sucursalId: 4, sucursal: 'Sucursal principal', bodegaId: 8, ubicacionId: 16, esCredito: false, fechaVencimientoCredito: null, numeroVenta: 'VT-0000000002', fechaVenta: '2026-10-07T15:00:00Z', clienteId: null, cliente: null, monedaId: 6, moneda: 'GTQ', tipoCambio: 1, subtotal: 50, descuento: 0, impuesto: 0, total: 50, estado: 'COMPLETADA', estadoId: 14, usuarioCreacionId: 1, usuario: 'admin', versionFila: 'Aw==', fechaCompletada: '2026-10-07T16:00:00Z', fechaAnulacion: null, motivoAnulacion: null, claveOperacion: 'uuid-demo' },
  detalles: [{ id: 5, numeroLinea: 1, productoId: 1, sku: 'HEL-CHO-001', producto: 'Helado Chocolate', productoPresentacionId: 1, presentacion: 'Litro', cantidad: 2, factorConversion: 1000, cantidadBase: 2000, unidad: 'L', unidadBase: 'ml', manejaExistencia: true, precioUnitario: 25, fuentePrecio: 'PRESENTACION', subtotalNeto: 50, porcentajeDescuento: 0, montoDescuento: 0, descuentoPrecioCatalogo: 0, impuesto: 0, totalLinea: 50, costoUnitarioBase: 0.01, costoTotal: 20, impuestos: [] }], movimientos: [],
  comprobante: { empresa: 'Heladería', razonSocial: 'Heladería Inicial', direccionSucursal: 'Zona 1, Guatemala', telefonoSucursal: '2222-0000', codigoCliente: null, usuarioFinalizacion: 'admin', zonaHoraria: 'America/Guatemala', cuentaPorCobrarId: null, saldoInicialCredito: null, pagos: [{ formaPago: 'Efectivo', aplicado: 20, recibido: 25, vuelto: 5, esEfectivo: true }, { formaPago: 'Tarjeta', aplicado: 30, recibido: 30, vuelto: 0, esEfectivo: false }] },
}
export const diarioDemo: ReporteDiario = {
  periodo: { fechaDesde: '2026-10-07', fechaHasta: '2026-10-07', zonaHoraria: 'America/Guatemala', sucursalId: 4 }, desdeUtc: '2026-10-07T06:00:00Z', hastaUtcExclusivo: '2026-10-08T06:00:00Z', generadoUtc: '2026-10-07T22:00:00Z', sucursal: 'Sucursal principal',
  ventasTotales: [{ monedaId: 6, moneda: 'GTQ', estado: 'COMPLETADA', cantidad: 2, subtotal: 100, descuentos: 0, impuestos: 0, total: 100, costo: 40, margenBruto: 60 }],
  cobrosPorForma: [{ monedaId: 6, moneda: 'GTQ', formaPagoId: 1, formaPago: 'Efectivo', aplicado: 30 }, { monedaId: 6, moneda: 'GTQ', formaPagoId: 2, formaPago: 'Tarjeta', aplicado: 30 }],
  finanzas: [{ monedaId: 6, moneda: 'GTQ', aperturas: 100, ingresos: 60, egresos: 5, cobrosVentas: 50, cobrosCxc: 10, pagosCxp: 0, sinClasificar: 0 }],
  creditosGenerados: [{ monedaId: 6, moneda: 'GTQ', cantidad: 1, importeOriginal: 50 }],
  ventas: pagina([{ id: 2, numero: 'VT-0000000002', fecha: '2026-10-07T16:00:00Z', cliente: null, esCredito: false, estado: 'COMPLETADA', total: 50, moneda: 'GTQ', usuario: 'admin', tieneComprobante: true, formasPago: ['Efectivo', 'Tarjeta'] }, { id: 3, numero: 'VT-0000000003', fecha: '2026-10-07T16:10:00Z', cliente: 'Cafetería del Parque', esCredito: true, estado: 'COMPLETADA', total: 50, moneda: 'GTQ', usuario: 'admin', tieneComprobante: true, formasPago: [] }]),
  operaciones: pagina([
    { id: 1, fecha: '2026-10-07T14:00:00Z', tipo: 'APERTURA_CAJA', documento: 'AC-0001', referencia: null, formaPago: 'Efectivo', moneda: 'GTQ', ingreso: 0, egreso: 0, apertura: 100, usuario: 'admin' },
    { id: 2, fecha: '2026-10-07T16:00:00Z', tipo: 'COBRO_VENTA', documento: 'MC-0002', referencia: null, formaPago: 'Efectivo', moneda: 'GTQ', ingreso: 20, egreso: 0, apertura: 0, usuario: 'admin' },
    { id: 3, fecha: '2026-10-07T16:00:00Z', tipo: 'COBRO_VENTA', documento: 'MC-0003', referencia: 'AUT-DEMO', formaPago: 'Tarjeta', moneda: 'GTQ', ingreso: 30, egreso: 0, apertura: 0, usuario: 'admin' },
    { id: 4, fecha: '2026-10-07T17:00:00Z', tipo: 'COBRO_CUENTA_POR_COBRAR', documento: 'MC-0004', referencia: null, formaPago: 'Efectivo', moneda: 'GTQ', ingreso: 10, egreso: 0, apertura: 0, usuario: 'admin' },
    { id: 5, fecha: '2026-10-07T18:00:00Z', tipo: 'EGRESO_CAJA', documento: 'MC-0005', referencia: null, formaPago: 'Efectivo', moneda: 'GTQ', ingreso: 0, egreso: 5, apertura: 0, usuario: 'admin' },
  ]),
  cajas: pagina([{ id: 4, caja: 'Caja principal', apertura: '2026-10-07T14:00:00Z', cierre: '2026-10-07T22:00:00Z', usuario: 'admin', estado: 'CERRADA', monedaId: 6, moneda: 'GTQ', saldoInicial: 100, ingresosOperativos: 60, egresosOperativos: 5, saldoTeorico: 155, efectivoEsperado: 125, efectivoContado: 125, diferencia: 0, conciliable: true }]),
}
