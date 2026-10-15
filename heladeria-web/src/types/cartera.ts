import type { CompraConsulta, CompraResumen } from './compras'

export type TipoCartera = 'cobrar' | 'pagar'
export interface CuentaCartera {
  id: number; empresaId: number; sucursalId: number; terceroId: number; tercero: string;
  documentoOrigenId: number; tipoDocumentoOrigen: string; numeroDocumentoOrigen: string; compraId: number | null;
  monedaId: number; moneda: string; fechaEmision: string; fechaVencimiento: string;
  importeOriginal: number; totalAplicado: number; saldoPendiente: number; estado: string; vencimiento: string;
  versionFila: string; operable: boolean; usuarioCreacionId: number | null; fechaCreacion: string | null;
  usuarioModificacionId: number | null; fechaModificacion: string | null;
}
export interface AplicacionCartera {
  id: number; pagoId: number; fecha: string; monto: number; formaPagoId: number | null; formaPago: string;
  referencia: string | null; claveOperacion: string | null; observacion: string | null; usuarioId: number; usuario: string;
  sesionCajaId: number | null; movimientoIds: number[]; estado: string;
  detalles: { id: number; formaPagoId: number; formaPago: string; monto: number; referencia: string | null }[];
}
export interface DetalleCartera { encabezado: CuentaCartera; historial: AplicacionCartera[] }
export interface ResumenCartera { monedaId: number; moneda: string; totalPendiente: number; totalVencido: number; totalVigente: number; cantidadPendientes: number; cantidadVencidas: number }
export interface SolicitudPagoCartera {
  claveOperacion: string; versionFila: string; sesionCajaId: number; versionCaja: string;
  formaPagoId: number; monto: number; referencia: string | null; observacion: string | null;
}
export interface SolicitudFacturaCartera {
  claveOperacion: string; ordenCompraId: number; versionCompra: string; serie: string | null;
  numeroDocumento: string; fechaDocumento: string; fechaVencimiento: string;
}
export interface ResultadoPagoCartera { pagoId: number; cuenta: DetalleCartera; reintento: boolean }
export interface ResultadoFacturaCartera { facturaId: number; cuenta: DetalleCartera; reintento: boolean }
// Extensión de lectura de Fase 9; no cambia los formularios de Compras aprobados.
export interface CompraCredito extends CompraResumen { esCredito: boolean | null; versionFila: string }
export interface DetalleCompraCredito extends Omit<CompraConsulta, 'encabezado'> { encabezado: CompraCredito }
