import type { CuentaCartera, DetalleCartera, DetalleCompraCredito, ResumenCartera } from '../types/cartera'
import { compraDetalle } from './datosFaseVisual3'

export const cuentaCartera: CuentaCartera = {
  id: 1, empresaId: 4, sucursalId: 4, terceroId: 27, tercero: 'Cafetería del Parque',
  documentoOrigenId: 12, tipoDocumentoOrigen: 'VENTA', numeroDocumentoOrigen: 'VT-0000000012', compraId: null,
  monedaId: 6, moneda: 'GTQ', fechaEmision: '2026-10-10', fechaVencimiento: '2026-10-20',
  importeOriginal: 8, totalAplicado: 0, saldoPendiente: 8, estado: 'PENDIENTE', vencimiento: 'VIGENTE', versionFila: 'CUENTA-V1', operable: true,
  usuarioCreacionId: 1, fechaCreacion: '2026-10-10T19:30:00Z', usuarioModificacionId: null, fechaModificacion: null,
}
export const detalleCartera: DetalleCartera = { encabezado: cuentaCartera, historial: [] }
export const resumenCartera: ResumenCartera = { monedaId: 6, moneda: 'GTQ', totalPendiente: 8, totalVigente: 8, totalVencido: 0, cantidadPendientes: 1, cantidadVencidas: 0 }
export const compraCredito: DetalleCompraCredito = { ...compraDetalle('RECIBIDA', 0), encabezado: {
  ...compraDetalle('RECIBIDA', 0).encabezado, id: 51, sucursalId: 4, esCredito: true, versionFila: 'COMPRA-V1', monedaId: 6,
} }
export function aplicarPagoDemo(c: DetalleCartera, monto: number, clave: string, forma = 'Efectivo'): DetalleCartera {
  const saldo = Math.round((c.encabezado.saldoPendiente - monto) * 100) / 100
  return { encabezado: { ...c.encabezado, saldoPendiente: saldo, totalAplicado: c.encabezado.totalAplicado + monto,
    estado: saldo === 0 ? 'PAGADA' : 'PARCIAL', vencimiento: saldo === 0 ? 'PAGADA' : c.encabezado.vencimiento, versionFila: 'CUENTA-V2' },
    historial: [...c.historial, { id: 80 + c.historial.length, pagoId: 70 + c.historial.length, fecha: '2026-10-14T20:31:27.275Z', monto, formaPagoId: 21, formaPago: forma,
      referencia: null, claveOperacion: clave, observacion: 'Abono confirmado', usuarioId: 1, usuario: 'admin', sesionCajaId: 7, movimientoIds: [170 + c.historial.length], estado: 'APLICADO', detalles: [] }] }
}
