export interface Caja { id: number; empresaId: number; sucursalId: number; codigo: string; nombre: string; estadoId: number; estado: string; sesionAbiertaId: number | null }
export interface FormaPagoCaja { id: number; codigo: string; nombre: string; esEfectivo: boolean; requiereReferencia: boolean; permiteCambio: boolean }
export interface ReferenciasCaja { empresaId: number; sucursalId: number; monedaId: number; moneda: string; formasPago: FormaPagoCaja[]; saldoUltimoCierre?: { sesionId: number; cajaId: number; numeroCierre: string; fechaCierre: string; efectivoContado: number } | null }
export interface SesionCajaResumen {
  id: number; cajaId: number; caja: string; empresaId: number; sucursalId: number;
  usuarioAperturaId: number; usuarioApertura: string; fechaApertura: string; saldoInicial: number; estado: string;
  monedaId: number | null; moneda: string | null; numeroApertura: string | null; numeroCierre: string | null;
  fechaCierre: string | null; usuarioCierreId: number | null; efectivoEsperadoCierre: number | null;
  efectivoContadoCierre: number | null; diferenciaCierre: number | null; totalIngresosCierre: number | null;
  totalEgresosCierre: number | null; arqueoCierreId: number | null; versionFila: string; esFase8: boolean;
}
export interface MovimientoCaja {
  id: number; numeroDocumento: string | null; tipo: string; fecha: string; monto: number;
  formaPagoId: number | null; formaPago: string | null; esEfectivo: boolean | null; referencia: string | null;
  observacion: string | null; usuarioId: number; usuario: string; pagoId: number | null; ventaId: number | null; numeroVenta: string | null;
}
export interface ArqueoCaja { id: number; fecha: string; esperado: number; contado: number; diferencia: number; usuarioId: number; usuario: string; observaciones: string | null; vigente: boolean }
export interface TotalTipoCaja { tipo: string; total: number }
export interface TotalFormaCaja { formaPagoId: number | null; formaPago: string; esEfectivo: boolean; saldoInicial: number; ingresos: number; egresos: number; neto: number }
export interface CobroDetalle { id: number; formaPagoId: number; formaPago: string; esEfectivo: boolean; aplicado: number; recibido: number; vuelto: number; referencia: string | null }
export interface CobroCaja { id: number; ventaId: number; numeroVenta: string; sesionId: number; fecha: string; usuarioId: number; total: number; monedaId: number; moneda: string; claveOperacion: string; detalles: CobroDetalle[] }
export interface SesionCaja { encabezado: SesionCajaResumen; movimientos: MovimientoCaja[]; totalesPorTipo: TotalTipoCaja[]; totalesPorFormaPago: TotalFormaCaja[]; saldoTeorico: number | null; efectivoEsperado: number | null; cobros: CobroCaja[]; arqueos: ArqueoCaja[]; usuarioCierre: string | null }
export interface ResultadoCobroCaja { cobro: CobroCaja; sesion: SesionCaja; reintento: boolean }
export interface VentaPendienteCaja { id: number; numeroVenta: string; fecha: string; clienteId: number | null; cliente: string | null; total: number; saldoPendiente: number; monedaId: number; moneda: string; versionFila: string }
export interface FormaCobro { formaPagoId: number; montoAplicado: number; montoRecibido: number; referencia?: string }
export interface SolicitudCobro { versionFila: string; versionVenta: string; ventaId: number; claveOperacion: string; pagos: FormaCobro[] }
