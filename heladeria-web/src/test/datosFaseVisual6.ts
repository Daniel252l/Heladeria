import type { ArqueoCaja, Caja, FormaCobro, ReferenciasCaja, ResultadoCobroCaja, SesionCaja, VentaPendienteCaja } from '../types/caja'

export const referenciasCaja: ReferenciasCaja = { empresaId: 4, sucursalId: 4, monedaId: 6, moneda: 'GTQ', formasPago: [
  { id: 21, codigo: 'EFECTIVO', nombre: 'Efectivo', esEfectivo: true, requiereReferencia: false, permiteCambio: true },
  { id: 34, codigo: 'TARJETA', nombre: 'Tarjeta', esEfectivo: false, requiereReferencia: false, permiteCambio: false },
] }
export const cajaDemo: Caja = { id: 9, empresaId: 4, sucursalId: 4, codigo: 'CAJA-CENTRAL', nombre: 'Caja central', estadoId: 1, estado: 'Activo', sesionAbiertaId: 7 }
export const pendienteCaja: VentaPendienteCaja = { id: 12, numeroVenta: 'VT-0000000012', fecha: '2026-10-08T20:42:15.376Z', clienteId: null, cliente: null, total: 16, saldoPendiente: 16, monedaId: 6, moneda: 'GTQ', versionFila: 'VENTA-V1' }
export const sesionCaja: SesionCaja = {
  encabezado: { id: 7, cajaId: 9, caja: 'Caja central', empresaId: 4, sucursalId: 4, usuarioAperturaId: 1, usuarioApertura: 'admin',
    fechaApertura: '2026-10-08T20:16:27.675Z', saldoInicial: 50, estado: 'ABIERTA', monedaId: 6, moneda: 'GTQ', numeroApertura: 'CA-0000000007',
    numeroCierre: null, fechaCierre: null, usuarioCierreId: null, efectivoEsperadoCierre: null, efectivoContadoCierre: null,
    diferenciaCierre: null, totalIngresosCierre: null, totalEgresosCierre: null, arqueoCierreId: null, versionFila: 'CAJA-V1', esFase8: true },
  saldoTeorico: 50, efectivoEsperado: 50, usuarioCierre: null, cobros: [], arqueos: [],
  movimientos: [{ id: 71, numeroDocumento: 'CA-0000000007', tipo: 'APERTURA_CAJA', fecha: '2026-10-08T20:16:27.675Z', monto: 50,
    formaPagoId: 21, formaPago: 'Efectivo', esEfectivo: true, referencia: 'CA-0000000007', observacion: 'Saldo inicial', usuarioId: 1, usuario: 'admin', pagoId: null, ventaId: null, numeroVenta: null }],
  totalesPorTipo: [{ tipo: 'APERTURA_CAJA', total: 50 }], totalesPorFormaPago: [{ formaPagoId: 21, formaPago: 'Efectivo', esEfectivo: true, saldoInicial: 50, ingresos: 0, egresos: 0, neto: 50 }],
}
export const arqueoCaja: ArqueoCaja = { id: 80, fecha: '2026-10-08T20:30:45.629Z', esperado: 50, contado: 48, diferencia: -2, usuarioId: 1, usuario: 'admin', observaciones: 'Conteo físico', vigente: true }
export const sesionArqueada: SesionCaja = { ...sesionCaja, encabezado: { ...sesionCaja.encabezado, versionFila: 'CAJA-V2' }, arqueos: [arqueoCaja] }
export const sesionCerrada: SesionCaja = { ...sesionArqueada, encabezado: { ...sesionArqueada.encabezado, versionFila: 'CAJA-V3', estado: 'CERRADA', numeroCierre: 'CC-0000000007',
  fechaCierre: '2026-10-08T20:37:15.338Z', usuarioCierreId: 1, efectivoEsperadoCierre: 50, efectivoContadoCierre: 48, diferenciaCierre: -2, totalIngresosCierre: 0, totalEgresosCierre: 0, arqueoCierreId: 80 }, usuarioCierre: 'admin' }
export function paginaCaja<T>(elementos: T[], pagina = 1, totalPaginas = 1) { return { elementos, pagina, tamanioPagina: 10, totalRegistros: totalPaginas > 1 ? 15 : elementos.length, totalPaginas } }
export function respuestaCobro(pagos: FormaCobro[], claveOperacion = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'): ResultadoCobroCaja {
  const cobro = { id: 90, ventaId: pendienteCaja.id, numeroVenta: pendienteCaja.numeroVenta, sesionId: 7, fecha: '2026-10-08T20:42:15.376Z', usuarioId: 1, total: 16, monedaId: 6, moneda: 'GTQ', claveOperacion,
    detalles: pagos.map((p, i) => ({ id: 91 + i, formaPagoId: p.formaPagoId, formaPago: referenciasCaja.formasPago.find((f) => f.id === p.formaPagoId)!.nombre,
      esEfectivo: p.formaPagoId === 21, aplicado: p.montoAplicado, recibido: p.montoRecibido, vuelto: p.montoRecibido - p.montoAplicado, referencia: p.referencia ?? null })) }
  const efectivo = pagos.filter((p) => p.formaPagoId === 21).reduce((s, p) => s + p.montoAplicado, 0)
  return { cobro, reintento: false, sesion: { ...sesionCaja, encabezado: { ...sesionCaja.encabezado, versionFila: 'CAJA-COBRADA' }, saldoTeorico: 66, efectivoEsperado: 50 + efectivo, cobros: [cobro],
    totalesPorTipo: [...sesionCaja.totalesPorTipo, { tipo: 'COBRO_VENTA', total: 16 }],
    totalesPorFormaPago: cobro.detalles.map((d) => ({ formaPagoId: d.formaPagoId, formaPago: d.formaPago, esEfectivo: d.esEfectivo, saldoInicial: d.esEfectivo ? 50 : 0, ingresos: d.aplicado, egresos: 0, neto: (d.esEfectivo ? 50 : 0) + d.aplicado })),
    movimientos: [...sesionCaja.movimientos, ...cobro.detalles.map((d) => ({ id: d.id, numeroDocumento: `CI-00000000${d.id}`, tipo: 'COBRO_VENTA', fecha: cobro.fecha,
      monto: d.aplicado, formaPagoId: d.formaPagoId, formaPago: d.formaPago, esEfectivo: d.esEfectivo, referencia: cobro.numeroVenta, observacion: 'Cobro total POS', usuarioId: 1, usuario: 'admin', pagoId: cobro.id, ventaId: cobro.ventaId, numeroVenta: cobro.numeroVenta }))],
  } }
}
